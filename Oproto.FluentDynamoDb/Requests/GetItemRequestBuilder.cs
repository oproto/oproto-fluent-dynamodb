using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.Requests.Interfaces;

namespace Oproto.FluentDynamoDb.Requests;

/// <summary>
/// Fluent builder for DynamoDB GetItem operations.
/// Provides a type-safe way to construct GetItem requests with support for key specification,
/// projection expressions, consistent reads, and attribute name mapping.
/// </summary>
/// <example>
/// <code>
/// // Get an item by primary key
/// var response = await table.Get
///     .WithKey("id", "123")
///     .ExecuteAsync();
/// 
/// // Get with projection and consistent read
/// var response = await table.Get
///     .WithKey("pk", "USER", "sk", "profile")
///     .WithProjection("#name, #email")
///     .WithAttribute("#name", "name")
///     .WithAttribute("#email", "email")
///     .UsingConsistentRead()
///     .ExecuteAsync();
/// </code>
/// </example>
public class GetItemRequestBuilder : IWithKey<GetItemRequestBuilder>, IWithAttributeNames<GetItemRequestBuilder>
{
    /// <summary>
    /// Initializes a new instance of the GetItemRequestBuilder.
    /// </summary>
    /// <param name="dynamoDbClient">The DynamoDB client to use for executing the request.</param>
    public GetItemRequestBuilder(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }
    
    private GetItemRequest _req = new GetItemRequest();
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly AttributeNameInternal _attrN = new AttributeNameInternal();
    
    /// <summary>
    /// Specifies the name of the table to get the item from.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder ForTable(string tableName)
    {
        _req.TableName = tableName;
        return this;
    }

    /// <summary>
    /// Specifies the primary key of the item to retrieve using AttributeValue objects.
    /// </summary>
    /// <param name="primaryKeyName">The name of the primary key attribute.</param>
    /// <param name="primaryKeyValue">The value of the primary key attribute.</param>
    /// <param name="sortKeyName">The name of the sort key attribute (optional for tables with composite keys).</param>
    /// <param name="sortKeyValue">The value of the sort key attribute (optional for tables with composite keys).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithKey(string primaryKeyName, AttributeValue primaryKeyValue, string? sortKeyName=null, AttributeValue? sortKeyValue = null)
    {
        _req.Key = new() { {primaryKeyName, primaryKeyValue } };
        if (sortKeyName!= null && sortKeyValue != null)
        {
            _req.Key.Add(sortKeyName, sortKeyValue);
        }
        return this;
    }

    /// <summary>
    /// Specifies a single key attribute using string values (automatically converted to DynamoDB string type).
    /// </summary>
    /// <param name="keyName">The name of the key attribute.</param>
    /// <param name="keyValue">The string value of the key attribute.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithKey(string keyName, string keyValue)
    {
        if (_req.Key == null) _req.Key = new();
        _req.Key.Add(keyName, new AttributeValue { S = keyValue });
        return this;
    }
    
    /// <summary>
    /// Specifies both primary key and sort key using string values (automatically converted to DynamoDB string type).
    /// Use this method for tables with composite primary keys.
    /// </summary>
    /// <param name="primaryKeyName">The name of the primary key attribute.</param>
    /// <param name="primaryKeyValue">The string value of the primary key attribute.</param>
    /// <param name="sortKeyName">The name of the sort key attribute.</param>
    /// <param name="sortKeyValue">The string value of the sort key attribute.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithKey(string primaryKeyName, string primaryKeyValue, string sortKeyName, string sortKeyValue)
    {
        if (_req.Key == null) _req.Key = new();
        _req.Key.Add(primaryKeyName, new AttributeValue { S = primaryKeyValue });
        _req.Key.Add(sortKeyName, new AttributeValue { S = sortKeyValue });
        return this;
    }
    
    /// <summary>
    /// Adds multiple attribute name mappings for use in projection expressions.
    /// This is useful when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="attributeNames">A dictionary mapping parameter names to actual attribute names.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithAttributes(Dictionary<string,string> attributeNames)
    {
        _attrN.WithAttributes(attributeNames);
        return this;
    }
    
    /// <summary>
    /// Adds multiple attribute name mappings using a configuration action.
    /// This is useful when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="attributeNameFunc">An action that configures the attribute name mappings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithAttributes(Action<Dictionary<string,string>> attributeNameFunc)
    {
        _attrN.WithAttributes(attributeNameFunc);
        return this;
    }

    /// <summary>
    /// Adds a single attribute name mapping for use in projection expressions.
    /// This is useful when attribute names conflict with DynamoDB reserved words.
    /// </summary>
    /// <param name="parameterName">The parameter name to use in expressions (e.g., "#name").</param>
    /// <param name="attributeName">The actual attribute name in the table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder WithAttribute(string parameterName, string attributeName)
    {
        _attrN.WithAttribute(parameterName, attributeName);
        return this;
    }

    /// <summary>
    /// Enables strongly consistent reads for this operation.
    /// By default, DynamoDB uses eventually consistent reads which are faster and consume less capacity,
    /// but may not reflect the most recent write operations. Use consistent reads when you need
    /// the most up-to-date data, but be aware this consumes twice the read capacity.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder UsingConsistentRead()
    {
        _req.ConsistentRead = true;
        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the item using a projection expression.
    /// This can reduce the amount of data transferred and improve performance.
    /// Use attribute name parameters (e.g., "#name") for reserved words.
    /// </summary>
    /// <param name="projectionExpression">A string that identifies the attributes to retrieve (e.g., "#name, email, #status").</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithProjection("#name, email, #status")
    /// .WithAttribute("#name", "name")
    /// .WithAttribute("#status", "status")
    /// </code>
    /// </example>
    public GetItemRequestBuilder WithProjection(string projectionExpression)
    {
        _req.ProjectionExpression = projectionExpression;
        return this;
    }

    /// <summary>
    /// Configures the response to include the total consumed capacity information.
    /// This is useful for monitoring and optimizing read capacity usage.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder ReturnTotalConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.TOTAL;
        return this;
    }
    
    /// <summary>
    /// Configures the level of consumed capacity information to return in the response.
    /// </summary>
    /// <param name="consumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public GetItemRequestBuilder ReturnConsumedCapacity(ReturnConsumedCapacity consumedCapacity)
    {
        _req.ReturnConsumedCapacity = consumedCapacity;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured GetItemRequest.
    /// This method is typically used for advanced scenarios where you need direct access to the request object.
    /// </summary>
    /// <returns>A configured GetItemRequest ready for execution.</returns>
    public GetItemRequest ToGetItemRequest()
    {
        _req.ExpressionAttributeNames = _attrN.AttributeNames;
        return _req;
    }
    
    /// <summary>
    /// Executes the GetItem operation asynchronously using the configured parameters.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the GetItemResponse.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown when the specified table doesn't exist.</exception>
    /// <exception cref="ProvisionedThroughputExceededException">Thrown when the request rate is too high.</exception>
    public async Task<GetItemResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _dynamoDbClient.GetItemAsync(this.ToGetItemRequest(), cancellationToken);
    }
}