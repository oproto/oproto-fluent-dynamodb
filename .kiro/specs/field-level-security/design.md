# Design Document

## Overview

This design implements field-level security through two complementary mechanisms: sensitive field redaction in logging and optional KMS-based encryption. The design maintains the library's AOT compatibility and follows the existing pattern of separating optional features into dedicated assemblies.

## Architecture

### Component Structure

```
Oproto.FluentDynamoDb.Attributes
├── SensitiveAttribute (new)
└── EncryptedAttribute (new)

Oproto.FluentDynamoDb
├── Logging/
│   └── SensitiveDataRedactor (new)
└── Storage/
    └── IFieldEncryptor (new interface)

Oproto.FluentDynamoDb.SourceGenerator
├── Analysis/
│   └── SecurityAttributeAnalyzer (new)
└── Generators/
    └── SecurityCodeGenerator (new)

Oproto.FluentDynamoDb.Encryption.Kms (new assembly)
├── AwsEncryptionSdkFieldEncryptor
├── IKmsKeyResolver
├── IBlobStorage
├── S3BlobStorage
├── FieldEncryptionException
└── AwsEncryptionSdkOptions
```

### Assembly Dependencies

- **Core Library**: No new external dependencies
- **Encryption.Kms Assembly**: 
  - AWS.EncryptionSDK (3.0.0+)
  - AWSSDK.S3 (3.7.0+) - for external blob storage
  - Oproto.FluentDynamoDb (reference)

## Encryption Context Flow

### Runtime Context Resolution

The encryption context (e.g., tenant ID, customer ID, region) is passed at runtime:

```csharp
// Option 1: Pass per operation (recommended - most explicit)
await userTable.PutItem(user)
    .WithEncryptionContext("tenant-123")
    .ExecuteAsync();

// Option 2: Use ambient context (for middleware scenarios)
// AsyncLocal ensures thread-safety and async-flow isolation
EncryptionContext.Current = "tenant-123";
await userTable.PutItem(user).ExecuteAsync();
// Context automatically flows through async calls
// Does NOT leak across requests/threads
```

### Key Resolution Flow

1. **Operation Context** - passed via WithEncryptionContext() or EncryptionContext.Current
2. **IKmsKeyResolver** - resolves context string to KMS key ARN at runtime
3. **DefaultKeyId** - fallback if no context provided

The context string (e.g., "tenant-123") is NOT the encryption key - it's an identifier passed to IKmsKeyResolver which returns the appropriate KMS key ARN from configuration, database, or external service.

## Components and Interfaces

### 1. Attributes (Oproto.FluentDynamoDb.Attributes)

#### SensitiveAttribute
```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute
{
    // Marker attribute - no properties needed
}
```

#### EncryptedAttribute
```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EncryptedAttribute : Attribute
{
    // Cache TTL for data keys (default 5 minutes)
    public int CacheTtlSeconds { get; set; } = 300;
}
```

### 2. Core Library Components

#### DynamoDbTableBase Extensions
```csharp
public abstract class DynamoDbTableBase<TEntity>
{
    protected IFieldEncryptor? FieldEncryptor { get; }
    
    // Gets context from operation or ambient context
    protected string? GetEncryptionContext()
    {
        return EncryptionContext.Current;
    }
}

// Extension methods for request builders
public static class EncryptionExtensions
{
    public static TBuilder WithEncryptionContext<TBuilder>(
        this TBuilder builder, 
        string context)
        where TBuilder : IRequestBuilder
    {
        // Sets encryption context for this specific operation
        // Overrides ambient context if set
    }
}
```

#### EncryptionContext (Ambient Context)
```csharp
/// <summary>
/// Thread-safe ambient context for encryption operations.
/// Uses AsyncLocal to ensure context flows through async calls
/// without leaking across threads or requests.
/// </summary>
public static class EncryptionContext
{
    private static readonly AsyncLocal<string?> _current = new();
    
    /// <summary>
    /// Gets or sets the current encryption context identifier.
    /// This is typically a tenant ID, customer ID, or other
    /// identifier that IKmsKeyResolver uses to determine the KMS key.
    /// </summary>
    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

### 3. Logging Components

#### SensitiveDataRedactor
```csharp
namespace Oproto.FluentDynamoDb.Logging;

internal static class SensitiveDataRedactor
{
    private const string RedactedPlaceholder = "[REDACTED]";
    
    public static Dictionary<string, AttributeValue> RedactSensitiveFields(
        Dictionary<string, AttributeValue> item,
        HashSet<string> sensitiveFieldNames)
    {
        // Create shallow copy and replace sensitive values
    }
    
    public static string RedactSensitiveValue(string fieldName, HashSet<string> sensitiveFields)
    {
        // Return placeholder if field is sensitive
    }
}
```

#### IFieldEncryptor Interface
```csharp
namespace Oproto.FluentDynamoDb.Storage;

public interface IFieldEncryptor
{
    Task<byte[]> EncryptAsync(
        byte[] plaintext,
        string fieldName,
        FieldEncryptionContext context,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> DecryptAsync(
        byte[] ciphertext,
        string fieldName,
        FieldEncryptionContext context,
        CancellationToken cancellationToken = default);
}

public class FieldEncryptionContext
{
    // Runtime context identifier (e.g., tenant ID, customer ID)
    // Passed to IKmsKeyResolver to determine KMS key
    public string? ContextId { get; init; }
    
    // Cache TTL from attribute
    public int CacheTtlSeconds { get; init; } = 300;
    
    // Entity identifier for external blob storage path
    public string? EntityId { get; init; }
}
```

### 4. Source Generator Enhancements

#### SecurityAttributeAnalyzer
```csharp
internal class SecurityAttributeAnalyzer
{
    public SecurityInfo AnalyzeProperty(IPropertySymbol property)
    {
        return new SecurityInfo
        {
            IsSensitive = HasAttribute(property, "SensitiveAttribute"),
            IsEncrypted = HasAttribute(property, "EncryptedAttribute"),
            EncryptionConfig = ExtractEncryptionConfig(property)
        };
    }
}

internal record SecurityInfo
{
    public bool IsSensitive { get; init; }
    public bool IsEncrypted { get; init; }
    public EncryptionConfig? EncryptionConfig { get; init; }
}

internal record EncryptionConfig
{
    public string? KeyId { get; init; }
    public int CacheTtlSeconds { get; init; }
}
```

#### SecurityCodeGenerator
Generates:
1. **Metadata**: Static HashSet of sensitive field names
2. **ToItem Encryption**: Calls to IFieldEncryptor.EncryptAsync before setting AttributeValue
3. **FromItem Decryption**: Calls to IFieldEncryptor.DecryptAsync after reading AttributeValue
4. **Logging Integration**: Passes sensitive field set to logging methods

### 5. Encryption Assembly (Oproto.FluentDynamoDb.Encryption.Kms)

#### AwsEncryptionSdkFieldEncryptor
```csharp
public class AwsEncryptionSdkFieldEncryptor : IFieldEncryptor
{
    private readonly ESDK _encryptionSdk;
    private readonly IKmsKeyResolver _keyResolver;
    private readonly IBlobStorage? _blobStorage;
    private readonly CachingCryptoMaterialsManager? _cachingCmm;
    private readonly ILogger? _logger;
    
    public AwsEncryptionSdkFieldEncryptor(
        IKmsKeyResolver keyResolver,
        AwsEncryptionSdkOptions? options = null,
        IBlobStorage? blobStorage = null,
        ILogger? logger = null)
    {
        _keyResolver = keyResolver;
        _blobStorage = blobStorage;
        _logger = logger;
        _encryptionSdk = AwsEncryptionSdkFactory.CreateDefaultAwsEncryptionSdk();
        
        // Setup caching if enabled
        if (options?.EnableCaching == true)
        {
            _cachingCmm = CreateCachingCmm(options);
        }
    }
    
    public async Task<byte[]> EncryptAsync(
        byte[] plaintext,
        string fieldName,
        FieldEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve KMS key ARN via _keyResolver.ResolveKeyId(context.ContextId)
        // 2. Create KMS keyring with resolved key
        // 3. Build encryption context with field name and context ID
        // 4. Encrypt using AWS Encryption SDK
        // 5. If IsExternalBlob:
        //    - Upload encrypted data to blob storage
        //    - Return blob reference (e.g., S3 URI) as bytes
        // 6. Else: Return encrypted message (AWS Encryption SDK format)
    }
    
    public async Task<byte[]> DecryptAsync(
        byte[] ciphertext,
        string fieldName,
        FieldEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. If IsExternalBlob:
        //    - Parse blob reference from ciphertext
        //    - Download encrypted data from blob storage
        //    - Use downloaded data as ciphertext
        // 2. Resolve KMS key ARN (for keyring)
        // 3. Create KMS keyring
        // 4. Decrypt using AWS Encryption SDK
        // 5. Validate encryption context matches expected values
        // 6. Return plaintext
    }
}
```

#### IBlobStorage Interface
```csharp
public interface IBlobStorage
{
    /// <summary>
    /// Uploads encrypted data and returns a reference (e.g., S3 URI).
    /// </summary>
    Task<string> UploadAsync(
        byte[] encryptedData,
        string fieldName,
        string? contextId,
        string? entityId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads encrypted data using a reference.
    /// </summary>
    Task<byte[]> DownloadAsync(
        string blobReference,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a blob (for cleanup).
    /// </summary>
    Task DeleteAsync(
        string blobReference,
        CancellationToken cancellationToken = default);
}
```

#### S3BlobStorage
```csharp
public class S3BlobStorage : IBlobStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string? _keyPrefix;
    
    public S3BlobStorage(
        IAmazonS3 s3Client,
        string bucketName,
        string? keyPrefix = null)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _keyPrefix = keyPrefix;
    }
    
    public async Task<string> UploadAsync(
        byte[] encryptedData,
        string fieldName,
        string? contextId,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        // Build S3 key: {prefix}/{contextId}/{entityId}/{fieldName}/{guid}
        // Upload to S3
        // Return S3 URI: s3://{bucket}/{key}
    }
    
    public async Task<byte[]> DownloadAsync(
        string blobReference,
        CancellationToken cancellationToken = default)
    {
        // Parse S3 URI
        // Download from S3
        // Return bytes
    }
    
    public async Task DeleteAsync(
        string blobReference,
        CancellationToken cancellationToken = default)
    {
        // Parse S3 URI
        // Delete from S3
    }
}
```

#### IKmsKeyResolver
```csharp
/// <summary>
/// Resolves encryption context to KMS key ARN.
/// Implement this to provide custom key resolution logic.
/// </summary>
public interface IKmsKeyResolver
{
    /// <summary>
    /// Resolves a context identifier to a KMS key ARN.
    /// </summary>
    /// <param name="contextId">Context identifier (e.g., tenant ID, customer ID, region)</param>
    /// <returns>KMS key ARN or alias</returns>
    string ResolveKeyId(string? contextId);
}

/// <summary>
/// Default implementation using a dictionary lookup with fallback.
/// </summary>
public class DefaultKmsKeyResolver : IKmsKeyResolver
{
    private readonly string _defaultKeyId;
    private readonly IReadOnlyDictionary<string, string>? _contextKeyMap;
    
    public DefaultKmsKeyResolver(
        string defaultKeyId,
        IReadOnlyDictionary<string, string>? contextKeyMap = null)
    {
        _defaultKeyId = defaultKeyId;
        _contextKeyMap = contextKeyMap;
    }
    
    public string ResolveKeyId(string? contextId)
    {
        if (contextId != null && _contextKeyMap?.TryGetValue(contextId, out var keyId) == true)
            return keyId;
        return _defaultKeyId;
    }
}
```

#### FieldEncryptionException
```csharp
public class FieldEncryptionException : Exception
{
    public string FieldName { get; }
    public string? ContextId { get; }
    public string? KeyId { get; }
    
    public FieldEncryptionException(
        string message,
        string fieldName,
        string? contextId = null,
        string? keyId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FieldName = fieldName;
        ContextId = contextId;
        KeyId = keyId;
    }
}
```

#### AwsEncryptionSdkOptions
```csharp
public class AwsEncryptionSdkOptions
{
    /// <summary>
    /// Default KMS key ARN used when no context is provided
    /// or context doesn't match any mapped keys.
    /// </summary>
    public string DefaultKeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional mapping of context identifiers to KMS key ARNs.
    /// Example: { "tenant-a": "arn:aws:kms:...", "tenant-b": "arn:aws:kms:..." }
    /// </summary>
    public Dictionary<string, string>? ContextKeyMap { get; set; }
    
    /// <summary>
    /// Enable data key caching (default: true).
    /// Uses AWS Encryption SDK's CachingCryptoMaterialsManager.
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Default cache TTL for data keys (seconds).
    /// Can be overridden per-field via EncryptedAttribute.
    /// </summary>
    public int DefaultCacheTtlSeconds { get; set; } = 300;
    
    /// <summary>
    /// Maximum number of messages encrypted with a single data key.
    /// AWS Encryption SDK best practice: limit reuse.
    /// </summary>
    public int MaxMessagesPerDataKey { get; set; } = 100;
    
    /// <summary>
    /// Maximum bytes encrypted with a single data key.
    /// </summary>
    public long MaxBytesPerDataKey { get; set; } = 100 * 1024 * 1024; // 100 MB
    
    /// <summary>
    /// Algorithm suite to use (default: AES_256_GCM_HKDF_SHA512_COMMIT_KEY_ECDSA_P384).
    /// AWS Encryption SDK 3.x uses key commitment by default.
    /// </summary>
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.AES_256_GCM_HKDF_SHA512_COMMIT_KEY_ECDSA_P384;
    
    /// <summary>
    /// S3 bucket name for external blob storage.
    /// Required if using IsExternalBlob = true on any fields.
    /// </summary>
    public string? ExternalBlobBucket { get; set; }
    
    /// <summary>
    /// Optional S3 key prefix for external blobs.
    /// Example: "encrypted-fields/" results in keys like "encrypted-fields/tenant-a/entity-123/FieldName/guid"
    /// </summary>
    public string? ExternalBlobKeyPrefix { get; set; }
    
    /// <summary>
    /// Threshold size (bytes) above which fields are automatically stored as external blobs.
    /// Default: 350KB (DynamoDB item size limit is 400KB).
    /// Set to null to disable automatic external storage.
    /// </summary>
    public int? AutoExternalBlobThreshold { get; set; } = 350 * 1024;
}
```

## Data Models

### Generated Metadata Structure

```csharp
// Generated in each entity's metadata class
public static class UserEntityMetadata
{
    private static readonly HashSet<string> SensitiveFields = new()
    {
        "Email",
        "PhoneNumber",
        "SocialSecurityNumber"
    };
    
    public static bool IsSensitiveField(string fieldName) 
        => SensitiveFields.Contains(fieldName);
}
```

### AWS Encryption SDK Message Format

The AWS Encryption SDK uses a standardized message format:

```
[Version (1 byte)]
[Type (1 byte)]
[Algorithm Suite ID (2 bytes)]
[Message ID (16 bytes)]
[AAD Length (2 bytes)]
[AAD (variable)]
[Encrypted Data Key Count (2 bytes)]
[Encrypted Data Keys (variable)]
[Content Type (1 byte)]
[Frame Length (4 bytes)]
[IV Length (1 byte)]
[Frame Count (4 bytes)]
[Encrypted Content (variable)]
[Signature (variable, algorithm-dependent)]
```

**Benefits:**
- Standardized format recognized by AWS services
- Built-in algorithm agility
- Key commitment (prevents key substitution attacks)
- Encryption context for additional authenticated data
- Signature for non-repudiation

### Encryption Context

Each encrypted field includes encryption context:
```csharp
{
    "field": "SensitiveData",
    "context": "tenant-123",  // Optional
    "entity": "CustomerData"   // Optional
}
```

This provides additional security and auditability through CloudTrail.

## Error Handling

### Logging Redaction Errors
- No exceptions thrown - redaction is best-effort
- Missing metadata defaults to no redaction
- Logs warning if redaction fails

### Encryption Errors

1. **KMS Access Denied**
   - Throw FieldEncryptionException with key ARN and context ID
   - Log error with full context (but not the key itself)
   - Include IAM policy guidance in exception message

2. **Data Key Generation Failure**
   - Throw FieldEncryptionException with KMS error details
   - AWS Encryption SDK handles retries internally
   - Log encryption failures

3. **Decryption Failure**
   - Throw FieldEncryptionException indicating corruption or wrong key
   - Include field name and entity context
   - AWS Encryption SDK validates message integrity automatically
   - Do not retry (data corruption/wrong key is not transient)

4. **Invalid Configuration**
   - Throw ArgumentException during setup
   - Validate key IDs are valid ARNs or aliases
   - Validate cache TTL is non-negative

## Testing Strategy

### Unit Tests

1. **SensitiveAttribute Tests**
   - Verify attribute can be applied to properties
   - Test attribute reflection

2. **EncryptedAttribute Tests**
   - Verify default cache TTL
   - Test custom CacheTtlSeconds

3. **SensitiveDataRedactor Tests**
   - Test redaction of single field
   - Test redaction of multiple fields
   - Test non-sensitive fields pass through
   - Test empty/null items

4. **Source Generator Tests**
   - Verify sensitive field metadata generation
   - Verify encryption code generation with context flow
   - Verify diagnostic for missing Encryption.Kms reference
   - Test combined Sensitive + Encrypted attributes

5. **AwsEncryptionSdkFieldEncryptor Tests**
   - Mock AWS Encryption SDK for encrypt/decrypt operations
   - Test encryption context inclusion
   - Test error handling for each failure mode
   - Test caching behavior with CachingCryptoMaterialsManager
   - Test algorithm suite configuration

6. **IKmsKeyResolver Tests**
   - Test default key resolution
   - Test context-specific key resolution
   - Test fallback to default when context not found

### Integration Tests

1. **End-to-End Encryption**
   - Create entity with encrypted fields
   - Put to DynamoDB
   - Verify encrypted format in DynamoDB
   - Get from DynamoDB
   - Verify decrypted values match

2. **Multi-Context Encryption**
   - Encrypt data for context A
   - Encrypt data for context B
   - Verify different keys used
   - Verify cross-context decryption fails

3. **Logging Redaction**
   - Enable logging
   - Perform operations on entities with sensitive fields
   - Verify log output contains [REDACTED]
   - Verify non-sensitive fields are logged

4. **Combined Security Features**
   - Entity with both Sensitive and Encrypted fields
   - Verify encryption happens
   - Verify logging redaction happens
   - Verify decryption works

## Usage Examples

### Example 1: Sensitive Fields Only (No Encryption)
```csharp
[DynamoDbEntity]
public class User
{
    [PartitionKey]
    public string UserId { get; set; }
    
    public string Name { get; set; }
    
    [Sensitive]  // Redacted from logs
    public string Email { get; set; }
    
    [Sensitive]  // Redacted from logs
    public string PhoneNumber { get; set; }
}

// Logs will show: { UserId: "123", Name: "John", Email: "[REDACTED]", PhoneNumber: "[REDACTED]" }
```

### Example 2: Multi-Context with Runtime Key Resolution
```csharp
[DynamoDbEntity]
public class CustomerData
{
    [PartitionKey]
    public string CustomerId { get; set; }
    
    [Encrypted]  // Uses IKmsKeyResolver with context
    [Sensitive]
    public string SensitiveData { get; set; }
}

// Setup - keys loaded from secure configuration (not hardcoded!)
var keyResolver = new DefaultKmsKeyResolver(
    defaultKeyId: configuration["Kms:DefaultKeyArn"],
    contextKeyMap: new Dictionary<string, string>
    {
        ["tenant-a"] = configuration["Kms:TenantA:KeyArn"],
        ["tenant-b"] = configuration["Kms:TenantB:KeyArn"]
    });

var options = new AwsEncryptionSdkOptions
{
    DefaultKeyId = configuration["Kms:DefaultKeyArn"],
    EnableCaching = true,
    DefaultCacheTtlSeconds = 300,
    MaxMessagesPerDataKey = 100
};

var encryptor = new AwsEncryptionSdkFieldEncryptor(keyResolver, options);
var table = new CustomerDataTable(dynamoClient, encryptor);

// Usage - context passed at runtime (NOT the encryption key itself)
await table.PutItem(customerData)
    .WithEncryptionContext("tenant-a")  // Resolver maps to tenant-a's KMS key
    .ExecuteAsync();

// Data is encrypted using AWS Encryption SDK format
// Encryption context includes: { "field": "SensitiveData", "context": "tenant-a" }
```

### Example 3: Ambient Context (Middleware Pattern)
```csharp
// In middleware or request handler
// AsyncLocal ensures thread-safety and prevents cross-request leakage
EncryptionContext.Current = httpContext.GetTenantId();

// All operations in this async flow use the context
await customerTable.PutItem(data).ExecuteAsync();
await customerTable.GetItem("key").ExecuteAsync();

// Context automatically cleared when request completes
```

### Example 4: External Blob Storage for Large Fields
```csharp
[DynamoDbEntity]
public class Document
{
    [PartitionKey]
    public string DocumentId { get; set; }
    
    public string Title { get; set; }
    
    [Encrypted(IsExternalBlob = true)]  // Stored in S3, not DynamoDB
    [Sensitive]
    public byte[] LargeEncryptedContent { get; set; }
}

// Setup with S3 blob storage
var s3Client = new AmazonS3Client();
var blobStorage = new S3BlobStorage(
    s3Client,
    bucketName: "my-encrypted-blobs",
    keyPrefix: "documents/");

var options = new AwsEncryptionSdkOptions
{
    DefaultKeyId = configuration["Kms:DefaultKeyArn"],
    ExternalBlobBucket = "my-encrypted-blobs",
    ExternalBlobKeyPrefix = "documents/",
    AutoExternalBlobThreshold = 350 * 1024  // Auto-externalize if > 350KB
};

var encryptor = new AwsEncryptionSdkFieldEncryptor(keyResolver, options, blobStorage);

// Usage - large content automatically stored in S3
await documentTable.PutItem(document).ExecuteAsync();
// DynamoDB stores: s3://my-encrypted-blobs/documents/tenant-a/doc-123/LargeEncryptedContent/guid
// S3 stores: Encrypted content using AWS Encryption SDK format
```

### Example 5: Custom Key Resolver
```csharp
// Load keys from database, external service, etc.
public class DatabaseKmsKeyResolver : IKmsKeyResolver
{
    private readonly IKeyRepository _keyRepo;
    
    public string ResolveKeyId(string? contextId)
    {
        if (contextId == null)
            return _defaultKey;
            
        // Load from database, cache, etc.
        return _keyRepo.GetKmsKeyForTenant(contextId);
    }
}
```

## Design Decisions

### 1. Separate Assembly for Encryption
**Rationale**: Avoids forcing KMS SDK dependency on all users. Many applications only need logging redaction.

### 2. AWS Encryption SDK
**Rationale**: Industry-standard encryption library. Provides algorithm agility, key commitment, and interoperability with other AWS services. Battle-tested security implementation.

### 3. No Hardcoded Keys in Attributes
**Rationale**: Security best practice. Keys should come from secure configuration, not source code. Prevents accidental key exposure in version control.

### 4. AsyncLocal for Ambient Context
**Rationale**: Thread-safe and async-flow-safe. Automatically prevents context leakage across requests/threads. Standard pattern for ambient context in .NET.

### 5. Generic Context Naming
**Rationale**: Not all use cases are multi-tenant. Context could be customer ID, region, environment, etc. Generic naming supports all scenarios.

### 6. Built-in Caching via CachingCryptoMaterialsManager
**Rationale**: AWS Encryption SDK provides production-ready caching with configurable limits. Minimizes KMS API calls while enforcing best practices (max messages/bytes per key).

### 7. Interface-Based Encryption
**Rationale**: Allows custom encryption implementations. Supports testing with mocks.

### 8. Source Generator Integration
**Rationale**: Zero runtime overhead. Compile-time validation. No reflection needed.

### 9. Binary Storage Format
**Rationale**: Encrypted data is binary. Using B attribute type is most efficient.

### 10. Field-Level Granularity
**Rationale**: Different fields have different sensitivity levels. Allows fine-grained control.

### 11. Encryption Context for Auditability
**Rationale**: AWS Encryption SDK's encryption context appears in CloudTrail logs, providing audit trail of what was encrypted and for which context/tenant.

## Migration and Compatibility

### Backward Compatibility
- New attributes are opt-in
- No breaking changes to existing APIs
- Existing entities continue to work without modification

### Adding Security to Existing Entities
1. Add SensitiveAttribute to properties
2. Rebuild to regenerate source
3. Optionally add Encryption.Kms package
4. Optionally add EncryptedAttribute to properties

### Data Migration for Encryption
- Encrypted fields are incompatible with existing plaintext data
- Migration strategy:
  1. Add new encrypted property
  2. Dual-write to both properties
  3. Backfill encrypted values
  4. Switch reads to encrypted property
  5. Remove old property

## Performance Considerations

### Logging Redaction
- O(1) HashSet lookup per field
- Minimal overhead (~1-2 microseconds per field)
- No allocations for non-sensitive fields

### Encryption
- AWS Encryption SDK caching reduces KMS calls significantly
- Default: max 100 messages or 100MB per data key
- Message overhead: ~200-300 bytes (includes headers, signature)
- AES-256-GCM with key commitment: ~1-5 microseconds per KB
- Thread-safe caching via CachingCryptoMaterialsManager

### Recommended Practices
- Use encryption only for truly sensitive fields
- Configure cache limits based on security requirements
- Consider field size when encrypting (large fields increase latency)
- Monitor KMS API usage and costs via CloudWatch
- Use encryption context for audit trails in CloudTrail
- Leverage AWS Encryption SDK's algorithm agility for future-proofing
