using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Oproto.FluentDynamoDb.NewtonsoftJson;

/// <summary>
/// Provides JSON serialization using Newtonsoft.Json.
/// </summary>
/// <remarks>
/// <para>
/// <strong>AOT Compatibility Notice:</strong> This serializer uses runtime reflection and has limited 
/// Native AOT compatibility. For full AOT support with no trim warnings, use 
/// <c>Oproto.FluentDynamoDb.SystemTextJson</c> instead.
/// </para>
/// <para>
/// This package is provided for compatibility with existing codebases that use Newtonsoft.Json.
/// The serializer settings are configured to minimize reflection usage where possible:
/// </para>
/// <list type="bullet">
/// <item><description>TypeNameHandling is disabled to avoid reflection-based type resolution</description></item>
/// <item><description>MetadataPropertyHandling is set to Ignore to avoid metadata processing</description></item>
/// <item><description>Uses DefaultContractResolver for standard serialization behavior</description></item>
/// </list>
/// <para>
/// Example usage:
/// <code>
/// var json = NewtonsoftJsonSerializer.Serialize(myObject);
/// var obj = NewtonsoftJsonSerializer.Deserialize&lt;MyType&gt;(json);
/// </code>
/// </para>
/// </remarks>
public static class NewtonsoftJsonSerializer
{
    /// <summary>
    /// JSON serializer settings configured for AOT-safe operation.
    /// These settings minimize reflection usage but do not eliminate it entirely.
    /// </summary>
    private static readonly JsonSerializerSettings Settings = new()
    {
        // Avoid reflection-based type handling
        TypeNameHandling = TypeNameHandling.None,
        
        // Use standard contract resolver
        ContractResolver = new DefaultContractResolver(),
        
        // Ignore metadata properties to avoid additional reflection
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        
        // Handle null values consistently
        NullValueHandling = NullValueHandling.Ignore,
        
        // Use ISO date format for consistency
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        
        // Preserve reference handling disabled to avoid metadata
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
    
    /// <summary>
    /// Serializes an object to JSON using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="value">The object to serialize</param>
    /// <returns>JSON string representation of the object</returns>
    /// <remarks>
    /// This method uses runtime reflection and may not be fully compatible with Native AOT compilation.
    /// Consider using <c>Oproto.FluentDynamoDb.SystemTextJson</c> for full AOT support.
    /// </remarks>
    public static string Serialize<T>(T value)
    {
        // Uses runtime reflection - not ideal for AOT but works
        return JsonConvert.SerializeObject(value, Settings);
    }
    
    /// <summary>
    /// Deserializes JSON to an object using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Deserialized object of type T</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized</exception>
    /// <remarks>
    /// This method uses runtime reflection and may not be fully compatible with Native AOT compilation.
    /// Consider using <c>Oproto.FluentDynamoDb.SystemTextJson</c> for full AOT support.
    /// </remarks>
    public static T? Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        
        // Uses runtime reflection - not ideal for AOT but works
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
