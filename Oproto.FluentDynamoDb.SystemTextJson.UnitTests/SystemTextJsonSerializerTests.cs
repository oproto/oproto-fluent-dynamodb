using System.Text.Json.Serialization;
using Oproto.FluentDynamoDb.SystemTextJson;

namespace Oproto.FluentDynamoDb.SystemTextJson.UnitTests;

public class SystemTextJsonSerializerTests
{
    [Fact]
    public void Serialize_WithSimpleObject_ProducesValidJson()
    {
        // Arrange
        var testObject = new TestData
        {
            Id = "test-123",
            Name = "Test Name",
            Value = 42
        };

        // Act
        var json = SystemTextJsonSerializer.Serialize(testObject, TestJsonContext.Default);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"Id\":\"test-123\"");
        json.Should().Contain("\"Name\":\"Test Name\"");
        json.Should().Contain("\"Value\":42");
    }

    [Fact]
    public void Deserialize_WithValidJson_ReconstructsObjectCorrectly()
    {
        // Arrange
        var json = "{\"Id\":\"test-456\",\"Name\":\"Another Test\",\"Value\":99}";

        // Act
        var result = SystemTextJsonSerializer.Deserialize<TestData>(json, TestJsonContext.Default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-456");
        result.Name.Should().Be("Another Test");
        result.Value.Should().Be(99);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        // Arrange
        var original = new TestData
        {
            Id = "round-trip-test",
            Name = "Round Trip",
            Value = 123
        };

        // Act
        var json = SystemTextJsonSerializer.Serialize(original, TestJsonContext.Default);
        var restored = SystemTextJsonSerializer.Deserialize<TestData>(json, TestJsonContext.Default);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Name.Should().Be(original.Name);
        restored.Value.Should().Be(original.Value);
    }

    [Fact]
    public void Serialize_WithComplexObject_HandlesNestedProperties()
    {
        // Arrange
        var complexObject = new ComplexTestData
        {
            Id = "complex-123",
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            },
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var json = SystemTextJsonSerializer.Serialize(complexObject, TestJsonContext.Default);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"Id\":\"complex-123\"");
        json.Should().Contain("\"Metadata\"");
        json.Should().Contain("\"Tags\"");
    }

    [Fact]
    public void Deserialize_WithComplexJson_ReconstructsNestedProperties()
    {
        // Arrange
        var json = "{\"Id\":\"complex-456\",\"Metadata\":{\"key1\":\"value1\",\"key2\":\"value2\"},\"Tags\":[\"tag1\",\"tag2\"]}";

        // Act
        var result = SystemTextJsonSerializer.Deserialize<ComplexTestData>(json, TestJsonContext.Default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("complex-456");
        result.Metadata.Should().HaveCount(2);
        result.Metadata["key1"].Should().Be("value1");
        result.Tags.Should().HaveCount(2);
        result.Tags.Should().Contain("tag1");
    }

    [Fact]
    public void RoundTrip_WithComplexObject_PreservesAllData()
    {
        // Arrange
        var original = new ComplexTestData
        {
            Id = "complex-round-trip",
            Metadata = new Dictionary<string, string>
            {
                ["author"] = "John Doe",
                ["version"] = "1.0"
            },
            Tags = new List<string> { "important", "reviewed" }
        };

        // Act
        var json = SystemTextJsonSerializer.Serialize(original, TestJsonContext.Default);
        var restored = SystemTextJsonSerializer.Deserialize<ComplexTestData>(json, TestJsonContext.Default);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Metadata.Should().BeEquivalentTo(original.Metadata);
        restored.Tags.Should().BeEquivalentTo(original.Tags);
    }

    [Fact]
    public void Serialize_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var testObject = new TestData { Id = "test", Name = "Test", Value = 1 };

        // Act
        var act = () => SystemTextJsonSerializer.Serialize(testObject, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Deserialize_WithNullJson_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SystemTextJsonSerializer.Deserialize<TestData>(null!, TestJsonContext.Default);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("json");
    }

    [Fact]
    public void Deserialize_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var json = "{\"Id\":\"test\"}";

        // Act
        var act = () => SystemTextJsonSerializer.Deserialize<TestData>(json, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Serialize_WithNullProperties_HandlesNullsCorrectly()
    {
        // Arrange
        var testObject = new TestData
        {
            Id = "test-null",
            Name = null,
            Value = 0
        };

        // Act
        var json = SystemTextJsonSerializer.Serialize(testObject, TestJsonContext.Default);
        var restored = SystemTextJsonSerializer.Deserialize<TestData>(json, TestJsonContext.Default);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be("test-null");
        restored.Name.Should().BeNull();
        restored.Value.Should().Be(0);
    }

    [Fact]
    public void WorksWithJsonSerializerContext_IsAotCompatible()
    {
        // This test verifies that the serializer works with JsonSerializerContext
        // which is required for AOT compatibility
        
        // Arrange
        var testObject = new TestData
        {
            Id = "aot-test",
            Name = "AOT Compatible",
            Value = 100
        };

        // Act - Using the context ensures AOT compatibility
        var json = SystemTextJsonSerializer.Serialize(testObject, TestJsonContext.Default);
        var restored = SystemTextJsonSerializer.Deserialize<TestData>(json, TestJsonContext.Default);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(testObject.Id);
        restored.Name.Should().Be(testObject.Name);
        restored.Value.Should().Be(testObject.Value);
    }
}

// Test data classes
public class TestData
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public int Value { get; set; }
}

public class ComplexTestData
{
    public string? Id { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

// JsonSerializerContext for AOT compatibility
[JsonSerializable(typeof(TestData))]
[JsonSerializable(typeof(ComplexTestData))]
internal partial class TestJsonContext : JsonSerializerContext
{
}
