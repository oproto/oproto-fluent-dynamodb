namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Provides thread-safe ambient context for encryption operations.
/// Uses AsyncLocal to ensure context flows through async calls without leaking across threads or requests.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a convenient way to set encryption context (e.g., tenant ID, customer ID, region)
/// that automatically flows through async operations without requiring explicit parameter passing.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> AsyncLocal ensures that context values are isolated per async flow.
/// Each async operation has its own logical context that doesn't leak to other concurrent operations
/// or threads. This makes it safe to use in multi-threaded environments like ASP.NET Core.
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <code>
/// // In middleware or request handler
/// EncryptionContext.Current = httpContext.GetTenantId();
/// 
/// // All operations in this async flow use the context
/// await customerTable.PutItem(data).ExecuteAsync();
/// await customerTable.GetItem("key").ExecuteAsync();
/// 
/// // Context automatically cleared when request completes
/// </code>
/// </para>
/// <para>
/// <strong>Alternative:</strong> For more explicit control, use WithEncryptionContext() on request builders
/// to set context per operation, which overrides the ambient context.
/// </para>
/// </remarks>
public static class EncryptionContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// Gets or sets the current encryption context identifier.
    /// This is typically a tenant ID, customer ID, region, or other identifier
    /// that the IKmsKeyResolver uses to determine the appropriate encryption key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The context identifier is NOT the encryption key itself - it's a logical identifier
    /// passed to IKmsKeyResolver which returns the appropriate KMS key ARN from configuration,
    /// database, or external service.
    /// </para>
    /// <para>
    /// This value is stored in AsyncLocal, which means:
    /// <list type="bullet">
    /// <item>It flows through async/await calls within the same logical operation</item>
    /// <item>It does NOT leak across different async operations or threads</item>
    /// <item>Each HTTP request in ASP.NET Core has its own isolated context</item>
    /// <item>Setting this value in one async flow does not affect other concurrent flows</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the effective encryption context, checking both operation-specific and ambient contexts.
    /// Operation-specific context (set via WithEncryptionContext) takes precedence over ambient context.
    /// </summary>
    /// <returns>The effective encryption context identifier, or null if neither is set.</returns>
    internal static string? GetEffectiveContext()
    {
        // Check for operation-specific context first (set via WithEncryptionContext extension)
        var operationContext = Requests.Extensions.EncryptionExtensions.GetOperationContext();
        if (operationContext != null)
        {
            return operationContext;
        }

        // Fall back to ambient context
        return Current;
    }
}
