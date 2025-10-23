using Amazon.DynamoDBv2.Model;

namespace Oproto.FluentDynamoDb.Logging;

/// <summary>
/// Utility class for redacting sensitive field values from DynamoDB items before logging.
/// This ensures compliance with data protection regulations by preventing sensitive data
/// from appearing in log output.
/// </summary>
public static class SensitiveDataRedactor
{
    private const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Creates a copy of the item dictionary with sensitive field values replaced by a redaction placeholder.
    /// Non-sensitive fields are preserved as-is. Field names are always preserved.
    /// </summary>
    /// <param name="item">The DynamoDB item to redact. Can be null.</param>
    /// <param name="sensitiveFieldNames">Set of field names that should be redacted. Can be null or empty.</param>
    /// <returns>
    /// A new dictionary with sensitive values redacted, or the original item if no sensitive fields are specified.
    /// Returns null if the input item is null.
    /// </returns>
    /// <remarks>
    /// This method performs a shallow copy of the dictionary. The AttributeValue objects themselves
    /// are not cloned, only the sensitive ones are replaced with new AttributeValue instances containing
    /// the redaction placeholder.
    /// </remarks>
    public static Dictionary<string, AttributeValue>? RedactSensitiveFields(
        Dictionary<string, AttributeValue>? item,
        HashSet<string>? sensitiveFieldNames)
    {
        // Handle null or empty cases - no redaction needed
        if (item == null || item.Count == 0)
            return item;

        if (sensitiveFieldNames == null || sensitiveFieldNames.Count == 0)
            return item;

        // Create a shallow copy and replace sensitive values
        var redactedItem = new Dictionary<string, AttributeValue>(item.Count);
        
        foreach (var kvp in item)
        {
            if (sensitiveFieldNames.Contains(kvp.Key))
            {
                // Replace sensitive value with redaction placeholder
                redactedItem[kvp.Key] = new AttributeValue { S = RedactedPlaceholder };
            }
            else
            {
                // Preserve non-sensitive values as-is
                redactedItem[kvp.Key] = kvp.Value;
            }
        }

        return redactedItem;
    }

    /// <summary>
    /// Returns either the redaction placeholder or the original value based on whether
    /// the field is marked as sensitive.
    /// </summary>
    /// <param name="value">The value to potentially redact.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="sensitiveFieldNames">Set of field names that should be redacted. Can be null or empty.</param>
    /// <returns>The redaction placeholder if the field is sensitive, otherwise the original value.</returns>
    public static string RedactIfSensitive(
        string value,
        string fieldName,
        HashSet<string>? sensitiveFieldNames)
    {
        if (sensitiveFieldNames != null && sensitiveFieldNames.Contains(fieldName))
            return RedactedPlaceholder;

        return value;
    }
}
