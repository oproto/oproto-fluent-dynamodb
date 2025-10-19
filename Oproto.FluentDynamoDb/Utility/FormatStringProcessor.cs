using System.Text.RegularExpressions;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.Utility;

/// <summary>
/// Shared utility for processing format strings with {0}, {1:format} syntax across all extension methods.
/// This centralizes the format string processing logic to ensure consistency and reduce code duplication.
/// </summary>
internal static class FormatStringProcessor
{
    /// <summary>
    /// Processes a format string by replacing {0}, {1:format} placeholders with generated parameter names
    /// and adding the corresponding values to the attribute value helper.
    /// </summary>
    /// <param name="format">The format string with placeholders.</param>
    /// <param name="args">The values to substitute.</param>
    /// <param name="attributeValueHelper">The helper to add generated parameters to.</param>
    /// <returns>A tuple containing the processed expression and the number of parameters generated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when format string is invalid or parameter count doesn't match.</exception>
    /// <exception cref="FormatException">Thrown when format specifiers are invalid or format string contains errors.</exception>
    public static (string processedExpression, int parameterCount) ProcessFormatString(
        string format,
        object[] args,
        AttributeValueInternal attributeValueHelper)
    {
        if (string.IsNullOrEmpty(format))
        {
            throw new ArgumentException("Format string cannot be null or empty.", nameof(format));
        }

        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (attributeValueHelper == null)
        {
            throw new ArgumentNullException(nameof(attributeValueHelper));
        }

        // Check for unmatched braces
        var openBraces = format.Count(c => c == '{');
        var closeBraces = format.Count(c => c == '}');
        if (openBraces != closeBraces)
        {
            throw new FormatException("Format string contains unmatched braces. Each '{' must have a corresponding '}'.");
        }

        // First check for any placeholders that look like format placeholders
        var allBracePattern = new Regex(@"\{[^}]*\}", RegexOptions.Compiled);
        var allBraceMatches = allBracePattern.Matches(format);

        if (allBraceMatches.Count == 0)
        {
            // No placeholders found, return as-is
            return (format, 0);
        }

        // Regular expression to match valid format placeholders like {0}, {1:o}, {2:F2}, and also negative ones like {-1}
        var formatPattern = new Regex(@"\{(-?\d+)(?::([^}]+))?\}", RegexOptions.Compiled);
        var matches = formatPattern.Matches(format);

        // Check for invalid placeholders by comparing all braces to valid ones
        if (allBraceMatches.Count > matches.Count)
        {
            // Find the invalid placeholders
            var validPlaceholders = matches.Cast<Match>().Select(m => m.Value).ToHashSet();
            var invalidPlaceholders = allBraceMatches.Cast<Match>()
                .Select(m => m.Value)
                .Where(placeholder => !validPlaceholders.Contains(placeholder))
                .ToList();

            // Extract just the parameter indices from invalid placeholders for the error message
            var invalidIndices = new List<string>();
            foreach (var placeholder in invalidPlaceholders)
            {
                // Extract content between braces
                var content = placeholder.Substring(1, placeholder.Length - 2);
                var colonIndex = content.IndexOf(':');
                var indexPart = colonIndex >= 0 ? content.Substring(0, colonIndex) : content;
                invalidIndices.Add(indexPart);
            }

            throw new FormatException($"Format string contains invalid parameter indices: {string.Join(", ", invalidIndices)}. Parameter indices must be non-negative integers.");
        }

        // Check for invalid parameter indices (negative numbers)
        var invalidNegativeIndices = new List<string>();
        var maxIndex = -1;
        var parameterIndices = new HashSet<int>();

        foreach (Match match in matches)
        {
            var indexStr = match.Groups[1].Value;
            if (int.TryParse(indexStr, out var index))
            {
                if (index < 0)
                {
                    invalidNegativeIndices.Add(index.ToString());
                }
                else
                {
                    maxIndex = Math.Max(maxIndex, index);
                    parameterIndices.Add(index);
                }
            }
        }

        // Throw if we found negative parameter indices
        if (invalidNegativeIndices.Count > 0)
        {
            throw new FormatException($"Format string contains invalid parameter indices: {string.Join(", ", invalidNegativeIndices)}. Parameter indices must be non-negative integers.");
        }

        // Check if we have enough arguments
        if (maxIndex >= args.Length)
        {
            throw new ArgumentException($"Format string references parameter index {maxIndex} but only {args.Length} arguments were provided. Ensure you have enough arguments for all parameter placeholders.", nameof(args));
        }

        // Process each placeholder and build the result
        var result = format;
        var parameterCount = 0;

        // First, generate parameters for all matches in left-to-right order
        var matchList = matches.Cast<Match>().OrderBy(m => m.Index).ToList();
        var parameterMap = new Dictionary<Match, string>();

        foreach (var match in matchList)
        {
            var argIndex = int.Parse(match.Groups[1].Value);
            var formatSpec = match.Groups[2].Success ? match.Groups[2].Value : null;

            try
            {
                var value = args[argIndex];
                var parameterName = attributeValueHelper.AddFormattedValue(value, formatSpec);
                parameterMap[match] = parameterName;
            }
            catch (FormatException ex) when (ex.Message.Contains("Boolean values do not support format strings") ||
                                           ex.Message.Contains("Enum values do not support format strings"))
            {
                // Re-throw with the expected message format for tests
                throw new FormatException($"Invalid format specifier '{formatSpec}' for parameter at index {argIndex}. {ex.Message}", ex);
            }
            catch (FormatException ex) when (ex.Message.Contains("is not a valid format string"))
            {
                // Re-throw with the expected message format for invalid format specifiers
                throw new FormatException($"Invalid format specifier '{formatSpec}' for parameter at index {argIndex}. {ex.Message}", ex);
            }
            catch (FormatException)
            {
                // Re-throw other format exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in FormatException with context
                throw new FormatException($"Failed to format parameter {argIndex} with format specifier '{formatSpec}': {ex.Message}", ex);
            }
        }

        // Now replace placeholders in reverse order to avoid index shifting issues
        var orderedMatches = matches.Cast<Match>().OrderByDescending(m => m.Index).ToList();
        foreach (var match in orderedMatches)
        {
            var parameterName = parameterMap[match];

            // Replace the placeholder with the generated parameter name
            result = result.Substring(0, match.Index) + parameterName + result.Substring(match.Index + match.Length);
            parameterCount++;
        }

        return (result, parameterCount);
    }
}