using Amazon.Lambda.DynamoDBEvents;

namespace Oproto.FluentDynamoDb.Streams;

public static class DynamoDbStreamRecordExtensions
{
    public static Task<DynamoDbStreamRecordProcessor> Process(this DynamoDBEvent.DynamodbStreamRecord record)
    {
        return new DynamoDbStreamRecordProcessor(record).Awaitable();
    }
}