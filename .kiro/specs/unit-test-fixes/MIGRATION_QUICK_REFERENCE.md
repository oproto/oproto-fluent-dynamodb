# Migration Quick Reference Guide

This guide provides quick reference patterns for migrating tests from string-based assertions to semantic assertions.

---

## Before You Start

1. ✅ Read the test file to understand what it's testing
2. ✅ Run the tests to ensure they all pass
3. ✅ Check if compilation verification is already added
4. ✅ Identify DynamoDB-specific checks to preserve

---

## Migration Patterns

### 1. Add Compilation Verification

**When:** Every test that generates code  
**Where:** After diagnostic checks, before assertions

```csharp
// Before
var result = MapperGenerator.GenerateEntityImplementation(entity);
result.Should().Contain("public static string Pk(");

// After
var result = MapperGenerator.GenerateEntityImplementation(entity);
var entitySource = CreateEntitySource(entity); // Helper method
CompilationVerifier.AssertGeneratedCodeCompiles(result, entitySource);
result.ShouldContainMethod("Pk");
```

**With Multiple Source Files:**
```csharp
var relatedSources = CreateRelatedEntitySources(entity);
CompilationVerifier.AssertGeneratedCodeCompiles(
    result, 
    new[] { entitySource }.Concat(relatedSources).ToArray()
);
```

---

### 2. Replace Method Existence Checks

**Pattern:** `Should().Contain("public static MethodName")`  
**Replace With:** `ShouldContainMethod("MethodName")`

```csharp
// Before
result.Should().Contain("public static string Pk(string id)");
result.Should().Contain("public static Dictionary<string, AttributeValue> ToDynamoDb");

// After
result.ShouldContainMethod("Pk");
result.ShouldContainMethod("ToDynamoDb");
```

**With "because" message:**
```csharp
result.ShouldContainMethod("Pk", "should generate partition key builder");
```

---

### 3. Replace Assignment Checks

**Pattern:** `Should().Contain("variableName = ")`  
**Replace With:** `ShouldContainAssignment("variableName")`

```csharp
// Before
result.Should().Contain("entity.Id = ");
result.Should().Contain("entity.Name = ");
result.Should().Contain("var keyValue = ");

// After
result.ShouldContainAssignment("entity.Id");
result.ShouldContainAssignment("entity.Name");
result.ShouldContainAssignment("keyValue");
```

**With "because" message:**
```csharp
result.ShouldContainAssignment("entity.Id", "should assign partition key value");
```

---

### 4. Replace LINQ Usage Checks

**Pattern:** `Should().Contain(".Select(")`  
**Replace With:** `ShouldUseLinqMethod("Select")`

```csharp
// Before
result.Should().Contain(".Select(");
result.Should().Contain(".ToList()");
result.Should().Contain(".ToHashSet()");
result.Should().Contain(".Where(");

// After
result.ShouldUseLinqMethod("Select");
result.ShouldUseLinqMethod("ToList");
result.ShouldUseLinqMethod("ToHashSet");
result.ShouldUseLinqMethod("Where");
```

**With "because" message:**
```csharp
result.ShouldUseLinqMethod("Select", "should transform collection elements");
```

---

### 5. Replace Type Reference Checks

**Pattern:** `Should().Contain("typeof(TypeName)")`  
**Replace With:** `ShouldReferenceType("TypeName")`

```csharp
// Before
result.Should().Contain("typeof(TestEntity)");
result.Should().NotContain("typeof(Id)");

// After
result.ShouldReferenceType("TestEntity");
// Note: No negative assertion needed - semantic check verifies correct type
```

**With "because" message:**
```csharp
result.ShouldReferenceType("TestEntity", "should use entity class name in error handling");
```

---

### 6. Preserve DynamoDB Attribute Type Checks

**Pattern:** `Should().Contain("S =")` etc.  
**Keep:** Yes, but add "because" messages

```csharp
// Before
result.Should().Contain("S =");
result.Should().Contain("N =");
result.Should().Contain("SS =");
result.Should().Contain("NS =");
result.Should().Contain("L =");
result.Should().Contain("M =");

// After (keep with descriptive messages)
result.Should().Contain("S =", "should use String type for string properties");
result.Should().Contain("N =", "should use Number type for numeric properties");
result.Should().Contain("SS =", "should use String Set for HashSet<string>");
result.Should().Contain("NS =", "should use Number Set for HashSet<int>");
result.Should().Contain("L =", "should use List type for List<T>");
result.Should().Contain("M =", "should use Map type for Dictionary<,>");
```

---

### 7. Preserve Null Handling Checks

**Pattern:** `Should().Contain("!= null")`  
**Keep:** Yes, but add "because" messages

```csharp
// Before
result.Should().Contain("if (typedEntity.Tags != null)");
result.Should().Contain("entity.Tags.Count > 0");

// After (keep with descriptive messages)
result.Should().Contain("!= null", "should check for null before adding to DynamoDB item");
result.Should().Contain("Count > 0", "should check for empty collections before adding to DynamoDB item");
```

---

### 8. Preserve Key Format Checks

**Pattern:** `Should().Contain("var keyValue = ")`  
**Keep:** Yes, but add "because" messages

```csharp
// Before
result.Should().Contain("var keyValue = \"tenant#\" + id;");
result.Should().Contain("var keyValue = \"txn#\" + transactionId.ToString();");

// After (keep with descriptive messages)
result.Should().Contain("var keyValue = \"tenant#\" + id", "should use correct partition key format");
result.Should().Contain("var keyValue = \"txn#\" + transactionId.ToString()", "should use correct sort key format");
```

---

## Decision Tree

```
Is this a string assertion?
├─ Yes → What is it checking?
│  ├─ Method existence → Use ShouldContainMethod()
│  ├─ Assignment → Use ShouldContainAssignment()
│  ├─ LINQ usage → Use ShouldUseLinqMethod()
│  ├─ Type reference → Use ShouldReferenceType()
│  ├─ DynamoDB attribute type (S, N, SS, NS, L, M) → Keep with "because"
│  ├─ Null handling (!= null, Count > 0) → Keep with "because"
│  ├─ Key format (prefix + value) → Keep with "because"
│  └─ Other → Evaluate case-by-case
└─ No → Leave as-is
```

---

## Common Mistakes to Avoid

### ❌ Don't Remove DynamoDB-Specific Checks

```csharp
// WRONG - Removes important behavior verification
result.ShouldContainMethod("ToDynamoDb");
// Missing: Check that it uses "SS =" for HashSet<string>

// RIGHT - Keeps DynamoDB-specific behavior check
result.ShouldContainMethod("ToDynamoDb");
result.Should().Contain("SS =", "should use String Set for HashSet<string>");
```

---

### ❌ Don't Forget Compilation Verification

```csharp
// WRONG - No compilation check
var result = MapperGenerator.GenerateEntityImplementation(entity);
result.ShouldContainMethod("ToDynamoDb");

// RIGHT - Verifies code compiles
var result = MapperGenerator.GenerateEntityImplementation(entity);
var entitySource = CreateEntitySource(entity);
CompilationVerifier.AssertGeneratedCodeCompiles(result, entitySource);
result.ShouldContainMethod("ToDynamoDb");
```

---

### ❌ Don't Use Semantic Assertions for Values

```csharp
// WRONG - Semantic assertions don't check constant values
result.ShouldContainAssignment("Id");
// Missing: Check that Id is assigned to "pk"

// RIGHT - Use string check for constant values
result.Should().Contain("public const string Id = \"pk\"", "should map Id property to pk attribute");
```

---

### ❌ Don't Forget "because" Messages

```csharp
// WRONG - No explanation for why check is needed
result.Should().Contain("SS =");

// RIGHT - Explains the DynamoDB-specific requirement
result.Should().Contain("SS =", "should use String Set for HashSet<string>");
```

---

## Validation Checklist

After migrating a test file:

- [ ] All tests pass
- [ ] Compilation verification added to all generator tests
- [ ] Structural checks replaced with semantic assertions
- [ ] DynamoDB-specific checks preserved with "because" messages
- [ ] Run tests with intentional formatting changes (should pass)
- [ ] Run tests with intentional code errors (should fail)
- [ ] Error messages are clear and actionable
- [ ] File header comment added documenting migration
- [ ] MIGRATION_STATUS.md updated

---

## Example: Complete Test Migration

### Before

```csharp
[Fact]
public void GenerateEntityImplementation_WithBasicEntity_ProducesCorrectCode()
{
    // Arrange
    var entity = new EntityModel
    {
        ClassName = "TestEntity",
        Namespace = "TestNamespace",
        Properties = new[]
        {
            new PropertyModel
            {
                PropertyName = "Id",
                AttributeName = "pk",
                PropertyType = "string",
                IsPartitionKey = true
            }
        }
    };

    // Act
    var result = MapperGenerator.GenerateEntityImplementation(entity);

    // Assert
    result.Should().Contain("namespace TestNamespace");
    result.Should().Contain("public partial class TestEntity");
    result.Should().Contain("public static Dictionary<string, AttributeValue> ToDynamoDb");
    result.Should().Contain("entity.Id = idValue.S");
    result.Should().Contain("item[\"pk\"] = new AttributeValue { S = typedEntity.Id }");
}
```

### After

```csharp
[Fact]
public void GenerateEntityImplementation_WithBasicEntity_ProducesCorrectCode()
{
    // Arrange
    var entity = new EntityModel
    {
        ClassName = "TestEntity",
        Namespace = "TestNamespace",
        Properties = new[]
        {
            new PropertyModel
            {
                PropertyName = "Id",
                AttributeName = "pk",
                PropertyType = "string",
                IsPartitionKey = true
            }
        }
    };

    // Act
    var result = MapperGenerator.GenerateEntityImplementation(entity);

    // Verify compilation
    var entitySource = CreateEntitySource(entity);
    CompilationVerifier.AssertGeneratedCodeCompiles(result, entitySource);

    // Assert structure
    result.Should().Contain("namespace TestNamespace");
    result.Should().Contain("public partial class TestEntity");
    result.ShouldContainMethod("ToDynamoDb");
    result.ShouldContainAssignment("entity.Id");
    
    // Assert DynamoDB-specific behavior
    result.Should().Contain("S =", "should use String type for string properties");
}
```

---

## Helper Methods

### CreateEntitySource (for MapperGeneratorTests)

```csharp
private static string CreateEntitySource(EntityModel entity)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();
    sb.AppendLine($"namespace {entity.Namespace}");
    sb.AppendLine("{");
    sb.AppendLine($"    public partial class {entity.ClassName}");
    sb.AppendLine("    {");
    foreach (var prop in entity.Properties)
    {
        sb.AppendLine($"        public {prop.PropertyType} {prop.PropertyName} {{ get; set; }}");
    }
    sb.AppendLine("    }");
    sb.AppendLine("}");
    return sb.ToString();
}
```

---

## Resources

- **SemanticAssertions Source:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/TestHelpers/SemanticAssertions.cs`
- **CompilationVerifier Source:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/TestHelpers/CompilationVerifier.cs`
- **Migration Status:** `.kiro/specs/unit-test-fixes/MIGRATION_STATUS.md`
- **Baseline Metrics:** `.kiro/specs/unit-test-fixes/BASELINE_METRICS.md`
- **Design Document:** `.kiro/specs/unit-test-fixes/design.md`
