using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Oproto.FluentDynamoDb.Requests;

public class BatchWriteItemRequestBuilder
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly BatchWriteItemRequest _req = new();

    public BatchWriteItemRequestBuilder(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }

    public BatchWriteItemRequestBuilder WriteToTable(string tableName, Action<BatchWriteItemBuilder> builderAction)
    {
        var builder = new BatchWriteItemBuilder(tableName);
        builderAction(builder);
        
        var writeRequests = builder.ToWriteRequests();
        if (writeRequests.Count > 0)
        {
            if (!_req.RequestItems.ContainsKey(tableName))
            {
                _req.RequestItems[tableName] = new List<WriteRequest>();
            }
            _req.RequestItems[tableName].AddRange(writeRequests);
        }
        
        return this;
    }

    public BatchWriteItemRequestBuilder ReturnTotalConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.TOTAL;
        return this;
    }

    public BatchWriteItemRequestBuilder ReturnConsumedCapacity(ReturnConsumedCapacity consumedCapacity)
    {
        _req.ReturnConsumedCapacity = consumedCapacity;
        return this;
    }

    public BatchWriteItemRequestBuilder ReturnIndexesConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.INDEXES;
        return this;
    }

    public BatchWriteItemRequestBuilder ReturnItemCollectionMetrics()
    {
        _req.ReturnItemCollectionMetrics = Amazon.DynamoDBv2.ReturnItemCollectionMetrics.SIZE;
        return this;
    }

    public BatchWriteItemRequestBuilder ReturnItemCollectionMetrics(ReturnItemCollectionMetrics itemCollectionMetrics)
    {
        _req.ReturnItemCollectionMetrics = itemCollectionMetrics;
        return this;
    }

    public BatchWriteItemRequest ToBatchWriteItemRequest()
    {
        return _req;
    }

    public async Task<BatchWriteItemResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _dynamoDbClient.BatchWriteItemAsync(this.ToBatchWriteItemRequest(), cancellationToken);
    }
}