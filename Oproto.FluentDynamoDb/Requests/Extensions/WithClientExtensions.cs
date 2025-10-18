using Amazon.DynamoDBv2;

namespace Oproto.FluentDynamoDb.Requests.Extensions;

/// <summary>
/// Extensions to support scoped client usage with request builders.
/// These extensions allow service layers to provide tenant-specific STS clients.
/// </summary>
public static class WithClientExtensions
{
    /// <summary>
    /// Creates a new GetItemRequestBuilder instance with a different DynamoDB client.
    /// This is useful for STS scoped clients with tenant-specific policies.
    /// Note: This creates a new builder instance. You'll need to reconfigure the request parameters.
    /// </summary>
    /// <param name="builder">The original GetItemRequestBuilder instance.</param>
    /// <param name="client">The scoped DynamoDB client to use.</param>
    /// <returns>A new GetItemRequestBuilder instance using the specified client.</returns>
    public static GetItemRequestBuilder WithClient(this GetItemRequestBuilder builder, IAmazonDynamoDB client)
    {
        return new GetItemRequestBuilder(client);
    }

    /// <summary>
    /// Creates a new QueryRequestBuilder instance with a different DynamoDB client.
    /// This is useful for STS scoped clients with tenant-specific policies.
    /// Note: This creates a new builder instance. You'll need to reconfigure the request parameters.
    /// </summary>
    /// <param name="builder">The original QueryRequestBuilder instance.</param>
    /// <param name="client">The scoped DynamoDB client to use.</param>
    /// <returns>A new QueryRequestBuilder instance using the specified client.</returns>
    public static QueryRequestBuilder WithClient(this QueryRequestBuilder builder, IAmazonDynamoDB client)
    {
        return new QueryRequestBuilder(client);
    }

    /// <summary>
    /// Creates a new PutItemRequestBuilder instance with a different DynamoDB client.
    /// This is useful for STS scoped clients with tenant-specific policies.
    /// Note: This creates a new builder instance. You'll need to reconfigure the request parameters.
    /// </summary>
    /// <param name="builder">The original PutItemRequestBuilder instance.</param>
    /// <param name="client">The scoped DynamoDB client to use.</param>
    /// <returns>A new PutItemRequestBuilder instance using the specified client.</returns>
    public static PutItemRequestBuilder WithClient(this PutItemRequestBuilder builder, IAmazonDynamoDB client)
    {
        return new PutItemRequestBuilder(client);
    }

    /// <summary>
    /// Creates a new UpdateItemRequestBuilder instance with a different DynamoDB client.
    /// This is useful for STS scoped clients with tenant-specific policies.
    /// Note: This creates a new builder instance. You'll need to reconfigure the request parameters.
    /// </summary>
    /// <param name="builder">The original UpdateItemRequestBuilder instance.</param>
    /// <param name="client">The scoped DynamoDB client to use.</param>
    /// <returns>A new UpdateItemRequestBuilder instance using the specified client.</returns>
    public static UpdateItemRequestBuilder WithClient(this UpdateItemRequestBuilder builder, IAmazonDynamoDB client)
    {
        return new UpdateItemRequestBuilder(client);
    }

    /// <summary>
    /// Creates a new DeleteItemRequestBuilder instance with a different DynamoDB client.
    /// This is useful for STS scoped clients with tenant-specific policies.
    /// Note: This creates a new builder instance. You'll need to reconfigure the request parameters.
    /// </summary>
    /// <param name="builder">The original DeleteItemRequestBuilder instance.</param>
    /// <param name="client">The scoped DynamoDB client to use.</param>
    /// <returns>A new DeleteItemRequestBuilder instance using the specified client.</returns>
    public static DeleteItemRequestBuilder WithClient(this DeleteItemRequestBuilder builder, IAmazonDynamoDB client)
    {
        return new DeleteItemRequestBuilder(client);
    }
}