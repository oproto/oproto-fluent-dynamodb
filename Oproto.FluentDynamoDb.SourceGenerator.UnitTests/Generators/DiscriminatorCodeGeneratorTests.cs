using Oproto.FluentDynamoDb.SourceGenerator.Generators;
using Oproto.FluentDynamoDb.SourceGenerator.Models;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Generators;

public class DiscriminatorCodeGeneratorTests
{
    [Fact]
    public void GenerateDiscriminatorValidation_WithExactMatch_GeneratesCorrectCode()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "entity_type",
            ExactValue = "USER",
            Strategy = DiscriminatorStrategy.ExactMatch
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("item.TryGetValue(\"entity_type\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator == \"USER\"");
        result.Should().Contain("DiscriminatorMismatchException.Create");
        result.Should().Contain("typeof(UserProjection)");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithStartsWith_GeneratesCorrectCode()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "USER#*",
            Strategy = DiscriminatorStrategy.StartsWith
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("item.TryGetValue(\"SK\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator != null && actualDiscriminator.StartsWith(\"USER#\")");
        result.Should().Contain("DiscriminatorMismatchException.Create");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithEndsWith_GeneratesCorrectCode()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "*#USER",
            Strategy = DiscriminatorStrategy.EndsWith
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("item.TryGetValue(\"SK\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator != null && actualDiscriminator.EndsWith(\"#USER\")");
        result.Should().Contain("DiscriminatorMismatchException.Create");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithContains_GeneratesCorrectCode()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "*#USER#*",
            Strategy = DiscriminatorStrategy.Contains
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("item.TryGetValue(\"SK\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator != null && actualDiscriminator.Contains(\"#USER#\")");
        result.Should().Contain("DiscriminatorMismatchException.Create");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithNullDiscriminator_ReturnsEmptyString()
    {
        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(null!, "UserProjection");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithInvalidDiscriminator_ReturnsEmptyString()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "",
            Strategy = DiscriminatorStrategy.None
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithComplexPattern_GeneratesCorrectCode()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "TENANT#*#USER#*",
            Strategy = DiscriminatorStrategy.Complex
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("item.TryGetValue(\"SK\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator != null");
        result.Should().Contain("DiscriminatorMismatchException.Create");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_HandlesNullValue_GeneratesNullCheck()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "USER#*",
            Strategy = DiscriminatorStrategy.StartsWith
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().Contain("actualDiscriminator != null");
        result.Should().Contain("else");
        result.Should().Contain("null");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_EscapesSpecialCharacters()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "entity_type",
            ExactValue = "USER\"WITH\\QUOTES",
            Strategy = DiscriminatorStrategy.ExactMatch
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().Contain("\\\"");
        result.Should().Contain("\\\\");
    }

    [Fact]
    public void GetDiscriminatorPropertyName_WithValidDiscriminator_ReturnsPropertyName()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "entity_type",
            ExactValue = "USER",
            Strategy = DiscriminatorStrategy.ExactMatch
        };

        // Act
        var result = DiscriminatorCodeGenerator.GetDiscriminatorPropertyName(discriminator);

        // Assert
        result.Should().Be("entity_type");
    }

    [Fact]
    public void GetDiscriminatorPropertyName_WithNullDiscriminator_ReturnsNull()
    {
        // Act
        var result = DiscriminatorCodeGenerator.GetDiscriminatorPropertyName(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDiscriminatorPropertyName_WithInvalidDiscriminator_ReturnsNull()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "",
            Strategy = DiscriminatorStrategy.None
        };

        // Act
        var result = DiscriminatorCodeGenerator.GetDiscriminatorPropertyName(discriminator);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GenerateDiscriminatorValidation_IncludesExpectedValueInError()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "SK",
            Pattern = "USER#*",
            Strategy = DiscriminatorStrategy.StartsWith
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().Contain("\"USER#*\"");
    }

    [Fact]
    public void GenerateDiscriminatorValidation_WithExactValue_UsesValueInError()
    {
        // Arrange
        var discriminator = new DiscriminatorConfig
        {
            PropertyName = "entity_type",
            ExactValue = "USER",
            Strategy = DiscriminatorStrategy.ExactMatch
        };

        // Act
        var result = DiscriminatorCodeGenerator.GenerateDiscriminatorValidation(discriminator, "UserProjection");

        // Assert
        result.Should().Contain("\"USER\"");
    }
}
