using Amazon.Lambda.DynamoDBEvents;

namespace Oproto.FluentDynamoDb.Streams;

/// <summary>
/// Extension methods for processing DynamoDB Stream records in AWS Lambda functions.
/// Provides a fluent interface for handling stream events with pattern matching and conditional processing.
/// </summary>
public static class DynamoDbStreamRecordExtensions
{
    /// <summary>
    /// Starts processing a DynamoDB Stream record using the fluent processing pipeline.
    /// This is the entry point for the stream processing fluent interface.
    /// </summary>
    /// <param name="record">The DynamoDB stream record to process.</param>
    /// <returns>A task containing a DynamoDbStreamRecordProcessor for further processing.</returns>
    /// <example>
    /// <code>
    /// await record.Process()
    ///     .OnMatch("pk", "USER#123", async processor => 
    ///     {
    ///         await processor.OnInsert(async r => Console.WriteLine("User created"));
    ///         await processor.OnUpdate(async r => Console.WriteLine("User updated"));
    ///     });
    /// </code>
    /// </example>
    public static Task<DynamoDbStreamRecordProcessor> Process(this DynamoDBEvent.DynamodbStreamRecord record)
    {
        return new DynamoDbStreamRecordProcessor(record).Awaitable();
    }
}