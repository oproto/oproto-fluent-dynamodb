using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.Requests.Interfaces;

namespace Oproto.FluentDynamoDb.Requests;

/// <summary>
/// Fluent builder for DynamoDB Query operations.
/// Query operations efficiently retrieve items using the primary key and optional sort key conditions.
/// This is the preferred method for retrieving multiple items when you know the primary key.
/// Query operations are much more efficient than Scan operations and should be used whenever possible.
/// </summary>
/// <example>
/// <code>
/// // Query items with a specific primary key
/// var response = await table.Query
///     .Where("pk = :pk")
///     .WithValue(":pk", "USER#123")
///     .ExecuteAsync();
/// 
/// // Query with sort key condition and filter
/// var response = await table.Query
///     .Where("pk = :pk AND begins_with(sk, :prefix)")
///     .WithFilter("#status = :status")
///     .WithValue(":pk", "USER#123")
///     .WithValue(":prefix", "ORDER#")
///     .WithValue(":status", "ACTIVE")
///     .WithAttribute("#status", "status")
///     .Take(10)
///     .ExecuteAsync();
/// </code>
/// </example>
public class QueryRequestBuilder :
    IWithAttributeNames<QueryRequestBuilder>, IWithConditionExpression<QueryRequestBuilder>, IWithAttributeValues<QueryRequestBuilder>
{
    /// <summary>
    /// Initializes a new instance of the QueryRequestBuilder.
    /// </summary>
    /// <param name="dynamoDbClient">The DynamoDB client to use for executing the request.</param>
    public QueryRequestBuilder(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }
    
    private QueryRequest _req = new QueryRequest();
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly AttributeValueInternal _attrV = new AttributeValueInternal();
    private readonly AttributeNameInternal _attrN = new AttributeNameInternal();
    
    /// <summary>
    /// Specifies the name of the table to query.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder ForTable(string tableName)
    {
        _req.TableName = tableName;
        return this;
    }
    
    /// <summary>
    /// Limits the number of items to evaluate (not necessarily the number of items returned).
    /// DynamoDB will stop evaluating items once this limit is reached, even if the filter expression
    /// hasn't been applied to all items. Use this for pagination and to control consumed capacity.
    /// </summary>
    /// <param name="limit">The maximum number of items to evaluate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder Take(int limit)
    {
        _req.Limit = limit;
        return this;
    }
    
    /// <summary>
    /// Configures the query to return only the count of items that match the query conditions,
    /// rather than the items themselves. This is more efficient when you only need to know
    /// how many items match your criteria.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder Count()
    {
        _req.Select = Select.COUNT;
        return this;
    }
    
    /// <summary>
    /// Enables strongly consistent reads for this query operation.
    /// By default, DynamoDB uses eventually consistent reads which are faster and consume less capacity,
    /// but may not reflect the most recent write operations. Use consistent reads when you need
    /// the most up-to-date data, but be aware this consumes twice the read capacity.
    /// Note: Consistent reads are not supported on Global Secondary Indexes.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder UsingConsistentRead()
    {
        _req.ConsistentRead = true;
        return this;
    }
    
    /// <summary>
    /// Adds a filter expression to further refine the query results after the key condition is applied.
    /// Filter expressions are applied after items are retrieved based on the key condition,
    /// so they don't reduce consumed read capacity but can reduce the amount of data transferred.
    /// </summary>
    /// <param name="filterExpression">A condition expression that filters the query results.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithFilter("#status = :status AND #amount > :minAmount")
    /// .WithAttribute("#status", "status")
    /// .WithAttribute("#amount", "amount")
    /// .WithValue(":status", "ACTIVE")
    /// .WithValue(":minAmount", 100)
    /// </code>
    /// </example>
    public QueryRequestBuilder WithFilter(string filterExpression)
    {
        _req.FilterExpression = filterExpression;
        return this;
    }
    
    /// <summary>
    /// Specifies a Global Secondary Index (GSI) or Local Secondary Index (LSI) to query.
    /// When querying an index, the key condition must use the index's key schema.
    /// </summary>
    /// <param name="indexName">The name of the index to query.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder UsingIndex(string indexName)
    {
        _req.IndexName = indexName;
        return this;
    }
    
    /// <summary>
    /// Specifies which attributes to retrieve using a projection expression.
    /// This can significantly reduce the amount of data transferred and improve performance.
    /// Use attribute name parameters (e.g., "#name") for reserved words.
    /// </summary>
    /// <param name="projectionExpression">A string that identifies the attributes to retrieve.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithProjection("#name, email, #status, createdAt")
    /// .WithAttribute("#name", "name")
    /// .WithAttribute("#status", "status")
    /// </code>
    /// </example>
    public QueryRequestBuilder WithProjection(string projectionExpression)
    {
        _req.ProjectionExpression = projectionExpression;
        _req.Select = Select.SPECIFIC_ATTRIBUTES;
        return this;
    }
    
    /// <summary>
    /// Specifies the starting point for pagination by providing the last evaluated key from a previous query.
    /// This is used to continue querying from where the previous operation left off.
    /// </summary>
    /// <param name="exclusiveStartKey">The primary key of the item where the previous query stopped.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder StartAt(Dictionary<string,AttributeValue> exclusiveStartKey)
    {
        _req.ExclusiveStartKey = exclusiveStartKey;
        return this;
    }
    
    /// <summary>
    /// Adds multiple attribute name mappings for use in expressions.
    /// This is essential when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="attributeNames">A dictionary mapping parameter names to actual attribute names.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithAttributes(Dictionary<string,string> attributeNames)
    {
        _attrN.WithAttributes(attributeNames);
        return this;
    }
    
    /// <summary>
    /// Adds multiple attribute name mappings using a configuration action.
    /// This is essential when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="attributeNameFunc">An action that configures the attribute name mappings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithAttributes(Action<Dictionary<string,string>> attributeNameFunc)
    {
        _attrN.WithAttributes(attributeNameFunc);
        return this;
    }

    /// <summary>
    /// Adds a single attribute name mapping for use in expressions.
    /// This is essential when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="parameterName">The parameter name to use in expressions (e.g., "#name").</param>
    /// <param name="attributeName">The actual attribute name in the table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithAttribute(string parameterName, string attributeName)
    {
        _attrN.WithAttribute(parameterName, attributeName);
        return this;
    }

    /// <summary>
    /// Adds multiple attribute values for use in key conditions and filter expressions.
    /// These values are referenced in expressions using parameter names (e.g., ":value").
    /// </summary>
    /// <param name="attributeValues">A dictionary mapping parameter names to AttributeValue objects.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValues(
        Dictionary<string, AttributeValue> attributeValues)
    {
        _attrV.WithValues(attributeValues);
        return this;
    }
    
    /// <summary>
    /// Adds multiple attribute values using a configuration action.
    /// These values are referenced in expressions using parameter names (e.g., ":value").
    /// </summary>
    /// <param name="attributeValueFunc">An action that configures the attribute value mappings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValues(
        Action<Dictionary<string, AttributeValue>> attributeValueFunc)
    {
        _attrV.WithValues(attributeValueFunc);
        return this;
    }
    
    /// <summary>
    /// Adds a string attribute value for use in expressions.
    /// The value is automatically converted to a DynamoDB string type.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":value").</param>
    /// <param name="attributeValue">The string value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValue(
        string attributeName, string? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    /// <summary>
    /// Adds a boolean attribute value for use in expressions.
    /// The value is automatically converted to a DynamoDB boolean type.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":active").</param>
    /// <param name="attributeValue">The boolean value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValue(
        string attributeName, bool? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    /// <summary>
    /// Adds a numeric attribute value for use in expressions.
    /// The value is automatically converted to a DynamoDB number type.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":amount").</param>
    /// <param name="attributeValue">The decimal value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValue(
        string attributeName, decimal? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    /// <summary>
    /// Adds a map attribute value (string dictionary) for use in expressions.
    /// The dictionary is automatically converted to a DynamoDB map type with string values.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":metadata").</param>
    /// <param name="attributeValue">The string dictionary to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValue(string attributeName, Dictionary<string, string> attributeValue,
        bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    /// <summary>
    /// Adds a map attribute value (AttributeValue dictionary) for use in expressions.
    /// This provides full control over the DynamoDB map structure and types.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":complex").</param>
    /// <param name="attributeValue">The AttributeValue dictionary to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder WithValue(string attributeName, Dictionary<string, AttributeValue> attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    /// <summary>
    /// Specifies the key condition expression that determines which items to retrieve.
    /// This is required for all Query operations and must specify the primary key condition.
    /// For composite keys, you can also include sort key conditions.
    /// </summary>
    /// <param name="conditionExpression">The key condition expression (e.g., "pk = :pk" or "pk = :pk AND begins_with(sk, :prefix)").</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Simple primary key condition
    /// .Where("pk = :pk")
    /// .WithValue(":pk", "USER#123")
    /// 
    /// // Composite key with sort key condition
    /// .Where("pk = :pk AND begins_with(sk, :prefix)")
    /// .WithValue(":pk", "USER#123")
    /// .WithValue(":prefix", "ORDER#")
    /// </code>
    /// </example>
    public QueryRequestBuilder Where(string conditionExpression)
    {
        _req.KeyConditionExpression = conditionExpression;
        return this;
    }
    
    /// <summary>
    /// Configures the response to include total consumed capacity information.
    /// This is useful for monitoring and optimizing read capacity usage across tables and indexes.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder ReturnTotalConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.TOTAL;
        return this;
    }
    
    /// <summary>
    /// Configures the response to include consumed capacity information for indexes only.
    /// This is useful when querying indexes and you want to monitor index-specific capacity usage.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder ReturnIndexConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.INDEXES;
        return this;
    }
    
    /// <summary>
    /// Configures the level of consumed capacity information to return in the response.
    /// </summary>
    /// <param name="consumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder ReturnConsumedCapacity(ReturnConsumedCapacity consumedCapacity)
    {
        _req.ReturnConsumedCapacity = consumedCapacity;
        return this;
    }

    /// <summary>
    /// Configures the query to return items in ascending order by sort key.
    /// This is the default behavior for Query operations.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder OrderAscending()
    {
        _req.ScanIndexForward = true;
        return this;
    }

    /// <summary>
    /// Configures the query to return items in descending order by sort key.
    /// This is useful when you want the most recent items first (assuming sort key represents time).
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder OrderDescending()
    {
        _req.ScanIndexForward = false;
        return this;
    }

    /// <summary>
    /// Configures the sort order for query results.
    /// </summary>
    /// <param name="ascending">True for ascending order (default), false for descending order.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryRequestBuilder ScanIndexForward(bool ascending = true)
    {
        _req.ScanIndexForward = ascending;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured QueryRequest.
    /// This method is typically used for advanced scenarios where you need direct access to the request object.
    /// </summary>
    /// <returns>A configured QueryRequest ready for execution.</returns>
    public QueryRequest ToQueryRequest()
    {
        _req.ExpressionAttributeNames = _attrN.AttributeNames;
        _req.ExpressionAttributeValues = _attrV.AttributeValues;
        return _req;
    }

    /// <summary>
    /// Executes the Query operation asynchronously using the configured parameters.
    /// Query operations are efficient and should be preferred over Scan operations whenever possible.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the QueryResponse.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown when the specified table or index doesn't exist.</exception>
    /// <exception cref="ProvisionedThroughputExceededException">Thrown when the request rate is too high.</exception>
    /// <exception cref="ValidationException">Thrown when the key condition expression is invalid.</exception>
    public async Task<QueryResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _dynamoDbClient.QueryAsync(ToQueryRequest(), cancellationToken);
    }
}