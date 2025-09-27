using Amazon.Lambda.DynamoDBEvents;

namespace Oproto.FluentDynamoDb.Streams;

/// <summary>
/// Event processor for DynamoDB Stream records that provides event type filtering.
/// Allows you to handle different types of DynamoDB events (INSERT, UPDATE, REMOVE) with specific logic.
/// </summary>
public class DynamoDbStreamRecordEventProcessor
{
    
    /// <summary>
    /// Gets the DynamoDB stream record being processed.
    /// </summary>
    public required DynamoDBEvent.DynamodbStreamRecord Record { get; init; }

    public async Task<DynamoDbStreamRecordEventProcessor> Awaitable()
    {
        return await Task.Run(() => this);
    }
}

public static class DynamoDbStreamRecordEventProcessorExtensions
{
    /// <summary>
    /// Executes the provided function if the stream record represents an INSERT event.
    /// INSERT events occur when new items are added to the DynamoDB table.
    /// </summary>
    /// <param name="processor">The event processor task.</param>
    /// <param name="onInsertFunc">The function to execute for INSERT events.</param>
    /// <returns>The event processor for further chaining.</returns>
    /// <example>
    /// <code>
    /// await processor.OnInsert(async record => 
    /// {
    ///     var newItem = record.Dynamodb.NewImage;
    ///     Console.WriteLine($"New item created: {newItem["id"].S}");
    /// });
    /// </code>
    /// </example>
    public static async Task<DynamoDbStreamRecordEventProcessor> OnInsert(this Task<DynamoDbStreamRecordEventProcessor> processor, Func<DynamoDBEvent.DynamodbStreamRecord, Task> onInsertFunc)
    {
        await processor;
        if (processor.Result.Record.EventName == "INSERT")
        {
            await onInsertFunc(processor.Result.Record);
        }
        return processor.Result;
    }

    /// <summary>
    /// Executes the provided function if the stream record represents an UPDATE event.
    /// UPDATE events occur when existing items in the DynamoDB table are modified.
    /// </summary>
    /// <param name="processor">The event processor task.</param>
    /// <param name="onUpdateFunc">The function to execute for UPDATE events.</param>
    /// <returns>The event processor for further chaining.</returns>
    /// <example>
    /// <code>
    /// await processor.OnUpdate(async record => 
    /// {
    ///     var oldItem = record.Dynamodb.OldImage;
    ///     var newItem = record.Dynamodb.NewImage;
    ///     Console.WriteLine($"Item updated: {newItem["id"].S}");
    /// });
    /// </code>
    /// </example>
    public static async Task<DynamoDbStreamRecordEventProcessor> OnUpdate(this Task<DynamoDbStreamRecordEventProcessor> processor, Func<DynamoDBEvent.DynamodbStreamRecord, Task> onUpdateFunc)
    {
        await processor;
        if (processor.Result.Record.EventName == "UPDATE")
        {
            await onUpdateFunc(processor.Result.Record);
        }
        return processor.Result;
    }

    /// <summary>
    /// Executes the provided function if the stream record represents a REMOVE event.
    /// REMOVE events occur when items are deleted from the DynamoDB table, either manually or via TTL.
    /// </summary>
    /// <param name="processor">The event processor task.</param>
    /// <param name="onDeleteFunc">The function to execute for REMOVE events.</param>
    /// <returns>The event processor for further chaining.</returns>
    /// <example>
    /// <code>
    /// await processor.OnDelete(async record => 
    /// {
    ///     var deletedItem = record.Dynamodb.OldImage;
    ///     Console.WriteLine($"Item deleted: {deletedItem["id"].S}");
    /// });
    /// </code>
    /// </example>
    public static async Task<DynamoDbStreamRecordEventProcessor> OnDelete(this Task<DynamoDbStreamRecordEventProcessor> processor, Func<DynamoDBEvent.DynamodbStreamRecord, Task> onDeleteFunc)
    {
        await processor;
        if (processor.Result.Record.EventName == "REMOVE")
        {
            await onDeleteFunc(processor.Result.Record);
        }
        return processor.Result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processor"></param>
    /// <param name="onDeleteFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordEventProcessor> OnNonTtlDelete(this Task<DynamoDbStreamRecordEventProcessor> processor, Func<DynamoDBEvent.DynamodbStreamRecord, Task> onDeleteFunc)
    {
        await processor;
        if (processor.Result.Record.EventName == "REMOVE" && !IsTtlUser(processor.Result.Record.UserIdentity))
        {
            await onDeleteFunc(processor.Result.Record);
        }
        return processor.Result;
    }

    /// <summary>
    /// Executes the provided function if the stream record represents a TTL-triggered REMOVE event.
    /// TTL deletes are automatically performed by DynamoDB when items expire based on their TTL attribute.
    /// </summary>
    /// <param name="processor">The event processor task.</param>
    /// <param name="onDeleteFunc">The function to execute for TTL REMOVE events.</param>
    /// <returns>The event processor for further chaining.</returns>
    /// <example>
    /// <code>
    /// await processor.OnTtlDelete(async record => 
    /// {
    ///     var expiredItem = record.Dynamodb.OldImage;
    ///     Console.WriteLine($"Item expired via TTL: {expiredItem["id"].S}");
    /// });
    /// </code>
    /// </example>
    public static async Task<DynamoDbStreamRecordEventProcessor> OnTtlDelete(this Task<DynamoDbStreamRecordEventProcessor> processor, Func<DynamoDBEvent.DynamodbStreamRecord, Task> onDeleteFunc)
    {
        await processor;
        if (processor.Result.Record.EventName == "REMOVE" && IsTtlUser(processor.Result.Record.UserIdentity))
        {
            await onDeleteFunc(processor.Result.Record);
        }
        return processor.Result;
    }

    private static bool IsTtlUser(DynamoDBEvent.Identity identity)
    {
        return identity is { Type: "Service", PrincipalId: "dynamodb.amazonaws.com" };
    }
}