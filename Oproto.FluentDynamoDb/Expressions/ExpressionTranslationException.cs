using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Base exception for expression translation errors.
/// Thrown when a C# lambda expression cannot be translated to DynamoDB expression syntax.
/// </summary>
/// <remarks>
/// <para><strong>Common Causes:</strong></para>
/// <list type="bullet">
/// <item><description>Using unsupported operators or methods</description></item>
/// <item><description>Referencing unmapped properties</description></item>
/// <item><description>Using non-key properties in Query().Where()</description></item>
/// <item><description>Complex expressions that cannot be translated</description></item>
/// <item><description>Method calls that reference the entity parameter</description></item>
/// </list>
/// 
/// <para><strong>Derived Exceptions:</strong></para>
/// <list type="bullet">
/// <item><description><see cref="UnmappedPropertyException"/>: Property doesn't map to DynamoDB attribute</description></item>
/// <item><description><see cref="InvalidKeyExpressionException"/>: Non-key property in Query().Where()</description></item>
/// <item><description><see cref="UnsupportedExpressionException"/>: Unsupported operator or method</description></item>
/// </list>
/// 
/// <para><strong>Debugging:</strong></para>
/// <para>
/// The <see cref="OriginalExpression"/> property contains the expression that caused the error,
/// which can be inspected for debugging purposes. The exception message provides specific
/// guidance on how to fix the issue.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.Name.ToUpper() == "JOHN")
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     // Message: "Method 'ToUpper' cannot be used on entity properties..."
///     Console.WriteLine(ex.Message);
///     Console.WriteLine($"Expression: {ex.OriginalExpression}");
/// }
/// 
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.PartitionKey == userId &amp;&amp; x.Status == "ACTIVE")
///         .ExecuteAsync();
/// }
/// catch (InvalidKeyExpressionException ex)
/// {
///     // Message: "Property 'Status' is not a key attribute..."
///     Console.WriteLine($"Non-key property: {ex.PropertyName}");
///     
///     // Fix: Move non-key condition to filter
///     table.Query
///         .Where&lt;User&gt;(x => x.PartitionKey == userId)
///         .WithFilter&lt;User&gt;(x => x.Status == "ACTIVE")
///         .ExecuteAsync();
/// }
/// </code>
/// </example>
public class ExpressionTranslationException : Exception
{
    /// <summary>
    /// Gets the original expression that caused the error, if available.
    /// </summary>
    public Expression? OriginalExpression { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionTranslationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The original expression that caused the error.</param>
    public ExpressionTranslationException(string message, Expression? expression = null)
        : base(message)
    {
        OriginalExpression = expression;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionTranslationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="expression">The original expression that caused the error.</param>
    public ExpressionTranslationException(string message, Exception innerException, Expression? expression = null)
        : base(message, innerException)
    {
        OriginalExpression = expression;
    }
}
