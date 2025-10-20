using Oproto.FluentDynamoDb.NewtonsoftJson;

namespace Oproto.FluentDynamoDb.NewtonsoftJson.UnitTests;

public class NewtonsoftJsonSerializerTests
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
        var json = NewtonsoftJsonSerializer.Serialize(testObject);

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
        var result = NewtonsoftJsonSerializer.Deserialize<TestData>(json);

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
        var json = NewtonsoftJsonSerializer.Serialize(original);
        var restored = NewtonsoftJsonSerializer.Deserialize<TestData>(json);

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
        var json = NewtonsoftJsonSerializer.Serialize(complexObject);

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
        var result = NewtonsoftJsonSerializer.Deserialize<ComplexTestData>(json);

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
        var json = NewtonsoftJsonSerializer.Serialize(original);
        var restored = NewtonsoftJsonSerializer.Deserialize<ComplexTestData>(json);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Metadata.Should().BeEquivalentTo(original.Metadata);
        restored.Tags.Should().BeEquivalentTo(original.Tags);
    }

    [Fact]
    public void Deserialize_WithNullJson_ThrowsArgumentNullException()
    {
        // Act
        var act = () => NewtonsoftJsonSerializer.Deserialize<TestData>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("json");
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
        var json = NewtonsoftJsonSerializer.Serialize(testObject);
        var restored = NewtonsoftJsonSerializer.Deserialize<TestData>(json);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be("test-null");
        restored.Name.Should().BeNull();
        restored.Value.Should().Be(0);
    }

    [Fact]
    public void UsesAotSafeSettings_NoTypeNameHandling()
    {
        // This test verifies that the serializer uses AOT-safe settings
        // by checking that TypeNameHandling is disabled
        
        // Arrange
        var testObject = new TestData
        {
            Id = "aot-safe-test",
            Name = "AOT Safe",
            Value = 100
        };

        // Act
        var json = NewtonsoftJsonSerializer.Serialize(testObject);

        // Assert
        // TypeNameHandling.None means no $type metadata should be in the JSON
        json.Should().NotContain("$type");
        json.Should().NotContain("$values");
        
        // Should still deserialize correctly
        var restored = NewtonsoftJsonSerializer.Deserialize<TestData>(json);
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(testObject.Id);
    }

    [Fact]
    public void Serialize_WithDateTimeProperties_UsesIsoFormat()
    {
        // Arrange
        var testObject = new TestDataWithDate
        {
            Id = "date-test",
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = NewtonsoftJsonSerializer.Serialize(testObject);

        // Assert
        json.Should().Contain("2024-01-15");
        
        // Should deserialize correctly
        var restored = NewtonsoftJsonSerializer.Deserialize<TestDataWithDate>(json);
        restored.Should().NotBeNull();
        restored!.CreatedAt.Should().BeCloseTo(testObject.CreatedAt, TimeSpan.FromSeconds(1));
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

public class TestDataWithDate
{
    public string? Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
