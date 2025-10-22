# Design Document

## Overview

This design adds comprehensive logging and diagnostics to the Oproto.FluentDynamoDb library through a three-layer approach:

1. **Core Abstraction**: Minimal `IDynamoDbLogger` interface in the core library with no external dependencies
2. **Source-Generated Logging**: Detailed logging calls emitted throughout generated entity mapping code
3. **Optional Integration**: MEL (Microsoft.Extensions.Logging) adapter package for seamless integration

The design prioritizes zero overhead when logging is disabled, minimal allocations when enabled, and rich contextual information for debugging AOT applications where stack traces are limited.

## Architecture

### Package Structure

```
Oproto.FluentDynamoDb/                           # Core library (.NET 8)
├── Logging/
│   ├── IDynamoDbLogger.cs                       # Minimal logging interface
│   ├── NoOpLogger.cs                            # Default no-op implementation
│   └── LogEventIds.cs                           # Event ID constants
└── Storage/
    └── DynamoDbTableBase.cs                     # Enhanced with logger support

Oproto.FluentDynamoDb.SourceGenerator/           # Source generator (.NET Standard 2.0)
├── Generators/
│   └── LoggingCodeGenerator.cs                  # Generates logging calls
└── Templates/
    └── LoggingTemplates.cs                      # Reusable logging code templates

Oproto.FluentDynamoDb.Logging.Extensions/        # MEL adapter (.NET 8)
└── MicrosoftExtensionsLoggingAdapter.cs         # ILogger adapter
```

### Design Principles

1. **Zero Dependency**: Core library has no logging framework dependencies
2. **Zero Overhead When Disabled**: No-op logger and conditional compilation eliminate all overhead
3. **Rich Context**: Every log includes entity type, property name, and operation details
4. **AOT-Friendly**: All logging code is generated at compile-time, no reflection
5. **Structured Logging**: Support for structured properties that work with modern log aggregation
6. **Backward Compatible**: Existing code works without changes


## Components and Interfaces

### 1. Core Logging Interface

```csharp
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
    bool IsEnabled(LogLevel logLevel);
    
    /// <summary>
    /// Logs a trace message (most verbose).
    /// </summary>
    void LogTrace(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void LogDebug(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void LogInformation(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    void LogError(int eventId, string message, params object[] args);
    
    /// <summary>
    /// Logs an error with exception.
    /// </summary>
    void LogError(int eventId, Exception exception, string message, params object[] args);
    
    /// <summary>
    /// Logs a critical error.
    /// </summary>
    void LogCritical(int eventId, Exception exception, string message, params object[] args);
}

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
```

### 2. No-Op Logger Implementation

```csharp
namespace Oproto.FluentDynamoDb.Logging;

/// <summary>
/// No-op logger that discards all log messages.
/// Used as default when no logger is configured.
/// </summary>
internal sealed class NoOpLogger : IDynamoDbLogger
{
    public static readonly NoOpLogger Instance = new();
    
    private NoOpLogger() { }
    
    public bool IsEnabled(LogLevel logLevel) => false;
    
    public void LogTrace(int eventId, string message, params object[] args) { }
    
    public void LogDebug(int eventId, string message, params object[] args) { }
    
    public void LogInformation(int eventId, string message, params object[] args) { }
    
    public void LogWarning(int eventId, string message, params object[] args) { }
    
    public void LogError(int eventId, string message, params object[] args) { }
    
    public void LogError(int eventId, Exception exception, string message, params object[] args) { }
    
    public void LogCritical(int eventId, Exception exception, string message, params object[] args) { }
}
```

### 3. Log Event IDs

```csharp
namespace Oproto.FluentDynamoDb.Logging;

/// <summary>
/// Event IDs for DynamoDB operations.
/// Organized by category for easy filtering.
/// </summary>
public static class LogEventIds
{
    // Mapping operations (1000-1999)
    public const int MappingToDynamoDbStart = 1000;
    public const int MappingToDynamoDbComplete = 1001;
    public const int MappingFromDynamoDbStart = 1010;
    public const int MappingFromDynamoDbComplete = 1011;
    public const int MappingPropertyStart = 1020;
    public const int MappingPropertyComplete = 1021;
    public const int MappingPropertySkipped = 1022;
    
    // Type conversions (2000-2999)
    public const int ConvertingMap = 2000;
    public const int ConvertingSet = 2010;
    public const int ConvertingList = 2020;
    public const int ConvertingTtl = 2030;
    public const int ConvertingJsonBlob = 2040;
    public const int ConvertingBlobReference = 2050;
    
    // DynamoDB operations (3000-3999)
    public const int ExecutingGetItem = 3000;
    public const int ExecutingPutItem = 3010;
    public const int ExecutingQuery = 3020;
    public const int ExecutingUpdate = 3030;
    public const int ExecutingTransaction = 3040;
    public const int OperationComplete = 3100;
    public const int ConsumedCapacity = 3110;
    
    // Errors (9000-9999)
    public const int MappingError = 9000;
    public const int ConversionError = 9010;
    public const int JsonSerializationError = 9020;
    public const int BlobStorageError = 9030;
    public const int DynamoDbOperationError = 9040;
}
```


### 4. Enhanced DynamoDbTableBase

```csharp
public abstract class DynamoDbTableBase
{
    protected IAmazonDynamoDB DynamoDbClient { get; }
    protected string TableName { get; }
    protected IDynamoDbLogger Logger { get; }
    
    // New constructor with logger support
    protected DynamoDbTableBase(
        IAmazonDynamoDB dynamoDbClient, 
        string tableName,
        IDynamoDbLogger? logger = null)
    {
        DynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        Logger = logger ?? NoOpLogger.Instance;
    }
    
    // Existing constructor remains for backward compatibility
    protected DynamoDbTableBase(IAmazonDynamoDB dynamoDbClient, string tableName)
        : this(dynamoDbClient, tableName, null)
    {
    }
}
```

### 5. Microsoft.Extensions.Logging Adapter

```csharp
namespace Oproto.FluentDynamoDb.Logging.Extensions;

using Microsoft.Extensions.Logging;
using Oproto.FluentDynamoDb.Logging;

/// <summary>
/// Adapter that bridges IDynamoDbLogger to Microsoft.Extensions.Logging.ILogger
/// </summary>
public class MicrosoftExtensionsLoggingAdapter : IDynamoDbLogger
{
    private readonly ILogger _logger;
    
    public MicrosoftExtensionsLoggingAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public bool IsEnabled(Oproto.FluentDynamoDb.Logging.LogLevel logLevel)
    {
        return _logger.IsEnabled(MapLogLevel(logLevel));
    }
    
    public void LogTrace(int eventId, string message, params object[] args)
    {
        _logger.LogTrace(new EventId(eventId), message, args);
    }
    
    public void LogDebug(int eventId, string message, params object[] args)
    {
        _logger.LogDebug(new EventId(eventId), message, args);
    }
    
    public void LogInformation(int eventId, string message, params object[] args)
    {
        _logger.LogInformation(new EventId(eventId), message, args);
    }
    
    public void LogWarning(int eventId, string message, params object[] args)
    {
        _logger.LogWarning(new EventId(eventId), message, args);
    }
    
    public void LogError(int eventId, string message, params object[] args)
    {
        _logger.LogError(new EventId(eventId), message, args);
    }
    
    public void LogError(int eventId, Exception exception, string message, params object[] args)
    {
        _logger.LogError(new EventId(eventId), exception, message, args);
    }
    
    public void LogCritical(int eventId, Exception exception, string message, params object[] args)
    {
        _logger.LogCritical(new EventId(eventId), exception, message, args);
    }
    
    private static Microsoft.Extensions.Logging.LogLevel MapLogLevel(
        Oproto.FluentDynamoDb.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Oproto.FluentDynamoDb.Logging.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
            Oproto.FluentDynamoDb.Logging.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            Oproto.FluentDynamoDb.Logging.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            Oproto.FluentDynamoDb.Logging.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            Oproto.FluentDynamoDb.Logging.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            Oproto.FluentDynamoDb.Logging.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            _ => Microsoft.Extensions.Logging.LogLevel.None
        };
    }
}

/// <summary>
/// Extension methods for easy adapter creation
/// </summary>
public static class LoggerExtensions
{
    public static IDynamoDbLogger ToDynamoDbLogger(this ILogger logger)
    {
        return new MicrosoftExtensionsLoggingAdapter(logger);
    }
    
    public static IDynamoDbLogger ToDynamoDbLogger(this ILoggerFactory loggerFactory, string categoryName)
    {
        var logger = loggerFactory.CreateLogger(categoryName);
        return new MicrosoftExtensionsLoggingAdapter(logger);
    }
}
```


## Source-Generated Logging

### 1. Generated ToDynamoDb with Logging

```csharp
// Generated code for entity mapping with comprehensive logging
public static Dictionary<string, AttributeValue> ToDynamoDb<TSelf>(
    TSelf entity,
    IDynamoDbLogger? logger = null) 
    where TSelf : IDynamoDbEntity
{
    #if !DISABLE_DYNAMODB_LOGGING
    logger?.LogTrace(LogEventIds.MappingToDynamoDbStart, 
        "Starting ToDynamoDb mapping for {EntityType}", 
        typeof(TSelf).Name);
    #endif
    
    var typedEntity = (Product)(object)entity;
    var item = new Dictionary<string, AttributeValue>();
    
    try
    {
        // Map partition key
        #if !DISABLE_DYNAMODB_LOGGING
        if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            logger.LogDebug(LogEventIds.MappingPropertyStart,
                "Mapping property {PropertyName} of type {PropertyType}",
                "Id", "String");
        }
        #endif
        
        item["pk"] = new AttributeValue { S = typedEntity.Id };
        
        // Map collection with logging
        if (typedEntity.Tags != null && typedEntity.Tags.Count > 0)
        {
            #if !DISABLE_DYNAMODB_LOGGING
            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug(LogEventIds.ConvertingSet,
                    "Converting {PropertyName} to String Set with {ElementCount} elements",
                    "Tags", typedEntity.Tags.Count);
            }
            #endif
            
            item["tags"] = new AttributeValue { SS = typedEntity.Tags.ToList() };
        }
        else
        {
            #if !DISABLE_DYNAMODB_LOGGING
            logger?.LogDebug(LogEventIds.MappingPropertySkipped,
                "Skipping empty collection {PropertyName}",
                "Tags");
            #endif
        }
        
        #if !DISABLE_DYNAMODB_LOGGING
        logger?.LogTrace(LogEventIds.MappingToDynamoDbComplete,
            "Completed ToDynamoDb mapping for {EntityType} with {AttributeCount} attributes",
            typeof(TSelf).Name, item.Count);
        #endif
        
        return item;
    }
    catch (Exception ex)
    {
        #if !DISABLE_DYNAMODB_LOGGING
        logger?.LogError(LogEventIds.MappingError, ex,
            "Failed to map {EntityType} to DynamoDB item",
            typeof(TSelf).Name);
        #endif
        throw;
    }
}
```

### 2. Generated FromDynamoDb with Logging

```csharp
public static TSelf FromDynamoDb<TSelf>(
    Dictionary<string, AttributeValue> item,
    IDynamoDbLogger? logger = null) 
    where TSelf : IDynamoDbEntity
{
    #if !DISABLE_DYNAMODB_LOGGING
    logger?.LogTrace(LogEventIds.MappingFromDynamoDbStart,
        "Starting FromDynamoDb mapping for {EntityType} with {AttributeCount} attributes",
        typeof(TSelf).Name, item.Count);
    #endif
    
    var entity = new Product();
    
    try
    {
        // Map partition key
        if (item.TryGetValue("pk", out var pkValue))
        {
            #if !DISABLE_DYNAMODB_LOGGING
            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug(LogEventIds.MappingPropertyStart,
                    "Mapping property {PropertyName} from {AttributeType}",
                    "Id", pkValue.S != null ? "String" : "Unknown");
            }
            #endif
            
            entity.Id = pkValue.S;
        }
        
        // Map collection with logging
        if (item.TryGetValue("tags", out var tagsValue) && tagsValue.SS != null)
        {
            #if !DISABLE_DYNAMODB_LOGGING
            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug(LogEventIds.ConvertingSet,
                    "Converting {PropertyName} from String Set with {ElementCount} elements",
                    "Tags", tagsValue.SS.Count);
            }
            #endif
            
            entity.Tags = new HashSet<string>(tagsValue.SS);
        }
        
        #if !DISABLE_DYNAMODB_LOGGING
        logger?.LogTrace(LogEventIds.MappingFromDynamoDbComplete,
            "Completed FromDynamoDb mapping for {EntityType}",
            typeof(TSelf).Name);
        #endif
        
        return (TSelf)(object)entity;
    }
    catch (Exception ex)
    {
        #if !DISABLE_DYNAMODB_LOGGING
        logger?.LogError(LogEventIds.MappingError, ex,
            "Failed to map DynamoDB item to {EntityType}",
            typeof(TSelf).Name);
        #endif
        throw;
    }
}
```


### 3. Generated Error Handling with Context

```csharp
// Map conversion with detailed error logging
if (typedEntity.Metadata != null && typedEntity.Metadata.Count > 0)
{
    try
    {
        #if !DISABLE_DYNAMODB_LOGGING
        if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            logger.LogDebug(LogEventIds.ConvertingMap,
                "Converting {PropertyName} to Map with {ElementCount} entries",
                "Metadata", typedEntity.Metadata.Count);
        }
        #endif
        
        var metadataMap = new Dictionary<string, AttributeValue>();
        foreach (var kvp in typedEntity.Metadata)
        {
            metadataMap[kvp.Key] = new AttributeValue { S = kvp.Value };
        }
        item["metadata"] = new AttributeValue { M = metadataMap };
    }
    catch (Exception ex)
    {
        #if !DISABLE_DYNAMODB_LOGGING
        logger?.LogError(LogEventIds.ConversionError, ex,
            "Failed to convert {PropertyName} to Map. PropertyType: {PropertyType}, ElementCount: {ElementCount}",
            "Metadata", "Dictionary<string, string>", typedEntity.Metadata.Count);
        #endif
        throw new DynamoDbMappingException("Metadata", "Product", 
            "Failed to convert dictionary to DynamoDB Map", ex);
    }
}
```

### 4. Request Builder Logging

```csharp
// Enhanced ExecuteAsync with logging
public async Task<QueryResponse> ExecuteAsync(CancellationToken cancellationToken = default)
{
    #if !DISABLE_DYNAMODB_LOGGING
    _logger?.LogInformation(LogEventIds.ExecutingQuery,
        "Executing Query on table {TableName}. KeyCondition: {KeyCondition}, FilterExpression: {FilterExpression}",
        _tableName, _req.KeyConditionExpression ?? "None", _req.FilterExpression ?? "None");
    
    if (_logger?.IsEnabled(LogLevel.Debug) == true && _attrV.AttributeValues.Count > 0)
    {
        _logger.LogDebug(LogEventIds.ExecutingQuery,
            "Query parameters: {ParameterCount} values",
            _attrV.AttributeValues.Count);
    }
    #endif
    
    try
    {
        var response = await _dynamoDbClient.QueryAsync(_req, cancellationToken);
        
        #if !DISABLE_DYNAMODB_LOGGING
        _logger?.LogInformation(LogEventIds.OperationComplete,
            "Query completed. ItemCount: {ItemCount}, ConsumedCapacity: {ConsumedCapacity}",
            response.Count, response.ConsumedCapacity?.CapacityUnits ?? 0);
        #endif
        
        return response;
    }
    catch (Exception ex)
    {
        #if !DISABLE_DYNAMODB_LOGGING
        _logger?.LogError(LogEventIds.DynamoDbOperationError, ex,
            "Query failed on table {TableName}",
            _tableName);
        #endif
        throw;
    }
}
```


## Usage Examples

### Basic Usage with No Logger

```csharp
// Default behavior - no logging
var table = new ProductsTable(dynamoDbClient, "products");

// Operations work as before, no logging overhead
await table.GetProductAsync(productId);
```

### Usage with Microsoft.Extensions.Logging

```csharp
// In Startup.cs or Program.cs
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Create table with logger
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<ProductsTable>().ToDynamoDbLogger();
var table = new ProductsTable(dynamoDbClient, "products", logger);

// Now all operations are logged
await table.GetProductAsync(productId);
// Logs:
// [Trace] Starting FromDynamoDb mapping for Product with 5 attributes
// [Debug] Mapping property Id from String
// [Debug] Mapping property Name from String
// [Debug] Converting Tags from String Set with 3 elements
// [Trace] Completed FromDynamoDb mapping for Product
```

### Usage with Custom Logger

```csharp
public class ConsoleLogger : IDynamoDbLogger
{
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
    
    public void LogInformation(int eventId, string message, params object[] args)
    {
        Console.WriteLine($"[INFO] {string.Format(message, args)}");
    }
    
    // Implement other methods...
}

var logger = new ConsoleLogger();
var table = new ProductsTable(dynamoDbClient, "products", logger);
```

### Conditional Compilation for Production

```csharp
// In .csproj for Release builds
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>$(DefineConstants);DISABLE_DYNAMODB_LOGGING</DefineConstants>
</PropertyGroup>

// All logging code is removed in Release builds
// Zero overhead, no allocations
```

### Structured Logging Queries

```csharp
// With Serilog or similar structured logging
// Query logs by entity type
SELECT * FROM logs 
WHERE EntityType = 'Product' 
  AND EventId BETWEEN 1000 AND 1999

// Query mapping errors
SELECT * FROM logs 
WHERE EventId = 9000 
  AND PropertyName = 'Metadata'

// Query DynamoDB operations
SELECT * FROM logs 
WHERE EventId BETWEEN 3000 AND 3999 
  AND TableName = 'products'
```

## Performance Considerations

### Zero Overhead When Disabled

```csharp
// With DISABLE_DYNAMODB_LOGGING defined, generated code becomes:
public static Dictionary<string, AttributeValue> ToDynamoDb<TSelf>(
    TSelf entity,
    IDynamoDbLogger? logger = null) 
    where TSelf : IDynamoDbEntity
{
    // All #if !DISABLE_DYNAMODB_LOGGING blocks are removed
    var typedEntity = (Product)(object)entity;
    var item = new Dictionary<string, AttributeValue>();
    
    item["pk"] = new AttributeValue { S = typedEntity.Id };
    
    if (typedEntity.Tags != null && typedEntity.Tags.Count > 0)
    {
        item["tags"] = new AttributeValue { SS = typedEntity.Tags.ToList() };
    }
    
    return item;
}
```

### Minimal Overhead When Enabled

```csharp
// IsEnabled check prevents expensive parameter evaluation
if (logger?.IsEnabled(LogLevel.Debug) == true)
{
    // This code only runs if Debug logging is enabled
    logger.LogDebug(LogEventIds.ConvertingSet,
        "Converting {PropertyName} to String Set with {ElementCount} elements",
        "Tags", typedEntity.Tags.Count);
}

// Null-conditional operator prevents NullReferenceException
logger?.LogTrace(LogEventIds.MappingToDynamoDbStart, 
    "Starting ToDynamoDb mapping for {EntityType}", 
    typeof(TSelf).Name);
```

### Allocation Optimization

```csharp
// Use string interpolation only when logging is enabled
if (logger?.IsEnabled(LogLevel.Debug) == true)
{
    // String formatting only happens if Debug is enabled
    logger.LogDebug(eventId, "Message with {Param}", value);
}

// Avoid boxing value types when possible
logger?.LogDebug(eventId, "Count: {Count}", count); // int boxed once
```


## Testing Strategy

### Unit Tests for Core Logging

```csharp
[Fact]
public void NoOpLogger_IsEnabled_AlwaysReturnsFalse()
{
    var logger = NoOpLogger.Instance;
    
    logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
    logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
    logger.IsEnabled(LogLevel.Information).Should().BeFalse();
}

[Fact]
public void NoOpLogger_LogMethods_DoNotThrow()
{
    var logger = NoOpLogger.Instance;
    
    var act = () =>
    {
        logger.LogTrace(1, "Test");
        logger.LogDebug(2, "Test");
        logger.LogError(3, new Exception(), "Test");
    };
    
    act.Should().NotThrow();
}
```

### Unit Tests for MEL Adapter

```csharp
[Fact]
public void MelAdapter_LogInformation_CallsUnderlyingLogger()
{
    var mockLogger = Substitute.For<ILogger>();
    var adapter = new MicrosoftExtensionsLoggingAdapter(mockLogger);
    
    adapter.LogInformation(1000, "Test message with {Param}", "value");
    
    mockLogger.Received(1).Log(
        Microsoft.Extensions.Logging.LogLevel.Information,
        Arg.Is<EventId>(e => e.Id == 1000),
        Arg.Any<object>(),
        null,
        Arg.Any<Func<object, Exception?, string>>());
}

[Fact]
public void MelAdapter_IsEnabled_MapsLogLevelsCorrectly()
{
    var mockLogger = Substitute.For<ILogger>();
    mockLogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug).Returns(true);
    
    var adapter = new MicrosoftExtensionsLoggingAdapter(mockLogger);
    
    adapter.IsEnabled(Oproto.FluentDynamoDb.Logging.LogLevel.Debug).Should().BeTrue();
}
```

### Integration Tests with Generated Code

```csharp
[Fact]
public void GeneratedCode_WithLogger_LogsMappingOperations()
{
    var logMessages = new List<string>();
    var logger = new TestLogger(logMessages);
    
    var entity = new Product { Id = "prod-123", Name = "Test" };
    var item = Product.ToDynamoDb(entity, logger);
    
    logMessages.Should().Contain(m => m.Contains("Starting ToDynamoDb mapping"));
    logMessages.Should().Contain(m => m.Contains("Mapping property Id"));
    logMessages.Should().Contain(m => m.Contains("Completed ToDynamoDb mapping"));
}

[Fact]
public void GeneratedCode_WithoutLogger_DoesNotThrow()
{
    var entity = new Product { Id = "prod-123", Name = "Test" };
    
    var act = () => Product.ToDynamoDb(entity, null);
    
    act.Should().NotThrow();
}

[Fact]
public void GeneratedCode_WithError_LogsException()
{
    var logMessages = new List<(int EventId, Exception? Ex, string Message)>();
    var logger = new TestLogger(logMessages);
    
    var entity = new Product { /* Invalid data */ };
    
    var act = () => Product.ToDynamoDb(entity, logger);
    
    act.Should().Throw<Exception>();
    logMessages.Should().Contain(m => 
        m.EventId == LogEventIds.MappingError && 
        m.Ex != null);
}
```

### Performance Tests

```csharp
[Fact]
public void NoOpLogger_HasZeroOverhead()
{
    var entity = new Product { Id = "prod-123", Name = "Test" };
    
    // Warmup
    for (int i = 0; i < 1000; i++)
    {
        Product.ToDynamoDb(entity, NoOpLogger.Instance);
    }
    
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 100000; i++)
    {
        Product.ToDynamoDb(entity, NoOpLogger.Instance);
    }
    sw.Stop();
    
    var withLogger = sw.ElapsedMilliseconds;
    
    sw.Restart();
    for (int i = 0; i < 100000; i++)
    {
        Product.ToDynamoDb(entity, null);
    }
    sw.Stop();
    
    var withoutLogger = sw.ElapsedMilliseconds;
    
    // Should be within 5% of each other
    withLogger.Should().BeCloseTo(withoutLogger, (long)(withoutLogger * 0.05));
}
```

## Migration Guide

### Adding Logging to Existing Tables

```csharp
// Before
public class ProductsTable : DynamoDbTableBase
{
    public ProductsTable(IAmazonDynamoDB client, string tableName)
        : base(client, tableName)
    {
    }
}

// After - with optional logger support
public class ProductsTable : DynamoDbTableBase
{
    public ProductsTable(
        IAmazonDynamoDB client, 
        string tableName,
        IDynamoDbLogger? logger = null)
        : base(client, tableName, logger)
    {
    }
    
    // Existing constructor still works for backward compatibility
    public ProductsTable(IAmazonDynamoDB client, string tableName)
        : base(client, tableName)
    {
    }
}
```

### Passing Logger to Generated Methods

```csharp
// Generated table methods automatically pass logger
public async Task<Product?> GetProductAsync(string productId)
{
    var response = await Get
        .WithKey("pk", productId)
        .ExecuteAsync();
    
    if (response.Item == null)
        return null;
    
    // Logger is passed from table instance
    return Product.FromDynamoDb<Product>(response.Item, Logger);
}
```

This design provides comprehensive logging support while maintaining the library's core principles of zero dependencies, AOT compatibility, and minimal overhead when logging is disabled.
