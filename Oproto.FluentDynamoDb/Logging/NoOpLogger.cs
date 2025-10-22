namespace Oproto.FluentDynamoDb.Logging;

/// <summary>
/// No-op logger that discards all log messages.
/// Used as default when no logger is configured.
/// This implementation has zero allocation overhead.
/// </summary>
public sealed class NoOpLogger : IDynamoDbLogger
{
    /// <summary>
    /// Gets the singleton instance of the no-op logger.
    /// </summary>
    public static readonly NoOpLogger Instance = new();
    
    private NoOpLogger() { }
    
    /// <summary>
    /// Always returns false to prevent any log message evaluation.
    /// </summary>
    public bool IsEnabled(LogLevel logLevel) => false;
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogTrace(int eventId, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogDebug(int eventId, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogInformation(int eventId, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogWarning(int eventId, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogError(int eventId, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogError(int eventId, Exception exception, string message, params object[] args) { }
    
    /// <summary>
    /// Does nothing. No-op implementation.
    /// </summary>
    public void LogCritical(int eventId, Exception exception, string message, params object[] args) { }
}
