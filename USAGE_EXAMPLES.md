# Oproto.FluentDynamoDb Usage Examples

This document demonstrates the enhanced functionality introduced in the fluent API refactoring, focusing on format string support in condition expressions.

## Migration Guide

### No Breaking Changes

The refactoring maintains full backward compatibility. Your existing code will continue to work without any changes:

```csharp
// This code continues to work exactly as before
var result = await table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithValue(":pk", "USER#123")
    .WithValue(":prefix", "ORDER#")
    .ExecuteAsync();
```

### Required Using Statement

To access the new extension methods, ensure you have the appropriate using statement:

```csharp
using Oproto.FluentDynamoDb.Requests.Extensions;
```

## What's New: Format String Support in Where() Methods

The new `Where(string format, params object[] args)` overload eliminates the ceremony of manual parameter naming for condition expressions.

### Basic Format String Usage

```csharp
// New format string approach - cleaner and more concise
var result = await table.Query
    .Where("pk = {0} AND begins_with(sk, {1})", "USER#123", "ORDER#")
    .ExecuteAsync();

// Equivalent to the old approach:
// .Where("pk = :p0 AND begins_with(sk, :p1)")
// .WithValue(":p0", "USER#123")
// .WithValue(":p1", "ORDER#")
```

### DateTime Formatting

Format DateTime values automatically using standard .NET format specifiers:

```csharp
var startDate = new DateTime(2024, 1, 1);
var endDate = DateTime.Now;

// ISO 8601 formatting with {0:o}
var result = await table.Query
    .Where("pk = {0} AND created BETWEEN {1:o} AND {2:o}", 
           "USER#123", startDate, endDate)
    .ExecuteAsync();

// Results in: "pk = :p0 AND created BETWEEN :p1 AND :p2"
// With values: ":p1" = "2024-01-01T00:00:00.000Z", ":p2" = "2024-01-15T10:30:00.000Z"
```

### Numeric Formatting

Format numeric values with precision control:

```csharp
var minAmount = 99.999m;
var maxAmount = 1000.5m;

// Fixed-point formatting with 2 decimal places
var result = await table.Query
    .Where("pk = {0} AND amount BETWEEN {1:F2} AND {2:F2}", 
           "USER#123", minAmount, maxAmount)
    .ExecuteAsync();

// Results in: ":p1" = "100.00", ":p2" = "1000.50"
```

### Enum Handling and Reserved Words

Enums are automatically converted to strings, and you can combine format strings with attribute name mapping:

```csharp
public enum OrderStatus { Pending, Processing, Completed, Cancelled }

var status = OrderStatus.Processing;

var result = await table.Query
    .Where("pk = {0} AND #status = {1}", "USER#123", status)
    .WithAttribute("#status", "status")  // Maps #status to actual "status" attribute
    .ExecuteAsync();

// Results in: "pk = :p0 AND #status = :p1" with ":p1" = "Processing"
```

**Why both `#status` and `{1}`?**
- `#status` is an attribute name parameter that maps to the actual column name "status" (needed because "status" is a DynamoDB reserved word)
- `{1}` is a value parameter that gets replaced with the enum value "Processing"
- You need `WithAttribute("#status", "status")` to tell DynamoDB what `#status` refers to

## Before/After Comparisons

### Simple Condition

**Before (still supported):**
```csharp
var result = await table.Query
    .Where("pk = :userId AND active = :active")
    .WithValue(":userId", "USER#123")
    .WithValue(":active", true)
    .ExecuteAsync();
```

**After (new format string approach):**
```csharp
var result = await table.Query
    .Where("pk = {0} AND active = {1}", "USER#123", true)
    .ExecuteAsync();
```

### Complex Condition with Multiple Types

**Before:**
```csharp
var startDate = DateTime.Now.AddDays(-30);
var minAmount = 100.00m;
var status = OrderStatus.Completed;

var result = await table.Query
    .Where("pk = :pk AND created > :startDate AND amount >= :minAmount AND #status = :status")
    .WithValue(":pk", "USER#123")
    .WithValue(":startDate", startDate.ToString("o"))  // Manual formatting
    .WithValue(":minAmount", minAmount.ToString("F2"))  // Manual formatting
    .WithValue(":status", status.ToString())            // Manual conversion
    .WithAttribute("#status", "status")
    .ExecuteAsync();
```

**After:**
```csharp
var startDate = DateTime.Now.AddDays(-30);
var minAmount = 100.00m;
var status = OrderStatus.Completed;

var result = await table.Query
    .Where("pk = {0} AND created > {1:o} AND amount >= {2:F2} AND #status = {3}", 
           "USER#123", startDate, minAmount, status)
    .WithAttribute("#status", "status")  // Still needed for reserved word mapping
    .ExecuteAsync();
```

## What Operations Support Format Strings

Format string support is available in **condition expressions and filter expressions** - specifically the `Where()` and `WithFilter()` methods:

### Query Operations
```csharp
// Key condition expression (Where)
var result = await table.Query
    .Where("pk = {0} AND begins_with(sk, {1})", "USER#123", "ORDER#")
    .ExecuteAsync();

// Filter expression (WithFilter) - applied after key condition
var result = await table.Query
    .Where("pk = {0}", "USER#123")
    .WithFilter("#status = {0} AND #amount > {1:F2}", "ACTIVE", 100.50m)
    .WithAttribute("#status", "status")
    .WithAttribute("#amount", "amount")
    .ExecuteAsync();
```

### Update Operations (Conditional)
```csharp
await table.Update
    .WithKey("pk", "USER#123", "sk", "ORDER#456")
    .Set("SET #status = :newStatus")  // Set expressions still use traditional parameters
    .Where("attribute_exists(pk) AND version = {0}", expectedVersion)  // Where uses format strings
    .WithValue(":newStatus", "COMPLETED")
    .ExecuteAsync();
```

### Delete Operations (Conditional)
```csharp
await table.Delete
    .WithKey("pk", "USER#123", "sk", "ORDER#456")
    .Where("#status = {0} AND version = {1}", OrderStatus.Cancelled, expectedVersion)
    .WithAttribute("#status", "status")
    .ExecuteAsync();
```

### Put Operations (Conditional)
```csharp
await table.Put
    .WithKey("pk", "USER#123", "sk", "ORDER#456")
    .Where("attribute_not_exists(pk) OR version < {0}", maxVersion)
    .ExecuteAsync();
```

## Filter Expression Format Strings

Filter expressions are applied after items are retrieved based on key conditions, reducing data transfer but not consumed capacity. The new `WithFilter(string format, params object[] args)` overload provides the same format string benefits as condition expressions.

### Basic Filter Usage

```csharp
// New format string approach for filters
var result = await table.Query
    .Where("pk = {0}", "USER#123")
    .WithFilter("#status = {0} AND #amount > {1}", "ACTIVE", 100)
    .WithAttribute("#status", "status")
    .WithAttribute("#amount", "amount")
    .ExecuteAsync();

// Equivalent to the old approach:
// .WithFilter("#status = :p0 AND #amount > :p1")
// .WithValue(":p0", "ACTIVE")
// .WithValue(":p1", 100)
```

### Filter with DateTime and Numeric Formatting

```csharp
var createdAfter = DateTime.Now.AddDays(-30);
var minAmount = 99.999m;
var maxAmount = 1000.5m;

var result = await table.Query
    .Where("pk = {0}", "USER#123")
    .WithFilter("#created > {0:o} AND #amount BETWEEN {1:F2} AND {2:F2}", 
                createdAfter, minAmount, maxAmount)
    .WithAttribute("#created", "createdDate")
    .WithAttribute("#amount", "amount")
    .ExecuteAsync();
```

### Complex Filter Conditions

```csharp
// Multiple conditions with various functions
var result = await table.Query
    .Where("pk = {0}", "USER#123")
    .WithFilter("(#status = {0} OR #status = {1}) AND contains(#tags, {2}) AND size(#items) > {3}", 
                "ACTIVE", "PENDING", "important", 5)
    .WithAttribute("#status", "status")
    .WithAttribute("#tags", "tags")
    .WithAttribute("#items", "items")
    .ExecuteAsync();
```

### Scan Operations with Filters

```csharp
// Scan with filter expressions (use sparingly - scans are expensive)
var result = await table.AsScannable().Scan
    .WithFilter("#status = {0} AND #created > {1:o} AND #amount BETWEEN {2:F2} AND {3:F2}", 
                "ACTIVE", DateTime.Now.AddDays(-7), 50.0m, 500.0m)
    .WithAttribute("#status", "status")
    .WithAttribute("#created", "createdDate")
    .WithAttribute("#amount", "amount")
    .Take(100)
    .ExecuteAsync();
```

### Filter vs Condition Expression

**Key Condition Expression (`Where`)**: Applied during the query to determine which items to retrieve. More efficient.
```csharp
.Where("pk = {0} AND begins_with(sk, {1})", "USER#123", "ORDER#")  // Efficient - uses index
```

**Filter Expression (`WithFilter`)**: Applied after items are retrieved. Reduces data transfer but not consumed capacity.
```csharp
.WithFilter("#status = {0} AND #amount > {1}", "ACTIVE", 100)  // Applied after retrieval
```

## Mixed Usage Patterns

You can mix the old parameter style with the new format string approach in the same builder:

```csharp
// Combine format strings with manual parameters
var result = await table.Query
    .Where("pk = {0} AND sk BETWEEN :startSk AND :endSk AND created > {1:o}", 
           "USER#123", DateTime.Now.AddDays(-7))
    .WithValue(":startSk", "ORDER#2024-01")
    .WithValue(":endSk", "ORDER#2024-02")
    .ExecuteAsync();
```

This flexibility allows for gradual migration and handles cases where you need more control over specific parameters.

## Supported Format Specifiers

| Format | Description | Example Input | Example Output |
|--------|-------------|---------------|----------------|
| `o` | ISO 8601 DateTime | `DateTime.Now` | `2024-01-15T10:30:00.000Z` |
| `F2` | Fixed-point with 2 decimals | `99.999m` | `100.00` |
| `X` | Hexadecimal uppercase | `255` | `FF` |
| `x` | Hexadecimal lowercase | `255` | `ff` |
| `D` | Decimal integer | `123` | `123` |
| `P2` | Percentage with 2 decimals | `0.1234m` | `12.34%` |

## Error Handling

The library provides clear error messages for common mistakes:

```csharp
try
{
    // Invalid: Mismatched parameter count
    await table.Query
        .Where("pk = {0} AND sk = {1}", "USER#123")  // Missing second parameter
        .ExecuteAsync();
}
catch (ArgumentException ex)
{
    // Error: "Format string references parameter index 1 but only 1 arguments were provided"
}

try
{
    // Invalid: Unsupported format specifier
    await table.Query
        .Where("pk = {0} AND amount = {1:InvalidFormat}", "USER#123", 100.50m)
        .ExecuteAsync();
}
catch (FormatException ex)
{
    // Error: "Invalid format specifier 'InvalidFormat' for parameter at index 1"
}
```

## Best Practices

1. **Use format strings for condition and filter expressions** - They're more concise and less error-prone than manual parameter naming
2. **Understand the difference between Where and WithFilter**:
   - `Where()`: Key condition expressions - efficient, uses indexes, reduces consumed capacity
   - `WithFilter()`: Filter expressions - applied after retrieval, reduces data transfer only
3. **Continue using traditional parameters for other expressions** - Set expressions, key specifications, etc. still use the traditional approach
4. **Leverage format specifiers** - Use `:o` for DateTime, `:F2` for decimals, etc.
5. **Handle reserved words** - Continue using `WithAttribute()` for DynamoDB reserved words like "status", "name", "type"
6. **Use filters judiciously** - Filter expressions don't reduce consumed read capacity, only data transfer
7. **Mix approaches when needed** - Combine format strings with manual parameters for complex scenarios

## Architecture Benefits

The refactoring provides these improvements:

- **Reduced Maintenance**: Extension methods eliminate code duplication across 15+ builder classes
- **Better Developer Experience**: Format strings reduce ceremony and improve readability
- **Full Backward Compatibility**: All existing code continues to work unchanged
- **AOT Compatibility**: No reflection or dynamic code generation
- **Type Safety**: Automatic type conversion with clear error messages

## Conclusion

The enhanced fluent API maintains full backward compatibility while providing significant improvements in developer experience for condition and filter expressions. The format string support eliminates much of the ceremony around parameter handling while maintaining type safety and AOT compatibility.

Choose the approach that best fits your scenario:
- Use format strings for condition expressions (`Where()` methods) and filter expressions (`WithFilter()` methods)
- Use the traditional approach for other expressions and when you need fine-grained control
- Mix both approaches when transitioning existing code or handling complex scenarios