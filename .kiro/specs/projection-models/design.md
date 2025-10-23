# Design Document

## Overview

The Projection Models feature introduces automatic generation and application of DynamoDB projection expressions through source generation. This feature reduces boilerplate code, prevents common mistakes, and optimizes query costs by fetching only required data. The design includes three major components: projection model definition through attributes, auto-generated GSI index properties on table classes, and runtime projection application with discriminator support for multi-entity queries.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Developer Code                            │
│  [DynamoDbProjection(typeof(Transaction))]                  │
│  partial class TransactionSummary { ... }                   │
│                                                              │
│  [GlobalSecondaryIndex("StatusIndex")]                      │
│  [UseProjection(typeof(TransactionSummary))]                │
│  public string StatusIndexPk { get; set; }                  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│              Source Generator (Build Time)                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 1. ProjectionModelAnalyzer                           │  │
│  │    - Detect [DynamoDbProjection] attributes          │  │
│  │    - Validate properties exist on source entity      │  │
│  │    - Extract discriminator information               │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 2. ProjectionExpressionGenerator                     │  │
│  │    - Generate projection expression strings          │  │
│  │    - Map property names to DynamoDB attributes       │  │
│  │    - Include discriminator in projection             │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 3. TableIndexGenerator                               │  │
│  │    - Detect GSI definitions across entities          │  │
│  │    - Generate index properties on table class        │  │
│  │    - Apply [UseProjection] constraints               │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 4. ProjectionMapperGenerator                         │  │
│  │    - Generate FromDynamoDb methods for projections   │  │
│  │    - Create type-safe hydration code                 │  │
│  │    - Handle discriminator-based routing              │  │
│  └──────────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                  Generated Code                              │
│  - TransactionSummary.g.cs (projection mapping)             │
│  - TransactionsTable.g.cs (index properties)                │
│  - ProjectionMetadata.g.cs (projection expressions)         │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│              Runtime (Query Execution)                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ QueryRequestBuilder Extensions                       │  │
│  │  - ToListAsync<TProjection>()                        │  │
│  │  - Auto-apply projection expression                  │  │
│  │  - Validate GSI projection constraints               │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Projection Hydration                                 │  │
│  │  - Deserialize DynamoDB response                     │  │
│  │  - Route by discriminator for multi-entity           │  │
│  │  - Populate projection model properties              │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Component Interaction Flow

1. **Build Time**: Source generator analyzes projection models and GSI definitions
2. **Code Generation**: Generates projection metadata, generic/non-generic index properties, and mapping code
3. **Runtime**: Index properties automatically apply projections; manual `.WithProjection()` calls override

### Projection Application Rules

1. **Generic Index with Projection**: `DynamoDbIndex<TDefault>` with projection expression
   - Projection is automatically applied to all queries
   - Manual `.WithProjection()` call overrides the automatic projection
   - `.ToListAsync<TOther>()` can override the result type (TDefault → TOther)
   
2. **Generic Index without Projection**: `DynamoDbIndex<TDefault>` without projection expression
   - No automatic projection (defaults to all fields)
   - Manual `.WithProjection()` call applies projection
   - `.ToListAsync<TOther>()` can override the result type
   
3. **Non-Generic Index**: `DynamoDbIndex` (legacy/manual)
   - No automatic projection
   - Manual `.WithProjection()` call applies projection
   - `.ToListAsync<T>()` specifies the result type
   
4. **Type Override Behavior**:
   - Index configured with `DynamoDbIndex<TransactionSummary>`
   - Query with `.ToListAsync<MinimalTransaction>()` → uses MinimalTransaction's projection
   - Query with `.ToListAsync<Transaction>()` → uses full entity (no projection)
   - The TDefault type parameter is a default, not a constraint
   
5. **Manual Override**: Any `.WithProjection()` call takes precedence over automatic projection

## Components and Interfaces

### 1. Projection Model Attribute

```csharp
namespace Oproto.FluentDynamoDb.Attributes;

/// <summary>
/// Marks a class as a projection model for a DynamoDB entity.
/// The source generator will create projection expressions and mapping code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DynamoDbProjectionAttribute : Attribute
{
    /// <summary>
    /// The source entity type that this projection derives from.
    /// </summary>
    public Type SourceEntityType { get; }
    
    /// <summary>
    /// Optional: Explicitly specify which properties to include.
    /// If null, all properties on the projection model are included.
    /// </summary>
    public string[]? IncludeProperties { get; set; }
    
    public DynamoDbProjectionAttribute(Type sourceEntityType)
    {
        SourceEntityType = sourceEntityType;
    }
}
```

### 2. UseProjection Attribute

```csharp
namespace Oproto.FluentDynamoDb.Attributes;

/// <summary>
/// Enforces that queries on a GSI must use a specific projection model.
/// This is an opt-in validation for GSIs that project only specific attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class UseProjectionAttribute : Attribute
{
    /// <summary>
    /// The projection model type that must be used when querying this GSI.
    /// </summary>
    public Type ProjectionType { get; }
    
    public UseProjectionAttribute(Type projectionType)
    {
        ProjectionType = projectionType;
    }
}
```

### 3. Projection Model Analyzer

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Analysis;

/// <summary>
/// Analyzes projection model classes and validates their configuration.
/// </summary>
public class ProjectionModelAnalyzer
{
    /// <summary>
    /// Analyzes a projection model class and extracts metadata.
    /// </summary>
    public ProjectionModel? AnalyzeProjection(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel);
    
    /// <summary>
    /// Validates that all projection properties exist on the source entity.
    /// </summary>
    private void ValidateProjectionProperties(
        ProjectionModel projection,
        EntityModel sourceEntity);
    
    /// <summary>
    /// Validates that property types match between projection and source.
    /// </summary>
    private void ValidatePropertyTypes(
        ProjectionModel projection,
        EntityModel sourceEntity);
}
```

### 4. Projection Expression Generator

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Generators;

/// <summary>
/// Generates projection expression strings and metadata.
/// </summary>
public class ProjectionExpressionGenerator
{
    /// <summary>
    /// Generates a projection expression string for a projection model.
    /// Example: "id, amount, created_date, entity_type"
    /// </summary>
    public string GenerateProjectionExpression(ProjectionModel projection);
    
    /// <summary>
    /// Generates metadata class containing projection information.
    /// </summary>
    public string GenerateProjectionMetadata(ProjectionModel projection);
    
    /// <summary>
    /// Ensures discriminator property is included in projection.
    /// </summary>
    private void IncludeDiscriminatorInProjection(
        ProjectionModel projection,
        EntityModel sourceEntity);
}
```

### 5. Table Index Generator

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Generators;

/// <summary>
/// Generates index properties on table classes.
/// </summary>
public class TableIndexGenerator
{
    /// <summary>
    /// Generates index properties for all GSIs defined across entities.
    /// Creates generic DynamoDbIndex<TProjection> if UseProjection is specified,
    /// otherwise creates non-generic DynamoDbIndex.
    /// </summary>
    public string GenerateIndexProperties(
        string tableName,
        IEnumerable<EntityModel> entities);
    
    /// <summary>
    /// Groups GSI definitions by index name across multiple entities.
    /// </summary>
    private Dictionary<string, GsiDefinition> GroupGsiDefinitions(
        IEnumerable<EntityModel> entities);
    
    /// <summary>
    /// Generates a single index property.
    /// If projection is specified, generates DynamoDbIndex<TProjection>.
    /// Otherwise generates non-generic DynamoDbIndex.
    /// </summary>
    private string GenerateIndexProperty(
        string indexName,
        GsiDefinition gsiDef);
    
    /// <summary>
    /// Example generated code with projection:
    /// public DynamoDbIndex<TransactionSummary> StatusIndex => 
    ///     new DynamoDbIndex<TransactionSummary>(
    ///         this, 
    ///         "StatusIndex", 
    ///         "id, amount, status, entity_type");
    /// 
    /// Example generated code without projection:
    /// public DynamoDbIndex Gsi1 => 
    ///     new DynamoDbIndex(this, "Gsi1");
    /// </summary>
    private string GenerateIndexPropertyCode(
        string indexName,
        string? projectionType,
        string? projectionExpression);
}
```

### 6. Query Builder Extensions

```csharp
namespace Oproto.FluentDynamoDb.Requests.Extensions;

/// <summary>
/// Extension methods for automatic projection application.
/// </summary>
public static class ProjectionExtensions
{
    /// <summary>
    /// Executes query and returns results as specified type.
    /// Automatically applies projection expression if TResult is a projection model.
    /// Can override the index's default projection type.
    /// </summary>
    /// <typeparam name="TResult">
    /// The result type - can be a projection model, full entity, or any type with FromDynamoDb method.
    /// </typeparam>
    /// <example>
    /// // Using index's default projection type
    /// var summaries = await table.StatusIndex.Query
    ///     .Where("status = :status")
    ///     .WithValue(":status", "ACTIVE")
    ///     .ToListAsync<TransactionSummary>();
    /// 
    /// // Overriding to use different projection type
    /// var minimal = await table.StatusIndex.Query
    ///     .Where("status = :status")
    ///     .WithValue(":status", "ACTIVE")
    ///     .ToListAsync<MinimalTransaction>();
    /// 
    /// // Overriding to use full entity (ignores auto-projection)
    /// var full = await table.StatusIndex.Query
    ///     .Where("status = :status")
    ///     .WithValue(":status", "ACTIVE")
    ///     .ToListAsync<Transaction>();
    /// </example>
    public static async Task<List<TResult>> ToListAsync<TResult>(
        this QueryRequestBuilder builder,
        CancellationToken cancellationToken = default)
        where TResult : class, new();
    
    /// <summary>
    /// Validates GSI projection constraints at runtime (if configured).
    /// Note: This is only enforced if [UseProjection] is explicitly set.
    /// </summary>
    private static void ValidateGsiProjection<TResult>(
        QueryRequestBuilder builder);
    
    /// <summary>
    /// Applies projection expression if TResult is a projection model
    /// and no manual projection has been set.
    /// </summary>
    private static QueryRequestBuilder ApplyProjectionIfNeeded<TResult>(
        QueryRequestBuilder builder);
}
```

### 7. Generic DynamoDbIndex

```csharp
namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Generic index that automatically applies projection for the specified type.
/// TDefault specifies the default projection type but can be overridden in queries.
/// </summary>
/// <typeparam name="TDefault">
/// The default projection/entity type for this index.
/// This type's projection is auto-applied but can be overridden with ToListAsync<TOther>().
/// </typeparam>
public class DynamoDbIndex<TDefault> where TDefault : class, new()
{
    private readonly DynamoDbTableBase _table;
    private readonly string _indexName;
    private readonly string? _projectionExpression;
    
    public DynamoDbIndex(
        DynamoDbTableBase table,
        string indexName,
        string? projectionExpression = null)
    {
        _table = table;
        _indexName = indexName;
        _projectionExpression = projectionExpression;
    }
    
    /// <summary>
    /// Gets the index name.
    /// </summary>
    public string Name => _indexName;
    
    /// <summary>
    /// Gets a query builder pre-configured with projection expression.
    /// The projection is automatically applied unless manually overridden.
    /// </summary>
    public QueryRequestBuilder Query
    {
        get
        {
            var builder = new QueryRequestBuilder(_table.DynamoDbClient)
                .ForTable(_table.Name)
                .UsingIndex(_indexName);
            
            // Auto-apply projection if available
            if (!string.IsNullOrEmpty(_projectionExpression))
            {
                builder = builder.WithProjection(_projectionExpression);
            }
            
            return builder;
        }
    }
    
    /// <summary>
    /// Executes query and returns results as TDefault (the index's default type).
    /// </summary>
    public async Task<List<TDefault>> QueryAsync(
        Action<QueryRequestBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        var builder = Query;
        configure(builder);
        return await builder.ToListAsync<TDefault>(cancellationToken);
    }
    
    /// <summary>
    /// Executes query and returns results as TResult (overriding the default type).
    /// Useful when the same GSI is used by multiple entity types.
    /// </summary>
    /// <example>
    /// // Index default is TransactionSummary
    /// var summaries = await table.StatusIndex.QueryAsync<TransactionSummary>(q => 
    ///     q.Where("status = :s").WithValue(":s", "ACTIVE"));
    /// 
    /// // Override to use different projection
    /// var minimal = await table.StatusIndex.QueryAsync<MinimalTransaction>(q => 
    ///     q.Where("status = :s").WithValue(":s", "ACTIVE"));
    /// </example>
    public async Task<List<TResult>> QueryAsync<TResult>(
        Action<QueryRequestBuilder> configure,
        CancellationToken cancellationToken = default)
        where TResult : class, new()
    {
        var builder = Query;
        configure(builder);
        return await builder.ToListAsync<TResult>(cancellationToken);
    }
}
```

### 8. Projection Hydration

```csharp
namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Handles hydration of projection models from DynamoDB responses.
/// </summary>
public class ProjectionHydrator
{
    /// <summary>
    /// Hydrates a list of projection models from query response.
    /// Handles discriminator-based routing for multi-entity queries.
    /// </summary>
    public static List<TProjection> HydrateProjections<TProjection>(
        QueryResponse response)
        where TProjection : class, new();
    
    /// <summary>
    /// Routes item to correct projection type based on discriminator.
    /// </summary>
    private static TProjection? RouteByDiscriminator<TProjection>(
        Dictionary<string, AttributeValue> item);
    
    /// <summary>
    /// Hydrates a single projection model from DynamoDB item.
    /// </summary>
    private static TProjection HydrateSingleProjection<TProjection>(
        Dictionary<string, AttributeValue> item)
        where TProjection : class, new();
}
```

## Data Models

### Projection Model

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Models;

/// <summary>
/// Represents a projection model during source generation.
/// </summary>
public class ProjectionModel
{
    /// <summary>
    /// The projection class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// The namespace of the projection class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// The source entity type that this projection derives from.
    /// </summary>
    public string SourceEntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Properties included in the projection.
    /// </summary>
    public ProjectionPropertyModel[] Properties { get; set; } = Array.Empty<ProjectionPropertyModel>();
    
    /// <summary>
    /// The generated projection expression string.
    /// </summary>
    public string ProjectionExpression { get; set; } = string.Empty;
    
    /// <summary>
    /// The discriminator property name if source entity uses discriminators.
    /// </summary>
    public string? DiscriminatorProperty { get; set; }
    
    /// <summary>
    /// The discriminator value for the source entity.
    /// </summary>
    public string? DiscriminatorValue { get; set; }
}
```

### Projection Property Model

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Models;

/// <summary>
/// Represents a property in a projection model.
/// </summary>
public class ProjectionPropertyModel
{
    /// <summary>
    /// The property name in the projection class.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// The property type.
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;
    
    /// <summary>
    /// The DynamoDB attribute name.
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the property is nullable.
    /// </summary>
    public bool IsNullable { get; set; }
    
    /// <summary>
    /// The corresponding property on the source entity.
    /// </summary>
    public PropertyModel? SourceProperty { get; set; }
}
```

### GSI Definition Model

```csharp
namespace Oproto.FluentDynamoDb.SourceGenerator.Models;

/// <summary>
/// Represents a GSI definition aggregated across multiple entities.
/// </summary>
public class GsiDefinition
{
    /// <summary>
    /// The GSI name.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;
    
    /// <summary>
    /// Entity types that use this GSI.
    /// </summary>
    public List<string> EntityTypes { get; set; } = new();
    
    /// <summary>
    /// Optional projection type constraint.
    /// </summary>
    public string? ProjectionType { get; set; }
    
    /// <summary>
    /// The projection expression for this GSI (if UseProjection is specified).
    /// </summary>
    public string? ProjectionExpression { get; set; }
    
    /// <summary>
    /// Partition key property name.
    /// </summary>
    public string? PartitionKeyProperty { get; set; }
    
    /// <summary>
    /// Sort key property name.
    /// </summary>
    public string? SortKeyProperty { get; set; }
}
```

## Error Handling

### Compilation Errors

1. **PROJ001**: Projection property does not exist on source entity
   - Message: "Property '{PropertyName}' on projection '{ProjectionType}' does not exist on source entity '{SourceEntity}'"
   - Severity: Error

2. **PROJ002**: Projection property type mismatch
   - Message: "Property '{PropertyName}' type '{ProjectionType}' does not match source entity type '{SourceType}'"
   - Severity: Error

3. **PROJ003**: Invalid source entity type
   - Message: "Source entity type '{SourceType}' for projection '{ProjectionType}' does not exist or is not a DynamoDB entity"
   - Severity: Error

4. **PROJ004**: Projection class must be partial
   - Message: "Projection class '{ClassName}' must be declared as partial"
   - Severity: Error

5. **PROJ005**: UseProjection references non-existent projection
   - Message: "UseProjection attribute on GSI '{IndexName}' references non-existent projection type '{ProjectionType}'"
   - Severity: Error

6. **PROJ006**: Multiple projections for same GSI
   - Message: "GSI '{IndexName}' has multiple conflicting UseProjection attributes"
   - Severity: Error

### Runtime Errors

1. **ProjectionValidationException**: Thrown when GSI projection constraint is violated
   ```csharp
   throw new ProjectionValidationException(
       $"GSI '{indexName}' requires projection type '{expectedType}' but query uses '{actualType}'");
   ```

2. **DiscriminatorMismatchException**: Thrown when discriminator doesn't match expected value
   ```csharp
   throw new DiscriminatorMismatchException(
       $"Expected discriminator '{expected}' but found '{actual}'");
   ```

### Warnings

1. **PROJ101**: Projection includes all properties (no optimization)
   - Message: "Projection '{ProjectionType}' includes all properties from source entity. Consider using the full entity type instead."
   - Severity: Warning

2. **PROJ102**: Large projection expression
   - Message: "Projection '{ProjectionType}' has {count} properties which may impact performance"
   - Severity: Warning

## Testing Strategy

### Unit Tests

1. **Projection Model Analysis Tests**
   - Test detection of [DynamoDbProjection] attribute
   - Test validation of projection properties against source entity
   - Test property type matching validation
   - Test discriminator inclusion in projections

2. **Projection Expression Generation Tests**
   - Test generation of projection expression strings
   - Test attribute name mapping
   - Test discriminator inclusion
   - Test handling of nullable properties

3. **Table Index Generation Tests**
   - Test GSI property generation on table classes
   - Test grouping of GSIs across multiple entities
   - Test UseProjection constraint application
   - Test backward compatibility with manual index instantiation

4. **Query Builder Extension Tests**
   - Test automatic projection application
   - Test GSI projection validation
   - Test manual projection expression override
   - Test non-projection type queries (no auto-projection)

5. **Projection Hydration Tests**
   - Test hydration of projection models from DynamoDB responses
   - Test discriminator-based routing
   - Test multi-entity query results
   - Test partial property population

### Integration Tests

1. **End-to-End Projection Tests**
   - Create projection model with [DynamoDbProjection]
   - Query using ToListAsync<TProjection>()
   - Verify projection expression is applied
   - Verify only projected properties are populated

2. **GSI Projection Enforcement Tests**
   - Define GSI with [UseProjection]
   - Attempt query with wrong type (should fail)
   - Query with correct projection type (should succeed)
   - Verify projection expression is applied

3. **Multi-Entity Projection Tests**
   - Create table with multiple entity types
   - Define projections for each entity type
   - Query and verify discriminator-based routing
   - Verify each item is hydrated to correct projection type

4. **Manual Configuration Tests**
   - Manually instantiate DynamoDbIndex with projection
   - Query and verify projection is applied
   - Test precedence of manual over generated configuration

### Performance Tests

1. **Projection Cost Optimization**
   - Measure read capacity units for full entity query
   - Measure read capacity units for projected query
   - Verify projected query consumes less capacity

2. **Code Generation Performance**
   - Measure source generator execution time
   - Test with large numbers of projection models
   - Verify incremental generation works correctly

## Migration and Backward Compatibility

### Backward Compatibility

1. **Existing Manual Index Instantiation**
   - Legacy pattern: `public DynamoDbIndex Gsi1 => new DynamoDbIndex(this, "Gsi1");`
   - This pattern continues to work unchanged
   - Generated index properties are additive, not breaking

2. **Existing Manual Projection Expressions**
   - Queries using `.WithProjection("id, amount")` continue to work
   - Manual projections take precedence over auto-generated ones
   - No changes required to existing code

3. **Existing ToListAsync() Usage**
   - Queries using full entity types continue to work identically
   - No automatic projection is applied to non-projection types
   - Behavior is unchanged for existing code

### Migration Path for Manual Configuration

For users not using source generation who want projection features:

```csharp
// Option 1: Non-generic with manual projection (flexible)
public class DynamoDbIndex
{
    // Existing constructor (unchanged)
    public DynamoDbIndex(DynamoDbTableBase table, string indexName) { }
    
    // New constructor with projection expression
    public DynamoDbIndex(
        DynamoDbTableBase table,
        string indexName,
        string projectionExpression)
    {
        // Projection is auto-applied to queries
    }
}

// Option 2: Generic with type-safe projection (recommended)
public class DynamoDbIndex<TProjection> where TProjection : class, new()
{
    public DynamoDbIndex(
        DynamoDbTableBase table,
        string indexName,
        string? projectionExpression = null)
    {
        // Projection is auto-applied to queries
        // TProjection provides type safety for results
    }
}

// Manual usage examples:

// Non-generic with manual projection
public DynamoDbIndex StatusIndex => new DynamoDbIndex(
    this,
    "StatusIndex",
    "id, amount, status");

// Generic with type-safe projection
public DynamoDbIndex<TransactionSummary> StatusIndex => 
    new DynamoDbIndex<TransactionSummary>(
        this,
        "StatusIndex",
        "id, amount, status");

// Generic without projection (defaults to all fields)
public DynamoDbIndex<Transaction> Gsi1 => 
    new DynamoDbIndex<Transaction>(this, "Gsi1");

// Query usage:
var results = await table.StatusIndex.Query
    .Where("status = :status")
    .WithValue(":status", "ACTIVE")
    .ToListAsync<TransactionSummary>(); // Projection auto-applied

// Override projection manually if needed:
var results = await table.StatusIndex.Query
    .Where("status = :status")
    .WithValue(":status", "ACTIVE")
    .WithProjection("id, amount") // Manual override
    .ToListAsync<TransactionSummary>();
```

### Breaking Changes

**None**. This feature is fully additive and maintains complete backward compatibility.

## Implementation Phases

### Phase 1: Core Projection Infrastructure
- Implement [DynamoDbProjection] attribute
- Implement ProjectionModelAnalyzer
- Implement ProjectionExpressionGenerator
- Generate projection metadata classes

### Phase 2: Query Builder Integration
- Implement ToListAsync<TProjection>() extension
- Implement automatic projection application
- Implement projection hydration logic
- Add runtime validation

### Phase 3: GSI Auto-Generation
- Implement [UseProjection] attribute
- Implement TableIndexGenerator
- Generate index properties on table classes
- Implement GSI projection validation

### Phase 4: Multi-Entity Support
- Implement discriminator detection in projections
- Implement discriminator-based routing
- Handle multiple projection types in single query
- Add comprehensive validation

### Phase 5: Manual Configuration Support
- Extend DynamoDbIndex constructor for manual projection config
- Implement runtime projection validation for manual config
- Add documentation and migration guide
- Ensure precedence rules work correctly

## Performance Considerations

### Build-Time Performance
- Use incremental source generation to avoid regenerating unchanged projections
- Cache projection metadata to avoid redundant analysis
- Minimize diagnostic overhead during validation

### Runtime Performance
- Projection expression application is zero-allocation (string constant)
- Hydration uses generated code (no reflection)
- Discriminator routing uses dictionary lookup (O(1))
- No performance impact on queries not using projections

### DynamoDB Cost Optimization
- Projected queries fetch only required attributes
- Reduces data transfer from DynamoDB
- Lowers consumed read capacity units
- Measurable cost savings for large items with many attributes

## Security Considerations

1. **Type Safety**: Compile-time validation prevents runtime type errors
2. **Injection Prevention**: Projection expressions are generated at compile-time (no user input)
3. **Discriminator Validation**: Runtime checks prevent incorrect type hydration
4. **AOT Compatibility**: No reflection means no runtime code generation vulnerabilities

## Monitoring and Diagnostics

### Logging

```csharp
// Log projection application
_logger.LogDebug(
    "Applying projection expression for type {ProjectionType}: {Expression}",
    typeof(TProjection).Name,
    projectionExpression);

// Log GSI projection validation
_logger.LogDebug(
    "Validating GSI projection constraint. Index: {IndexName}, Required: {RequiredType}, Actual: {ActualType}",
    indexName,
    requiredType,
    actualType);

// Log discriminator routing
_logger.LogTrace(
    "Routing item by discriminator. Value: {DiscriminatorValue}, Target: {TargetType}",
    discriminatorValue,
    targetType);
```

### Metrics

- Count of projection models generated
- Count of auto-generated index properties
- Projection expression application rate
- GSI projection validation failures
- Discriminator routing success/failure rate

## Open Questions and Future Enhancements

### Open Questions
1. Should we support projection inheritance (projection of a projection)?
2. How should we handle computed properties in projections?
3. Should projections support custom deserialization logic?

### Future Enhancements
1. **Projection Composition**: Combine multiple projections
2. **Dynamic Projections**: Runtime projection expression building
3. **Projection Caching**: Cache hydrated projection instances
4. **Projection Validation**: Validate projection expressions against actual GSI schema
5. **IDE Integration**: IntelliSense support for projection properties
