using Oproto.FluentDynamoDb.SourceGenerator.Generators;
using Oproto.FluentDynamoDb.SourceGenerator.Models;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Generators;

public class ProjectionExpressionGeneratorDiscriminatorTests
{
    [Fact]
    public void GenerateProjectionExpression_WithEntityDiscriminator_IncludesDiscriminatorProperty()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" },
                new ProjectionPropertyModel { PropertyName = "Name", AttributeName = "name" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "entity_type",
                ExactValue = "USER",
                Strategy = DiscriminatorStrategy.ExactMatch
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionExpression(projection);

        // Assert
        result.Should().Contain("pk");
        result.Should().Contain("name");
        result.Should().Contain("entity_type");
    }

    [Fact]
    public void GenerateProjectionExpression_WithGsiDiscriminator_IncludesBothDiscriminators()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" },
                new ProjectionPropertyModel { PropertyName = "Status", AttributeName = "status" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "entity_type",
                ExactValue = "USER",
                Strategy = DiscriminatorStrategy.ExactMatch
            },
            GsiDiscriminator = new DiscriminatorConfig
            {
                PropertyName = "GSI1SK",
                Pattern = "USER#*",
                Strategy = DiscriminatorStrategy.StartsWith
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionExpression(projection);

        // Assert
        result.Should().Contain("pk");
        result.Should().Contain("status");
        result.Should().Contain("entity_type");
        result.Should().Contain("GSI1SK");
    }

    [Fact]
    public void GenerateProjectionExpression_WithSameDiscriminatorInProperties_DoesNotDuplicate()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" },
                new ProjectionPropertyModel { PropertyName = "EntityType", AttributeName = "entity_type" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "entity_type",
                ExactValue = "USER",
                Strategy = DiscriminatorStrategy.ExactMatch
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionExpression(projection);

        // Assert
        var parts = result.Split(',').Select(p => p.Trim()).ToList();
        parts.Count(p => p == "entity_type").Should().Be(1);
    }

    [Fact]
    public void GenerateProjectionExpression_WithSameGsiDiscriminatorAsEntity_DoesNotDuplicate()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "SK",
                Pattern = "USER#*",
                Strategy = DiscriminatorStrategy.StartsWith
            },
            GsiDiscriminator = new DiscriminatorConfig
            {
                PropertyName = "SK",
                Pattern = "USER#*",
                Strategy = DiscriminatorStrategy.StartsWith
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionExpression(projection);

        // Assert
        var parts = result.Split(',').Select(p => p.Trim()).ToList();
        parts.Count(p => p == "SK").Should().Be(1);
    }

    [Fact]
    public void GenerateProjectionExpression_WithNoDiscriminator_DoesNotIncludeDiscriminator()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" },
                new ProjectionPropertyModel { PropertyName = "Name", AttributeName = "name" }
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionExpression(projection);

        // Assert
        result.Should().Be("pk, name");
        result.Should().NotContain("entity_type");
    }

    [Fact]
    public void GenerateFromDynamoDbMethod_WithDiscriminator_IncludesValidation()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk", PropertyType = "string" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "entity_type",
                ExactValue = "USER",
                Strategy = DiscriminatorStrategy.ExactMatch
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateFromDynamoDbMethod(projection);

        // Assert
        result.Should().Contain("Validate discriminator value");
        result.Should().Contain("item.TryGetValue(\"entity_type\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator == \"USER\"");
        result.Should().Contain("DiscriminatorMismatchException.Create");
    }

    [Fact]
    public void GenerateFromDynamoDbMethod_WithNoDiscriminator_SkipsValidation()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk", PropertyType = "string" }
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateFromDynamoDbMethod(projection);

        // Assert
        result.Should().NotContain("Validate discriminator");
        result.Should().NotContain("DiscriminatorMismatchException");
    }

    [Fact]
    public void GenerateFromDynamoDbMethod_WithStartsWithPattern_GeneratesStartsWithCheck()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk", PropertyType = "string" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "SK",
                Pattern = "USER#*",
                Strategy = DiscriminatorStrategy.StartsWith
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateFromDynamoDbMethod(projection);

        // Assert
        result.Should().Contain("item.TryGetValue(\"SK\", out var discriminatorAttr)");
        result.Should().Contain("actualDiscriminator.StartsWith(\"USER#\")");
    }

    [Fact]
    public void GenerateProjectionMetadata_WithDiscriminator_IncludesDiscriminatorInfo()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "entity_type",
                ExactValue = "USER",
                Strategy = DiscriminatorStrategy.ExactMatch
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionMetadata(projection);

        // Assert
        result.Should().Contain("DiscriminatorProperty = \"entity_type\"");
        result.Should().Contain("DiscriminatorValue = \"USER\"");
    }

    [Fact]
    public void GenerateProjectionMetadata_WithPatternDiscriminator_IncludesPattern()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" }
            },
            Discriminator = new DiscriminatorConfig
            {
                PropertyName = "SK",
                Pattern = "USER#*",
                Strategy = DiscriminatorStrategy.StartsWith
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionMetadata(projection);

        // Assert
        result.Should().Contain("DiscriminatorProperty = \"SK\"");
        result.Should().Contain("DiscriminatorPattern = \"USER#*\"");
    }

    [Fact]
    public void GenerateProjectionMetadata_WithNoDiscriminator_DoesNotIncludeDiscriminatorInfo()
    {
        // Arrange
        var projection = new ProjectionModel
        {
            ClassName = "UserProjection",
            Namespace = "Test",
            SourceEntityType = "User",
            Properties = new[]
            {
                new ProjectionPropertyModel { PropertyName = "Id", AttributeName = "pk" }
            }
        };

        // Act
        var result = ProjectionExpressionGenerator.GenerateProjectionMetadata(projection);

        // Assert
        result.Should().NotContain("DiscriminatorProperty");
        result.Should().NotContain("DiscriminatorValue");
        result.Should().NotContain("DiscriminatorPattern");
    }
}
