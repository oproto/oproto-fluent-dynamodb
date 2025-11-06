# Design Document

## Overview

This design addresses the 170 remaining build errors in the integration test project. The errors fall into four main categories:

1. **Missing Table Accessors** (132 errors): Generated table classes lack accessor properties for entity types
2. **Missing Entity Types** (18 errors): Test entities referenced but not defined
3. **Missing API Methods** (18 errors): Request builder methods not implemented
4. **Missing Test Infrastructure** (2 errors): Test helper types not defined

The solution involves creating missing test entities, ensuring the source generator produces required table accessors, implementing missing API methods, and adding test infrastructure.

## Architecture

### Component Overview

```
Integration Test Project
├── TestEntities/
│   ├── MultiEntityOrderTestEntity.cs (NEW)
│   ├── MultiEntityOrderLineEntity.cs (NEW)
│   ├── TransactionOrderEntity.cs (NEW)
│   ├── TransactionOrderLineEntity.cs (NEW)
│   └── TransactionPaymentTestEntity.cs (NEW)
├── TableGeneration/
│   ├── MultiEntityTableTests.cs (EXISTING - expects generated table)
│   └── TransactionOperationTests.cs (EXISTING - expects generated table)
└── RealWorld/
    └── OperationContextIntegrationTests.cs (EXISTING - needs API methods)

Source Generator
├── Generators/
│   └── TableGenerator.cs (VERIFY - should generate accessors)
└── Analysis/
    └── EntityAnalyzer.cs (VERIFY - should detect all entities)

Main Library
└── Requests/
    ├── UpdateItemRequestBuilder.cs (ADD - ReturnValues method)
    ├── PutItemRequestBuilder.cs (ADD - ReturnValues method)
    ├── TransactGetItemsRequestBuilder.cs (ADD - ToDynamoDbResponseAsync)
    └── TransactWriteItemsRequestBuilder.cs (ADD - ToDynamoDbResponseAsync)
```

### Error Category Breakdown

**CS1061 Errors (132 total):**
- 46: `MultiEntityTestTable.Orders` accessor missing
- 40: `TransactionTestTable.Orders` accessor missing
- 16: `TransactionTestTable.OrderLines` accessor missing
- 10: `MultiEntityTestTable.OrderLines` accessor missing
- 8: `MultiEntityOrderTestEntity.Item` property missing
- 4: `UpdateItemRequestBuilder<T>.ReturnValues()` method missing
- 2: `TransactionTestTable.Payments` accessor missing
- 2: `TransactWriteItemsRequestBuilder.ToDynamoDbResponseAsync()` missing
- 2: `TransactGetItemsRequestBuilder.ToDynamoDbResponseAsync()` missing
- 2: `PutItemRequestBuilder<T>.ReturnValues()` method missing

**CS1501 Errors (18 total):**
- Generic method type inference failures (likely related to missing entity types)

**CS0411 Errors (18 total):**
- Cannot infer type arguments (likely related to missing entity types)

**CS0103 Errors (2 total):**
- `TransactionPaymentTestEntity` type not defined

## Components and Interfaces

### 1. Test Entity Definitions

#### MultiEntityOrderTestEntity
```csharp
[DynamoDbEntity]
[DynamoDbTable("MultiEntityTestTable")]
public partial class MultiEntityOrderTestEntity
{
    [PartitionKey]
    public string Id { get; set; } = string.Empty;
    
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Item { get; set; } = string.Empty; // Referenced in tests
}
```

#### MultiEntityOrderLineEntity
```csharp
[DynamoDbEntity]
[DynamoDbTable("MultiEntityTestTable")]
public partial class MultiEntityOrderLineEntity
{
    [PartitionKey]
    public string Id { get; set; } = string.Empty;
    
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
```

#### TransactionOrderEntity
```csharp
[DynamoDbEntity]
[DynamoDbTable("TransactionTestTable")]
public partial class TransactionOrderEntity
{
    [PartitionKey]
    public string Id { get; set; } = string.Empty;
    
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
```

#### TransactionOrderLineEntity
```csharp
[DynamoDbEntity]
[DynamoDbTable("TransactionTestTable")]
public partial class TransactionOrderLineEntity
{
    [PartitionKey]
    public string Id { get; set; } = string.Empty;
    
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
```

#### TransactionPaymentTestEntity
```csharp
[DynamoDbEntity]
[DynamoDbTable("TransactionTestTable")]
public partial class TransactionPaymentTestEntity
{
    [PartitionKey]
    public string Id { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}
```

### 2. Generated Table Classes (Source Generator Output)

The source generator should produce:

#### MultiEntityTestTable (Generated)
```csharp
public partial class MultiEntityTestTable : DynamoDbTableBase
{
    public MultiEntityTestTable(IAmazonDynamoDB client, string tableName) 
        : base(client, tableName) { }
    
    // Generated accessor properties
    public EntityAccessor<MultiEntityOrderTestEntity> Orders => 
        new EntityAccessor<MultiEntityOrderTestEntity>(this);
    
    public EntityAccessor<MultiEntityOrderLineEntity> OrderLines => 
        new EntityAccessor<MultiEntityOrderLineEntity>(this);
}
```

#### TransactionTestTable (Generated)
```csharp
public partial class TransactionTestTable : DynamoDbTableBase
{
    public TransactionTestTable(IAmazonDynamoDB client, string tableName) 
        : base(client, tableName) { }
    
    // Generated accessor properties
    public EntityAccessor<TransactionOrderEntity> Orders => 
        new EntityAccessor<TransactionOrderEntity>(this);
    
    public EntityAccessor<TransactionOrderLineEntity> OrderLines => 
        new EntityAccessor<TransactionOrderLineEntity>(this);
    
    public EntityAccessor<TransactionPaymentTestEntity> Payments => 
        new EntityAccessor<TransactionPaymentTestEntity>(this);
}
```

### 3. Request Builder API Extensions

#### UpdateItemRequestBuilder<T>
```csharp
public class UpdateItemRequestBuilder<T> where T : class
{
    // Existing methods...
    
    /// <summary>
    /// Specifies which values to return in the response.
    /// </summary>
    public UpdateItemRequestBuilder<T> ReturnValues(ReturnValue returnValue)
    {
        _request.ReturnValues = returnValue;
        return this;
    }
}
```

#### PutItemRequestBuilder<T>
```csharp
public class PutItemRequestBuilder<T> where T : class
{
    // Existing methods...
    
    /// <summary>
    /// Specifies which values to return in the response.
    /// </summary>
    public PutItemRequestBuilder<T> ReturnValues(ReturnValue returnValue)
    {
        _request.ReturnValues = returnValue;
        return this;
    }
}
```

#### TransactGetItemsRequestBuilder
```csharp
public class TransactGetItemsRequestBuilder
{
    // Existing methods...
    
    /// <summary>
    /// Executes the transactional get operation and returns the DynamoDB response.
    /// </summary>
    public async Task<TransactGetItemsResponse> ToDynamoDbResponseAsync(
        CancellationToken cancellationToken = default)
    {
        var request = ToRequest();
        return await _client.TransactGetItemsAsync(request, cancellationToken);
    }
}
```

#### TransactWriteItemsRequestBuilder
```csharp
public class TransactWriteItemsRequestBuilder
{
    // Existing methods...
    
    /// <summary>
    /// Executes the transactional write operation and returns the DynamoDB response.
    /// </summary>
    public async Task<TransactWriteItemsResponse> ToDynamoDbResponseAsync(
        CancellationToken cancellationToken = default)
    {
        var request = ToRequest();
        return await _client.TransactWriteItemsAsync(request, cancellationToken);
    }
}
```

## Data Models

### Entity Naming Conventions

The source generator follows these conventions for accessor properties:

1. **Entity Name Pattern**: `{Prefix}{EntityType}Entity`
2. **Accessor Name Pattern**: Pluralized `{Prefix}{EntityType}` (e.g., `Order` → `Orders`)
3. **Table Name**: Specified via `[DynamoDbTable]` attribute

Examples:
- `MultiEntityOrderTestEntity` → accessor: `Orders`
- `TransactionOrderLineEntity` → accessor: `OrderLines`
- `TransactionPaymentTestEntity` → accessor: `Payments`

### Table Structure

Both test tables use single-table design with multiple entity types:

**MultiEntityTestTable:**
- Entities: Orders, OrderLines
- Key Schema: Partition Key only (Id)
- Access Pattern: Direct key access per entity type

**TransactionTestTable:**
- Entities: Orders, OrderLines, Payments
- Key Schema: Partition Key only (Id)
- Access Pattern: Transactional operations across entity types

## Error Handling

### Source Generator Diagnostics

The source generator should emit diagnostics when:
1. Multiple entities share a table but have conflicting key schemas
2. Entity types cannot be analyzed (missing attributes, invalid types)
3. Table names are ambiguous or missing

### Build-Time Validation

The build process should:
1. Verify all entity types have required attributes
2. Ensure table classes can be generated for all referenced tables
3. Validate that accessor properties match entity types

### Runtime Validation

Tests should verify:
1. Generated table classes have correct accessor properties
2. Accessor properties return functional entity accessors
3. Operations through accessors work end-to-end

## Testing Strategy

### Unit Tests (Source Generator)

1. **Test Entity Analysis**
   - Verify analyzer detects all entities in a table
   - Verify analyzer extracts correct entity metadata
   - Verify analyzer handles multiple entities per table

2. **Test Table Generation**
   - Verify generator creates table classes with correct names
   - Verify generator creates accessor properties for all entities
   - Verify generated code compiles without errors

3. **Test Accessor Naming**
   - Verify pluralization logic (Order → Orders, OrderLine → OrderLines)
   - Verify special cases (Payment → Payments, not Paymentes)
   - Verify naming conflicts are handled

### Integration Tests (Library)

1. **Test Multi-Entity Tables**
   - Verify table classes are generated correctly
   - Verify accessor properties are accessible
   - Verify operations through accessors work end-to-end

2. **Test Transaction Operations**
   - Verify TransactWrite with multiple entity types
   - Verify TransactGet with multiple entity types
   - Verify transaction response methods work correctly

3. **Test Request Builder APIs**
   - Verify ReturnValues() method on Put/Update builders
   - Verify ToDynamoDbResponseAsync() on transaction builders
   - Verify method chaining works correctly

## Implementation Notes

### Source Generator Changes

The source generator likely already has the logic to generate accessor properties, but may need:
1. Verification that it detects all entities in a table
2. Verification that it generates accessors for all detected entities
3. Possible fixes to accessor naming logic

### Request Builder Changes

The request builder changes are straightforward additions:
1. Add ReturnValues() method to builders that support it
2. Add ToDynamoDbResponseAsync() to transaction builders
3. Ensure methods follow existing patterns and conventions

### Test Entity Creation

Test entities should:
1. Use realistic property names and types
2. Include all properties referenced in tests
3. Follow existing test entity patterns
4. Use appropriate attributes for table configuration

## Dependencies

- **AWSSDK.DynamoDBv2**: For DynamoDB types (ReturnValue, responses)
- **Source Generator**: Must be functioning correctly to generate table classes
- **Test Infrastructure**: Must support table creation and cleanup

## Performance Considerations

- Source generator performance should not be impacted (same number of entities)
- Test execution time should not increase significantly
- Generated code size will increase slightly (more accessor properties)

## Security Considerations

- No security implications (test code only)
- Test entities should not contain sensitive data patterns
- Generated code should follow same security practices as existing code
