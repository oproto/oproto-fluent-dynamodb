---
title: "Attribute Reference"
category: "reference"
order: 1
keywords: ["attributes", "annotations", "DynamoDbTable", "PartitionKey", "SortKey", "Computed", "Extracted", "RelatedEntity"]
related: ["FormatSpecifiers.md", "Troubleshooting.md"]
---

[Documentation](../README.md) > [Reference](README.md) > Attribute Reference

# Attribute Reference

---

This reference guide documents all attributes used to define DynamoDB entities with source generation. Each attribute configures how the source generator creates mapping code, field constants, and key builders for your entities.

## Overview

Oproto.FluentDynamoDb uses attributes to define entity metadata at compile time. The source generator reads these attributes and generates:

- **Field constants**: Type-safe field name references
- **Key builders**: Methods to construct partition and sort keys
- **Mapper code**: Serialization and deserialization logic

## [DynamoDbTable]

Marks a class as a DynamoDB entity and specifies the table name.

### Purpose

This attribute is required on every entity class. It tells the source generator that this class represents a DynamoDB entity and specifies which table it maps to.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `tableName` | `string` | Yes | The DynamoDB table name |
| `EntityDiscriminator` | `string` | No | Optional discriminator for multi-type tables |

### Properties

**EntityDiscriminator**: When multiple entity types share the same table, use this property to distinguish between them. The discriminator is typically stored in a dedicated attribute.

### Example

```csharp
// Basic usage
[DynamoDbTable("users")]
public partial class User
{
    // Properties...
}

// With entity discriminator for multi-type tables
[DynamoDbTable("entities", EntityDiscriminator = "USER")]
public partial class User
{
    // Properties...
}

[DynamoDbTable("entities", EntityDiscriminator = "ORDER")]
public partial class Order
{
    // Properties...
}
```


## [DynamoDbAttribute]

Maps a property to a DynamoDB attribute with a specific name.

### Purpose

This attribute defines the mapping between a C# property and a DynamoDB attribute name. It's required for every property you want to persist in DynamoDB.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `attributeName` | `string` | Yes | The DynamoDB attribute name |

### Example

```csharp
[DynamoDbTable("users")]
public partial class User
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string UserId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("email")]
    public string Email { get; set; } = string.Empty;
    
    [DynamoDbAttribute("created_at")]
    public DateTime CreatedAt { get; set; }
    
    // Property name matches attribute name
    [DynamoDbAttribute("status")]
    public string Status { get; set; } = string.Empty;
}
```

### Best Practices

- Use consistent naming conventions (snake_case or camelCase)
- Keep attribute names short to reduce storage costs
- Use abbreviated names for frequently accessed attributes (e.g., "pk" for partition key)


## [PartitionKey]

Marks a property as the partition key for a DynamoDB table.

### Purpose

Identifies the property that serves as the partition key (also called hash key). Every DynamoDB table requires exactly one partition key. The source generator uses this to create key builder methods.

### Parameters

None (constructor has no parameters)

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Prefix` | `string?` | `null` | Optional prefix added to the partition key value |
| `Separator` | `string?` | `"#"` | Separator used when combining key components |

### Example

```csharp
[DynamoDbTable("users")]
public partial class User
{
    // Simple partition key
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string UserId { get; set; } = string.Empty;
}

// With prefix for key namespacing
[DynamoDbTable("entities")]
public partial class User
{
    [PartitionKey(Prefix = "USER")]
    [DynamoDbAttribute("pk")]
    public string UserId { get; set; } = string.Empty;
    
    // Stored in DynamoDB as: "USER#user123"
}

// Custom separator
[DynamoDbTable("entities")]
public partial class Product
{
    [PartitionKey(Prefix = "PRODUCT", Separator = "|")]
    [DynamoDbAttribute("pk")]
    public string ProductId { get; set; } = string.Empty;
    
    // Stored in DynamoDB as: "PRODUCT|prod456"
}
```

### Generated Code

The source generator creates a key builder method:

```csharp
// Generated for User class
public static class UserKeys
{
    public static string Pk(string userId) => $"USER#{userId}";
}

// Usage
var key = UserKeys.Pk("user123"); // Returns "USER#user123"
```


## [SortKey]

Marks a property as the sort key (range key) for a DynamoDB table.

### Purpose

Identifies the property that serves as the sort key. Sort keys are optional but enable range queries and allow multiple items with the same partition key. The source generator creates key builder methods for sort keys.

### Parameters

None (constructor has no parameters)

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Prefix` | `string?` | `null` | Optional prefix added to the sort key value |
| `Separator` | `string?` | `"#"` | Separator used when combining key components |

### Example

```csharp
[DynamoDbTable("orders")]
public partial class Order
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string CustomerId { get; set; } = string.Empty;
    
    // Simple sort key
    [SortKey]
    [DynamoDbAttribute("sk")]
    public string OrderId { get; set; } = string.Empty;
}

// With prefix for composite entities
[DynamoDbTable("entities")]
public partial class OrderItem
{
    [PartitionKey(Prefix = "ORDER")]
    [DynamoDbAttribute("pk")]
    public string OrderId { get; set; } = string.Empty;
    
    [SortKey(Prefix = "ITEM")]
    [DynamoDbAttribute("sk")]
    public string ItemId { get; set; } = string.Empty;
    
    // Stored as: pk="ORDER#order123", sk="ITEM#item456"
}

// Timestamp-based sort key
[DynamoDbTable("events")]
public partial class Event
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string UserId { get; set; } = string.Empty;
    
    [SortKey(Prefix = "EVENT")]
    [DynamoDbAttribute("sk")]
    public string Timestamp { get; set; } = string.Empty;
}
```

### Generated Code

```csharp
// Generated for OrderItem class
public static class OrderItemKeys
{
    public static string Pk(string orderId) => $"ORDER#{orderId}";
    public static string Sk(string itemId) => $"ITEM#{itemId}";
}

// Usage
var partitionKey = OrderItemKeys.Pk("order123");
var sortKey = OrderItemKeys.Sk("item456");
```


## [GlobalSecondaryIndex]

Marks a property as part of a Global Secondary Index (GSI).

### Purpose

Defines properties that participate in Global Secondary Indexes, enabling alternative query patterns. The source generator creates GSI-specific field constants and key builders.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `indexName` | `string` | Yes | The name of the Global Secondary Index |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsPartitionKey` | `bool` | `false` | Whether this property is the GSI partition key |
| `IsSortKey` | `bool` | `false` | Whether this property is the GSI sort key |
| `KeyFormat` | `string?` | `null` | Format pattern for composite keys |

### Example

```csharp
[DynamoDbTable("users")]
public partial class User
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string UserId { get; set; } = string.Empty;
    
    [SortKey]
    [DynamoDbAttribute("sk")]
    public string Username { get; set; } = string.Empty;
    
    // GSI for querying by email
    [GlobalSecondaryIndex("email-index", IsPartitionKey = true)]
    [DynamoDbAttribute("email")]
    public string Email { get; set; } = string.Empty;
    
    // GSI for querying by status and created date
    [GlobalSecondaryIndex("status-index", IsPartitionKey = true)]
    [DynamoDbAttribute("status")]
    public string Status { get; set; } = string.Empty;
    
    [GlobalSecondaryIndex("status-index", IsSortKey = true)]
    [DynamoDbAttribute("created_at")]
    public DateTime CreatedAt { get; set; }
}

// Composite GSI key with format
[DynamoDbTable("products")]
public partial class Product
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string ProductId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("category")]
    public string Category { get; set; } = string.Empty;
    
    [DynamoDbAttribute("subcategory")]
    public string Subcategory { get; set; } = string.Empty;
    
    // Composite GSI key: "Electronics#Laptops"
    [GlobalSecondaryIndex("category-index", IsPartitionKey = true, KeyFormat = "{0}#{1}")]
    [Computed(nameof(Category), nameof(Subcategory))]
    [DynamoDbAttribute("category_sk")]
    public string CategoryKey { get; set; } = string.Empty;
}
```

### Generated Code

```csharp
// Generated GSI field constants
public static class UserFields
{
    public const string Email = "email";
    public const string Status = "status";
    public const string CreatedAt = "created_at";
}

// Generated GSI metadata
public static class UserIndexes
{
    public const string EmailIndex = "email-index";
    public const string StatusIndex = "status-index";
}
```

### See Also

- [Global Secondary Indexes Guide](../advanced-topics/GlobalSecondaryIndexes.md)
- [Querying Data](../core-features/QueryingData.md)


## [Computed]

Specifies that a property value should be computed from other properties before mapping to DynamoDB.

### Purpose

Creates composite keys or derived values by combining multiple source properties. This is essential for single-table design patterns where you need to construct keys from multiple entity attributes.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourceProperties` | `params string[]` | Yes | Names of source properties to combine |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Format` | `string?` | `null` | Format string for combining values (e.g., `"{0}#{1}"`) |
| `Separator` | `string` | `"#"` | Separator when no format is specified |

### Example

```csharp
[DynamoDbTable("entities")]
public partial class Order
{
    [DynamoDbAttribute("customer_id")]
    public string CustomerId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("order_id")]
    public string OrderId { get; set; } = string.Empty;
    
    // Computed partition key: "CUSTOMER#cust123"
    [PartitionKey]
    [Computed(nameof(CustomerId), Format = "CUSTOMER#{0}")]
    [DynamoDbAttribute("pk")]
    public string PartitionKey { get; set; } = string.Empty;
    
    // Computed sort key: "ORDER#order456"
    [SortKey]
    [Computed(nameof(OrderId), Format = "ORDER#{0}")]
    [DynamoDbAttribute("sk")]
    public string SortKey { get; set; } = string.Empty;
}

// Multiple source properties with custom format
[DynamoDbTable("products")]
public partial class Product
{
    [DynamoDbAttribute("category")]
    public string Category { get; set; } = string.Empty;
    
    [DynamoDbAttribute("brand")]
    public string Brand { get; set; } = string.Empty;
    
    [DynamoDbAttribute("sku")]
    public string Sku { get; set; } = string.Empty;
    
    // Computed key: "Electronics#Sony#SKU12345"
    [PartitionKey]
    [Computed(nameof(Category), nameof(Brand), nameof(Sku), Format = "{0}#{1}#{2}")]
    [DynamoDbAttribute("pk")]
    public string CompositeKey { get; set; } = string.Empty;
}

// Using default separator
[DynamoDbTable("users")]
public partial class User
{
    [DynamoDbAttribute("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [DynamoDbAttribute("last_name")]
    public string LastName { get; set; } = string.Empty;
    
    // Computed: "John#Doe" (using default # separator)
    [Computed(nameof(FirstName), nameof(LastName))]
    [DynamoDbAttribute("full_name_key")]
    public string FullNameKey { get; set; } = string.Empty;
}
```

### Behavior

- Computed properties are calculated **before** saving to DynamoDB
- The source generator creates code to automatically populate computed properties
- Format strings use standard .NET string formatting (`{0}`, `{1}`, etc.)
- If no format is specified, properties are joined with the separator

### See Also

- [Extracted Attribute](#extracted)
- [Entity Definition Guide](../core-features/EntityDefinition.md)


## [Extracted]

Specifies that a property value should be extracted from a composite key property after mapping from DynamoDB.

### Purpose

Extracts individual components from composite keys when reading from DynamoDB. This is the inverse of `[Computed]` - while `[Computed]` combines values before saving, `[Extracted]` splits values after loading.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourceProperty` | `string` | Yes | Name of the property containing the composite key |
| `index` | `int` | Yes | Zero-based index of the component to extract |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Separator` | `string` | `"#"` | Separator used to split the composite key |

### Example

```csharp
[DynamoDbTable("entities")]
public partial class Order
{
    // Composite partition key stored as "CUSTOMER#cust123"
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string PartitionKey { get; set; } = string.Empty;
    
    // Extract customer ID from partition key
    [Extracted(nameof(PartitionKey), 1)]
    public string CustomerId { get; set; } = string.Empty;
    
    // Composite sort key stored as "ORDER#order456#2024-01-15"
    [SortKey]
    [DynamoDbAttribute("sk")]
    public string SortKey { get; set; } = string.Empty;
    
    // Extract order ID (index 1)
    [Extracted(nameof(SortKey), 1)]
    public string OrderId { get; set; } = string.Empty;
    
    // Extract date (index 2)
    [Extracted(nameof(SortKey), 2)]
    public string OrderDate { get; set; } = string.Empty;
}

// Custom separator
[DynamoDbTable("products")]
public partial class Product
{
    // Stored as "Electronics|Laptops|Dell"
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string CompositeKey { get; set; } = string.Empty;
    
    [Extracted(nameof(CompositeKey), 0, Separator = "|")]
    public string Category { get; set; } = string.Empty;
    
    [Extracted(nameof(CompositeKey), 1, Separator = "|")]
    public string Subcategory { get; set; } = string.Empty;
    
    [Extracted(nameof(CompositeKey), 2, Separator = "|")]
    public string Brand { get; set; } = string.Empty;
}
```

### Behavior

- Extracted properties are populated **after** loading from DynamoDB
- The source generator creates code to automatically split and extract values
- Index is zero-based (0 = first component, 1 = second, etc.)
- If the source property doesn't contain enough components, the extracted property will be empty

### Common Pattern: Round-Trip Computed and Extracted

```csharp
[DynamoDbTable("orders")]
public partial class Order
{
    // Source properties
    [DynamoDbAttribute("customer_id")]
    public string CustomerId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("order_id")]
    public string OrderId { get; set; } = string.Empty;
    
    // Computed when saving: "CUSTOMER#cust123"
    [PartitionKey]
    [Computed(nameof(CustomerId), Format = "CUSTOMER#{0}")]
    [DynamoDbAttribute("pk")]
    public string PartitionKey { get; set; } = string.Empty;
    
    // Alternative: Extract when loading (if you don't store customer_id separately)
    // [Extracted(nameof(PartitionKey), 1)]
    // public string CustomerId { get; set; } = string.Empty;
}
```

### See Also

- [Computed Attribute](#computed)
- [Entity Definition Guide](../core-features/EntityDefinition.md)


## [RelatedEntity]

Marks a property as a related entity that should be automatically populated based on sort key patterns when querying.

### Purpose

Enables automatic population of related entities in composite entity patterns. When you query for a parent entity, related entities matching the sort key pattern are automatically loaded into the specified property.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sortKeyPattern` | `string` | Yes | Sort key pattern to match (supports wildcards like `"audit#*"`) |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EntityType` | `Type?` | `null` | Type of the related entity (defaults to property type) |

### Example

```csharp
// Parent entity
[DynamoDbTable("orders")]
public partial class Order
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string OrderId { get; set; } = string.Empty;
    
    [SortKey]
    [DynamoDbAttribute("sk")]
    public string SortKey { get; set; } = string.Empty;
    
    [DynamoDbAttribute("total")]
    public decimal Total { get; set; }
    
    // Related entities with pattern "ITEM#*"
    [RelatedEntity("ITEM#*")]
    public List<OrderItem> Items { get; set; } = new();
    
    // Single related entity with exact match
    [RelatedEntity("SUMMARY")]
    public OrderSummary? Summary { get; set; }
    
    // Related audit records
    [RelatedEntity("AUDIT#*")]
    public List<AuditRecord> AuditRecords { get; set; } = new();
}

// Related entity
[DynamoDbTable("orders")]
public partial class OrderItem
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string OrderId { get; set; } = string.Empty;
    
    [SortKey(Prefix = "ITEM")]
    [DynamoDbAttribute("sk")]
    public string ItemId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("product_name")]
    public string ProductName { get; set; } = string.Empty;
    
    [DynamoDbAttribute("quantity")]
    public int Quantity { get; set; }
}

// Usage
var response = await table.Query
    .WithKey(OrderFields.OrderId, OrderKeys.Pk("order123"))
    .ExecuteAsync<Order>();

var order = await response.ToCompositeEntityAsync<Order>();
// order.Items is automatically populated with all items matching "ITEM#*"
// order.Summary is populated if an item with sk="SUMMARY" exists
```

### Sort Key Patterns

| Pattern | Description | Example Matches |
|---------|-------------|-----------------|
| `"ITEM#*"` | Wildcard match | `ITEM#1`, `ITEM#2`, `ITEM#abc` |
| `"SUMMARY"` | Exact match | `SUMMARY` only |
| `"AUDIT#*"` | Prefix match | `AUDIT#2024-01-15`, `AUDIT#log1` |
| `"*"` | Match all | Any sort key value |

### Behavior

- Related entities are populated when using `ToCompositeEntityAsync<T>()`
- Wildcard patterns (`*`) match multiple items and populate collections
- Exact patterns match single items and populate single properties
- If no matching items are found, collections are empty and single properties are null
- The source generator creates the mapping logic automatically

### See Also

- [Composite Entities Guide](../advanced-topics/CompositeEntities.md)
- [Entity Definition](../core-features/EntityDefinition.md)


## [Queryable]

Marks a property as queryable and specifies the supported operations and indexes.

### Purpose

Provides metadata about which DynamoDB operations are supported for a property and which indexes it's available in. This attribute is primarily used for future LINQ expression support and documentation purposes.

### Parameters

None (constructor has no parameters)

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SupportedOperations` | `DynamoDbOperation[]` | `[]` | Array of supported DynamoDB operations |
| `AvailableInIndexes` | `string[]?` | `null` | Indexes where this property is available for querying |

### Supported Operations

The `DynamoDbOperation` enum defines these operations:

| Operation | Description | DynamoDB Operator |
|-----------|-------------|-------------------|
| `Equals` | Equality comparison | `=` |
| `BeginsWith` | String prefix match | `begins_with()` |
| `Between` | Range comparison | `BETWEEN` |
| `GreaterThan` | Greater than | `>` |
| `LessThan` | Less than | `<` |
| `Contains` | Set/string contains | `contains()` |
| `In` | Multiple value match | `IN` |

### Example

```csharp
[DynamoDbTable("users")]
public partial class User
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    [Queryable(SupportedOperations = new[] { DynamoDbOperation.Equals })]
    public string UserId { get; set; } = string.Empty;
    
    [SortKey]
    [DynamoDbAttribute("sk")]
    [Queryable(SupportedOperations = new[] { 
        DynamoDbOperation.Equals,
        DynamoDbOperation.BeginsWith,
        DynamoDbOperation.Between
    })]
    public string Username { get; set; } = string.Empty;
    
    [GlobalSecondaryIndex("email-index", IsPartitionKey = true)]
    [DynamoDbAttribute("email")]
    [Queryable(
        SupportedOperations = new[] { DynamoDbOperation.Equals },
        AvailableInIndexes = new[] { "email-index" }
    )]
    public string Email { get; set; } = string.Empty;
    
    [GlobalSecondaryIndex("status-index", IsPartitionKey = true)]
    [DynamoDbAttribute("status")]
    [Queryable(
        SupportedOperations = new[] { DynamoDbOperation.Equals, DynamoDbOperation.In },
        AvailableInIndexes = new[] { "status-index" }
    )]
    public string Status { get; set; } = string.Empty;
    
    [DynamoDbAttribute("tags")]
    [Queryable(SupportedOperations = new[] { DynamoDbOperation.Contains })]
    public List<string> Tags { get; set; } = new();
}
```

### Current Usage

This attribute is currently informational and used for:

- **Documentation**: Clearly indicates which operations are valid for each property
- **Future LINQ Support**: Metadata for potential LINQ-to-DynamoDB query translation
- **Code Generation**: May be used by future source generator enhancements

### Best Practices

- Document the intended query patterns for your entities
- Specify operations that make sense for the property type
- List all indexes where the property can be queried
- Use this to communicate query capabilities to other developers

### See Also

- [Querying Data](../core-features/QueryingData.md)
- [Global Secondary Indexes](../advanced-topics/GlobalSecondaryIndexes.md)

---

## [TimeToLive]

Marks a property as a Time-To-Live (TTL) field for automatic item expiration.

### Purpose

Enables DynamoDB's TTL feature, which automatically deletes items after a specified time. The property value is stored as Unix epoch seconds (number of seconds since January 1, 1970 UTC).

### Parameters

None

### Supported Types

- `DateTime` or `DateTime?`
- `DateTimeOffset` or `DateTimeOffset?`

### Example

```csharp
[DynamoDbTable("sessions")]
public partial class Session
{
    [DynamoDbAttribute("session_id")]
    public string SessionId { get; set; }
    
    [DynamoDbAttribute("ttl")]
    [TimeToLive]
    public DateTime? ExpiresAt { get; set; }
}

// Usage
var session = new Session
{
    SessionId = "sess-123",
    ExpiresAt = DateTime.UtcNow.AddHours(1) // Expires in 1 hour
};
```

### Important Notes

- Only ONE TTL field is allowed per entity (DYNDB105 error if violated)
- Must enable TTL on the table: `aws dynamodb update-time-to-live --table-name TABLE --time-to-live-specification "Enabled=true, AttributeName=ttl"`
- DynamoDB typically deletes expired items within 48 hours
- Use UTC times to avoid timezone issues
- Cannot be combined with `[JsonBlob]` or `[BlobReference]` (DYNDB104 error)

### See Also

- [Advanced Types Guide](../advanced-topics/AdvancedTypes.md#time-to-live-ttl-fields)
- [Advanced Types Examples](../examples/AdvancedTypesExamples.md#ttl-examples)

---

## [DynamoDbMap]

Marks a property for explicit conversion to a DynamoDB Map (M) type.

### Purpose

Converts custom objects to nested DynamoDB Map structures. The nested type must be marked with `[DynamoDbEntity]` to generate required mapping code, ensuring AOT compatibility without reflection.

### Parameters

None

### Example

```csharp
// Nested type MUST have [DynamoDbEntity]
[DynamoDbEntity]
public partial class Address
{
    [DynamoDbAttribute("street")]
    public string Street { get; set; }
    
    [DynamoDbAttribute("city")]
    public string City { get; set; }
}

[DynamoDbTable("customers")]
public partial class Customer
{
    [DynamoDbAttribute("pk")]
    public string CustomerId { get; set; }
    
    [DynamoDbAttribute("address")]
    [DynamoDbMap]
    public Address ShippingAddress { get; set; }
}
```

### Important Notes

- Nested type MUST be marked with `[DynamoDbEntity]` (DYNDB107 error if missing)
- Uses compile-time generated methods instead of reflection for AOT compatibility
- Nested types can themselves contain maps, creating deep hierarchies
- `Dictionary<string, string>` and `Dictionary<string, AttributeValue>` don't need this attribute

### See Also

- [Advanced Types Guide](../advanced-topics/AdvancedTypes.md#maps)
- [Advanced Types Examples](../examples/AdvancedTypesExamples.md#map-examples)

---

## [JsonBlob]

Marks a property for JSON serialization before storing in DynamoDB.

### Purpose

Serializes complex objects to JSON strings for storage in DynamoDB string attributes. Supports both System.Text.Json (AOT-compatible) and Newtonsoft.Json.

### Parameters

None

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `InlineThreshold` | `int?` | `null` | Maximum size (bytes) before using external blob storage |

### Example

```csharp
// Configure serializer at assembly level
[assembly: DynamoDbJsonSerializer(JsonSerializerType.SystemTextJson)]

public class DocumentContent
{
    public string Title { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

[DynamoDbTable("documents")]
public partial class Document
{
    [DynamoDbAttribute("doc_id")]
    public string DocumentId { get; set; }
    
    [DynamoDbAttribute("content")]
    [JsonBlob]
    public DocumentContent Content { get; set; }
}
```

### Package Requirements

Must reference one of:
- `Oproto.FluentDynamoDb.SystemTextJson` (recommended for AOT)
- `Oproto.FluentDynamoDb.NewtonsoftJson` (limited AOT support)

### Important Notes

- Requires JSON serializer package reference (DYNDB102 error if missing)
- System.Text.Json generates `JsonSerializerContext` for full AOT support
- Newtonsoft.Json uses runtime reflection with limited AOT support
- Can be combined with `[BlobReference]` for large objects
- Cannot be combined with `[TimeToLive]` (DYNDB104 error)

### See Also

- [Advanced Types Guide](../advanced-topics/AdvancedTypes.md#json-blob-serialization)
- [Advanced Types Examples](../examples/AdvancedTypesExamples.md#json-blob-examples)

---

## [BlobReference]

Marks a property for external blob storage with only a reference in DynamoDB.

### Purpose

Stores large data externally (e.g., S3) with only a reference key in DynamoDB. Useful for data larger than DynamoDB's 400KB item limit.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `provider` | `BlobProvider` | Yes | Blob storage provider (S3 or Custom) |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BucketName` | `string?` | `null` | S3 bucket name |
| `KeyPrefix` | `string?` | `null` | S3 key prefix |
| `ProviderType` | `Type?` | `null` | Custom provider type |

### Example

```csharp
[DynamoDbTable("files")]
public partial class FileMetadata
{
    [DynamoDbAttribute("file_id")]
    public string FileId { get; set; }
    
    [DynamoDbAttribute("data_ref")]
    [BlobReference(BlobProvider.S3, BucketName = "my-files", KeyPrefix = "uploads")]
    public byte[] Data { get; set; }
}

// Setup
var s3Client = new AmazonS3Client();
var blobProvider = new S3BlobProvider(s3Client, "my-files", "uploads");

// Save (async methods required for blob operations)
var item = await FileMetadata.ToDynamoDbAsync(file, blobProvider);

// Load
var loaded = await FileMetadata.FromDynamoDbAsync<FileMetadata>(item, blobProvider);
```

### Package Requirements

Must reference a blob provider package:
- `Oproto.FluentDynamoDb.BlobStorage.S3`

### Important Notes

- Requires blob provider package reference (DYNDB103 error if missing)
- Generates async `ToDynamoDbAsync` and `FromDynamoDbAsync` methods
- Can be combined with `[JsonBlob]` to serialize then store externally
- Cannot be combined with `[TimeToLive]` (DYNDB104 error)
- Blob provider must be passed to async methods

### See Also

- [Advanced Types Guide](../advanced-topics/AdvancedTypes.md#external-blob-storage)
- [Advanced Types Examples](../examples/AdvancedTypesExamples.md#blob-reference-examples)

---

## [DynamoDbJsonSerializer]

Assembly-level attribute to configure the JSON serializer for `[JsonBlob]` properties.

### Purpose

Specifies which JSON serializer to use when multiple serializer packages are referenced.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `serializerType` | `JsonSerializerType` | Yes | SystemTextJson or NewtonsoftJson |

### Example

```csharp
// At assembly level (typically in a separate file or at top of Program.cs)
[assembly: DynamoDbJsonSerializer(JsonSerializerType.SystemTextJson)]

namespace MyApp
{
    // Your entities...
}
```

### Serializer Types

| Type | AOT Support | Notes |
|------|-------------|-------|
| `SystemTextJson` | ✅ Full | Recommended for AOT projects |
| `NewtonsoftJson` | ⚠️ Limited | Uses runtime reflection |

### Important Notes

- Only needed when both serializer packages are referenced
- If only one package is referenced, it's used automatically
- Applies to all `[JsonBlob]` properties in the assembly

### See Also

- [Advanced Types Guide](../advanced-topics/AdvancedTypes.md#json-blob-serialization)
- [AOT Compatibility](../advanced-topics/AdvancedTypes.md#aot-compatibility)

---

## Summary

These attributes work together to define your DynamoDB entity schema:

### Core Attributes
1. **[DynamoDbTable]**: Required on every entity class
2. **[DynamoDbAttribute]**: Required on every persisted property
3. **[PartitionKey]** and **[SortKey]**: Define table keys
4. **[GlobalSecondaryIndex]**: Enable alternative query patterns

### Composite Key Attributes
5. **[Computed]** and **[Extracted]**: Handle composite keys
6. **[RelatedEntity]**: Enable composite entity patterns

### Advanced Type Attributes
7. **[TimeToLive]**: Automatic item expiration
8. **[DynamoDbMap]**: Nested object mapping
9. **[JsonBlob]**: JSON serialization
10. **[BlobReference]**: External blob storage
11. **[DynamoDbJsonSerializer]**: Configure JSON serializer (assembly-level)

### Metadata Attributes
12. **[Queryable]**: Document query capabilities

The source generator reads these attributes at compile time and generates type-safe code for working with DynamoDB.


## See Also

- [Entity Definition](../core-features/EntityDefinition.md) - See attributes used in context
- [First Entity Guide](../getting-started/FirstEntity.md) - Step-by-step entity creation
- [Global Secondary Indexes](../advanced-topics/GlobalSecondaryIndexes.md) - GSI attribute usage
- [Composite Entities](../advanced-topics/CompositeEntities.md) - RelatedEntity attribute usage
- [Troubleshooting](Troubleshooting.md) - Common attribute-related issues
