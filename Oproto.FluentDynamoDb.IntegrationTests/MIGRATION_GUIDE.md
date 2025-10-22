# Test Migration Guide

This guide helps you migrate existing source generator tests to use the improved testing infrastructure with compilation verification and semantic assertions.

## Table of Contents

- [Overview](#overview)
- [Migration Strategy](#migration-strategy)
- [Adding Compilation Verification](#adding-compilation-verification)
- [Replacing String Checks with Semantic Assertions](#replacing-string-checks-with-semantic-assertions)
- [When to Use Each Test Type](#when-to-use-each-test-type)
- [Migration Examples](#migration-examples)
- [Common Patterns](#common-patterns)
- [Migration Checklist](#migration-checklist)

## Overview

The testing infrastructure has been upgraded to provide three complementary test layers:

1. **Compilation Tests**: Verify generated code compiles without errors
2. **Semantic Tests**: Check code structure using syntax tree analysis
3. **Integration Tests**: Verify code works with actual DynamoDB

This guide focuses on migrating existing unit tests to use compilation verification and semantic assertions, making them more maintainable and less brittle.

## Migration Strategy

### Incremental Approach

**Don't rewrite everything at once.** Instead:

1. **Start with high-value tests**: Tests that break frequently or test critical functionality
2. **Add compilation verification first**: Quick win with minimal changes
3. **Gradually replace string checks**: Replace brittle string matching with semantic assertions
4. **Keep critical string checks**: Some DynamoDB-specific checks are best left as strings
5. **Add integration tests for new features**: Use integration tests for new advanced type features

### Prioritization

**High Priority** (migrate first):
- Tests that break due to formatting changes
- Tests for core mapping logic
- Tests for advanced type handling
- Tests that are hard to understand

**Medium Priority**:
- Tests for key generation
- Tests for field generation
- Tests with complex string matching

**Low Priority** (can wait):
- Tests that rarely break
- Tests with simple, clear string checks
- Tests for edge cases

## Adding Compilation Verification

Compilation verification ensures generated code compiles without errors. This catches breaking changes early.

### Basic Pattern

**Before:**
```csharp
[Fact]
public void Generator_WithHashSetInt_GeneratesNumberSetConversion()
{
    // Arrange
    var source = @"
using System.Collections.Generic;
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    public partial class TestEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; }
        
        [DynamoDbAttribute(""category_ids"")]
        public HashSet<int>? CategoryIds { get; set; }
    }
}";

    // Act
    var result = GenerateCode(source);
    var entityCode = GetGeneratedSource(result, "TestEntity.g.cs");
    
    // Assert
    result.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
    entityCode.Should().Contain("NS =");
}
```

**After:**
```csharp
[Fact]
public void Generator_WithHashSetInt_GeneratesNumberSetConversion()
{
    // Arrange
    var source = @"
using System.Collections.Generic;
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    public partial class TestEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; }
        
        [DynamoDbAttribute(""category_ids"")]
        public HashSet<int>? CategoryIds { get; set; }
    }
}";

    // Act
    var result = GenerateCode(source);
    var entityCode = GetGeneratedSource(result, "TestEntity.g.cs");
    
    // Assert - Add compilation verification
    result.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
    CompilationVerifier.AssertGeneratedCodeCompiles(entityCode);
    
    entityCode.Should().Contain("NS =");
}
```

**Key Changes:**
- Added `CompilationVerifier.AssertGeneratedCodeCompiles(entityCode)`
- Placed after diagnostic check but before string assertions
- No other changes needed

### With External Type References

If generated code references types from other files:

```csharp
[Fact]
public void Generator_WithRelatedEntity_GeneratesCorrectReferences()
{
    var entitySource = @"...";
    var relatedEntitySource = @"...";
    
    var result = GenerateCode(entitySource, relatedEntitySource);
    var entityCode = GetGeneratedSource(result, "TestEntity.g.cs");
    
    // Pass additional sources for compilation
    CompilationVerifier.AssertGeneratedCodeCompiles(
        entityCode, 
        relatedEntitySource);
    
    // Rest of assertions...
}
```

### Benefits

- **Catches compilation errors**: Detects breaking changes immediately
- **Minimal changes**: Add one line to existing tests
- **Clear error messages**: Shows exact compilation errors with line numbers
- **No false positives**: Only fails on actual compilation issues

## Replacing String Checks with Semantic Assertions

Semantic assertions use syntax tree analysis instead of string matching, making tests less brittle.

### Method Existence

**Before:**
```csharp
entityCode.Should().Contain("public static string Pk(string id)");
```

**After:**
```csharp
entityCode.ShouldContainMethod("Pk");
```

**Benefits:**
- Doesn't break on formatting changes
- Doesn't care about parameter names
- Doesn't care about access modifiers (if not important)

### Assignment Verification

**Before:**
```csharp
entityCode.Should().Contain("var keyValue = \"tenant#\" + id;");
```

**After:**
```csharp
entityCode.ShouldContainAssignment("keyValue");
entityCode.Should().Contain("tenant#", "should use tenant prefix");
```

**Benefits:**
- Doesn't break on whitespace changes
- Separates structure check from value check
- More descriptive failure messages

### LINQ Usage

**Before:**
```csharp
entityCode.Should().Contain(".Select(");
entityCode.Should().Contain(".ToList()");
```

**After:**
```csharp
entityCode.ShouldUseLinqMethod("Select");
entityCode.ShouldUseLinqMethod("ToList");
```

**Benefits:**
- Doesn't break on formatting
- Clearer intent
- Better error messages

### Type References

**Before:**
```csharp
entityCode.Should().Contain("AttributeValue");
entityCode.Should().Contain("Dictionary<string, AttributeValue>");
```

**After:**
```csharp
entityCode.ShouldReferenceType("AttributeValue");
entityCode.Should().Contain("Dictionary<string, AttributeValue>", 
    "should use correct dictionary type");
```

**Benefits:**
- Separates type usage from specific declarations
- More flexible for refactoring

### When to Keep String Checks

**Keep string checks for:**

1. **DynamoDB-specific values**
   ```csharp
   entityCode.Should().Contain("NS =", "should use Number Set for HashSet<int>");
   entityCode.Should().Contain("SS =", "should use String Set for HashSet<string>");
   ```

2. **Specific format strings**
   ```csharp
   entityCode.Should().Contain("tenant#", "should use correct partition key prefix");
   ```

3. **Critical business logic**
   ```csharp
   entityCode.Should().Contain("if (value == null)", "should handle null values");
   ```

4. **When semantic check is too complex**
   ```csharp
   // If checking for specific lambda expression structure
   entityCode.Should().Contain(".Select(x => x.ToString())");
   ```

## When to Use Each Test Type

### Unit Tests (Generator Tests)

**Use for:**
- Testing code generation logic
- Verifying correct syntax tree analysis
- Testing attribute detection
- Testing diagnostic reporting

**Example:**
```csharp
[Fact]
public void Generator_WithInvalidAttribute_ReportsDiagnostic()
{
    var source = @"...";
    var result = GenerateCode(source);
    
    result.Diagnostics.Should().Contain(d => 
        d.Id == "FDDB001" && 
        d.Severity == DiagnosticSeverity.Error);
}
```

### Compilation Tests

**Use for:**
- Verifying generated code compiles
- Catching breaking changes
- Ensuring type references are correct

**Example:**
```csharp
[Fact]
public void Generator_WithComplexEntity_GeneratesCompilableCode()
{
    var source = @"...";
    var result = GenerateCode(source);
    var entityCode = GetGeneratedSource(result, "Entity.g.cs");
    
    CompilationVerifier.AssertGeneratedCodeCompiles(entityCode);
}
```

### Semantic Tests

**Use for:**
- Verifying code structure
- Checking method/property existence
- Verifying LINQ usage
- Testing without brittle string matching

**Example:**
```csharp
[Fact]
public void Generator_WithHashSet_GeneratesSelectMethod()
{
    var source = @"...";
    var result = GenerateCode(source);
    var entityCode = GetGeneratedSource(result, "Entity.g.cs");
    
    entityCode.ShouldContainMethod("ToDynamoDb");
    entityCode.ShouldUseLinqMethod("Select");
    entityCode.ShouldUseLinqMethod("ToList");
}
```

### Integration Tests

**Use for:**
- Verifying end-to-end functionality
- Testing with actual DynamoDB
- Verifying round-trip data integrity
- Testing complex scenarios

**Example:**
```csharp
[Collection("DynamoDB Local")]
public class HashSetIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task HashSetInt_RoundTrip_PreservesAllValues()
    {
        var entity = new TestEntity
        {
            Id = "test-1",
            CategoryIds = new HashSet<int> { 1, 2, 3 }
        };
        
        var loaded = await SaveAndLoadAsync(entity);
        
        loaded.CategoryIds.Should().BeEquivalentTo(entity.CategoryIds);
    }
}
```

## Migration Examples

### Example 1: Simple Generator Test

**Before:**
```csharp
[Fact]
public void MapperGenerator_GeneratesFromDynamoDbMethod()
{
    var source = @"
[DynamoDbTable(""users"")]
public partial class User
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""name"")]
    public string Name { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "User.g.cs");
    
    code.Should().Contain("public static User FromDynamoDb");
    code.Should().Contain("var entity = new User();");
    code.Should().Contain("entity.Id = ");
    code.Should().Contain("entity.Name = ");
}
```

**After:**
```csharp
[Fact]
public void MapperGenerator_GeneratesFromDynamoDbMethod()
{
    var source = @"
[DynamoDbTable(""users"")]
public partial class User
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""name"")]
    public string Name { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "User.g.cs");
    
    // Add compilation verification
    CompilationVerifier.AssertGeneratedCodeCompiles(code);
    
    // Use semantic assertions
    code.ShouldContainMethod("FromDynamoDb");
    code.ShouldReferenceType("User");
    code.ShouldContainAssignment("entity.Id");
    code.ShouldContainAssignment("entity.Name");
}
```

### Example 2: Advanced Type Test

**Before:**
```csharp
[Fact]
public void Generator_WithHashSetString_GeneratesStringSetConversion()
{
    var source = @"
[DynamoDbTable(""products"")]
public partial class Product
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""tags"")]
    public HashSet<string>? Tags { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "Product.g.cs");
    
    code.Should().Contain("if (entity.Tags != null && entity.Tags.Count > 0)");
    code.Should().Contain("[\"tags\"] = new AttributeValue { SS = entity.Tags.ToList() }");
    code.Should().Contain("entity.Tags = item[\"tags\"].SS.ToHashSet();");
}
```

**After:**
```csharp
[Fact]
public void Generator_WithHashSetString_GeneratesStringSetConversion()
{
    var source = @"
[DynamoDbTable(""products"")]
public partial class Product
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""tags"")]
    public HashSet<string>? Tags { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "Product.g.cs");
    
    // Add compilation verification
    CompilationVerifier.AssertGeneratedCodeCompiles(code);
    
    // Use semantic assertions for structure
    code.ShouldContainAssignment("entity.Tags");
    code.ShouldUseLinqMethod("ToList");
    code.ShouldUseLinqMethod("ToHashSet");
    
    // Keep string checks for DynamoDB-specific behavior
    code.Should().Contain("SS =", "should use String Set for HashSet<string>");
    code.Should().Contain("entity.Tags != null && entity.Tags.Count > 0", 
        "should check for null and empty before adding to item");
}
```

### Example 3: Complex Mapping Test

**Before:**
```csharp
[Fact]
public void Generator_WithListDecimal_GeneratesListConversion()
{
    var source = @"
[DynamoDbTable(""orders"")]
public partial class Order
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""prices"")]
    public List<decimal>? Prices { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "Order.g.cs");
    
    code.Should().Contain("if (entity.Prices != null && entity.Prices.Count > 0)");
    code.Should().Contain("[\"prices\"] = new AttributeValue");
    code.Should().Contain("L = entity.Prices.Select(x => new AttributeValue { N = x.ToString() }).ToList()");
    code.Should().Contain("entity.Prices = item[\"prices\"].L.Select(x => decimal.Parse(x.N)).ToList();");
}
```

**After:**
```csharp
[Fact]
public void Generator_WithListDecimal_GeneratesListConversion()
{
    var source = @"
[DynamoDbTable(""orders"")]
public partial class Order
{
    [PartitionKey]
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
    
    [DynamoDbAttribute(""prices"")]
    public List<decimal>? Prices { get; set; }
}";

    var result = GenerateCode(source);
    var code = GetGeneratedSource(result, "Order.g.cs");
    
    // Add compilation verification
    CompilationVerifier.AssertGeneratedCodeCompiles(code);
    
    // Use semantic assertions for structure
    code.ShouldContainAssignment("entity.Prices");
    code.ShouldUseLinqMethod("Select");
    code.ShouldUseLinqMethod("ToList");
    code.ShouldReferenceType("AttributeValue");
    
    // Keep string checks for DynamoDB-specific behavior
    code.Should().Contain("L =", "should use List for List<decimal>");
    code.Should().Contain("N =", "should use Number type for decimal");
    code.Should().Contain("decimal.Parse", "should parse decimal from string");
    code.Should().Contain("entity.Prices != null && entity.Prices.Count > 0",
        "should check for null and empty before adding to item");
}
```

## Common Patterns

### Pattern 1: Null Handling

**Before:**
```csharp
code.Should().Contain("if (entity.Property != null)");
```

**After:**
```csharp
code.Should().Contain("entity.Property != null", 
    "should check for null before accessing property");
```

### Pattern 2: Empty Collection Handling

**Before:**
```csharp
code.Should().Contain("if (entity.Items != null && entity.Items.Count > 0)");
```

**After:**
```csharp
code.Should().Contain("entity.Items != null && entity.Items.Count > 0",
    "should check for null and empty before adding to DynamoDB item");
```

### Pattern 3: Type Conversion

**Before:**
```csharp
code.Should().Contain(".Select(x => x.ToString())");
code.Should().Contain(".Select(x => int.Parse(x))");
```

**After:**
```csharp
code.ShouldUseLinqMethod("Select");
code.Should().Contain("ToString()", "should convert to string for DynamoDB");
code.Should().Contain("int.Parse", "should parse int from string");
```

### Pattern 4: DynamoDB Attribute Types

**Before:**
```csharp
code.Should().Contain("new AttributeValue { S = ");
code.Should().Contain("new AttributeValue { N = ");
code.Should().Contain("new AttributeValue { SS = ");
code.Should().Contain("new AttributeValue { NS = ");
code.Should().Contain("new AttributeValue { L = ");
code.Should().Contain("new AttributeValue { M = ");
```

**After:**
```csharp
code.ShouldReferenceType("AttributeValue");
code.Should().Contain("S =", "should use String type");
code.Should().Contain("N =", "should use Number type");
code.Should().Contain("SS =", "should use String Set type");
code.Should().Contain("NS =", "should use Number Set type");
code.Should().Contain("L =", "should use List type");
code.Should().Contain("M =", "should use Map type");
```

## Migration Checklist

Use this checklist when migrating a test file:

### Before Starting

- [ ] Read the test file and understand what it's testing
- [ ] Identify tests that break frequently
- [ ] Identify tests with complex string matching
- [ ] Check if integration tests would be more appropriate

### During Migration

- [ ] Add `CompilationVerifier.AssertGeneratedCodeCompiles()` to each test
- [ ] Replace method existence checks with `ShouldContainMethod()`
- [ ] Replace assignment checks with `ShouldContainAssignment()`
- [ ] Replace LINQ checks with `ShouldUseLinqMethod()`
- [ ] Replace type reference checks with `ShouldReferenceType()`
- [ ] Keep DynamoDB-specific string checks
- [ ] Keep critical business logic string checks
- [ ] Add descriptive "because" messages to remaining string checks

### After Migration

- [ ] Run tests to ensure they still pass
- [ ] Verify error messages are clear when tests fail
- [ ] Check that tests don't break on formatting changes
- [ ] Update test documentation if needed
- [ ] Consider adding integration tests for the feature

### Example Checklist for a Test File

```
File: MapperGeneratorTests.cs
- [x] Added compilation verification to all tests
- [x] Replaced method checks with ShouldContainMethod()
- [x] Replaced assignment checks with ShouldContainAssignment()
- [x] Kept DynamoDB attribute type checks as strings
- [x] Added "because" messages to string checks
- [x] All tests pass
- [x] Error messages are clear
- [ ] Consider adding integration test for complex mapping scenario
```

## Tips and Best Practices

### 1. Start Small

Migrate one test file at a time. Don't try to migrate everything at once.

### 2. Test Your Migration

After migrating a test, intentionally break the generated code to verify the test catches it.

### 3. Balance Precision and Maintainability

Don't over-specify. Check what matters, not every detail.

**Too specific:**
```csharp
code.Should().Contain("public static User FromDynamoDb(Dictionary<string, AttributeValue> item)");
```

**Better:**
```csharp
code.ShouldContainMethod("FromDynamoDb");
code.ShouldReferenceType("AttributeValue");
```

### 4. Use Descriptive "Because" Messages

When keeping string checks, explain why:

```csharp
code.Should().Contain("NS =", "should use Number Set for HashSet<int>");
```

### 5. Group Related Assertions

```csharp
// Structure checks
code.ShouldContainMethod("ToDynamoDb");
code.ShouldContainMethod("FromDynamoDb");

// Type checks
code.ShouldReferenceType("AttributeValue");
code.ShouldReferenceType("Dictionary");

// DynamoDB-specific checks
code.Should().Contain("SS =", "should use String Set");
code.Should().Contain("entity.Tags.ToList()", "should convert HashSet to List");
```

### 6. Consider Integration Tests

If a test is verifying end-to-end behavior, consider writing an integration test instead:

**Unit test (checks generated code):**
```csharp
[Fact]
public void Generator_WithHashSet_GeneratesCorrectCode()
{
    // ... checks generated code structure
}
```

**Integration test (verifies it works):**
```csharp
[Fact]
public async Task HashSet_RoundTrip_PreservesValues()
{
    var entity = new TestEntity { Tags = new HashSet<string> { "a", "b" } };
    var loaded = await SaveAndLoadAsync(entity);
    loaded.Tags.Should().BeEquivalentTo(entity.Tags);
}
```

Both are valuable, but integration tests provide higher confidence.

## Getting Help

If you encounter issues during migration:

1. Check this guide for similar examples
2. Review the [Test Writing Guide](./TEST_WRITING_GUIDE.md)
3. Look at recently migrated test files for patterns
4. Ask in team chat or create a GitHub issue

## Additional Resources

- [Integration Test README](./README.md)
- [Test Writing Guide](./TEST_WRITING_GUIDE.md)
- [CompilationVerifier Source](../Oproto.FluentDynamoDb.SourceGenerator.UnitTests/TestHelpers/CompilationVerifier.cs)
- [SemanticAssertions Source](../Oproto.FluentDynamoDb.SourceGenerator.UnitTests/TestHelpers/SemanticAssertions.cs)
