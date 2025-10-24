# LINQ Expression Support

## Overview

FluentDynamoDb provides LINQ-style expression support, allowing you to write type-safe queries using C# lambda expressions. This feature combines the power of compile-time type checking with the flexibility of DynamoDB's query language, eliminating common errors and improving developer productivity.

### Key Benefits

- **Type Safety**: Catch property name typos at compile time instead of runtime
- **IntelliSense Support**: Get autocomplete suggestions for properties and methods
- **Refactoring Safety**: Rename properties with confidence - expressions update automatically
- **Cleaner Code**: No manual parameter naming or attribute mapping required
- **Better Errors**: Clear, actionable error messages guide you to correct usage
- **AOT Compatible**: Works seamlessly in Native AOT environments

### Quick Example

```csharp
// Expression-based (type-safe, recommended)
await table.Query
    .Where<User>(x => x.PartitionKey == userId && x.SortKey.StartsWith("ORDER#"))
    .WithFilter<User>(x => x.Status == "ACTIVE" && x.Age >= 18)
    .ExecuteAsync();

// String-based equivalent (format strings)
await table.Query
    .Where("pk = {0} AND begins_with(sk, {1})", userId, "ORDER#")
    .WithFilter("#status = {0} AND #age >= {1}", "ACTIVE", 18)
    .WithAttribute("#status", "status")
    .WithAttribute("#age", "age")
    .ExecuteAsync();

// String-based equivalent (manual parameters)
await table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithValue(":pk", userId)
    .WithValue(":prefix", "ORDER#")
    .WithFilter("#status = :status AND #age >= :age")
    .WithAttribute("#status", "status")
    .WithAttribute("#age", "age")
    .WithValue(":status", "ACTIVE")
    .WithValue(":age", 18)
    .ExecuteAsync();
```

## Supported Operators

### Comparison Operators

All standard comparison operators are supported:

```csharp
// Equality
table.Query.Where<User>(x => x.Id == userId);
// Translates to: #attr0 = :p0

// Inequality
table.Query.WithFilter<User>(x => x.Status != "DELETED");
// Translates to: #attr0 <> :p0

// Less than
table.Query.WithFilter<User>(x => x.Age < 65);
// Translates to: #attr0 < :p0

// Greater than
table.Query.WithFilter<User>(x => x.Score > 100);
// Translates to: #attr0 > :p0

// Less than or equal
table.Query.WithFilter<User>(x => x.Age <= 18);
// Translates to: #attr0 <= :p0

// Greater than or equal
table.Query.WithFilter<User>(x => x.Score >= 50);
// Translates to: #attr0 >= :p0
```

### Logical Operators

Combine conditions with logical operators:

```csharp
// AND
table.Query.Where<User>(x => x.PartitionKey == userId && x.SortKey == sortKey);
// Translates to: (#attr0 = :p0) AND (#attr1 = :p1)

// OR
table.Query.WithFilter<User>(x => x.Type == "A" || x.Type == "B");
// Translates to: (#attr0 = :p0) OR (#attr0 = :p1)

// NOT
table.Query.WithFilter<User>(x => !x.Deleted);
// Translates to: NOT (#attr0)

// Complex combinations with parentheses
table.Query.WithFilter<User>(x => 
    (x.Active && x.Score > 50) || x.Premium);
// Translates to: ((#attr0) AND (#attr1 > :p0)) OR (#attr2)
```

## DynamoDB Functions

### StartsWith (begins_with)

Use `string.StartsWith()` for prefix matching on sort keys:

```csharp
table.Query
    .Where<Order>(x => x.CustomerId == customerId && x.OrderId.StartsWith("ORDER#"))
    .ExecuteAsync();
// Translates to: #attr0 = :p0 AND begins_with(#attr1, :p1)
```

### Contains

Use `string.Contains()` for substring matching:

```csharp
table.Query
    .WithFilter<User>(x => x.Email.Contains("@example.com"))
    .ExecuteAsync();
// Translates to: contains(#attr0, :p0)
```

### Between

Use the `Between()` extension method for range queries:

```csharp
table.Query
    .Where<User>(x => x.PartitionKey == userId && x.SortKey.Between("2024-01", "2024-12"))
    .ExecuteAsync();
// Translates to: #attr0 = :p0 AND #attr1 BETWEEN :p1 AND :p2
```

### AttributeExists

Check if an attribute exists:

```csharp
table.Query
    .WithFilter<User>(x => x.PhoneNumber.AttributeExists())
    .ExecuteAsync();
// Translates to: attribute_exists(#attr0)
```

### AttributeNotExists

Check if an attribute does not exist:

```csharp
table.Scan
    .WithFilter<User>(x => x.DeletedAt.AttributeNotExists())
    .ExecuteAsync();
// Translates to: attribute_not_exists(#attr0)
```

### Size

Get the size of a collection attribute:

```csharp
table.Query
    .WithFilter<User>(x => x.Items.Size() > 5)
    .ExecuteAsync();
// Translates to: size(#attr0) > :p0
```

## Value Capture

### Constants

Direct constant values are automatically captured:

```csharp
// String constant
table.Query.Where<User>(x => x.Id == "USER#123");

// Numeric constant
table.Query.WithFilter<User>(x => x.Age >= 18);

// Boolean constant
table.Query.WithFilter<User>(x => x.Active == true);

// Enum constant
table.Query.WithFilter<Order>(x => x.Status == OrderStatus.Pending);
```

### Local Variables

Variables from the surrounding scope are captured:

```csharp
var userId = "USER#123";
var minAge = 18;
var status = "ACTIVE";

table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter<User>(x => x.Age >= minAge && x.Status == status)
    .ExecuteAsync();
```

### Closure Captures

Properties from captured objects are evaluated:

```csharp
var user = GetCurrentUser();
var config = GetConfiguration();

table.Query
    .Where<Order>(x => x.CustomerId == user.Id)
    .WithFilter<Order>(x => x.Total > config.MinOrderAmount)
    .ExecuteAsync();
```

### Method Calls on Captured Values

You can call methods on captured values (but not on entity properties):

```csharp
// ✓ Valid: Method call on captured value
var userId = GetUserId();
table.Query
    .Where<User>(x => x.PartitionKey == userId.ToString())
    .ExecuteAsync();

// ✓ Valid: Complex expression on captured value
var date = DateTime.Now;
table.Query
    .WithFilter<Order>(x => x.CreatedDate > date.AddDays(-30))
    .ExecuteAsync();

// ✗ Invalid: Method call on entity property
table.Query
    .WithFilter<User>(x => x.Name.ToUpper() == "JOHN") // Error!
    .ExecuteAsync();
```

## Query vs Filter Expressions

Understanding the difference between `Where()` and `WithFilter()` is crucial for efficient queries.

### Where() - Key Condition Expression

Use `Where()` for partition key and sort key conditions. These are applied **before** reading items from DynamoDB:

```csharp
// Efficient: Only reads matching items
table.Query
    .Where<User>(x => x.PartitionKey == userId && x.SortKey.StartsWith("ORDER#"))
    .ExecuteAsync();
```

**Restrictions:**
- Only partition key and sort key properties allowed
- Reduces consumed read capacity
- Most efficient way to query

### WithFilter() - Filter Expression

Use `WithFilter()` for non-key attributes. These are applied **after** reading items:

```csharp
// Less efficient: Reads all items for userId, then filters
table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter<User>(x => x.Status == "ACTIVE")
    .ExecuteAsync();
```

**Characteristics:**
- Any property allowed
- Applied after items are read
- Reduces data transfer but not read capacity
- Still more efficient than filtering in application code

### Common Mistake

```csharp
// ✗ Error: Non-key property in Where()
table.Query
    .Where<User>(x => x.PartitionKey == userId && x.Status == "ACTIVE")
    .ExecuteAsync();
// Throws: InvalidKeyExpressionException
// "Property 'Status' is not a key attribute and cannot be used in Query().Where(). 
//  Use WithFilter() instead."

// ✓ Correct: Move non-key condition to WithFilter()
table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter<User>(x => x.Status == "ACTIVE")
    .ExecuteAsync();
```

## Three Approaches Comparison

FluentDynamoDb supports three approaches for writing queries. Choose based on your needs:

### 1. Expression-Based (Recommended)

**Best for:** Type-safe queries with known properties

```csharp
await table.Query
    .Where<User>(x => x.PartitionKey == userId && x.SortKey.StartsWith("ORDER#"))
    .WithFilter<User>(x => x.Status == "ACTIVE" && x.Age >= 18)
    .ExecuteAsync();
```

**Advantages:**
- ✓ Compile-time type checking
- ✓ IntelliSense support
- ✓ Refactoring safety
- ✓ Automatic parameter generation
- ✓ Clear error messages

**Disadvantages:**
- ✗ Not suitable for dynamic queries
- ✗ Limited to supported operators and functions

### 2. Format Strings

**Best for:** Simpler than manual parameters, more flexible than expressions

```csharp
await table.Query
    .Where("pk = {0} AND begins_with(sk, {1})", userId, "ORDER#")
    .WithFilter("#status = {0} AND #age >= {1}", "ACTIVE", 18)
    .WithAttribute("#status", "status")
    .WithAttribute("#age", "age")
    .ExecuteAsync();
```

**Advantages:**
- ✓ Simpler than manual parameters
- ✓ Supports all DynamoDB features
- ✓ Good for dynamic queries

**Disadvantages:**
- ✗ No compile-time type checking
- ✗ Manual attribute name mapping required

### 3. Manual Parameters

**Best for:** Maximum control and complex scenarios

```csharp
await table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithValue(":pk", userId)
    .WithValue(":prefix", "ORDER#")
    .WithFilter("#status = :status AND #age >= :age")
    .WithAttribute("#status", "status")
    .WithAttribute("#age", "age")
    .WithValue(":status", "ACTIVE")
    .WithValue(":age", 18)
    .ExecuteAsync();
```

**Advantages:**
- ✓ Maximum control
- ✓ Supports all DynamoDB features
- ✓ Explicit parameter management

**Disadvantages:**
- ✗ Most verbose
- ✗ Manual parameter naming
- ✗ No compile-time checking

## Migration Guide

### From Manual Parameters to Expressions

```csharp
// Before: Manual parameters (most verbose)
await table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithFilter("#status = :status AND #age >= :minAge")
    .WithAttribute("#status", "status")
    .WithAttribute("#age", "age")
    .WithValue(":pk", userId)
    .WithValue(":prefix", "ORDER#")
    .WithValue(":status", "ACTIVE")
    .WithValue(":minAge", 18)
    .ExecuteAsync();

// After: Expression-based (type-safe, concise)
await table.Query
    .Where<User>(x => x.PartitionKey == userId && x.SortKey.StartsWith("ORDER#"))
    .WithFilter<User>(x => x.Status == "ACTIVE" && x.Age >= 18)
    .ExecuteAsync();

// Benefits:
// - 60% less code
// - No manual parameter naming
// - No manual attribute mapping
// - Compile-time type checking
// - IntelliSense support
```

### Gradual Migration Strategy

You can mix approaches in the same query:

```csharp
// Step 1: Migrate Where() to expression-based
await table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter("#status = {0}", "ACTIVE")
    .WithAttribute("#status", "status")
    .ExecuteAsync();

// Step 2: Migrate WithFilter() to expression-based
await table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter<User>(x => x.Status == "ACTIVE")
    .ExecuteAsync();
```

### When to Keep String-Based

Keep string-based expressions for:

1. **Dynamic queries built at runtime**
```csharp
var conditions = new List<string>();
var values = new List<object>();
if (includeActive) 
{
    conditions.Add($"#status = {{{values.Count}}}");
    values.Add("ACTIVE");
}
var expression = string.Join(" AND ", conditions);
table.Query.Where(expression, values.ToArray());
```

2. **Complex expressions not yet supported**
```csharp
table.Query
    .Where("attribute_type(#data, {0})", "S")
    .WithAttribute("#data", "data")
    .ExecuteAsync();
```

3. **Existing code that works well**
- No need to migrate if string-based code is working
- Focus migration efforts on new code

## Valid vs Invalid Patterns

### ✓ Valid Patterns

```csharp
// Property access
x => x.PropertyName

// Constant values
x => x.Id == "USER#123"

// Local variables
x => x.Id == userId

// Closure captures
x => x.Id == user.Id

// Method calls on captured values
x => x.Id == userId.ToString()
x => x.CreatedDate > date.AddDays(-30)

// Complex conditions
x => (x.Active && x.Score > 50) || x.Premium

// DynamoDB functions
x => x.Name.StartsWith("John")
x => x.Age.Between(18, 65)
x => x.Email.AttributeExists()
x => x.Items.Size() > 0
```

### ✗ Invalid Patterns

```csharp
// Assignment (use == for comparison)
x => x.Id = "123" // ✗ Error
x => x.Id == "123" // ✓ Correct

// Method calls on entity properties
x => x.Name.ToUpper() == "JOHN" // ✗ Error
var upperName = "JOHN";
x => x.Name == upperName // ✓ Correct

// Methods referencing entity parameter
x => x.Id == MyFunction(x) // ✗ Error
var computedId = MyFunction(someValue);
x => x.Id == computedId // ✓ Correct

// LINQ operations on entity properties
x => x.Items.Select(i => i.Name).Contains("test") // ✗ Error
x => x.Items.Contains("test") // ✓ Correct (if Items is a collection)

// Unsupported operators
x => x.Age % 2 == 0 // ✗ Error (modulo not supported)
// Filter in application code after retrieval instead

// Complex transformations
x => x.Items.Where(i => i.Active).Count() > 0 // ✗ Error
x => x.Items.Size() > 0 // ✓ Correct
```

## Troubleshooting Common Issues

### Issue: InvalidKeyExpressionException

**Error:**
```
Property 'Status' is not a key attribute and cannot be used in Query().Where(). 
Use WithFilter() instead.
```

**Solution:**
Move non-key properties to `WithFilter()`:

```csharp
// ✗ Wrong
table.Query.Where<User>(x => x.PartitionKey == userId && x.Status == "ACTIVE");

// ✓ Correct
table.Query
    .Where<User>(x => x.PartitionKey == userId)
    .WithFilter<User>(x => x.Status == "ACTIVE");
```

### Issue: UnmappedPropertyException

**Error:**
```
Property 'Email' on type 'User' does not map to a DynamoDB attribute.
```

**Solution:**
Add `[DynamoDbAttribute]` to the property:

```csharp
[DynamoDbTable("users")]
public partial class User
{
    [DynamoDbAttribute("email")] // Add this
    public string Email { get; set; }
}
```

### Issue: UnsupportedExpressionException

**Error:**
```
Method 'ToUpper' cannot be used on entity properties in DynamoDB expressions.
```

**Solution:**
Transform values before the query:

```csharp
// ✗ Wrong
table.Query.WithFilter<User>(x => x.Name.ToUpper() == "JOHN");

// ✓ Correct
var upperName = "JOHN";
table.Query.WithFilter<User>(x => x.Name == upperName);
```

### Issue: Method References Entity Parameter

**Error:**
```
Method 'myFunction' cannot reference the entity parameter or its properties.
```

**Solution:**
Evaluate the method before the query:

```csharp
// ✗ Wrong
table.Query.Where<User>(x => x.Id == myFunction(x));

// ✓ Correct
var computedId = myFunction(someValue);
table.Query.Where<User>(x => x.Id == computedId);
```

## Performance Considerations

### Expression Caching

Expression translation is cached automatically:

```csharp
// First call - translates and caches
await table.Query
    .Where<User>(x => x.PartitionKey == userId1)
    .ExecuteAsync();

// Second call - uses cached translation
await table.Query
    .Where<User>(x => x.PartitionKey == userId2)
    .ExecuteAsync();
// Same expression structure, different value
// Translation is cached, only parameter values differ
```

### Overhead

Expression-based queries have minimal overhead:
- Expression tree is built by compiler (zero runtime allocation)
- Translation uses StringBuilder (minimal allocations)
- Caching eliminates repeated translation cost
- Typically < 5% overhead vs string-based

### When Performance Matters

For performance-critical paths:
- Use expression-based for frequently-used patterns (benefits from caching)
- Use string-based for one-off dynamic queries
- Profile your specific use case

## Complete Examples

### E-commerce Order Query

```csharp
var customerId = "CUSTOMER#123";
var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
var minAmount = 50.00m;

var orders = await table.Query
    .Where<Order>(x => 
        x.CustomerId == customerId && 
        x.OrderDate.Between(thirtyDaysAgo, DateTime.UtcNow))
    .WithFilter<Order>(x => 
        x.Status == "SHIPPED" && 
        x.Total >= minAmount && 
        x.Items.Size() > 0)
    .ExecuteAsync();
```

### User Search with Multiple Criteria

```csharp
var region = "US-WEST";
var minAge = 18;

var users = await table.Query
    .OnIndex("RegionIndex")
    .Where<User>(x => x.Region == region)
    .WithFilter<User>(x => 
        x.Active && 
        x.Age >= minAge && 
        x.Email.AttributeExists() && 
        x.EmailVerified)
    .ExecuteAsync();
```

### Conditional Put

```csharp
var newUser = new User 
{ 
    Id = "USER#123", 
    Name = "John Doe" 
};

await table.PutItem(newUser)
    .WithCondition<User>(x => x.Id.AttributeNotExists())
    .ExecuteAsync();
```

### Scan with Complex Filter

```csharp
var minScore = 1000;

var users = await table.Scan
    .WithFilter<User>(x => 
        x.Premium || 
        (x.Score >= minScore && x.Active))
    .ExecuteAsync();
```

## Additional Resources

- **[EXPRESSION_EXAMPLES.md](../../Oproto.FluentDynamoDb/Expressions/EXPRESSION_EXAMPLES.md)** - Comprehensive examples and patterns
- **[Querying Data Guide](QueryingData.md)** - General querying documentation
- **[Expression Formatting Guide](ExpressionFormatting.md)** - Format string approach
- **[Troubleshooting Guide](../reference/Troubleshooting.md)** - Common issues and solutions

## Summary

LINQ expression support provides a type-safe, intuitive way to write DynamoDB queries:

- Use `Where<T>()` for key conditions (partition key and sort key)
- Use `WithFilter<T>()` for non-key attributes
- Supported operators: `==`, `!=`, `<`, `>`, `<=`, `>=`, `&&`, `||`, `!`
- Supported functions: `StartsWith`, `Contains`, `Between`, `AttributeExists`, `AttributeNotExists`, `Size`
- Value capture works for constants, variables, closures, and method calls on captured values
- Method calls on entity properties are not supported
- Expression translation is cached for performance
- Clear error messages guide you to correct usage
- String-based expressions remain available for complex scenarios
- You can mix expression-based and string-based in the same query
