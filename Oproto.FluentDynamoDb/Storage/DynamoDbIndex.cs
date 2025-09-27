using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Represents a DynamoDB Global Secondary Index (GSI) or Local Secondary Index (LSI).
/// Provides a convenient way to query indexes with the correct table and index configuration.
/// </summary>
/// <example>
/// <code>
/// // Define an index in your table class
/// public DynamoDbIndex StatusIndex => new DynamoDbIndex(this, "StatusIndex");
/// 
/// // Query the index
/// var results = await table.StatusIndex.Query
///     .Where("gsi1pk = :status")
///     .WithValue(":status", "ACTIVE")
///     .ExecuteAsync();
/// </code>
/// </example>
public class DynamoDbIndex
{
    /// <summary>
    /// Initializes a new instance of the DynamoDbIndex.
    /// </summary>
    /// <param name="table">The parent table that contains this index.</param>
    /// <param name="indexName">The name of the index as defined in DynamoDB.</param>
    public DynamoDbIndex(DynamoDbTableBase table, string indexName)
    {
        _table = table;
        Name = indexName;
    }

    private readonly DynamoDbTableBase _table;
    
    /// <summary>
    /// Gets the name of the index.
    /// </summary>
    public string Name { get; private init; }
    
    /// <summary>
    /// Gets a query builder pre-configured to query this specific index.
    /// The builder is automatically configured with the correct table name and index name.
    /// 
    /// Note: When querying an index, you must use the index's key schema in your key condition expression.
    /// Global Secondary Indexes (GSI) do not support consistent reads.
    /// </summary>
    /// <example>
    /// <code>
    /// // Query a GSI with its own key schema
    /// var results = await myIndex.Query
    ///     .Where("gsi1pk = :pk AND begins_with(gsi1sk, :prefix)")
    ///     .WithValue(":pk", "STATUS#ACTIVE")
    ///     .WithValue(":prefix", "USER#")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public QueryRequestBuilder Query => new QueryRequestBuilder(_table.DynamoDbClient).ForTable(_table.Name).UsingIndex(Name);
}