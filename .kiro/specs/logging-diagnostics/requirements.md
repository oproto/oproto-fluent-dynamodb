# Requirements Document

## Introduction

This feature adds comprehensive logging and diagnostics capabilities to the Oproto.FluentDynamoDb library to address the challenges of debugging in AOT (Ahead-of-Time) compiled environments where stack traces are often unhelpful. The logging system will provide detailed context at every step of entity mapping, type conversion, and DynamoDB operations without requiring a dependency on Microsoft.Extensions.Logging in the core library.

The implementation uses a lightweight abstraction pattern where the core library defines a minimal logging interface, the source generator emits detailed logging calls throughout generated code, and an optional adapter package provides integration with Microsoft.Extensions.Logging for applications that use it.

## Glossary

- **IDynamoDbLogger**: Minimal logging interface defined in the core library
- **Source-Generated Logging**: Logging calls emitted by the source generator in generated mapping code
- **MEL**: Microsoft.Extensions.Logging, the standard .NET logging framework
- **Logging Adapter**: Package that bridges IDynamoDbLogger to Microsoft.Extensions.Logging
- **AOT Debugging**: Debugging in Native AOT environments where reflection-based stack traces are limited
- **Contextual Logging**: Logging that includes entity names, property names, and operation context
- **Log Level**: Severity level (Trace, Debug, Information, Warning, Error, Critical)

## Requirements

### Requirement 1: Lightweight Logging Abstraction

**User Story:** As a library maintainer, I want a minimal logging interface in the core library so that logging doesn't introduce heavy dependencies.

#### Acceptance Criteria

1. WHEN the core library defines IDynamoDbLogger interface, THE Interface SHALL include methods for Trace, Debug, Information, Warning, Error, and Critical levels
2. WHEN the interface is defined, THE Interface SHALL accept message templates and structured parameters
3. WHEN the interface is defined, THE Interface SHALL support exception logging with context
4. WHEN no logger is configured, THE Library SHALL use a no-op logger that discards all log messages
5. WHEN the interface is used, THE Implementation SHALL not require any external logging framework dependencies in the core library

### Requirement 2: Source-Generated Logging in Mapping Code

**User Story:** As a developer debugging AOT issues, I want detailed logging in generated mapping code so that I can trace exactly where failures occur.

#### Acceptance Criteria

1. WHEN the source generator creates ToDynamoDb methods, THE Generated_Code SHALL log entry with entity type and instance details
2. WHEN the source generator creates ToDynamoDb methods, THE Generated_Code SHALL log each property being mapped with property name and value type
3. WHEN the source generator creates ToDynamoDb methods, THE Generated_Code SHALL log exit with item attribute count
4. WHEN the source generator creates FromDynamoDb methods, THE Generated_Code SHALL log entry with item attribute count
5. WHEN the source generator creates FromDynamoDb methods, THE Generated_Code SHALL log each property being reconstructed with property name and source attribute type

### Requirement 3: Error Context Logging

**User Story:** As a developer debugging failures, I want errors to include full context so that I can identify the exact operation that failed.

#### Acceptance Criteria

1. WHEN mapping fails, THE Generated_Code SHALL log error with entity type, property name, and attempted operation
2. WHEN type conversion fails, THE Generated_Code SHALL log error with source type, target type, and value
3. WHEN collection conversion fails, THE Generated_Code SHALL log error with collection type, element count, and failure point
4. WHEN JSON serialization fails, THE Generated_Code SHALL log error with property name, type, and serializer being used
5. WHEN blob storage fails, THE Generated_Code SHALL log error with reference key, provider type, and operation

### Requirement 4: Performance-Conscious Logging

**User Story:** As a developer, I want logging to have minimal performance impact so that it doesn't slow down my application.

#### Acceptance Criteria

1. WHEN logger is not configured, THE No-op_Logger SHALL have zero allocation overhead
2. WHEN logging at Trace or Debug level, THE Generated_Code SHALL check if level is enabled before constructing log messages
3. WHEN logging structured data, THE Generated_Code SHALL avoid unnecessary string allocations
4. WHEN logging in hot paths, THE Generated_Code SHALL use efficient string interpolation
5. WHEN logging is disabled, THE Generated_Code SHALL not evaluate expensive parameters

### Requirement 5: Microsoft.Extensions.Logging Integration

**User Story:** As a developer using Microsoft.Extensions.Logging, I want seamless integration so that DynamoDB logs flow through my existing logging infrastructure.

#### Acceptance Criteria

1. WHEN I reference Oproto.FluentDynamoDb.Logging.Extensions package, THE Package SHALL provide an adapter for ILogger
2. WHEN I configure the adapter, THE Adapter SHALL map IDynamoDbLogger levels to ILogger levels
3. WHEN I use structured logging, THE Adapter SHALL preserve log message templates and parameters
4. WHEN I use log scopes, THE Adapter SHALL support ILogger scope functionality
5. WHEN exceptions are logged, THE Adapter SHALL pass exceptions to ILogger with full context

### Requirement 6: Logger Configuration

**User Story:** As a developer, I want to configure logging for DynamoDB operations so that I can control log output.

#### Acceptance Criteria

1. WHEN I create a DynamoDbTableBase instance, THE Constructor SHALL accept an optional IDynamoDbLogger parameter
2. WHEN I don't provide a logger, THE Library SHALL use a no-op logger by default
3. WHEN I provide a logger, THE Library SHALL use it for all operations on that table instance
4. WHEN I use generated table classes, THE Generated_Code SHALL pass the logger to mapping methods
5. WHEN I use ExecuteAsync methods, THE Library SHALL log operation start, parameters, and completion

### Requirement 7: Structured Logging Support

**User Story:** As a developer, I want structured logging so that I can query and filter logs effectively.

#### Acceptance Criteria

1. WHEN logging entity operations, THE Generated_Code SHALL include EntityType as a structured property
2. WHEN logging property mapping, THE Generated_Code SHALL include PropertyName and PropertyType as structured properties
3. WHEN logging DynamoDB operations, THE Generated_Code SHALL include TableName and OperationType as structured properties
4. WHEN logging errors, THE Generated_Code SHALL include ErrorCode and FailurePoint as structured properties
5. WHEN using MEL adapter, THE Adapter SHALL preserve all structured properties as log state

### Requirement 8: Log Level Guidelines

**User Story:** As a developer, I want consistent log levels so that I can filter logs appropriately.

#### Acceptance Criteria

1. WHEN entering/exiting mapping methods, THE Generated_Code SHALL log at Trace level
2. WHEN mapping individual properties, THE Generated_Code SHALL log at Debug level
3. WHEN skipping null or empty values, THE Generated_Code SHALL log at Debug level
4. WHEN encountering unexpected but handled conditions, THE Generated_Code SHALL log at Warning level
5. WHEN operations fail, THE Generated_Code SHALL log at Error level with full context

### Requirement 9: Request Builder Logging

**User Story:** As a developer, I want logging in request builders so that I can trace query construction.

#### Acceptance Criteria

1. WHEN building queries, THE Request_Builders SHALL log key conditions and filter expressions at Debug level
2. WHEN adding parameters, THE Request_Builders SHALL log parameter names and types at Trace level
3. WHEN executing requests, THE Request_Builders SHALL log operation type and table name at Information level
4. WHEN requests complete, THE Request_Builders SHALL log consumed capacity and item count at Information level
5. WHEN requests fail, THE Request_Builders SHALL log error details at Error level

### Requirement 10: Conditional Compilation Support

**User Story:** As a developer, I want to disable logging in production builds so that I can eliminate logging overhead entirely.

#### Acceptance Criteria

1. WHEN DISABLE_DYNAMODB_LOGGING is defined, THE Generated_Code SHALL omit all logging calls
2. WHEN DISABLE_DYNAMODB_LOGGING is defined, THE Core_Library SHALL use a no-op logger implementation
3. WHEN DISABLE_DYNAMODB_LOGGING is defined, THE Compiled_Code SHALL have no logging overhead
4. WHEN DISABLE_DYNAMODB_LOGGING is not defined, THE Generated_Code SHALL include full logging support
5. WHEN conditional compilation is used, THE Generated_Code SHALL remain syntactically valid in both modes

### Requirement 11: Diagnostic Event IDs

**User Story:** As a developer, I want event IDs for log messages so that I can identify specific log types programmatically.

#### Acceptance Criteria

1. WHEN logging mapping operations, THE Generated_Code SHALL use event IDs in the 1000-1999 range
2. WHEN logging type conversions, THE Generated_Code SHALL use event IDs in the 2000-2999 range
3. WHEN logging DynamoDB operations, THE Generated_Code SHALL use event IDs in the 3000-3999 range
4. WHEN logging errors, THE Generated_Code SHALL use event IDs in the 9000-9999 range
5. WHEN using MEL adapter, THE Adapter SHALL preserve event IDs in ILogger calls

### Requirement 12: Logger Null Safety

**User Story:** As a developer, I want the library to handle null loggers gracefully so that logging is truly optional.

#### Acceptance Criteria

1. WHEN logger is null, THE Generated_Code SHALL not throw NullReferenceException
2. WHEN logger is null, THE Generated_Code SHALL use null-conditional operators for all logging calls
3. WHEN logger is null, THE Generated_Code SHALL not evaluate log message parameters
4. WHEN logger is null, THE Library SHALL function identically to when a no-op logger is provided
5. WHEN logger is null, THE Generated_Code SHALL have minimal performance overhead

### Requirement 13: Exception Logging

**User Story:** As a developer, I want exceptions logged with full context so that I can diagnose failures in production.

#### Acceptance Criteria

1. WHEN catching exceptions in generated code, THE Generated_Code SHALL log the exception with LogError
2. WHEN logging exceptions, THE Generated_Code SHALL include entity type, property name, and operation context
3. WHEN logging exceptions, THE Generated_Code SHALL include the exception type and message
4. WHEN logging exceptions, THE Generated_Code SHALL include relevant property values (sanitized if needed)
5. WHEN re-throwing exceptions, THE Generated_Code SHALL ensure the exception is logged before re-throwing

### Requirement 14: Backward Compatibility

**User Story:** As a developer, I want existing code to work without changes so that logging is purely additive.

#### Acceptance Criteria

1. WHEN upgrading to the logging-enabled version, THE Existing_Code SHALL compile without modifications
2. WHEN not providing a logger, THE Library SHALL behave identically to previous versions
3. WHEN using existing constructors, THE Library SHALL use default no-op logger
4. WHEN using existing methods, THE Library SHALL not require logger parameters
5. WHEN logging is added, THE Public_API SHALL remain unchanged except for optional logger parameters

### Requirement 15: Documentation and Examples

**User Story:** As a developer, I want clear documentation on logging so that I can configure it effectively.

#### Acceptance Criteria

1. WHEN reading documentation, THE Documentation SHALL explain how to configure IDynamoDbLogger
2. WHEN reading documentation, THE Documentation SHALL provide examples of MEL integration
3. WHEN reading documentation, THE Documentation SHALL explain log levels and when each is used
4. WHEN reading documentation, THE Documentation SHALL provide examples of structured logging queries
5. WHEN reading documentation, THE Documentation SHALL explain how to disable logging for production builds
