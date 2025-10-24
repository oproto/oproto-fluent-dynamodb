# Design Document: LINQ Expression Support

## Overview

This design adds LINQ-style expression support to FluentDynamoDb, enabling developers to write type-safe queries using C# lambda expressions. The implementation translates expression trees to DynamoDB expression syntax in an AOT-compatible manner, leveraging existing entity metadata and parameter generation infrastructure.

The design focuses on query, filter, and condition expressions. Update expressions are explicitly out of scope and will be addressed in a future iteration.

## Architecture

### High-Level Flow

```
C# Lambda Expression
    ↓
Expression Tree Analysis (ExpressionTranslator)
    ↓
DynamoDB Expression Syntax + Parameters
    ↓
Existing Request Builder Infrastructure
    ↓
DynamoDB API Call
```

### Key Components

1. **ExpressionTranslator**: Core component that walks expression trees and generates DynamoDB syntax
2. **ExpressionContext**: Tracks state during translation (parameters, attribute names, validation mode)
3. **Extension Methods**: New overloads on existing Where/WithFilter/WithCondition methods
4. **Entity Metadata Integration**: Uses existing PropertyMetadata for validation
5. **Parameter Generation**: Integrates with existing AttributeValueInternal infrastructure

## Components and Interfaces

### 1. ExpressionTranslator

The core translation engine that converts expression trees to DynamoDB syntax.

```csharp
namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Translates C# lambda expressions to DynamoDB expression syntax.
/// AOT-safe implementation that analyzes expression trees without dynamic code generation.
/// </summary>
public class ExpressionTranslator
{
    /// <summary>
    /// Translates a lambda expression to DynamoDB expression syntax.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried</typeparam>
    /// <param name="expression">The lambda expression to translate</param>
    /// <param name="context">The translation context</param>
    /// <returns>The DynamoDB expression string</returns>
    public string Translate<TEntity>(
        Expression<Func<TEntity, bool>> expression, 
        ExpressionContext context);
    
    // Private methods for visiting different expression node types
    private string VisitBinary(BinaryExpression node, ExpressionContext context);
    private string VisitMember(MemberExpression node, ExpressionContext context);
    private string VisitConstant(ConstantExpression node, ExpressionContext context);
    private string VisitMethodCall(MethodCallExpression node, ExpressionContext context);
    private string VisitUnary(UnaryExpression node, ExpressionContext context);
    
    // Validation helpers
    private bool ReferencesEntityParameter(Expression node, ParameterExpression entityParam);
    private bool IsEntityPropertyAccess(Expression node, ParameterExpression entityParam);
}
```

**Responsibilities:**
- Walk expression trees recursively
- Generate DynamoDB expression syntax
- Validate property access against entity metadata
- Capture values and generate parameters
- Handle operator mapping (==, <, >, &&, ||, etc.)
- Support DynamoDB-specific functions (begins_with, contains, between, etc.)
- Reject invalid expressions (assignments, unsupported methods, transformations)
- Distinguish between entity property access and value capture

**AOT Safety:**
- No Expression.Compile() calls
- No reflection-emit
- All expression node types handled explicitly
- Static analysis only
- Expression trees are built by the C# compiler at compile time
- We only read the pre-built tree structure

**Expression Validation:**
- **Entity side (left)**: Only property access allowed (x.PropertyName)
- **Value side (right)**: Constants, variables, closures, and method calls that DON'T reference the entity parameter
- **Reject**: 
  - Assignments (x.Id = value)
  - Unsupported methods on entity properties (x.Name.ToUpper())
  - Methods that reference the entity parameter (myFunction(x) or myFunction(x.Name))
  - Complex transformations that can't execute in DynamoDB

### 2. ExpressionContext

Maintains state during expression translation.

```csharp
namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Context for expression translation, tracking parameters and validation state.
/// </summary>
public class ExpressionContext
{
    /// <summary>
    /// The attribute value helper for parameter generation.
    /// </summary>
    public AttributeValueInternal AttributeValues { get; }
    
    /// <summary>
    /// The attribute name helper for reserved word handling.
    /// </summary>
    public AttributeNameInternal AttributeNames { get; }
    
    /// <summary>
    /// Entity metadata for property validation.
    /// </summary>
    public EntityMetadata? EntityMetadata { get; }
    
    /// <summary>
    /// Validation mode for the expression context.
    /// </summary>
    public ExpressionValidationMode ValidationMode { get; }
    
    /// <summary>
    /// Parameter generator for unique parameter names.
    /// </summary>
    public ParameterGenerator ParameterGenerator { get; }
    
    public ExpressionContext(
        AttributeValueInternal attributeValues,
        AttributeNameInternal attributeNames,
        EntityMetadata? entityMetadata,
        ExpressionValidationMode validationMode);
}

/// <summary>
/// Validation mode for expression translation.
/// </summary>
public enum ExpressionValidationMode
{
    /// <summary>
    /// No validation - any property can be referenced (for filter/condition expressions).
    /// </summary>
    None,
    
    /// <summary>
    /// Key-only validation - only partition key and sort key properties allowed (for Query().Where()).
    /// </summary>
    KeysOnly
}
```

**Responsibilities:**
- Track generated parameters
- Track attribute name mappings
- Provide entity metadata for validation
- Specify validation rules based on context
- Generate unique parameter names

### 3. Extension Method Overloads

New overloads on existing extension methods to accept lambda expressions.

```csharp
namespace Oproto.FluentDynamoDb.Requests.Extensions;

public static class WithConditionExpressionExtensions
{
    // Existing methods remain unchanged
    public static T Where<T>(this IWithConditionExpression<T> builder, string conditionExpression);
    public static T Where<T>(this IWithConditionExpression<T> builder, string format, params object[] args);
    
    // NEW: Expression-based overload
    /// <summary>
    /// Specifies the condition expression using a C# lambda expression.
    /// </summary>
    public static T Where<T, TEntity>(
        this IWithConditionExpression<T> builder, 
        Expression<Func<TEntity, bool>> expression,
        EntityMetadata? metadata = null)
    {
        var context = new ExpressionContext(
            builder.GetAttributeValueHelper(),
            builder.GetAttributeNameHelper(),
            metadata,
            ExpressionValidationMode.KeysOnly); // For Query().Where()
            
        var translator = new ExpressionTranslator();
        var expressionString = translator.Translate(expression, context);
        
        return builder.SetConditionExpression(expressionString);
    }
}

public static class WithFilterExpressionExtensions
{
    // Existing methods remain unchanged
    public static T WithFilter<T>(this IWithFilterExpression<T> builder, string filterExpression);
    public static T WithFilter<T>(this IWithFilterExpression<T> builder, string format, params object[] args);
    
    // NEW: Expression-based overload
    /// <summary>
    /// Specifies the filter expression using a C# lambda expression.
    /// </summary>
    public static T WithFilter<T, TEntity>(
        this IWithFilterExpression<T> builder, 
        Expression<Func<TEntity, bool>> expression,
        EntityMetadata? metadata = null)
    {
        var context = new ExpressionContext(
            builder.GetAttributeValueHelper(),
            builder.GetAttributeNameHelper(),
            metadata,
            ExpressionValidationMode.None); // No key restrictions for filters
            
        var translator = new ExpressionTranslator();
        var expressionString = translator.Translate(expression, context);
        
        return builder.SetFilterExpression(expressionString);
    }
}
```

**Design Notes:**
- Extension methods maintain the existing fluent API pattern
- EntityMetadata is optional - if not provided, validation is skipped
- ValidationMode differs between Where() and WithFilter()
- Integrates seamlessly with existing string-based methods

### 4. Operator Mapping

Mapping between C# operators and DynamoDB expression syntax.

| C# Operator | DynamoDB Syntax | Notes |
|-------------|-----------------|-------|
| `==` | `=` | Equality |
| `!=` | `<>` | Inequality |
| `<` | `<` | Less than |
| `>` | `>` | Greater than |
| `<=` | `<=` | Less than or equal |
| `>=` | `>=` | Greater than or equal |
| `&&` | `AND` | Logical AND |
| `\|\|` | `OR` | Logical OR |
| `!` | `NOT` | Logical NOT |

### 5. Method Call Mapping

Mapping between C# methods and DynamoDB functions.

| C# Method | DynamoDB Function | Example |
|-----------|-------------------|---------|
| `string.StartsWith(value)` | `begins_with(attr, value)` | `x.Name.StartsWith("John")` → `begins_with(#name, :p0)` |
| `string.Contains(value)` | `contains(attr, value)` | `x.Tags.Contains("urgent")` → `contains(#tags, :p0)` |
| `Between(low, high)` | `attr BETWEEN low AND high` | `x.Age.Between(18, 65)` → `#age BETWEEN :p0 AND :p1` |
| `AttributeExists()` | `attribute_exists(attr)` | `x.OptionalField.AttributeExists()` → `attribute_exists(#optionalField)` |
| `AttributeNotExists()` | `attribute_not_exists(attr)` | `x.OptionalField.AttributeNotExists()` → `attribute_not_exists(#optionalField)` |
| `Size()` | `size(attr)` | `x.Items.Size()` → `size(#items)` |

**Implementation Approach:**
- Create extension methods for Between(), AttributeExists(), etc. on common types
- These methods are markers - they're never actually executed
- ExpressionTranslator recognizes these methods and generates appropriate DynamoDB syntax

```csharp
namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Extension methods for DynamoDB expression support.
/// These methods are markers for expression translation and should not be called directly.
/// </summary>
public static class DynamoDbExpressionExtensions
{
    /// <summary>
    /// Generates a BETWEEN condition in DynamoDB expressions.
    /// </summary>
    [ExpressionOnly]
    public static bool Between<T>(this T value, T low, T high) where T : IComparable<T>
        => throw new InvalidOperationException("This method is only for use in expressions");
    
    /// <summary>
    /// Generates an attribute_exists() function in DynamoDB expressions.
    /// </summary>
    [ExpressionOnly]
    public static bool AttributeExists<T>(this T value)
        => throw new InvalidOperationException("This method is only for use in expressions");
    
    /// <summary>
    /// Generates an attribute_not_exists() function in DynamoDB expressions.
    /// </summary>
    [ExpressionOnly]
    public static bool AttributeNotExists<T>(this T value)
        => throw new InvalidOperationException("This method is only for use in expressions");
    
    /// <summary>
    /// Generates a size() function in DynamoDB expressions.
    /// </summary>
    [ExpressionOnly]
    public static int Size<T>(this IEnumerable<T> collection)
        => throw new InvalidOperationException("This method is only for use in expressions");
}

/// <summary>
/// Marks methods that are only valid within expression trees.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ExpressionOnlyAttribute : Attribute { }
```

## Data Models

### Expression Translation Result

```csharp
namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Result of expression translation.
/// </summary>
public class TranslationResult
{
    /// <summary>
    /// The generated DynamoDB expression string.
    /// </summary>
    public string Expression { get; init; } = string.Empty;
    
    /// <summary>
    /// Expression attribute names that were generated.
    /// </summary>
    public Dictionary<string, string> AttributeNames { get; init; } = new();
    
    /// <summary>
    /// Expression attribute values that were generated.
    /// </summary>
    public Dictionary<string, AttributeValue> AttributeValues { get; init; } = new();
}
```

## Error Handling

### Exception Types

```csharp
namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Base exception for expression translation errors.
/// </summary>
public class ExpressionTranslationException : Exception
{
    public Expression? OriginalExpression { get; }
    
    public ExpressionTranslationException(string message, Expression? expression = null)
        : base(message)
    {
        OriginalExpression = expression;
    }
}

/// <summary>
/// Thrown when an expression references an unmapped property.
/// </summary>
public class UnmappedPropertyException : ExpressionTranslationException
{
    public string PropertyName { get; }
    public Type EntityType { get; }
    
    public UnmappedPropertyException(string propertyName, Type entityType, Expression? expression = null)
        : base($"Property '{propertyName}' on type '{entityType.Name}' does not map to a DynamoDB attribute", expression)
    {
        PropertyName = propertyName;
        EntityType = entityType;
    }
}

/// <summary>
/// Thrown when an expression uses an unsupported operator or method.
/// </summary>
public class UnsupportedExpressionException : ExpressionTranslationException
{
    public ExpressionType? ExpressionType { get; }
    public string? MethodName { get; }
    
    public UnsupportedExpressionException(string message, Expression? expression = null)
        : base(message, expression) { }
}

/// <summary>
/// Thrown when a Query().Where() expression references non-key attributes.
/// </summary>
public class InvalidKeyExpressionException : ExpressionTranslationException
{
    public string PropertyName { get; }
    
    public InvalidKeyExpressionException(string propertyName, Expression? expression = null)
        : base($"Property '{propertyName}' is not a key attribute and cannot be used in Query().Where(). Use WithFilter() instead.", expression)
    {
        PropertyName = propertyName;
    }
}
```

### Error Messages

Clear, actionable error messages:

```
// Unmapped property
"Property 'Email' on type 'UserEntity' does not map to a DynamoDB attribute. 
Ensure the property has a [DynamoDbAttribute] or is included in entity configuration."

// Non-key in Query().Where()
"Property 'Status' is not a key attribute and cannot be used in Query().Where(). 
Use WithFilter() to filter on non-key attributes."

// Unsupported operator
"The operator 'Modulo' is not supported in DynamoDB expressions. 
Supported operators: ==, !=, <, >, <=, >=, &&, ||, !"

// Unsupported method on entity property
"Method 'ToUpper()' cannot be used on entity properties in DynamoDB expressions. 
DynamoDB expressions cannot execute C# methods on data. 
Supported methods: StartsWith, Contains, Between, AttributeExists, AttributeNotExists, Size."

// Assignment expression
"Assignment expressions are not supported in DynamoDB queries. 
Use comparison operators (==, <, >, etc.) instead of assignment (=)."

// Method call that references entity parameter
"Method 'myFunction' cannot reference the entity parameter or its properties. 
DynamoDB expressions cannot execute C# methods with entity data. 
Only constants and captured variables are allowed on the right side of comparisons.
Example: 'x => x.Id == userId' is valid, but 'x => x.Id == myFunction(x)' is not."

// Complex expression
"Expression is too complex to translate. Consider using string-based expressions 
with Where(string) or WithFilter(string) for complex scenarios."
```

## Testing Strategy

### Unit Tests

1. **Expression Translation Tests**
   - Test each operator mapping (==, <, >, &&, ||, etc.)
   - Test each method mapping (StartsWith, Contains, Between, etc.)
   - Test nested expressions (a && (b || c))
   - Test value capture (constants, variables, closures)
   - Test parameter generation uniqueness

2. **Validation Tests**
   - Test key-only validation for Query().Where()
   - Test unmapped property detection
   - Test non-queryable property detection
   - Test error message clarity
   - Test rejection of assignment expressions
   - Test rejection of unsupported method calls on entity properties
   - Test rejection of complex transformations (x.Name.ToUpper())

3. **Integration with Existing Infrastructure**
   - Test parameter generation with AttributeValueInternal
   - Test attribute name generation with AttributeNameInternal
   - Test mixing expression-based and string-based calls

4. **Edge Cases**
   - Null value handling
   - Enum value handling
   - DateTime value handling
   - Collection property handling
   - Nullable property handling

5. **Invalid Expression Detection**
   - Test that `x => x.Id = value` is rejected (assignment)
   - Test that `x => x.Id == myFunction(x)` is rejected (method references entity parameter)
   - Test that `x => x.Id == myFunction(x.Name)` is rejected (method references entity property)
   - Test that `x => x.Name.ToUpper() == "JOHN"` is rejected (transformation on entity property)
   - Test that `x => x.Items.Select(i => i.Name)` is rejected (LINQ on entity property)
   - Test that value-side method calls are allowed: `x => x.Id == userId.ToString()` ✓
   - Test that captured closures are allowed: `x => x.Id == user.Id` ✓
   - Test that constants are allowed: `x => x.Id == "USER#123"` ✓

### Integration Tests

1. **End-to-End Query Tests**
   - Query with expression-based Where()
   - Query with expression-based WithFilter()
   - Scan with expression-based WithFilter()
   - Mixed expression and string-based calls

2. **DynamoDB Compatibility Tests**
   - Verify generated expressions work with actual DynamoDB
   - Test against DynamoDB Local
   - Verify parameter values are correctly formatted

3. **AOT Compatibility Tests**
   - Build and run in Native AOT environment
   - Verify no runtime code generation
   - Verify trimming compatibility
   - Test closure captures in AOT (local variables, captured fields)
   - Test generic method expressions in AOT
   - Test complex nested closures in AOT
   - Test with various value types (primitives, strings, DateTime, enums)
   - Verify expression tree reading works in AOT
   - Test that compiled AOT binary produces identical results to JIT

## Performance Considerations

### Caching Strategy

Expression translation can be expensive. Implement caching for repeated expressions:

```csharp
/// <summary>
/// Cache for translated expressions to avoid repeated analysis.
/// </summary>
public class ExpressionCache
{
    private readonly ConcurrentDictionary<ExpressionCacheKey, string> _cache = new();
    
    public string GetOrAdd(
        Expression expression, 
        ExpressionValidationMode mode,
        Func<string> translator)
    {
        var key = new ExpressionCacheKey(expression, mode);
        return _cache.GetOrAdd(key, _ => translator());
    }
}

internal record ExpressionCacheKey(Expression Expression, ExpressionValidationMode Mode);
```

**Caching Considerations:**
- Cache key includes expression and validation mode
- Cache is thread-safe (ConcurrentDictionary)
- Cache does NOT include parameter values (those change per call)
- Cache only stores the expression string template
- Consider cache size limits for long-running applications

### Allocation Optimization

- Reuse StringBuilder for expression building
- Minimize string allocations during tree walking
- Use ArrayPool for temporary collections if needed
- Avoid LINQ in hot paths

### Performance Targets

- Expression translation: < 10ms for typical expressions
- Overhead vs string-based: < 5%
- Memory allocation: < 1KB per translation (excluding cached values)

## Migration Path

### Backward Compatibility

All existing APIs remain unchanged:

```csharp
// Existing string-based (still works)
table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithValue(":pk", userId)
    .WithValue(":prefix", "ORDER#")
    .ExecuteAsync();

// Existing format string (still works)
table.Query
    .Where("pk = {0} AND begins_with(sk, {1})", userId, "ORDER#")
    .ExecuteAsync();

// NEW: Expression-based
table.Query
    .Where<UserEntity>(x => x.PartitionKey == userId && x.SortKey.StartsWith("ORDER#"))
    .ExecuteAsync();
```

### Adoption Strategy

1. **Phase 1**: Release expression support as opt-in
2. **Phase 2**: Document expression patterns and examples
3. **Phase 3**: Encourage adoption through improved IntelliSense and type safety
4. **Phase 4**: Consider deprecating string-based APIs in future major version (optional)

## Implementation Phases

### Phase 1: Core Translation Engine
- Implement ExpressionTranslator
- Implement ExpressionContext
- Support basic operators (==, <, >, &&, ||)
- Support property access and value capture
- Basic error handling

### Phase 2: DynamoDB Functions
- Implement DynamoDbExpressionExtensions
- Support StartsWith, Contains, Between
- Support AttributeExists, AttributeNotExists, Size
- Enhanced error messages

### Phase 3: Validation and Metadata
- Integrate with EntityMetadata
- Implement key-only validation for Query().Where()
- Validate property mappings
- Comprehensive error messages

### Phase 4: Extension Methods
- Add expression overloads to Where()
- Add expression overloads to WithFilter()
- Add expression overloads to WithCondition()
- Integration with existing builders

### Phase 5: Optimization and Caching
- Implement expression caching
- Optimize allocations
- Performance testing and tuning

### Phase 6: AOT Compatibility Testing
- Create test project with Native AOT enabled
- Test closure captures with various scenarios
- Test generic method expressions
- Test complex nested expressions
- Verify identical behavior between JIT and AOT
- Document any AOT-specific limitations discovered

### Phase 7: Documentation and Examples
- XML documentation comments
- Code examples for common patterns
- Migration guide from string-based to expression-based
- Troubleshooting guide
- Document what expressions are valid vs invalid

## Open Questions and Future Considerations

### Questions for Discussion

1. **Metadata Requirement**: Should EntityMetadata be required or optional?
   - Required: Better validation, clearer errors
   - Optional: More flexible, works without source generator
   - **Recommendation**: Optional with degraded validation when absent

2. **Cache Scope**: Should expression cache be per-builder or global?
   - Per-builder: Simpler, no thread safety concerns
   - Global: Better cache hit rate, more memory efficient
   - **Recommendation**: Global with thread-safe implementation

3. **Computed Attributes**: How should computed attributes be handled?
   - Allow in expressions, map to computed attribute name
   - Validate that computed value is queryable
   - **Recommendation**: Allow with validation

### Future Enhancements

1. **Update Expression Support**: Separate spec for SET, REMOVE, ADD, DELETE operations
2. **Projection Expression Support**: Select specific properties using expressions
3. **Advanced Functions**: Support for more DynamoDB functions (attribute_type, etc.)
4. **Expression Composition**: Combine multiple expressions with And()/Or() methods
5. **Source Generator Integration**: Generate expression helpers per entity type
6. **Roslyn Analyzer**: Compile-time validation of expressions

## Dependencies

### Internal Dependencies
- Oproto.FluentDynamoDb.Requests (extension methods)
- Oproto.FluentDynamoDb.Storage (EntityMetadata)
- Oproto.FluentDynamoDb.Utility (AttributeValueConverter)

### External Dependencies
- System.Linq.Expressions (expression tree analysis)
- No new external NuGet packages required

### AOT Compatibility
- All code must be AOT-safe
- No Expression.Compile() usage
- No reflection-emit
- All expression node types handled explicitly
