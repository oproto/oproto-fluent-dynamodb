using Microsoft.Extensions.Logging;
using Oproto.FluentDynamoDb.Logging;

namespace Oproto.FluentDynamoDb.Logging.Extensions;

/// <summary>
/// Extension methods for easy creation of DynamoDB logger adapters from Microsoft.Extensions.Logging types.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Converts an ILogger to an IDynamoDbLogger using the adapter.
    /// </summary>
    /// <param name="logger">The Microsoft.Extensions.Logging.ILogger instance.</param>
    /// <returns>An IDynamoDbLogger that wraps the provided ILogger.</returns>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public static IDynamoDbLogger ToDynamoDbLogger(this ILogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return new MicrosoftExtensionsLoggingAdapter(logger);
    }

    /// <summary>
    /// Creates an IDynamoDbLogger from an ILoggerFactory with the specified category name.
    /// </summary>
    /// <param name="loggerFactory">The Microsoft.Extensions.Logging.ILoggerFactory instance.</param>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>An IDynamoDbLogger that wraps a logger created from the factory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory or categoryName is null.</exception>
    public static IDynamoDbLogger ToDynamoDbLogger(this ILoggerFactory loggerFactory, string categoryName)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (categoryName == null)
        {
            throw new ArgumentNullException(nameof(categoryName));
        }

        var logger = loggerFactory.CreateLogger(categoryName);
        return new MicrosoftExtensionsLoggingAdapter(logger);
    }

    /// <summary>
    /// Creates an IDynamoDbLogger from an ILoggerFactory with the specified type as the category.
    /// </summary>
    /// <typeparam name="T">The type to use as the logger category.</typeparam>
    /// <param name="loggerFactory">The Microsoft.Extensions.Logging.ILoggerFactory instance.</param>
    /// <returns>An IDynamoDbLogger that wraps a logger created from the factory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
    public static IDynamoDbLogger ToDynamoDbLogger<T>(this ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        var logger = loggerFactory.CreateLogger<T>();
        return new MicrosoftExtensionsLoggingAdapter(logger);
    }
}
