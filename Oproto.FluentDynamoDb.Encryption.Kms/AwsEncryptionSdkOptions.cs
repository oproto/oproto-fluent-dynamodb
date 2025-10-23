namespace Oproto.FluentDynamoDb.Encryption.Kms;

/// <summary>
/// Configuration options for AWS Encryption SDK field-level encryption.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the <see cref="AwsEncryptionSdkFieldEncryptor"/>,
/// including key resolution, caching policies, and algorithm selection.
/// </para>
/// <para>
/// <strong>Security Best Practices:</strong>
/// </para>
/// <list type="bullet">
/// <item>Load KMS key ARNs from secure configuration (AWS Secrets Manager, Parameter Store, etc.)</item>
/// <item>Never hardcode KMS key ARNs in source code</item>
/// <item>Use key commitment algorithms (default) to prevent key substitution attacks</item>
/// <item>Enable caching to reduce KMS API calls and costs</item>
/// <item>Set appropriate limits for MaxMessagesPerDataKey and MaxBytesPerDataKey</item>
/// <item>Use encryption context for audit trails in CloudTrail</item>
/// </list>
/// </remarks>
/// <example>
/// <strong>Example: Basic configuration</strong>
/// <code>
/// var options = new AwsEncryptionSdkOptions
/// {
///     DefaultKeyId = configuration["Kms:DefaultKeyArn"],
///     EnableCaching = true,
///     DefaultCacheTtlSeconds = 300
/// };
/// </code>
/// </example>
/// <example>
/// <strong>Example: Multi-tenant configuration</strong>
/// <code>
/// var options = new AwsEncryptionSdkOptions
/// {
///     DefaultKeyId = configuration["Kms:DefaultKeyArn"],
///     ContextKeyMap = new Dictionary&lt;string, string&gt;
///     {
///         ["tenant-a"] = configuration["Kms:TenantA:KeyArn"],
///         ["tenant-b"] = configuration["Kms:TenantB:KeyArn"]
///     },
///     EnableCaching = true,
///     DefaultCacheTtlSeconds = 300,
///     MaxMessagesPerDataKey = 100,
///     MaxBytesPerDataKey = 100 * 1024 * 1024
/// };
/// </code>
/// </example>
public sealed class AwsEncryptionSdkOptions
{
    /// <summary>
    /// Gets or sets the default KMS key ARN or alias used when no context is provided
    /// or when the context doesn't match any mapped keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be a valid KMS key ARN (e.g., "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012")
    /// or a KMS key alias (e.g., "alias/my-encryption-key").
    /// </para>
    /// <para>
    /// <strong>Security Note:</strong> Load this value from secure configuration, not hardcoded in source code.
    /// </para>
    /// </remarks>
    public string DefaultKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional mapping of context identifiers to KMS key ARNs or aliases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows different contexts (e.g., tenants, customers, regions) to use different KMS keys
    /// for data isolation and security. The context identifier is passed at runtime via
    /// <c>WithEncryptionContext()</c> or <c>EncryptionContext.Current</c>.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// {
    ///     ["tenant-a"] = "arn:aws:kms:us-east-1:123456789012:key/tenant-a-key-id",
    ///     ["tenant-b"] = "arn:aws:kms:us-east-1:123456789012:key/tenant-b-key-id"
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public Dictionary<string, string>? ContextKeyMap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether data key caching is enabled.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the AWS Encryption SDK's <c>CachingCryptoMaterialsManager</c> is used
    /// to cache data keys, reducing KMS API calls and improving performance.
    /// </para>
    /// <para>
    /// Caching is controlled by <see cref="DefaultCacheTtlSeconds"/>, <see cref="MaxMessagesPerDataKey"/>,
    /// and <see cref="MaxBytesPerDataKey"/>. Data keys are automatically rotated when any limit is reached.
    /// </para>
    /// <para>
    /// Disable caching only if you have specific security requirements that mandate generating
    /// a new data key for every encryption operation.
    /// </para>
    /// </remarks>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache TTL (time-to-live) for data keys in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value can be overridden per-field using the <c>CacheTtlSeconds</c> property
    /// on the <c>EncryptedAttribute</c>.
    /// </para>
    /// <para>
    /// After the TTL expires, a new data key is generated from KMS. This provides a balance
    /// between performance (fewer KMS calls) and security (regular key rotation).
    /// </para>
    /// <para>
    /// Only applies when <see cref="EnableCaching"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    public int DefaultCacheTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of messages that can be encrypted with a single data key.
    /// Default is 100.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an AWS Encryption SDK best practice to limit the amount of data encrypted
    /// with a single data key. When this limit is reached, a new data key is automatically
    /// generated from KMS.
    /// </para>
    /// <para>
    /// Only applies when <see cref="EnableCaching"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    public int MaxMessagesPerDataKey { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of bytes that can be encrypted with a single data key.
    /// Default is 104,857,600 bytes (100 MB).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an AWS Encryption SDK best practice to limit the amount of data encrypted
    /// with a single data key. When this limit is reached, a new data key is automatically
    /// generated from KMS.
    /// </para>
    /// <para>
    /// Only applies when <see cref="EnableCaching"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    public long MaxBytesPerDataKey { get; set; } = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Gets or sets the algorithm suite identifier to use for encryption.
    /// Default is "AES_256_GCM_HKDF_SHA512_COMMIT_KEY_ECDSA_P384".
    /// </summary>
    /// <remarks>
    /// <para>
    /// AWS Encryption SDK 3.x uses key commitment by default to prevent key substitution attacks.
    /// The default algorithm provides:
    /// </para>
    /// <list type="bullet">
    /// <item>AES-256-GCM encryption</item>
    /// <item>HKDF-SHA512 key derivation</item>
    /// <item>Key commitment (prevents key substitution attacks)</item>
    /// <item>ECDSA P-384 signature for non-repudiation</item>
    /// </list>
    /// <para>
    /// Valid values include:
    /// </para>
    /// <list type="bullet">
    /// <item>AES_256_GCM_HKDF_SHA512_COMMIT_KEY_ECDSA_P384 (recommended, default)</item>
    /// <item>AES_256_GCM_HKDF_SHA512_COMMIT_KEY</item>
    /// <item>AES_192_GCM_HKDF_SHA384_ECDSA_P384</item>
    /// </list>
    /// <para>
    /// <strong>Security Note:</strong> Always use algorithms with key commitment (COMMIT_KEY) to prevent
    /// key substitution attacks.
    /// </para>
    /// </remarks>
    public string Algorithm { get; set; } = "AES_256_GCM_HKDF_SHA512_COMMIT_KEY_ECDSA_P384";
}
