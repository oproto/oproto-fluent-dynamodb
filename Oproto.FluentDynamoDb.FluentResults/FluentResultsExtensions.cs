using FluentResults;
using Oproto.FluentDynamoDb.Requests;
using Oproto.FluentDynamoDb.Requests.Extensions;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.FluentResults;

/// <summary>
/// FluentResults extensions for enhanced ExecuteAsync methods.
/// These extensions wrap the enhanced ExecuteAsync methods to return Result&lt;T&gt; instead of throwing exceptions.
/// </summary>
public static class FluentResultsExtensions
{
    /// <summary>
    /// Executes a GetItem operation and maps the result to a strongly-typed entity, returning a Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The GetItemRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the GetItemResponse with mapped entity or error details.</returns>
    public static async Task<Result<GetItemResponse<T>>> ExecuteAsyncResult<T>(
        this GetItemRequestBuilder builder, 
        CancellationToken cancellationToken = default) 
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ExecuteAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute GetItem operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a Query operation and maps each DynamoDB item to a separate entity instance (1:1 mapping), returning a Result&lt;T&gt;.
    /// Each DynamoDB item becomes a separate T instance in the returned list.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The QueryRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the list of mapped entities or error details.</returns>
    public static async Task<Result<List<T>>> ToListAsyncResult<T>(
        this QueryRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ToListAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute Query operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a Query operation and combines multiple DynamoDB items into composite entities (N:1 mapping), returning a Result&lt;T&gt;.
    /// Multiple DynamoDB items with the same partition key are combined into single T instances.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The QueryRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the list of composite entities or error details.</returns>
    public static async Task<Result<List<T>>> ToCompositeEntityListAsyncResult<T>(
        this QueryRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ToCompositeEntityListAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute Query operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a Query operation and returns a single composite entity (N:1 mapping), returning a Result&lt;T&gt;.
    /// Multiple DynamoDB items with the same partition key are combined into a single T instance.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The QueryRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the single composite entity or error details.</returns>
    public static async Task<Result<T?>> ToCompositeEntityAsyncResult<T>(
        this QueryRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ToCompositeEntityAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute Query operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a Scan operation and maps each DynamoDB item to a separate entity instance (1:1 mapping), returning a Result&lt;T&gt;.
    /// Each DynamoDB item becomes a separate T instance in the returned list.
    /// Warning: Scan operations can be expensive on large tables. Use Query operations when possible.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The ScanRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the list of mapped entities or error details.</returns>
    public static async Task<Result<List<T>>> ToListAsyncResult<T>(
        this ScanRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ToListAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute Scan operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a Scan operation and combines multiple DynamoDB items into composite entities (N:1 mapping), returning a Result&lt;T&gt;.
    /// Multiple DynamoDB items with the same partition key are combined into single T instances.
    /// Warning: Scan operations can be expensive on large tables. Use Query operations when possible.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The ScanRequestBuilder instance.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the list of composite entities or error details.</returns>
    public static async Task<Result<List<T>>> ToCompositeEntityListAsyncResult<T>(
        this ScanRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            var response = await EnhancedExecuteAsyncExtensions.ToCompositeEntityListAsync<T>(builder, cancellationToken);
            return Result.Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute Scan operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Configures the PutItem operation to use a strongly-typed entity, returning a Result.
    /// The entity is automatically converted to DynamoDB AttributeValue format.
    /// For multi-item entities, only the first item is used for PutItem operations.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The PutItemRequestBuilder instance.</param>
    /// <param name="item">The entity instance to put.</param>
    /// <returns>A Result containing the configured builder or error details.</returns>
    public static Result<PutItemRequestBuilder> WithItemResult<T>(
        this PutItemRequestBuilder builder, 
        T item) 
        where T : class, IDynamoDbEntity
    {
        try
        {
            var configuredBuilder = EnhancedExecuteAsyncExtensions.WithItem(builder, item);
            return Result.Ok(configuredBuilder);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to configure PutItem with {typeof(T).Name} entity: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes a PutItem operation with a strongly-typed entity, returning a Result.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="builder">The PutItemRequestBuilder instance.</param>
    /// <param name="item">The entity instance to put.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Result indicating success or containing error details.</returns>
    public static async Task<Result> ExecuteAsyncResult<T>(
        this PutItemRequestBuilder builder,
        T item,
        CancellationToken cancellationToken = default)
        where T : class, IDynamoDbEntity
    {
        try
        {
            await EnhancedExecuteAsyncExtensions.WithItem(builder, item).ExecuteAsync(cancellationToken);
            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions as they should not be wrapped
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute PutItem operation for {typeof(T).Name}: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Gets all DynamoDB items for a multi-item entity, returning a Result.
    /// This is useful for batch operations or when you need to work with individual items.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IDynamoDbEntity.</typeparam>
    /// <param name="entity">The entity instance to convert.</param>
    /// <returns>A Result containing the list of DynamoDB items or error details.</returns>
    public static Result<List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>>> GetDynamoDbItemsResult<T>(T entity) 
        where T : class, IDynamoDbEntity
    {
        try
        {
            var items = T.ToDynamoDbMultiple(entity);
            return Result.Ok(items);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to convert {typeof(T).Name} entity to DynamoDB items: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }
}