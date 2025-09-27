using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.Model;

namespace Oproto.FluentDynamoDb.Utility;

/// <summary>
/// JSON serialization context for AOT compatibility.
/// Provides pre-compiled serialization support for DynamoDB types used in pagination and other operations.
/// This ensures the library works correctly with Native AOT compilation by avoiding runtime reflection.
/// </summary>
[JsonSerializable(typeof(AttributeValue))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<AttributeValue>))]
[JsonSerializable(typeof(Dictionary<string,AttributeValue>))]
internal partial class SerializationContext : JsonSerializerContext
{
    
}