using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.Requests.Interfaces;

namespace Oproto.FluentDynamoDb.Requests;

public class ScanRequestBuilder :
    IWithAttributeNames<ScanRequestBuilder>, IWithAttributeValues<ScanRequestBuilder>
{
    public ScanRequestBuilder(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }
    
    private ScanRequest _req = new ScanRequest();
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly AttributeValueInternal _attrV = new AttributeValueInternal();
    private readonly AttributeNameInternal _attrN = new AttributeNameInternal();
    
    public ScanRequestBuilder ForTable(string tableName)
    {
        _req.TableName = tableName;
        return this;
    }
    
    public ScanRequestBuilder WithFilter(string filterExpression)
    {
        _req.FilterExpression = filterExpression;
        return this;
    }
    
    public ScanRequestBuilder WithProjection(string projectionExpression)
    {
        _req.ProjectionExpression = projectionExpression;
        _req.Select = Select.SPECIFIC_ATTRIBUTES;
        return this;
    }
    
    public ScanRequestBuilder UsingIndex(string indexName)
    {
        _req.IndexName = indexName;
        return this;
    }
    
    public ScanRequestBuilder Take(int limit)
    {
        _req.Limit = limit;
        return this;
    }
    
    public ScanRequestBuilder StartAt(Dictionary<string, AttributeValue> exclusiveStartKey)
    {
        _req.ExclusiveStartKey = exclusiveStartKey;
        return this;
    }
    
    public ScanRequestBuilder UsingConsistentRead()
    {
        _req.ConsistentRead = true;
        return this;
    }
    
    public ScanRequestBuilder WithSegment(int segment, int totalSegments)
    {
        _req.Segment = segment;
        _req.TotalSegments = totalSegments;
        return this;
    }
    
    public ScanRequestBuilder Count()
    {
        _req.Select = Select.COUNT;
        return this;
    }
    
    public ScanRequestBuilder ReturnTotalConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.TOTAL;
        return this;
    }
    
    public ScanRequestBuilder ReturnIndexConsumedCapacity()
    {
        _req.ReturnConsumedCapacity = Amazon.DynamoDBv2.ReturnConsumedCapacity.INDEXES;
        return this;
    }
    
    public ScanRequestBuilder ReturnConsumedCapacity(ReturnConsumedCapacity consumedCapacity)
    {
        _req.ReturnConsumedCapacity = consumedCapacity;
        return this;
    }
    
    public ScanRequestBuilder WithAttributes(Dictionary<string, string> attributeNames)
    {
        _attrN.WithAttributes(attributeNames);
        return this;
    }
    
    public ScanRequestBuilder WithAttributes(Action<Dictionary<string, string>> attributeNameFunc)
    {
        _attrN.WithAttributes(attributeNameFunc);
        return this;
    }

    public ScanRequestBuilder WithAttribute(string parameterName, string attributeName)
    {
        _attrN.WithAttribute(parameterName, attributeName);
        return this;
    }

    public ScanRequestBuilder WithValues(
        Dictionary<string, AttributeValue> attributeValues)
    {
        _attrV.WithValues(attributeValues);
        return this;
    }
    
    public ScanRequestBuilder WithValues(
        Action<Dictionary<string, AttributeValue>> attributeValueFunc)
    {
        _attrV.WithValues(attributeValueFunc);
        return this;
    }
    
    public ScanRequestBuilder WithValue(
        string attributeName, string? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    public ScanRequestBuilder WithValue(
        string attributeName, bool? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    public ScanRequestBuilder WithValue(
        string attributeName, decimal? attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    public ScanRequestBuilder WithValue(string attributeName, Dictionary<string, string> attributeValue,
        bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }
    
    public ScanRequestBuilder WithValue(string attributeName, Dictionary<string, AttributeValue> attributeValue, bool conditionalUse = true)
    {
        _attrV.WithValue(attributeName, attributeValue, conditionalUse);
        return this;
    }

    public ScanRequest ToScanRequest()
    {
        _req.ExpressionAttributeNames = _attrN.AttributeNames;
        _req.ExpressionAttributeValues = _attrV.AttributeValues;
        return _req;
    }

    public async Task<ScanResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _dynamoDbClient.ScanAsync(ToScanRequest(), cancellationToken);
    }
}