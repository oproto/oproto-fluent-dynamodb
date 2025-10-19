using FluentAssertions;
using Oproto.FluentDynamoDb.SourceGenerator.Generators;
using Oproto.FluentDynamoDb.SourceGenerator.Models;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Generators;

public class MapperGeneratorBugFixTests
{
    [Fact]
    public void GenerateEntityImplementation_WithPropertyNames_UsesEntityClassNameInTypeofExpressions()
    {
        // Arrange - This test specifically verifies the fix for the CS0246 error
        // where property names were being used as types instead of the entity class name
        var entity = new EntityModel
        {
            ClassName = "TestEntity",
            Namespace = "TestNamespace",
            TableName = "test-table",
            Properties = new[]
            {
                new PropertyModel
                {
                    PropertyName = "Id",
                    AttributeName = "pk",
                    PropertyType = "string",
                    IsPartitionKey = true
                },
                new PropertyModel
                {
                    PropertyName = "Data",
                    AttributeName = "data",
                    PropertyType = "string"
                }
            }
        };

        // Act
        var result = MapperGenerator.GenerateEntityImplementation(entity);

        // Assert - Verify that the generated code uses typeof(TestEntity) instead of typeof(Id) or typeof(Data)
        result.Should().Contain("typeof(TestEntity)");
        result.Should().NotContain("typeof(Id)");
        result.Should().NotContain("typeof(Data)");

        // Verify the specific error handling code is correct
        result.Should().Contain("throw DynamoDbMappingException.PropertyConversionFailed(");
        result.Should().Contain("typeof(TestEntity),");
        result.Should().Contain("\"Id\",");
        result.Should().Contain("typeof(string),");
    }
}