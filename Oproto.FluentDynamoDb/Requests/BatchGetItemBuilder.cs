using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.Requests.Interfaces;

namespace Oproto.FluentDynamoDb.Requests;

public class BatchGetItemBuilder : IWithKey<BatchGetItemBuilder>, IWithAttributeNames<BatchGetItemBuilder>
{
    private readonly KeysAndAttributes _keysAndAttributes = new KeysAndAttributes();
    private readonly string _tableName;
    private readonly AttributeNameInternal _attrN = new AttributeNameInternal();

    public BatchGetItemBuilder(string tableName)
    {
        _tableName = tableName;
        _keysAndAttributes.Keys = new List<Dictionary<string, AttributeValue>>();
    }

    public BatchGetItemBuilder WithKey(string primaryKeyName, AttributeValue primaryKeyValue, string? sortKeyName = null, AttributeValue? sortKeyValue = null)
    {
        var key = new Dictionary<string, AttributeValue> { { primaryKeyName, primaryKeyValue } };
        if (sortKeyName != null && sortKeyValue != null)
        {
            key.Add(sortKeyName, sortKeyValue);
        }
        _keysAndAttributes.Keys.Add(key);
        return this;
    }

    public BatchGetItemBuilder WithKey(string keyName, string keyValue)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            { keyName, new AttributeValue { S = keyValue } }
        };
        _keysAndAttributes.Keys.Add(key);
        return this;
    }

    public BatchGetItemBuilder WithKey(string primaryKeyName, string primaryKeyValue, string sortKeyName, string sortKeyValue)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            { primaryKeyName, new AttributeValue { S = primaryKeyValue } },
            { sortKeyName, new AttributeValue { S = sortKeyValue } }
        };
        _keysAndAttributes.Keys.Add(key);
        return this;
    }

    public BatchGetItemBuilder WithProjection(string projectionExpression)
    {
        _keysAndAttributes.ProjectionExpression = projectionExpression;
        return this;
    }

    public BatchGetItemBuilder UsingConsistentRead()
    {
        _keysAndAttributes.ConsistentRead = true;
        return this;
    }

    public BatchGetItemBuilder WithAttributes(Dictionary<string, string> attributeNames)
    {
        _attrN.WithAttributes(attributeNames);
        return this;
    }

    public BatchGetItemBuilder WithAttributes(Action<Dictionary<string, string>> attributeNameFunc)
    {
        _attrN.WithAttributes(attributeNameFunc);
        return this;
    }

    public BatchGetItemBuilder WithAttribute(string parameterName, string attributeName)
    {
        _attrN.WithAttribute(parameterName, attributeName);
        return this;
    }

    public KeysAndAttributes ToKeysAndAttributes()
    {
        if (_attrN.AttributeNames.Count > 0)
        {
            _keysAndAttributes.ExpressionAttributeNames = _attrN.AttributeNames;
        }
        return _keysAndAttributes;
    }
}