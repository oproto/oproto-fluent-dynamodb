using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.DynamoDBEvents;

namespace Oproto.FluentDynamoDb.Attributes;

/// <summary>
/// Extension methods for working with DynamoDB AttributeValue dictionaries.
/// Provides convenient methods for safely accessing attribute values from DynamoDB items.
/// </summary>
public static class AttributeValueExtensions
{
    /// <summary>
    /// Safely retrieves an AttributeValue from a dictionary by key.
    /// Returns null if the key doesn't exist, avoiding KeyNotFoundException.
    /// </summary>
    /// <param name="values">The dictionary of attribute values (typically from a DynamoDB item).</param>
    /// <param name="key">The attribute name to retrieve.</param>
    /// <returns>The AttributeValue if found, otherwise null.</returns>
    /// <example>
    /// <code>
    /// var item = response.Item;
    /// var nameValue = item.ForKey("name");
    /// var name = nameValue?.S; // Safely get string value
    /// </code>
    /// </example>
    public static AttributeValue? ForKey(this IDictionary<string, AttributeValue> values, string key)
    {
        AttributeValue? value = null;
        if (!values.TryGetValue(key, out value)) return null;
        return value;
    }
    
    /// <summary>
    /// Safely retrieves a DynamoDB Stream AttributeValue from a dictionary by key.
    /// Returns null if the key doesn't exist, avoiding KeyNotFoundException.
    /// This overload is specifically for DynamoDB Stream events.
    /// </summary>
    /// <param name="values">The dictionary of stream attribute values.</param>
    /// <param name="key">The attribute name to retrieve.</param>
    /// <returns>The stream AttributeValue if found, otherwise null.</returns>
    /// <example>
    /// <code>
    /// var oldImage = record.Dynamodb.OldImage;
    /// var statusValue = oldImage.ForKey("status");
    /// var status = statusValue?.S; // Safely get string value
    /// </code>
    /// </example>
    public static DynamoDBEvent.AttributeValue? ForKey(this IDictionary<string, DynamoDBEvent.AttributeValue> values, string key)
    {
        DynamoDBEvent.AttributeValue? value = null;
        if (!values.TryGetValue(key, out value)) return null;
        return value;
    }
}