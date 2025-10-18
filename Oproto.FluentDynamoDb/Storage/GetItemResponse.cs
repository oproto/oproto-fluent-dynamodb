using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Strongly-typed response for GetItem operations.
/// </summary>
/// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
public class GetItemResponse<T> where T : class, IDynamoDbEntity
{
    /// <summary>
    /// Gets or sets the mapped entity item, or null if no item was found.
    /// </summary>
    public T? Item { get; set; }

    /// <summary>
    /// Gets or sets the consumed capacity information for the operation.
    /// </summary>
    public ConsumedCapacity? ConsumedCapacity { get; set; }

    /// <summary>
    /// Gets or sets the response metadata from DynamoDB.
    /// </summary>
    public ResponseMetadata? ResponseMetadata { get; set; }
}