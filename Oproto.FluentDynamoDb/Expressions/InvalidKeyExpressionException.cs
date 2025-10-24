using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Thrown when a Query().Where() expression references non-key attributes.
/// This occurs when trying to use non-key properties in a key condition expression.
/// </summary>
/// <remarks>
/// <para><strong>DynamoDB Query Restrictions:</strong></para>
/// <para>
/// DynamoDB Query operations require that key condition expressions (the WHERE clause)
/// only reference the partition key and sort key. Non-key attributes cannot be used
/// in key conditions because DynamoDB uses the key condition to efficiently locate items
/// in the table or index.
/// </para>
/// 
/// <para><strong>Key vs Filter Expressions:</strong></para>
/// <list type="table">
/// <listheader><term>Expression Type</term><description>Allowed Properties</description><description>When Applied</description><description>Performance Impact</description></listheader>
/// <item><term>Key Condition (Where)</term><description>Partition key, Sort key only</description><description>Before reading items</description><description>Efficient - only reads matching items</description></item>
/// <item><term>Filter Expression (WithFilter)</term><description>Any property</description><description>After reading items</description><description>Less efficient - reads then filters</description></item>
/// </list>
/// 
/// <para><strong>Resolution:</strong></para>
/// <para>
/// Move non-key conditions from Where() to WithFilter(). The key condition should only
/// specify the partition key (required) and optionally the sort key. All other conditions
/// should use WithFilter().
/// </para>
/// 
/// <para><strong>Performance Consideration:</strong></para>
/// <para>
/// Filter expressions are applied after items are read from DynamoDB, so they reduce
/// data transfer but not consumed read capacity. Design your table schema to use
/// sort keys for commonly filtered attributes when possible.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✗ Invalid: Non-key property in Where()
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.PartitionKey == userId &amp;&amp; x.Status == "ACTIVE")
///         .ExecuteAsync();
/// }
/// catch (InvalidKeyExpressionException ex)
/// {
///     Console.WriteLine($"Non-key property: {ex.PropertyName}"); // "Status"
///     Console.WriteLine(ex.Message);
///     // "Property 'Status' is not a key attribute and cannot be used in Query().Where()..."
/// }
/// 
/// // ✓ Valid: Move non-key condition to WithFilter()
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId)
///     .WithFilter&lt;User&gt;(x => x.Status == "ACTIVE")
///     .ExecuteAsync();
/// 
/// // ✗ Invalid: Multiple non-key properties
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.PartitionKey == userId &amp;&amp; x.Status == "ACTIVE" &amp;&amp; x.Age >= 18)
///         .ExecuteAsync();
/// }
/// catch (InvalidKeyExpressionException ex)
/// {
///     // Will throw for the first non-key property encountered
/// }
/// 
/// // ✓ Valid: All non-key conditions in filter
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId)
///     .WithFilter&lt;User&gt;(x => x.Status == "ACTIVE" &amp;&amp; x.Age >= 18)
///     .ExecuteAsync();
/// 
/// // ✓ Valid: Key condition with partition key and sort key
/// table.Query
///     .Where&lt;Order&gt;(x => x.PartitionKey == customerId &amp;&amp; x.SortKey.StartsWith("ORDER#2024"))
///     .WithFilter&lt;Order&gt;(x => x.Status == "SHIPPED" &amp;&amp; x.Total > 100)
///     .ExecuteAsync();
/// 
/// // ✓ Valid: Sort key with BETWEEN
/// table.Query
///     .Where&lt;Order&gt;(x => x.PartitionKey == customerId &amp;&amp; x.SortKey.Between("ORDER#2024-01", "ORDER#2024-12"))
///     .WithFilter&lt;Order&gt;(x => x.Status == "COMPLETED")
///     .ExecuteAsync();
/// 
/// // Understanding the difference
/// // This query:
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId)
///     .WithFilter&lt;User&gt;(x => x.Status == "ACTIVE")
///     .ExecuteAsync();
/// 
/// // Executes as:
/// // 1. DynamoDB reads all items with PartitionKey == userId (efficient)
/// // 2. DynamoDB filters out items where Status != "ACTIVE" (after reading)
/// // 3. Returns only matching items
/// // Note: You still pay read capacity for all items in step 1
/// 
/// // Better table design for frequently filtered attributes:
/// // Use a GSI with Status as the partition key or part of the sort key
/// // Then you can query efficiently:
/// table.Query
///     .OnIndex("StatusIndex")
///     .Where&lt;User&gt;(x => x.Status == "ACTIVE" &amp;&amp; x.UserId.StartsWith(userPrefix))
///     .ExecuteAsync();
/// </code>
/// </example>
public class InvalidKeyExpressionException : ExpressionTranslationException
{
    /// <summary>
    /// Gets the name of the property that is not a key attribute.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidKeyExpressionException"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the non-key property.</param>
    /// <param name="expression">The original expression.</param>
    public InvalidKeyExpressionException(string propertyName, Expression? expression = null)
        : base(
            $"Property '{propertyName}' is not a key attribute and cannot be used in Query().Where(). " +
            $"Use WithFilter() to filter on non-key attributes.",
            expression)
    {
        PropertyName = propertyName;
    }
}
