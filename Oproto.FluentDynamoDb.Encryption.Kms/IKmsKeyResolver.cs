namespace Oproto.FluentDynamoDb.Encryption.Kms;

/// <summary>
/// Resolves encryption context identifiers to AWS KMS key ARNs or aliases.
/// Implement this interface to provide custom key resolution logic based on your application's requirements.
/// </summary>
/// <remarks>
/// <para>
/// The context identifier is a runtime value (e.g., tenant ID, customer ID, region) that determines
/// which KMS key should be used for encryption operations. This allows different data contexts to use
/// different encryption keys for isolation and security.
/// </para>
/// <para>
/// <strong>Usage Examples:</strong>
/// </para>
/// <example>
/// <strong>Example 1: Simple default key resolver</strong>
/// <code>
/// public class SimpleKeyResolver : IKmsKeyResolver
/// {
///     private readonly string _keyArn;
///     
///     public SimpleKeyResolver(string keyArn)
///     {
///         _keyArn = keyArn;
///     }
///     
///     public string ResolveKeyId(string? contextId)
///     {
///         return _keyArn;
///     }
/// }
/// </code>
/// </example>
/// <example>
/// <strong>Example 2: Multi-tenant key resolver</strong>
/// <code>
/// public class TenantKeyResolver : IKmsKeyResolver
/// {
///     private readonly IKeyRepository _keyRepo;
///     private readonly string _defaultKey;
///     
///     public TenantKeyResolver(IKeyRepository keyRepo, string defaultKey)
///     {
///         _keyRepo = keyRepo;
///         _defaultKey = defaultKey;
///     }
///     
///     public string ResolveKeyId(string? contextId)
///     {
///         if (contextId == null)
///             return _defaultKey;
///             
///         // Load tenant-specific key from database or configuration
///         return _keyRepo.GetKmsKeyForTenant(contextId) ?? _defaultKey;
///     }
/// }
/// </code>
/// </example>
/// <example>
/// <strong>Example 3: Region-based key resolver</strong>
/// <code>
/// public class RegionKeyResolver : IKmsKeyResolver
/// {
///     private readonly Dictionary&lt;string, string&gt; _regionKeys;
///     private readonly string _defaultKey;
///     
///     public RegionKeyResolver(Dictionary&lt;string, string&gt; regionKeys, string defaultKey)
///     {
///         _regionKeys = regionKeys;
///         _defaultKey = defaultKey;
///     }
///     
///     public string ResolveKeyId(string? contextId)
///     {
///         if (contextId != null &amp;&amp; _regionKeys.TryGetValue(contextId, out var keyArn))
///             return keyArn;
///         return _defaultKey;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IKmsKeyResolver
{
    /// <summary>
    /// Resolves a context identifier to an AWS KMS key ARN or alias.
    /// </summary>
    /// <param name="contextId">
    /// Optional context identifier (e.g., tenant ID, customer ID, region) that determines
    /// which KMS key to use. If null, should return a default key.
    /// </param>
    /// <returns>
    /// AWS KMS key ARN (e.g., "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012")
    /// or KMS key alias (e.g., "alias/my-encryption-key").
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called during encryption and decryption operations to determine which KMS key
    /// to use. The implementation should be thread-safe as it may be called concurrently.
    /// </para>
    /// <para>
    /// The returned key ARN or alias must have appropriate permissions for the calling principal
    /// to perform kms:GenerateDataKey (for encryption) and kms:Decrypt (for decryption) operations.
    /// </para>
    /// </remarks>
    string ResolveKeyId(string? contextId);
}
