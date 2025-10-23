# Comprehensive Discriminator System Design

## Overview

This design enhances the discriminator system to support multiple discriminator strategies used in single-table DynamoDB designs:
1. **Attribute-based discriminators** (e.g., `entity_type = "USER"`)
2. **Sort key prefix patterns** (e.g., `SK = "USER#123"`)
3. **GSI-specific discriminators** (different from primary key discriminators)
4. **Composite key patterns** (e.g., `PK = "TENANT#abc", SK = "USER#123"`)

## Problem Statement

Current implementation:
- Hardcodes discriminator property as `"entity_type"`
- Doesn't support sort-key-based discriminators
- Doesn't handle GSI-specific discriminator strategies
- Assumes discriminator is always a simple attribute value match

Real-world single-table designs use various discriminator strategies:
- Sort key prefixes: `USER#`, `ORDER#`, `PRODUCT#`
- Composite patterns: `TENANT#abc#USER#123`
- GSI sort keys with different patterns than primary keys
- Attribute-based types for some queries, key-based for others

## Design Goals

1. **Flexibility**: Support all common discriminator patterns
2. **Backward Compatibility**: Existing code continues to work
3. **Type Safety**: Compile-time validation where possible
4. **Performance**: No runtime reflection or string parsing overhead
5. **Clarity**: Clear, intuitive API for developers

## Discriminator Strategies

### 1. Attribute-Based Discriminator

**Use Case**: Simple type field in the item
```csharp
[DynamoDbTable("MyTable", 
    DiscriminatorProperty = "entity_type",
    DiscriminatorValue = "USER")]
public partial class User { }
```

**DynamoDB Item**:
```json
{
  "PK": "USER#123",
  "SK": "METADATA",
  "entity_type": "USER",
  "name": "John"
}
```

### 2. Sort Key Prefix Pattern

**Use Case**: Entity type encoded in sort key
```csharp
[DynamoDbTable("MyTable",
    DiscriminatorProperty = "SK",
    DiscriminatorPattern = "USER#*")]
public partial class User { }
```

**DynamoDB Item**:
```json
{
  "PK": "TENANT#abc",
  "SK": "USER#123",
  "name": "John"
}
```

### 3. Sort Key Exact Match

**Use Case**: Fixed sort key value for entity type
```csharp
[DynamoDbTable("MyTable",
    DiscriminatorProperty = "SK",
    DiscriminatorValue = "METADATA")]
public partial class UserMetadata { }
```

**DynamoDB Item**:
```json
{
  "PK": "USER#123",
  "SK": "METADATA",
  "email": "john@example.com"
}
```

### 4. GSI-Specific Discriminator

**Use Case**: Different discriminator strategy for GSI queries
```csharp
[DynamoDbTable("MyTable")]
public partial class User 
{
    [GlobalSecondaryIndex("StatusIndex",
        DiscriminatorProperty = "GSI1SK",
        DiscriminatorPattern = "USER#*")]
    public string Status { get; set; }
}
```

**DynamoDB Item**:
```json
{
  "PK": "TENANT#abc",
  "SK": "USER#123",
  "GSI1PK": "STATUS#ACTIVE",
  "GSI1SK": "USER#2024-01-15",
  "name": "John"
}
```

## Attribute Design

### Enhanced DynamoDbTableAttribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class DynamoDbTableAttribute : Attribute
{
    public string TableName { get; }
    
    /// <summary>
    /// The property name containing the discriminator (e.g., "entity_type", "SK", "PK").
    /// If null, no discriminator validation is performed.
    /// </summary>
    public string? DiscriminatorProperty { get; set; }
    
    /// <summary>
    /// The exact value to match for this entity type.
    /// Mutually exclusive with DiscriminatorPattern.
    /// </summary>
    public string? DiscriminatorValue { get; set; }
    
    /// <summary>
    /// A pattern to match for this entity type (supports * wildcard).
    /// Examples: "USER#*", "*#USER#*", "USER"
    /// Mutually exclusive with DiscriminatorValue.
    /// </summary>
    public string? DiscriminatorPattern { get; set; }
    
    /// <summary>
    /// Legacy property for backward compatibility.
    /// Equivalent to setting DiscriminatorProperty="entity_type" and DiscriminatorValue.
    /// </summary>
    [Obsolete("Use DiscriminatorProperty and DiscriminatorValue instead")]
    public string? EntityDiscriminator { get; set; }
}
```

### Enhanced GlobalSecondaryIndexAttribute

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class GlobalSecondaryIndexAttribute : Attribute
{
    public string IndexName { get; }
    public bool IsPartitionKey { get; set; }
    public bool IsSortKey { get; set; }
    
    /// <summary>
    /// GSI-specific discriminator property (overrides table-level discriminator for this GSI).
    /// </summary>
    public string? DiscriminatorProperty { get; set; }
    
    /// <summary>
    /// GSI-specific discriminator value.
    /// </summary>
    public string? DiscriminatorValue { get; set; }
    
    /// <summary>
    /// GSI-specific discriminator pattern.
    /// </summary>
    public string? DiscriminatorPattern { get; set; }
}
```

## Pattern Matching Logic

### Pattern Syntax

- `*` - Wildcard matching zero or more characters
- Exact string - Must match exactly
- No regex support (for performance and simplicity)

### Examples

| Pattern | Matches | Doesn't Match |
|---------|---------|---------------|
| `USER#*` | `USER#123`, `USER#abc` | `ADMIN#123`, `USER` |
| `*#USER#*` | `TENANT#abc#USER#123` | `USER#123`, `TENANT#abc` |
| `USER` | `USER` | `USER#123`, `USERS` |
| `*USER*` | `USER`, `USER#123`, `ADMIN#USER` | `USR`, `ADMINS` |

### Generated Matching Code

For attribute-based exact match:
```csharp
public static bool MatchesDiscriminator(Dictionary<string, AttributeValue> item)
{
    if (!item.TryGetValue("entity_type", out var attr))
        return false;
    return attr.S == "USER";
}
```

For sort key pattern:
```csharp
public static bool MatchesDiscriminator(Dictionary<string, AttributeValue> item)
{
    if (!item.TryGetValue("SK", out var attr))
        return false;
    var value = attr.S;
    return value != null && value.StartsWith("USER#");
}
```

For complex pattern `*#USER#*`:
```csharp
public static bool MatchesDiscriminator(Dictionary<string, AttributeValue> item)
{
    if (!item.TryGetValue("SK", out var attr))
        return false;
    var value = attr.S;
    return value != null && value.Contains("#USER#");
}
```

## Source Generator Changes

### EntityModel Enhancement

```csharp
public class EntityModel
{
    // ... existing properties ...
    
    /// <summary>
    /// Discriminator configuration for the entity.
    /// </summary>
    public DiscriminatorConfig? Discriminator { get; set; }
}

public class DiscriminatorConfig
{
    /// <summary>
    /// The property name containing the discriminator (e.g., "entity_type", "SK").
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// The exact value to match (if using exact match strategy).
    /// </summary>
    public string? ExactValue { get; set; }
    
    /// <summary>
    /// The pattern to match (if using pattern match strategy).
    /// </summary>
    public string? Pattern { get; set; }
    
    /// <summary>
    /// The matching strategy to use.
    /// </summary>
    public DiscriminatorStrategy Strategy { get; set; }
}

public enum DiscriminatorStrategy
{
    None,
    ExactMatch,
    StartsWith,
    EndsWith,
    Contains,
    Custom
}
```

### IndexModel Enhancement

```csharp
public class IndexModel
{
    // ... existing properties ...
    
    /// <summary>
    /// GSI-specific discriminator configuration (overrides entity-level discriminator).
    /// </summary>
    public DiscriminatorConfig? GsiDiscriminator { get; set; }
}
```

### ProjectionModel Enhancement

```csharp
public class ProjectionModel
{
    // ... existing properties ...
    
    /// <summary>
    /// Discriminator configuration for the projection (inherited from source entity).
    /// </summary>
    public DiscriminatorConfig? Discriminator { get; set; }
    
    /// <summary>
    /// GSI-specific discriminator if this projection is for a specific GSI.
    /// </summary>
    public DiscriminatorConfig? GsiDiscriminator { get; set; }
}
```

## Projection Expression Generation

### Including Discriminator in Projection

The projection expression must include the discriminator property:

```csharp
public static string GenerateProjectionExpression(ProjectionModel projection)
{
    var attributeNames = new List<string>();
    
    // Add projection properties
    foreach (var property in projection.Properties)
    {
        if (!string.IsNullOrEmpty(property.AttributeName))
            attributeNames.Add(property.AttributeName);
    }
    
    // Add discriminator property if configured
    if (projection.Discriminator != null)
    {
        var discriminatorAttr = projection.Discriminator.PropertyName;
        if (!attributeNames.Contains(discriminatorAttr))
            attributeNames.Add(discriminatorAttr);
    }
    
    // Add GSI discriminator if different from entity discriminator
    if (projection.GsiDiscriminator != null && 
        projection.GsiDiscriminator.PropertyName != projection.Discriminator?.PropertyName)
    {
        var gsiDiscriminatorAttr = projection.GsiDiscriminator.PropertyName;
        if (!attributeNames.Contains(gsiDiscriminatorAttr))
            attributeNames.Add(gsiDiscriminatorAttr);
    }
    
    return string.Join(", ", attributeNames);
}
```

## Hydration with Discriminator Validation

### Generated FromDynamoDb Method

```csharp
public static TProjection FromDynamoDb(Dictionary<string, AttributeValue> item)
{
    if (item == null)
        throw new ArgumentNullException(nameof(item));
    
    try
    {
        // Validate discriminator
        if (!MatchesDiscriminator(item))
        {
            throw DiscriminatorMismatchException.Create(
                typeof(TProjection),
                expectedDiscriminator: "USER#*",
                actualDiscriminator: GetDiscriminatorValue(item));
        }
        
        var projection = new TProjection();
        // ... property mapping ...
        return projection;
    }
    catch (Exception ex) when (ex is not DiscriminatorMismatchException)
    {
        throw new DynamoDbMappingException(...);
    }
}

private static bool MatchesDiscriminator(Dictionary<string, AttributeValue> item)
{
    if (!item.TryGetValue("SK", out var attr))
        return false;
    var value = attr.S;
    return value != null && value.StartsWith("USER#");
}

private static string? GetDiscriminatorValue(Dictionary<string, AttributeValue> item)
{
    if (!item.TryGetValue("SK", out var attr))
        return null;
    return attr.S;
}
```

## Query Extensions Enhancement

### Multi-Entity Query Support

```csharp
public static async Task<List<TResult>> ToListAsync<TResult>(
    this QueryRequestBuilder builder,
    CancellationToken cancellationToken = default)
    where TResult : class, new()
{
    // Apply projection
    builder = ApplyProjectionIfNeeded<TResult>(builder);
    
    // Execute query
    var response = await builder.ExecuteAsync(cancellationToken);
    
    // Hydrate with discriminator filtering
    return HydrateResults<TResult>(response.Items);
}

private static List<TResult> HydrateResults<TResult>(List<Dictionary<string, AttributeValue>> items)
    where TResult : class, new()
{
    var results = new List<TResult>();
    var fromDynamoDbMethod = GetFromDynamoDbMethod<TResult>();
    
    foreach (var item in items)
    {
        try
        {
            // FromDynamoDb will validate discriminator and throw if mismatch
            var result = fromDynamoDbMethod.Invoke(null, new object[] { item }) as TResult;
            if (result != null)
                results.Add(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is DiscriminatorMismatchException)
        {
            // Item doesn't match this projection type - skip it
            // This is expected in multi-entity queries
            continue;
        }
        catch (Exception ex)
        {
            // Other errors should propagate
            throw;
        }
    }
    
    return results;
}
```

## Migration Path

### Backward Compatibility

1. **Legacy `EntityDiscriminator` property**: Automatically maps to new system
   ```csharp
   // Old way (still works)
   [DynamoDbTable("MyTable", EntityDiscriminator = "USER")]
   
   // Equivalent to:
   [DynamoDbTable("MyTable", 
       DiscriminatorProperty = "entity_type",
       DiscriminatorValue = "USER")]
   ```

2. **No discriminator specified**: No validation performed (current behavior)

3. **Gradual adoption**: Can mix old and new styles in same codebase

### Migration Examples

#### Example 1: Simple Attribute Discriminator
```csharp
// Before
[DynamoDbTable("MyTable", EntityDiscriminator = "USER")]
public partial class User { }

// After (explicit)
[DynamoDbTable("MyTable",
    DiscriminatorProperty = "entity_type",
    DiscriminatorValue = "USER")]
public partial class User { }
```

#### Example 2: Sort Key Pattern
```csharp
// Before (not supported)
// Had to manually filter results

// After
[DynamoDbTable("MyTable",
    DiscriminatorProperty = "SK",
    DiscriminatorPattern = "USER#*")]
public partial class User { }
```

#### Example 3: GSI-Specific Discriminator
```csharp
// Before (not supported)

// After
[DynamoDbTable("MyTable")]
public partial class User 
{
    [GlobalSecondaryIndex("StatusIndex",
        DiscriminatorProperty = "GSI1SK",
        DiscriminatorPattern = "USER#*")]
    public string Status { get; set; }
}
```

## Performance Considerations

1. **Pattern Compilation**: Patterns are analyzed at compile-time and converted to optimal runtime checks
2. **No Regex**: Simple string operations (StartsWith, Contains, EndsWith) for performance
3. **Early Exit**: Discriminator check happens before property mapping
4. **Zero Allocation**: Pattern matching uses string methods that don't allocate

## Testing Strategy

### Unit Tests

1. Pattern parsing and validation
2. Discriminator matching logic generation
3. Projection expression inclusion of discriminator properties
4. Backward compatibility with legacy EntityDiscriminator

### Integration Tests

1. Attribute-based discriminator queries
2. Sort key pattern matching
3. GSI-specific discriminator queries
4. Multi-entity queries with mixed discriminator strategies
5. Migration from legacy to new discriminator system

## Open Questions

1. **Should we support multiple discriminators?** (e.g., both PK and SK patterns)
2. **Should we support OR logic?** (e.g., match "USER#*" OR "ADMIN#*")
3. **Should we support custom discriminator functions?** (for complex logic)
4. **How to handle discriminator in composite entity queries?**

## Future Enhancements

1. **Discriminator inference**: Automatically detect discriminator patterns from entity definitions
2. **Query optimization**: Use discriminator in query conditions when possible
3. **Discriminator registry**: Central registry of all discriminators for debugging
4. **Visual tooling**: IDE support for visualizing discriminator patterns
