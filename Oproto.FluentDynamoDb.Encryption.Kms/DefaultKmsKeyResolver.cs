namespace Oproto.FluentDynamoDb.Encryption.Kms;

/// <summary>
/// Default implementation of <see cref="IKmsKeyResolver"/> that uses a dictionary lookup
/// with fallback to a default key.
/// </summary>
/// <remarks>
/// <para>
/// This resolver is suitable for scenarios where you have a fixed set of context-to-key mappings
/// that can be configured at application startup. For dynamic key resolution (e.g., loading from
/// a database), implement a custom <see cref="IKmsKeyResolver"/>.
/// </para>
/// <para>
/// The resolver performs a case-sensitive dictionary lookup on the context identifier.
/// If the context is null or not found in the map, it returns the default key.
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Multi-tenant configuration</strong>
/// <code>
/// var contextKeyMap = new Dictionary&lt;string, string&gt;
/// {
///     ["tenant-a"] = "arn:aws:kms:us-east-1:123456789012:key/tenant-a-key-id",
///     ["tenant-b"] = "arn:aws:kms:us-east-1:123456789012:key/tenant-b-key-id",
///     ["tenant-c"] = "arn:aws:kms:us-west-2:123456789012:key/tenant-c-key-id"
/// };
/// 
/// var resolver = new DefaultKmsKeyResolver(
///     defaultKeyId: "arn:aws:kms:us-east-1:123456789012:key/default-key-id",
///     contextKeyMap: contextKeyMap
/// );
/// 
/// // Returns tenant-a's key
/// var keyA = resolver.ResolveKeyId("tenant-a");
/// 
/// // Returns default key (context not in map)
/// var keyDefault = resolver.ResolveKeyId("unknown-tenant");
/// 
/// // Returns default key (null context)
/// var keyNull = resolver.ResolveKeyId(null);
/// </code>
/// </example>
public sealed class DefaultKmsKeyResolver : IKmsKeyResolver
{
    private readonly string _defaultKeyId;
    private readonly IReadOnlyDictionary<string, string>? _contextKeyMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultKmsKeyResolver"/> class.
    /// </summary>
    /// <param name="defaultKeyId">
    /// The default KMS key ARN or alias to use when no context is provided or when the context
    /// is not found in the context key map. Must not be null or empty.
    /// </param>
    /// <param name="contextKeyMap">
    /// Optional dictionary mapping context identifiers to KMS key ARNs or aliases.
    /// If null, all contexts will resolve to the default key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="defaultKeyId"/> is null or empty.
    /// </exception>
    public DefaultKmsKeyResolver(
        string defaultKeyId,
        IReadOnlyDictionary<string, string>? contextKeyMap = null)
    {
        if (string.IsNullOrWhiteSpace(defaultKeyId))
            throw new ArgumentException("Default key ID cannot be null or empty.", nameof(defaultKeyId));

        _defaultKeyId = defaultKeyId;
        _contextKeyMap = contextKeyMap;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This implementation performs a case-sensitive lookup in the context key map.
    /// If the context identifier is found, the corresponding KMS key is returned.
    /// Otherwise, the default key is returned.
    /// </para>
    /// <para>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// </para>
    /// </remarks>
    public string ResolveKeyId(string? contextId)
    {
        if (contextId != null && _contextKeyMap?.TryGetValue(contextId, out var keyId) == true)
        {
            return keyId;
        }

        return _defaultKeyId;
    }
}
