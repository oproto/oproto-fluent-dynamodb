using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Thrown when an expression uses an unsupported operator or method.
/// This occurs when the expression contains patterns that cannot be translated to DynamoDB syntax.
/// </summary>
/// <remarks>
/// <para><strong>Unsupported Patterns:</strong></para>
/// <list type="bullet">
/// <item><description>Assignment expressions: x => x.Id = "123"</description></item>
/// <item><description>Method calls on entity properties: x => x.Name.ToUpper()</description></item>
/// <item><description>Methods referencing entity parameter: x => x.Id == MyFunction(x)</description></item>
/// <item><description>LINQ operations on entity properties: x => x.Items.Select(...)</description></item>
/// <item><description>Unsupported operators: %, ^, &amp;, |, etc.</description></item>
/// <item><description>Complex transformations that can't execute in DynamoDB</description></item>
/// </list>
/// 
/// <para><strong>Supported Patterns:</strong></para>
/// <list type="bullet">
/// <item><description>Comparison operators: ==, !=, &lt;, &gt;, &lt;=, &gt;=</description></item>
/// <item><description>Logical operators: &amp;&amp;, ||, !</description></item>
/// <item><description>String methods: StartsWith(), Contains()</description></item>
/// <item><description>DynamoDB functions: Between(), AttributeExists(), AttributeNotExists(), Size()</description></item>
/// <item><description>Method calls on captured values: x => x.Id == userId.ToString()</description></item>
/// </list>
/// 
/// <para><strong>Resolution:</strong></para>
/// <list type="number">
/// <item><description>Use supported operators and methods</description></item>
/// <item><description>Pre-compute transformations before the expression</description></item>
/// <item><description>Use string-based expressions for complex scenarios</description></item>
/// <item><description>Move unsupported logic to application code after retrieval</description></item>
/// </list>
/// 
/// <para><strong>Exception Properties:</strong></para>
/// <para>
/// The exception provides <see cref="ExpressionType"/> and <see cref="MethodName"/> properties
/// to help identify what specific operator or method caused the error.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✗ Invalid: Method call on entity property
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.Name.ToUpper() == "JOHN")
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "Method 'ToUpper' cannot be used on entity properties..."
/// }
/// 
/// // ✓ Valid: Pre-compute transformation
/// var upperName = "JOHN";
/// table.Query
///     .Where&lt;User&gt;(x => x.Name == upperName)
///     .ExecuteAsync();
/// 
/// // ✗ Invalid: Assignment expression
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.Id = "123")
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "Assignment expressions are not supported..."
/// }
/// 
/// // ✓ Valid: Use comparison operator
/// table.Query
///     .Where&lt;User&gt;(x => x.Id == "123")
///     .ExecuteAsync();
/// 
/// // ✗ Invalid: Method referencing entity parameter
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.Id == ComputeId(x))
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "Method 'ComputeId' cannot reference the entity parameter..."
/// }
/// 
/// // ✓ Valid: Pre-compute value
/// var computedId = ComputeId(someValue);
/// table.Query
///     .Where&lt;User&gt;(x => x.Id == computedId)
///     .ExecuteAsync();
/// 
/// // ✗ Invalid: LINQ on entity property
/// try
/// {
///     table.Query
///         .WithFilter&lt;User&gt;(x => x.Items.Select(i => i.Name).Contains("test"))
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     Console.WriteLine(ex.Message);
/// }
/// 
/// // ✓ Valid: Use string Contains directly
/// table.Query
///     .WithFilter&lt;User&gt;(x => x.Items.Contains("test"))
///     .ExecuteAsync();
/// 
/// // ✗ Invalid: Unsupported operator (modulo)
/// try
/// {
///     table.Query
///         .WithFilter&lt;User&gt;(x => x.Age % 2 == 0)
///         .ExecuteAsync();
/// }
/// catch (UnsupportedExpressionException ex)
/// {
///     Console.WriteLine($"Unsupported operator: {ex.ExpressionType}");
/// }
/// 
/// // ✓ Valid: Filter in application code after retrieval
/// var users = await table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId)
///     .ExecuteAsync();
/// var evenAgeUsers = users.Items.Where(u => u.Age % 2 == 0);
/// </code>
/// </example>
public class UnsupportedExpressionException : ExpressionTranslationException
{
    /// <summary>
    /// Gets the expression type that is not supported, if applicable.
    /// </summary>
    public ExpressionType? ExpressionType { get; }

    /// <summary>
    /// Gets the method name that is not supported, if applicable.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The original expression.</param>
    public UnsupportedExpressionException(string message, Expression? expression = null)
        : base(message, expression)
    {
        if (expression != null)
        {
            ExpressionType = expression.NodeType;
            
            if (expression is MethodCallExpression methodCall)
            {
                MethodName = methodCall.Method.Name;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expressionType">The unsupported expression type.</param>
    /// <param name="expression">The original expression.</param>
    public UnsupportedExpressionException(string message, ExpressionType expressionType, Expression? expression = null)
        : base(message, expression)
    {
        ExpressionType = expressionType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="methodName">The unsupported method name.</param>
    /// <param name="expression">The original expression.</param>
    public UnsupportedExpressionException(string message, string methodName, Expression? expression = null)
        : base(message, expression)
    {
        MethodName = methodName;
    }
}
