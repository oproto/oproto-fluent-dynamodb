namespace Oproto.FluentDynamoDb.Logging;

/// <summary>
/// Minimal logging interface for DynamoDB operations.
/// Designed to be lightweight and not require external dependencies.
/// </summary>
public interface IDynamoDbLogger
{
    /// <summary>
    /// Checks if the specified log level is enabled.
    /// Used to avoid expensive parameter evaluation when logging is disabled.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if the log level is enabled; otherwise, false.</returns>
    bool IsEnabled(LogLevel logLevel);
    
    /// <summary>
    /// Logs a trace message (most verbose).
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogTrace(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogDebug(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogInformation(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogWarning(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogError(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an error with exception.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogError(int eventId, Exception exception, string message, params object[] args);
    
    /// <summary>
    /// Logs a critical error.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogCritical(int eventId, Exception exception, string message, params object[] args);
}
