namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Validation mode for expression translation.
/// Determines which properties are allowed in expressions based on the context.
/// </summary>
/// <remarks>
/// <para><strong>DynamoDB Query Restrictions:</strong></para>
/// <para>
/// DynamoDB Query operations have strict requirements for key condition expressions:
/// they can only reference the partition key and sort key. Non-key attributes must
/// be filtered using filter expressions, which are applied after the query retrieves items.
/// </para>
/// 
/// <para><strong>Mode Selection:</strong></para>
/// <list type="table">
/// <listheader><term>Context</term><description>Mode</description><description>Reason</description></listheader>
/// <item><term>Query().Where()</term><description>KeysOnly</description><description>DynamoDB requires key condition expressions to only reference keys</description></item>
/// <item><term>WithFilter()</term><description>None</description><description>Filter expressions can reference any attribute</description></item>
/// <item><term>WithCondition()</term><description>None</description><description>Condition expressions can reference any attribute</description></item>
/// <item><term>Scan().WithFilter()</term><description>None</description><description>Scan filters can reference any attribute</description></item>
/// </list>
/// 
/// <para><strong>Error Behavior:</strong></para>
/// <para>
/// When KeysOnly mode is active and a non-key property is referenced, an
/// <see cref="InvalidKeyExpressionException"/> is thrown with a clear message
/// directing the developer to use WithFilter() instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // KeysOnly mode - only keys allowed
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId &amp;&amp; x.SortKey.StartsWith("ORDER#"))
///     .ExecuteAsync();
/// // ✓ Valid - both properties are keys
/// 
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId &amp;&amp; x.Status == "ACTIVE")
///     .ExecuteAsync();
/// // ✗ Invalid - Status is not a key property
/// // Throws: InvalidKeyExpressionException
/// 
/// // None mode - any property allowed
/// table.Query
///     .Where&lt;User&gt;(x => x.PartitionKey == userId)
///     .WithFilter&lt;User&gt;(x => x.Status == "ACTIVE" &amp;&amp; x.Age >= 18)
///     .ExecuteAsync();
/// // ✓ Valid - filter expressions can reference any property
/// </code>
/// </example>
public enum ExpressionValidationMode
{
    /// <summary>
    /// No validation - any property can be referenced (for filter/condition expressions).
    /// Used in WithFilter() and WithCondition() contexts where DynamoDB allows any attribute.
    /// </summary>
    None,
    
    /// <summary>
    /// Key-only validation - only partition key and sort key properties allowed (for Query().Where()).
    /// Used in Query().Where() context where DynamoDB requires key condition expressions to only reference keys.
    /// Non-key properties will trigger an <see cref="InvalidKeyExpressionException"/>.
    /// </summary>
    KeysOnly
}
