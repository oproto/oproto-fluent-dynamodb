using System.Text.Json.Serialization;

namespace Oproto.FluentDynamoDb.SystemTextJson;

/// <summary>
/// Provides AOT-compatible JSON serialization using System.Text.Json with JsonSerializerContext.
/// This serializer is fully compatible with Native AOT compilation and produces no trim warnings.
/// </summary>
/// <remarks>
/// To use this serializer:
/// 1. Reference the Oproto.FluentDynamoDb.SystemTextJson package
/// 2. Create a JsonSerializerContext for your types
/// 3. Use the Serialize/Deserialize methods with your context
/// 
/// Example:
/// <code>
/// [JsonSerializable(typeof(MyType))]
/// internal partial class MyJsonContext : JsonSerializerContext { }
/// 
/// var json = SystemTextJsonSerializer.Serialize(myObject, MyJsonContext.Default.MyType);
/// var obj = SystemTextJsonSerializer.Deserialize&lt;MyType&gt;(json, MyJsonContext.Default.MyType);
/// </code>
/// </remarks>
public static class SystemTextJsonSerializer
{
    /// <summary>
    /// Serializes an object to JSON using the provided JsonSerializerContext.
    /// This method is AOT-compatible and produces no trim warnings.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="value">The object to serialize</param>
    /// <param name="context">The JsonSerializerContext containing type information</param>
    /// <returns>JSON string representation of the object</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    public static string Serialize<T>(T value, JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        return System.Text.Json.JsonSerializer.Serialize(value, typeof(T), context);
    }
    
    /// <summary>
    /// Deserializes JSON to an object using the provided JsonSerializerContext.
    /// This method is AOT-compatible and produces no trim warnings.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="context">The JsonSerializerContext containing type information</param>
    /// <returns>Deserialized object of type T</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or context is null</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when JSON is invalid or cannot be deserialized</exception>
    public static T? Deserialize<T>(string json, JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(context);
        
        return (T?)System.Text.Json.JsonSerializer.Deserialize(json, typeof(T), context);
    }
}
