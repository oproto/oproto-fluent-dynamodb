# Test Writing Guide

This guide provides templates and best practices for writing tests in the Oproto.FluentDynamoDb project.

## Table of Contents

- [Test Organization](#test-organization)
- [Test Templates](#test-templates)
- [Test Data Builders](#test-data-builders)
- [Best Practices](#best-practices)
- [Common Scenarios](#common-scenarios)
- [Naming Conventions](#naming-conventions)

## Test Organization

### Project Structure

Tests are organized into three main categories:

```
Oproto.FluentDynamoDb.SourceGenerator.UnitTests/
├── Generators/              # Tests for code generators
├── Analysis/                # Tests for analyzers
├── Models/                  # Tests for model classes
└── TestHelpers/            # Shared test utilities

Oproto.FluentDynamoDb.IntegrationTests/
├── Infrastructure/          # Test infrastructure
├── AdvancedTypes/          # Tests for HashSet, List, Dictionary
├── BasicTypes/             # Tests for basic CRUD operations
├── RealWorld/              # Complex scenario tests
└── TestEntities/           # Test entity definitions
```

### File Organization

- One test class per production class
- Group related tests using nested classes or regions
- Keep test files focused and manageable (< 500 lines)

## Test Templates

### Unit Test Template (Generator Test)

```csharp
using Xunit;
using FluentAssertions;
using Oproto.FluentDynamoDb.SourceGenerator.UnitTests.TestHelpers;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Generators
{
    public class MyGeneratorTests
    {
        [Fact]
        public void Generator_WithScenario_GeneratesExpectedCode()
        {
            // Arrange
            var source = @"
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    public partial class TestEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; }
        
        [DynamoDbAttribute(""name"")]
        public string Name { get; set; }
    }
}";

            // Act
            var result = GenerateCode(source);
            var entityCode = GetGeneratedSource(result, "TestEntity.g.cs");
            
            // Assert - No compilation errors
            result.Diagnostics.Should().NotContain(d => 
                d.Severity == DiagnosticSeverity.Error);
            
            // Assert - Code compiles
            CompilationVerifier.AssertGeneratedCodeCompiles(entityCode);
            
            // Assert - Semantic checks
            entityCode.ShouldContainMethod("ToDynamoDb");
            entityCode.ShouldContainMethod("FromDynamoDb");
            
            // Assert - DynamoDB-specific checks
            entityCode.Should().Contain("S =", "should use String type");
        }
        
        private GeneratorTestResult GenerateCode(string source)
        {
            // Implementation provided by test base class or helper
            throw new NotImplementedException();
        }
        
        private string GetGeneratedSource(GeneratorTestResult result, string fileName)
        {
            // Implementation provided by test base class or helper
            throw new NotImplementedException();
        }
    }
}
```

### Integration Test Template

```csharp
using Xunit;
using FluentAssertions;
using Oproto.FluentDynamoDb.IntegrationTests.Infrastructure;
using Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

namespace Oproto.FluentDynamoDb.IntegrationTests.AdvancedTypes
{
    [Collection("DynamoDB Local")]
    public class MyFeatureIntegrationTests : IntegrationTestBase
    {
        public MyFeatureIntegrationTests(DynamoDbLocalFixture fixture) 
            : base(fixture)
        {
        }
        
        public override async Task InitializeAsync()
        {
            await CreateTableAsync<TestEntity>();
        }
        
        [Fact]
        public async Task Feature_Scenario_ExpectedBehavior()
        {
            // Arrange
            var entity = new TestEntity
            {
                Id = "test-1",
                Name = "Test Name"
            };
            
            // Act
            var loaded = await SaveAndLoadAsync(entity);
            
            // Assert
            loaded.Should().BeEquivalentTo(entity);
        }
        
        [Fact]
        public async Task Feature_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var entity = new TestEntity
            {
                Id = "test-2",
                Name = null
            };
            
            // Act
            var loaded = await SaveAndLoadAsync(entity);
            
            // Assert
            loaded.Name.Should().BeNull();
        }
    }
}
```

### Diagnostic Test Template

```csharp
[Fact]
public void Analyzer_WithInvalidCode_ReportsDiagnostic()
{
    // Arrange
    var source = @"
[DynamoDbTable(""test-table"")]
public partial class TestEntity
{
    // Missing PartitionKey attribute
    [DynamoDbAttribute(""pk"")]
    public string Id { get; set; }
}";

    // Act
    var result = GenerateCode(source);
    
    // Assert
    result.Diagnostics.Should().Contain(d => 
        d.Id == "FDDB001" && 
        d.Severity == DiagnosticSeverity.Error &&
        d.GetMessage().Contains("PartitionKey"));
}
```

## Test Data Builders

### Basic Builder Pattern

```csharp
public class TestEntityBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string? _name;
    private int _age;
    
    public TestEntityBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public TestEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public TestEntityBuilder WithAge(int age)
    {
        _age = age;
        return this;
    }
    
    public TestEntity Build()
    {
        return new TestEntity
        {
            Id = _id,
            Name = _name,
            Age = _age
        };
    }
}
```

### Usage in Tests

```csharp
[Fact]
public async Task Entity_WithMultipleProperties_RoundTripsCorrectly()
{
    // Arrange - Clean and readable
    var entity = new TestEntityBuilder()
        .WithId("test-1")
        .WithName("John Doe")
        .WithAge(30)
        .Build();
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Should().BeEquivalentTo(entity);
}
```

### Advanced Builder with Collections

```csharp
public class AdvancedTypesEntityBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private HashSet<int>? _categoryIds;
    private HashSet<string>? _tags;
    private List<string>? _itemIds;
    private List<decimal>? _prices;
    private Dictionary<string, string>? _metadata;
    
    public AdvancedTypesEntityBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithCategoryIds(params int[] ids)
    {
        _categoryIds = new HashSet<int>(ids);
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithTags(params string[] tags)
    {
        _tags = new HashSet<string>(tags);
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithItemIds(params string[] ids)
    {
        _itemIds = new List<string>(ids);
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithPrices(params decimal[] prices)
    {
        _prices = new List<decimal>(prices);
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithMetadata(Dictionary<string, string> metadata)
    {
        _metadata = metadata;
        return this;
    }
    
    public AdvancedTypesEntityBuilder WithMetadata(params (string key, string value)[] pairs)
    {
        _metadata = pairs.ToDictionary(p => p.key, p => p.value);
        return this;
    }
    
    public AdvancedTypesEntity Build()
    {
        return new AdvancedTypesEntity
        {
            Id = _id,
            CategoryIds = _categoryIds,
            Tags = _tags,
            ItemIds = _itemIds,
            Prices = _prices,
            Metadata = _metadata
        };
    }
}
```

### Usage with Advanced Builder

```csharp
[Fact]
public async Task ComplexEntity_WithAllTypes_RoundTripsCorrectly()
{
    // Arrange
    var entity = new AdvancedTypesEntityBuilder()
        .WithId("test-1")
        .WithCategoryIds(1, 2, 3, 5, 8)
        .WithTags("new", "featured", "sale")
        .WithItemIds("item-1", "item-2", "item-3")
        .WithPrices(9.99m, 19.99m, 29.99m)
        .WithMetadata(
            ("key1", "value1"),
            ("key2", "value2"))
        .Build();
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Should().BeEquivalentTo(entity);
}
```

## Best Practices

### 1. Test Naming

Use descriptive names that explain what is being tested:

**Good:**
```csharp
[Fact]
public async Task HashSetInt_RoundTrip_PreservesAllValues()

[Fact]
public async Task HashSet_WithNullValue_LoadsAsNull()

[Fact]
public async Task HashSet_WithEmptySet_OmitsFromDynamoDB()
```

**Bad:**
```csharp
[Fact]
public async Task Test1()

[Fact]
public async Task HashSetTest()

[Fact]
public async Task TestHashSet()
```

### 2. Arrange-Act-Assert Pattern

Always use clear AAA structure:

```csharp
[Fact]
public async Task Example_Test()
{
    // Arrange - Set up test data
    var entity = new TestEntity { Id = "test-1" };
    
    // Act - Perform the operation
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert - Verify the result
    loaded.Should().BeEquivalentTo(entity);
}
```

### 3. One Assertion Per Test

Focus each test on one specific behavior:

**Good:**
```csharp
[Fact]
public async Task HashSet_WithNullValue_LoadsAsNull()
{
    var entity = new TestEntity { Id = "test-1", Tags = null };
    var loaded = await SaveAndLoadAsync(entity);
    loaded.Tags.Should().BeNull();
}

[Fact]
public async Task HashSet_WithEmptySet_OmitsFromDynamoDB()
{
    var entity = new TestEntity { Id = "test-1", Tags = new HashSet<string>() };
    var item = TestEntity.ToDynamoDb(entity);
    item.Should().NotContainKey("tags");
}
```

**Bad:**
```csharp
[Fact]
public async Task HashSet_Tests()
{
    // Testing multiple things in one test
    var entity1 = new TestEntity { Tags = null };
    var loaded1 = await SaveAndLoadAsync(entity1);
    loaded1.Tags.Should().BeNull();
    
    var entity2 = new TestEntity { Tags = new HashSet<string>() };
    var item = TestEntity.ToDynamoDb(entity2);
    item.Should().NotContainKey("tags");
}
```

### 4. Use Descriptive Assertion Messages

Add "because" messages to clarify intent:

```csharp
entityCode.Should().Contain("NS =", 
    "should use Number Set for HashSet<int>");

entityCode.Should().Contain("entity.Tags != null && entity.Tags.Count > 0",
    "should check for null and empty before adding to DynamoDB item");
```

### 5. Test Edge Cases

Don't just test the happy path:

```csharp
[Fact]
public async Task HashSet_WithNullValue_LoadsAsNull()
{
    // Test null handling
}

[Fact]
public async Task HashSet_WithEmptySet_OmitsFromDynamoDB()
{
    // Test empty collection handling
}

[Fact]
public async Task HashSet_WithSingleValue_PreservesValue()
{
    // Test single item
}

[Fact]
public async Task HashSet_WithMultipleValues_PreservesAllValues()
{
    // Test multiple items
}
```

### 6. Keep Tests Independent

Each test should be able to run independently:

```csharp
// Good - Each test creates its own data
[Fact]
public async Task Test1()
{
    var entity = new TestEntity { Id = "test-1" };
    // ...
}

[Fact]
public async Task Test2()
{
    var entity = new TestEntity { Id = "test-2" };
    // ...
}

// Bad - Tests depend on shared state
private TestEntity _sharedEntity;

[Fact]
public async Task Test1()
{
    _sharedEntity = new TestEntity { Id = "test-1" };
    // ...
}

[Fact]
public async Task Test2()
{
    // Depends on Test1 running first
    var loaded = await LoadAsync(_sharedEntity.Id);
    // ...
}
```

### 7. Use Builders for Complex Setup

When entities have many properties, use builders:

```csharp
// Good - Clean and readable
var entity = new ComplexEntityBuilder()
    .WithId("test-1")
    .WithName("Test")
    .WithTags("tag1", "tag2")
    .WithMetadata(("key", "value"))
    .Build();

// Bad - Verbose and hard to read
var entity = new ComplexEntity
{
    Id = "test-1",
    Name = "Test",
    Tags = new HashSet<string> { "tag1", "tag2" },
    Metadata = new Dictionary<string, string> { ["key"] = "value" },
    // ... many more properties
};
```

## Common Scenarios

### Scenario 1: Testing Round-Trip Data Integrity

```csharp
[Fact]
public async Task Entity_RoundTrip_PreservesAllProperties()
{
    // Arrange
    var entity = new TestEntityBuilder()
        .WithId("test-1")
        .WithName("John Doe")
        .WithAge(30)
        .Build();
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Should().BeEquivalentTo(entity);
}
```

### Scenario 2: Testing Null Handling

```csharp
[Fact]
public async Task Entity_WithNullProperty_LoadsAsNull()
{
    // Arrange
    var entity = new TestEntity
    {
        Id = "test-1",
        Name = null
    };
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Name.Should().BeNull();
}
```

### Scenario 3: Testing Empty Collections

```csharp
[Fact]
public async Task Entity_WithEmptyCollection_OmitsFromDynamoDB()
{
    // Arrange
    var entity = new TestEntity
    {
        Id = "test-1",
        Tags = new HashSet<string>()
    };
    
    // Act
    var item = TestEntity.ToDynamoDb(entity);
    
    // Assert
    item.Should().NotContainKey("tags",
        "empty collections should not be stored in DynamoDB");
}
```

### Scenario 4: Testing Collection Order Preservation

```csharp
[Fact]
public async Task List_RoundTrip_PreservesOrder()
{
    // Arrange
    var entity = new TestEntity
    {
        Id = "test-1",
        ItemIds = new List<string> { "item-3", "item-1", "item-2" }
    };
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.ItemIds.Should().Equal(entity.ItemIds,
        "list order should be preserved");
}
```

### Scenario 5: Testing Set Uniqueness

```csharp
[Fact]
public async Task HashSet_RoundTrip_RemovesDuplicates()
{
    // Arrange
    var entity = new TestEntity
    {
        Id = "test-1",
        Tags = new HashSet<string> { "tag1", "tag2", "tag1" } // Duplicate
    };
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Tags.Should().HaveCount(2,
        "HashSet should remove duplicates");
    loaded.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
}
```

### Scenario 6: Testing Complex Nested Structures

```csharp
[Fact]
public async Task Entity_WithNestedCollections_RoundTripsCorrectly()
{
    // Arrange
    var entity = new ComplexEntityBuilder()
        .WithId("test-1")
        .WithCategoryIds(1, 2, 3)
        .WithTags("new", "featured")
        .WithItemIds("item-1", "item-2")
        .WithPrices(9.99m, 19.99m)
        .WithMetadata(
            ("key1", "value1"),
            ("key2", "value2"))
        .Build();
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.Should().BeEquivalentTo(entity, options => options
        .ComparingByMembers<ComplexEntity>());
}
```

### Scenario 7: Testing Query Operations

```csharp
[Fact]
public async Task Query_WithAdvancedTypes_ReturnsCorrectResults()
{
    // Arrange
    var entity1 = new TestEntityBuilder()
        .WithId("test-1")
        .WithTags("featured")
        .Build();
    
    var entity2 = new TestEntityBuilder()
        .WithId("test-2")
        .WithTags("sale")
        .Build();
    
    await SaveAsync(entity1);
    await SaveAsync(entity2);
    
    // Act
    var results = await QueryByTagAsync("featured");
    
    // Assert
    results.Should().ContainSingle();
    results.First().Id.Should().Be("test-1");
}
```

### Scenario 8: Testing Update Operations

```csharp
[Fact]
public async Task Update_CollectionProperty_UpdatesCorrectly()
{
    // Arrange
    var entity = new TestEntityBuilder()
        .WithId("test-1")
        .WithTags("tag1", "tag2")
        .Build();
    
    await SaveAsync(entity);
    
    // Act - Update tags
    entity.Tags = new HashSet<string> { "tag3", "tag4" };
    await SaveAsync(entity);
    
    var loaded = await LoadAsync(entity.Id);
    
    // Assert
    loaded.Tags.Should().BeEquivalentTo(new[] { "tag3", "tag4" });
}
```

## Naming Conventions

### Test Class Names

- Format: `{ClassName}Tests`
- Examples:
  - `MapperGeneratorTests`
  - `HashSetIntegrationTests`
  - `EntityAnalyzerTests`

### Test Method Names

- Format: `{Feature}_{Scenario}_{ExpectedBehavior}`
- Use underscores to separate parts
- Be descriptive and specific

**Examples:**

```csharp
// Good
[Fact]
public async Task HashSetInt_RoundTrip_PreservesAllValues()

[Fact]
public async Task HashSet_WithNullValue_LoadsAsNull()

[Fact]
public async Task Generator_WithHashSetInt_GeneratesNumberSetConversion()

// Bad
[Fact]
public async Task Test1()

[Fact]
public async Task HashSetTest()

[Fact]
public async Task TestRoundTrip()
```

### Variable Names

Use descriptive names:

```csharp
// Good
var entity = new TestEntity { ... };
var loaded = await SaveAndLoadAsync(entity);
var result = GenerateCode(source);

// Bad
var e = new TestEntity { ... };
var x = await SaveAndLoadAsync(e);
var r = GenerateCode(s);
```

### Builder Names

- Format: `{EntityName}Builder`
- Examples:
  - `TestEntityBuilder`
  - `AdvancedTypesEntityBuilder`
  - `ComplexEntityBuilder`

## Test Organization Tips

### Group Related Tests

Use nested classes or regions:

```csharp
public class HashSetIntegrationTests : IntegrationTestBase
{
    public class IntegerHashSets : HashSetIntegrationTests
    {
        [Fact]
        public async Task RoundTrip_PreservesAllValues() { }
        
        [Fact]
        public async Task WithNullValue_LoadsAsNull() { }
    }
    
    public class StringHashSets : HashSetIntegrationTests
    {
        [Fact]
        public async Task RoundTrip_PreservesAllValues() { }
        
        [Fact]
        public async Task WithEmptySet_OmitsFromDynamoDB() { }
    }
}
```

### Separate Positive and Negative Tests

```csharp
public class MyFeatureTests
{
    public class SuccessCases
    {
        [Fact]
        public void ValidInput_ReturnsExpectedResult() { }
    }
    
    public class ErrorCases
    {
        [Fact]
        public void InvalidInput_ThrowsException() { }
    }
}
```

### Use Theory for Similar Tests

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(5, 10, 15)]
[InlineData(100, 200, 300)]
public async Task HashSetInt_WithVariousValues_PreservesAllValues(
    int value1, int value2, int value3)
{
    // Arrange
    var entity = new TestEntityBuilder()
        .WithCategoryIds(value1, value2, value3)
        .Build();
    
    // Act
    var loaded = await SaveAndLoadAsync(entity);
    
    // Assert
    loaded.CategoryIds.Should().BeEquivalentTo(new[] { value1, value2, value3 });
}
```

## Additional Resources

- [Integration Test README](./README.md)
- [Migration Guide](./MIGRATION_GUIDE.md)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)

## Quick Reference

### Test Template Checklist

- [ ] Descriptive test name following convention
- [ ] Clear Arrange-Act-Assert structure
- [ ] One assertion per test (or closely related assertions)
- [ ] Descriptive "because" messages on assertions
- [ ] Uses builders for complex setup
- [ ] Tests edge cases (null, empty, single, multiple)
- [ ] Independent from other tests
- [ ] Includes compilation verification (for generator tests)
- [ ] Uses semantic assertions where appropriate
- [ ] Keeps DynamoDB-specific string checks

### Common Imports

```csharp
// Unit tests
using Xunit;
using FluentAssertions;
using Oproto.FluentDynamoDb.SourceGenerator.UnitTests.TestHelpers;

// Integration tests
using Xunit;
using FluentAssertions;
using Oproto.FluentDynamoDb.IntegrationTests.Infrastructure;
using Oproto.FluentDynamoDb.IntegrationTests.TestEntities;
using Oproto.FluentDynamoDb.IntegrationTests.TestEntities.Builders;
```

### Common Assertions

```csharp
// Equality
loaded.Should().BeEquivalentTo(entity);
loaded.Name.Should().Be("Expected");

// Null checks
loaded.Name.Should().BeNull();
loaded.Name.Should().NotBeNull();

// Collections
loaded.Tags.Should().HaveCount(3);
loaded.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
loaded.ItemIds.Should().Equal(entity.ItemIds); // Order matters

// Compilation
CompilationVerifier.AssertGeneratedCodeCompiles(code);

// Semantic
code.ShouldContainMethod("ToDynamoDb");
code.ShouldContainAssignment("entity.Name");
code.ShouldUseLinqMethod("Select");
code.ShouldReferenceType("AttributeValue");

// String checks with reasons
code.Should().Contain("NS =", "should use Number Set");
```
