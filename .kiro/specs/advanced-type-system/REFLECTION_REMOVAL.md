# Reflection Removal for Nested Map Types

## Problem

Tasks 6.3 and 6.4 initially used reflection (`GetProperty`, `SetValue`, `GetValue`) to handle custom objects marked with `[DynamoDbMap]`. This violated the library's core principle of zero runtime reflection and broke AOT compatibility.

## Solution

Instead of using reflection, we now use **nested source-generated method calls**:

### For `ToDynamoDb` (Task 6.3)
```csharp
// OLD (with reflection):
var type = typeof(CustomType);
foreach (var prop in type.GetProperties())
{
    var value = prop.GetValue(obj);
    // ... convert value
}

// NEW (no reflection):
var nestedMap = CustomType.ToDynamoDb(typedEntity.PropertyName);
if (nestedMap != null && nestedMap.Count > 0)
{
    item["attribute_name"] = new AttributeValue { M = nestedMap };
}
```

### For `FromDynamoDb` (Task 6.4)
```csharp
// OLD (with reflection):
var type = typeof(CustomType);
var prop = type.GetProperty(name);
prop.SetValue(obj, value);

// NEW (no reflection):
entity.PropertyName = CustomType.FromDynamoDb<CustomType>(mapValue.M);
```

## Requirements

For this to work, **nested types must be marked with `[DynamoDbEntity]`**:

```csharp
[DynamoDbTable("products")]
public partial class Product
{
    [DynamoDbAttribute("attributes")]
    [DynamoDbMap]
    public ProductAttributes Attributes { get; set; }  // ← Must be [DynamoDbEntity]
}

[DynamoDbEntity]  // ← Required!
public partial class ProductAttributes
{
    [DynamoDbAttribute("color")]
    public string Color { get; set; }
    
    [DynamoDbAttribute("size")]
    public int? Size { get; set; }
}
```

## Benefits

1. **AOT Compatible**: No reflection means full Native AOT support
2. **Type Safe**: Compiler validates nested types have required methods
3. **Composable**: Nested types can themselves contain maps, creating deep hierarchies
4. **Performance**: Direct method calls instead of reflection overhead
5. **Maintainable**: All mapping logic is source-generated and visible

## Diagnostics

Added `DYNDB107` diagnostic to catch missing `[DynamoDbEntity]` on nested map types:

```
Error DYNDB107: Property 'Attributes' with [DynamoDbMap] has type 'ProductAttributes' 
which must be marked with [DynamoDbEntity] to generate mapping code. Nested map types 
require source-generated ToDynamoDb/FromDynamoDb methods to maintain AOT compatibility.
```

## Files Changed

1. **MapperGenerator.cs**: Removed reflection, added nested method calls
2. **DiagnosticDescriptors.cs**: Added DYNDB107 diagnostic
3. **design.md**: Updated examples and added nested type requirements
4. **tasks.md**: Updated 6.3 and 6.4 to reflect no-reflection approach
