# Design Document

## Overview

This design transforms the Oproto.FluentDynamoDb library from property-based builder access to method-based builder access, preparing the API for future LINQ expression support while simplifying the handling of different table schemas. The refactoring addresses three key pain points:

1. **LINQ Preparation**: Property-based access (`table.Query.Where()`) cannot naturally evolve to accept lambda expressions, while method-based access (`table.Query().Where()`) can easily become `table.Query(x => x.Id == id)`

2. **Key Complexity**: Current Get operations require different approaches for single vs composite keys, while method-based access allows natural overloading: `Get(pk)` vs `Get(pk, sk)`

3. **Index Simplification**: Generic index types (`DynamoDbIndex<T>`) create complexity when the same index is used by multiple entity types, while method-based queries can specify the result type at query time

The refactoring maintains full backward compatibility through deprecation warnings while providing a cleaner, more intuitive API.

## Architecture

### Current Architecture

```csharp
// Property-based access
public abstract class DynamoDbTableBase
{
    public QueryRequestBuilder Query => new QueryRequestBuilder(DynamoDbClient).ForTable(Name);
    public GetItemRequestBuilder Get => new GetItemRequestBuilder(DynamoDbClient).ForTable(Name);
    public UpdateItemRequestBuilder Update => new UpdateItemRequestBuilder(DynamoDbClient).ForTable(Name);
    public DeleteItemRequestBuilder Delete => new DeleteItemRequestBuilder(DynamoDbClient).ForTable(Name);
}

// Usage - Old style
var results = await table.Query
    .Where("pk = :pk")
    .WithValue(":pk", "USER#123")
    .ExecuteAsync();

// Usage - Modern format string style
var results = await table.Query
    .Where("pk = {0}", "USER#123")
    .ExecuteAsync();

var item = await table.Get
    .WithKey("id", "123")
    .ExecuteAsync();
```

**Problems:**
- Cannot evolve to `table.Query(x => x.Id == id)` - properties don't accept parameters
- Get operations require separate `.WithKey()` call regardless of table schema
- No way to provide key values or expressions at the point of builder instantiation
- Cannot have both property and method with same name (C# limitation)

### New Method-Based Architecture

```csharp
// Method-based access (BREAKING CHANGE - properties removed)
public abstract class DynamoDbTableBase
{
    // Query with optional format string expression
    public QueryRequestBuilder Query() => 
        new QueryRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    public QueryRequestBuilder Query(string keyConditionExpression, params object[] values) => 
        Query().Where(keyConditionExpression, values);
    
    // Get methods - base class provides parameterless version only
    // Derived classes (manual or generated) provide typed overloads
    public virtual GetItemRequestBuilder Get() => 
        new GetItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    // Update methods - base class provides parameterless version only
    public virtual UpdateItemRequestBuilder Update() => 
        new UpdateItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    // Delete methods - base class provides parameterless version only
    public virtual DeleteItemRequestBuilder Delete() => 
        new DeleteItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    // Put remains simple
    public PutItemRequestBuilder Put() => 
        new PutItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
}

// Manual table definition example - single partition key
public class UsersTable : DynamoDbTableBase
{
    public UsersTable(IAmazonDynamoDB client) : base(client, "Users") { }
    
    // Override to provide key-specific overload
    public GetItemRequestBuilder Get(string userId) => 
        base.Get().WithKey("id", userId);
    
    public UpdateItemRequestBuilder Update(string userId) => 
        base.Update().WithKey("id", userId);
    
    public DeleteItemRequestBuilder Delete(string userId) => 
        base.Delete().WithKey("id", userId);
}

// Manual table definition example - composite key
public class TransactionsTable : DynamoDbTableBase
{
    public TransactionsTable(IAmazonDynamoDB client) : base(client, "Transactions") { }
    
    // Override to provide key-specific overloads
    public GetItemRequestBuilder Get(string pk, string sk) => 
        base.Get().WithKey("pk", pk, "sk", sk);
    
    public UpdateItemRequestBuilder Update(string pk, string sk) => 
        base.Update().WithKey("pk", pk, "sk", sk);
    
    public DeleteItemRequestBuilder Delete(string pk, string sk) => 
        base.Delete().WithKey("pk", pk, "sk", sk);
}

// Usage - New API with format strings
var results = await table.Query("pk = {0}", "USER#123").ExecuteAsync();

var results = await table.Query("pk = {0} AND sk > {1}", "USER#123", "2024-01-01").ExecuteAsync();

var item = await table.Get("123").ExecuteAsync();

var item = await table.Get("USER#123", "PROFILE").ExecuteAsync();

// Future LINQ support (not in this spec, but enabled by this design)
var results = await table.Query(x => x.Status == "ACTIVE").ToListAsync();
```

**Benefits:**
- Natural path to LINQ: `Query()` → `Query(expression, params)` → `Query(lambda)`
- Format string expressions at query instantiation: `Query("pk = {0}", value)`
- Key values provided at instantiation: `Get(id)` vs `Get().WithKey(id)`
- Source generator creates correct overloads based on actual table schema
- Cleaner API for common cases while maintaining flexibility
- **BREAKING CHANGE**: Property-based access removed (this is v2.0)

## Components and Interfaces

### DynamoDbTableBase Modifications

```csharp
public abstract class DynamoDbTableBase : IDynamoDbTable
{
    // Existing constructor and properties remain unchanged
    public IAmazonDynamoDB DynamoDbClient { get; private init; }
    public string Name { get; private init; }
    protected IDynamoDbLogger Logger { get; private init; }
    protected IFieldEncryptor? FieldEncryptor { get; private init; }
    
    // NEW: Method-based builder access
    /// <summary>
    /// Creates a new Query operation builder for this table.
    /// Use this to query items using the primary key or a secondary index.
    /// </summary>
    /// <returns>A QueryRequestBuilder configured for this table.</returns>
    /// <example>
    /// <code>
    /// // Manual query configuration
    /// var results = await table.Query()
    ///     .Where("pk = {0}", "USER#123")
    ///     .ExecuteAsync();
    /// 
    /// // Or use the expression overload
    /// var results = await table.Query("pk = {0}", "USER#123").ExecuteAsync();
    /// </code>
    /// </example>
    public QueryRequestBuilder Query() => 
        new QueryRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    /// <summary>
    /// Creates a new Query operation builder with a key condition expression.
    /// Uses format string syntax for parameters: {0}, {1}, etc.
    /// </summary>
    /// <param name="keyConditionExpression">The key condition expression with format placeholders.</param>
    /// <param name="values">The values to substitute into the expression.</param>
    /// <returns>A QueryRequestBuilder configured with the key condition.</returns>
    /// <example>
    /// <code>
    /// // Simple partition key query
    /// var results = await table.Query("pk = {0}", "USER#123").ExecuteAsync();
    /// 
    /// // Composite key query
    /// var results = await table.Query("pk = {0} AND sk > {1}", "USER#123", "2024-01-01").ExecuteAsync();
    /// 
    /// // With begins_with
    /// var results = await table.Query("pk = {0} AND begins_with(sk, {1})", "USER#123", "ORDER#").ExecuteAsync();
    /// </code>
    /// </example>
    public QueryRequestBuilder Query(string keyConditionExpression, params object[] values) => 
        Query().Where(keyConditionExpression, values);
    
    /// <summary>
    /// Creates a new GetItem operation builder for this table.
    /// Base implementation provides parameterless version.
    /// Derived classes should override to provide key-specific overloads.
    /// </summary>
    /// <returns>A GetItemRequestBuilder configured for this table.</returns>
    /// <example>
    /// <code>
    /// // Manual key configuration
    /// var item = await table.Get()
    ///     .WithKey("id", "123")
    ///     .WithProjection("name, email")
    ///     .ExecuteAsync();
    /// 
    /// // Or use derived class overload (if available)
    /// var item = await table.Get("123").ExecuteAsync();
    /// </code>
    /// </example>
    public virtual GetItemRequestBuilder Get() => 
        new GetItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    /// <summary>
    /// Creates a new UpdateItem operation builder for this table.
    /// Base implementation provides parameterless version.
    /// Derived classes should override to provide key-specific overloads.
    /// </summary>
    /// <returns>An UpdateItemRequestBuilder configured for this table.</returns>
    public virtual UpdateItemRequestBuilder Update() => 
        new UpdateItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    /// <summary>
    /// Creates a new DeleteItem operation builder for this table.
    /// Base implementation provides parameterless version.
    /// Derived classes should override to provide key-specific overloads.
    /// </summary>
    /// <returns>A DeleteItemRequestBuilder configured for this table.</returns>
    public virtual DeleteItemRequestBuilder Delete() => 
        new DeleteItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    /// <summary>
    /// Creates a new PutItem operation builder for this table.
    /// </summary>
    /// <returns>A PutItemRequestBuilder configured for this table.</returns>
    public PutItemRequestBuilder Put() => 
        new PutItemRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
    
    // Existing AsScannable() method remains unchanged
    public IScannableDynamoDbTable AsScannable() => new ScannableDynamoDbTable(this);
}

// Example derived class implementations

/// <summary>
/// Example: Manual table with single partition key
/// </summary>
public class UsersTable : DynamoDbTableBase
{
    public UsersTable(IAmazonDynamoDB client) : base(client, "Users") { }
    
    // Override Get to provide key-specific overload
    public GetItemRequestBuilder Get(string userId) => 
        base.Get().WithKey("id", userId);
    
    public UpdateItemRequestBuilder Update(string userId) => 
        base.Update().WithKey("id", userId);
    
    public DeleteItemRequestBuilder Delete(string userId) => 
        base.Delete().WithKey("id", userId);
}

/// <summary>
/// Example: Manual table with composite key
/// </summary>
public class TransactionsTable : DynamoDbTableBase
{
    public TransactionsTable(IAmazonDynamoDB client) : base(client, "Transactions") { }
    
    // Override Get to provide key-specific overload
    public GetItemRequestBuilder Get(string pk, string sk) => 
        base.Get().WithKey("pk", pk, "sk", sk);
    
    public UpdateItemRequestBuilder Update(string pk, string sk) => 
        base.Update().WithKey("pk", pk, "sk", sk);
    
    public DeleteItemRequestBuilder Delete(string pk, string sk) => 
        base.Delete().WithKey("pk", pk, "sk", sk);
}
```

### DynamoDbIndex Modifications

```csharp
/// <summary>
/// Represents a DynamoDB Global Secondary Index (GSI) or Local Secondary Index (LSI).
/// Provides method-based access to query operations using expression strings.
/// </summary>
public class DynamoDbIndex
{
    private readonly DynamoDbTableBase _table;
    private readonly string? _projectionExpression;

    /// <summary>
    /// Initializes a new instance of the DynamoDbIndex.
    /// </summary>
    /// <param name="table">The parent table that contains this index.</param>
    /// <param name="indexName">The name of the index as defined in DynamoDB.</param>
    public DynamoDbIndex(DynamoDbTableBase table, string indexName)
    {
        _table = table;
        Name = indexName;
        _projectionExpression = null;
    }

    /// <summary>
    /// Initializes a new instance of the DynamoDbIndex with a projection expression.
    /// </summary>
    /// <param name="table">The parent table that contains this index.</param>
    /// <param name="indexName">The name of the index as defined in DynamoDB.</param>
    /// <param name="projectionExpression">The projection expression to automatically apply to queries.</param>
    public DynamoDbIndex(DynamoDbTableBase table, string indexName, string projectionExpression)
    {
        _table = table;
        Name = indexName;
        _projectionExpression = projectionExpression;
    }

    public string Name { get; private init; }

    /// <summary>
    /// Creates a new Query operation builder for this index.
    /// Use this when you need to manually configure the query.
    /// </summary>
    /// <returns>A QueryRequestBuilder configured for this index.</returns>
    /// <example>
    /// <code>
    /// var results = await index.Query()
    ///     .Where("gsi1pk = {0}", "STATUS#ACTIVE")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public QueryRequestBuilder Query()
    {
        var builder = new QueryRequestBuilder(_table.DynamoDbClient)
            .ForTable(_table.Name)
            .UsingIndex(Name);

        if (!string.IsNullOrEmpty(_projectionExpression))
        {
            builder = builder.WithProjection(_projectionExpression);
        }

        return builder;
    }
    
    /// <summary>
    /// Creates a new Query operation builder with a key condition expression.
    /// Uses format string syntax for parameters: {0}, {1}, etc.
    /// </summary>
    /// <param name="keyConditionExpression">The key condition expression with format placeholders.</param>
    /// <param name="values">The values to substitute into the expression.</param>
    /// <returns>A QueryRequestBuilder configured with the key condition.</returns>
    /// <example>
    /// <code>
    /// // Simple partition key query
    /// var results = await index.Query("gsi1pk = {0}", "STATUS#ACTIVE").ExecuteAsync();
    /// 
    /// // Composite key query
    /// var results = await index.Query("gsi1pk = {0} AND gsi1sk > {1}", "STATUS#ACTIVE", "2024-01-01").ExecuteAsync();
    /// 
    /// // With begins_with
    /// var results = await index.Query("gsi1pk = {0} AND begins_with(gsi1sk, {1})", "STATUS#ACTIVE", "USER#").ExecuteAsync();
    /// </code>
    /// </example>
    public QueryRequestBuilder Query(string keyConditionExpression, params object[] values)
    {
        return Query().Where(keyConditionExpression, values);
    }
}

/// <summary>
/// Generic DynamoDB index with a default projection type.
/// Maintained for backward compatibility but simplified to use method-based queries with expression strings.
/// </summary>
/// <typeparam name="TDefault">The default projection/entity type for this index.</typeparam>
public class DynamoDbIndex<TDefault> where TDefault : class, new()
{
    private readonly DynamoDbIndex _innerIndex;

    public DynamoDbIndex(
        DynamoDbTableBase table,
        string indexName,
        string? projectionExpression = null)
    {
        _innerIndex = new DynamoDbIndex(table, indexName, projectionExpression);
    }

    public string Name => _innerIndex.Name;

    /// <summary>
    /// Creates a new Query operation builder for this index.
    /// </summary>
    public QueryRequestBuilder Query() => _innerIndex.Query();
    
    /// <summary>
    /// Creates a new Query operation builder with a key condition expression.
    /// Uses format string syntax for parameters: {0}, {1}, etc.
    /// </summary>
    public QueryRequestBuilder Query(string keyConditionExpression, params object[] values) => 
        _innerIndex.Query(keyConditionExpression, values);
    
    // QueryAsync methods for backward compatibility with generic index usage
    public async Task<List<TDefault>> QueryAsync(
        Action<QueryRequestBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        var builder = Query();
        configure(builder);
        var response = await builder.ExecuteAsync(cancellationToken);
        // TODO: Implement proper hydration in future tasks
        return new List<TDefault>();
    }
    
    public async Task<List<TResult>> QueryAsync<TResult>(
        Action<QueryRequestBuilder> configure,
        CancellationToken cancellationToken = default)
        where TResult : class, new()
    {
        var builder = Query();
        configure(builder);
        var response = await builder.ExecuteAsync(cancellationToken);
        // TODO: Implement proper hydration in future tasks
        return new List<TResult>();
    }
}
```

### Source Generator Modifications

The source generator will be updated to generate method-based table classes with appropriate overloads:

```csharp
// Generated code example for a table with single partition key
[DynamoDbTable("Users")]
public partial class UsersTable : DynamoDbTableBase
{
    public UsersTable(IAmazonDynamoDB client) : base(client, "Users") { }
    
    // Generated Get overload for single key
    public GetItemRequestBuilder Get(string id) => 
        base.Get().WithKey("id", id);
    
    // Generated Update overload for single key
    public UpdateItemRequestBuilder Update(string id) => 
        base.Update().WithKey("id", id);
    
    // Generated Delete overload for single key
    public DeleteItemRequestBuilder Delete(string id) => 
        base.Delete().WithKey("id", id);
    
    // Indexes with method-based access and key names
    public DynamoDbIndex EmailIndex => new DynamoDbIndex(
        this, 
        "EmailIndex", 
        "email",  // partition key name for Query(email) overload
        null,     // no sort key
        "id, email, name"  // projection
    );
}

// Generated code example for a table with composite key
[DynamoDbTable("Transactions")]
public partial class TransactionsTable : DynamoDbTableBase
{
    public TransactionsTable(IAmazonDynamoDB client) : base(client, "Transactions") { }
    
    // Generated Get overload for composite key
    public GetItemRequestBuilder Get(string pk, string sk) => 
        base.Get().WithKey("pk", pk, "sk", sk);
    
    // Generated Update overload for composite key
    public UpdateItemRequestBuilder Update(string pk, string sk) => 
        base.Update().WithKey("pk", pk, "sk", sk);
    
    // Generated Delete overload for composite key
    public DeleteItemRequestBuilder Delete(string pk, string sk) => 
        base.Delete().WithKey("pk", pk, "sk", sk);
    
    // Indexes with projection expression
    public DynamoDbIndex StatusIndex => new DynamoDbIndex(
        this, 
        "StatusIndex", 
        "id, amount, status, entity_type"  // projection
    );
}
```

**Key Points:**
- Source generator analyzes entity attributes to determine key structure
- Only generates the appropriate overload (single key OR composite key, not both)
- Index definitions use projection expressions for automatic field selection
- Index queries use expression strings consistent with table queries
- No abstract PartitionKeyName/SortKeyName properties needed - key names are embedded in generated methods

## Data Models

No new data models are required. The existing request builder classes remain unchanged internally.

## Error Handling

### Expression String Errors

```csharp
// Invalid expression string format
try
{
    await index.Query("gsi1pk = {0} AND invalid syntax", "value").ExecuteAsync();
}
catch (ArgumentException ex)
{
    // Error from expression parsing
}
```

### Compilation Errors (Breaking Changes)

```csharp
// Old property-based access no longer compiles
var results = await table.Query  // Compilation error: 'Query' is a method, not a property
    .Where("pk = :pk")
    .WithValue(":pk", "USER#123")
    .ExecuteAsync();

// Must be changed to:
var results = await table.Query()
    .Where("pk = {0}", "USER#123")
    .ExecuteAsync();

// Or use the expression overload:
var results = await table.Query("pk = {0}", "USER#123").ExecuteAsync();
```

## Testing Strategy

### Unit Tests

1. **Method-based builder instantiation**
   - Verify Query(), Get(), Update(), Delete(), Put() return correct builder types
   - Test builder configuration (table name, logger) is properly set

2. **Key parameter overloads**
   - Test Get(pk), Get(pk, sk) configure keys correctly
   - Test Update(pk), Update(pk, sk) configure keys correctly
   - Test Delete(pk), Delete(pk, sk) configure keys correctly
   - Verify error handling for mismatched key configurations

3. **Index query methods**
   - Test Query() returns properly configured builder
   - Test Query(expression, values) configures key condition correctly
   - Test expression strings with partition key only
   - Test expression strings with composite keys
   - Test expression strings with various operators (=, >, <, >=, <=, begins_with, between)
   - Verify projection expressions are automatically applied

4. **Deprecation warnings**
   - Verify [Obsolete] attributes are present
   - Test that deprecated properties still work
   - Verify warning messages are helpful

### Integration Tests

1. **End-to-end operations**
   - Test complete query workflows with method-based API
   - Test Get/Update/Delete operations with key parameters
   - Test index queries with various key configurations
   - Verify results match property-based API behavior

2. **Mixed usage scenarios**
   - Test using both deprecated and new APIs in same codebase
   - Verify no conflicts or unexpected behavior

3. **Source generator output**
   - Test generated tables have correct method signatures
   - Verify generated key name properties
   - Test generated index configurations

### Breaking Change Tests

1. **Property-based access removed**
   - Verify property-based access no longer compiles
   - Confirm method-based access works correctly
   - Test migration path from old to new API

## Implementation Notes

### Breaking Change

This refactoring removes property-based builder access entirely. Since the library is unreleased, no migration strategy is needed.

### Implementation Order

1. Update DynamoDbTableBase with method-based API
2. Update DynamoDbIndex classes
3. Create example manual table implementations
4. Update source generator
5. Update tests and documentation
6. Update RealworldExample code

## Performance Considerations

### Method Call Overhead
- Method-based access has identical performance to property-based
- Both create new builder instances on each call
- No additional allocations or overhead

### Key Pre-Configuration
- Get(pk) and similar methods add one additional method call
- Overhead is negligible compared to network I/O
- Convenience outweighs minimal performance cost

## Security Considerations

### No Security Impact
- This is purely an API surface change
- No changes to how data is transmitted or stored
- No new security considerations introduced

## Future LINQ Support

This design enables future LINQ expression support:

```csharp
// Current (after this spec)
var results = await table.Query()
    .Where("pk = :pk AND #status = :status")
    .WithValue(":pk", "USER#123")
    .WithValue(":status", "ACTIVE")
    .WithAttribute("#status", "status")
    .ExecuteAsync();

// Future (enabled by method-based API)
var results = await table.Query(x => x.PartitionKey == "USER#123" && x.Status == "ACTIVE")
    .ToListAsync();

// Future (with projection)
var results = await table.Query(x => x.PartitionKey == "USER#123")
    .Select(x => new { x.Id, x.Name })
    .ToListAsync();
```

The method-based API provides the foundation for this evolution without requiring breaking changes.
