using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oproto.FluentDynamoDb.SourceGenerator.Analysis;
using Oproto.FluentDynamoDb.SourceGenerator.Diagnostics;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Analysis;

/// <summary>
/// Tests for stream conversion attribute validation, specifically verifying that
/// Amazon.Lambda.DynamoDBEvents package reference is validated when GenerateStreamConversion is used.
/// </summary>
public class StreamConversionValidationTests
{
    [Fact]
    public void AnalyzeEntity_WithGenerateStreamConversion_AndMissingLambdaPackage_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    [GenerateStreamConversion]
    public partial class TestEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; } = string.Empty;
        
        [DynamoDbAttribute(""name"")]
        public string Name { get; set; } = string.Empty;
    }
}";

        var (classDecl, semanticModel) = ParseSourceWithoutLambdaPackage(source);
        var analyzer = new EntityAnalyzer();

        // Act
        var result = analyzer.AnalyzeEntity(classDecl, semanticModel);

        // Assert
        result.Should().NotBeNull("entity should still be analyzed even with missing package");
        result!.GenerateStreamConversion.Should().BeTrue("attribute was present");
        
        // Should report SEC002 diagnostic for missing Lambda package
        analyzer.Diagnostics.Should().Contain(d => d.Id == "SEC002", 
            "missing Amazon.Lambda.DynamoDBEvents package should be reported");
        
        var lambdaDiagnostic = analyzer.Diagnostics.First(d => d.Id == "SEC002");
        lambdaDiagnostic.Severity.Should().Be(DiagnosticSeverity.Error, 
            "missing package should be an error");
        lambdaDiagnostic.GetMessage().Should().Contain("Amazon.Lambda.DynamoDBEvents", 
            "error message should mention the missing package");
        lambdaDiagnostic.GetMessage().Should().Contain("TestEntity", 
            "error message should mention the entity name");
    }

    [Fact]
    public void AnalyzeEntity_WithGenerateStreamConversion_AndLambdaPackagePresent_NoError()
    {
        // Arrange
        var source = @"
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    [GenerateStreamConversion]
    public partial class TestEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; } = string.Empty;
        
        [DynamoDbAttribute(""data"")]
        public string Data { get; set; } = string.Empty;
    }
}";

        var (classDecl, semanticModel) = ParseSourceWithLambdaPackage(source);
        var analyzer = new EntityAnalyzer();

        // Act
        var result = analyzer.AnalyzeEntity(classDecl, semanticModel);

        // Assert
        result.Should().NotBeNull();
        result!.GenerateStreamConversion.Should().BeTrue("attribute was present");
        
        // Should NOT report SEC002 diagnostic when package is present
        analyzer.Diagnostics.Should().NotContain(d => d.Id == "SEC002", 
            "no error should be reported when Lambda package is referenced");
    }

    [Fact]
    public void AnalyzeEntity_WithoutGenerateStreamConversion_NoValidation()
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
        public string Id { get; set; } = string.Empty;
        
        [DynamoDbAttribute(""name"")]
        public string Name { get; set; } = string.Empty;
    }
}";

        var (classDecl, semanticModel) = ParseSourceWithoutLambdaPackage(source);
        var analyzer = new EntityAnalyzer();

        // Act
        var result = analyzer.AnalyzeEntity(classDecl, semanticModel);

        // Assert
        result.Should().NotBeNull();
        result!.GenerateStreamConversion.Should().BeFalse("attribute was not present");
        
        // Should NOT report SEC002 diagnostic when attribute is not used
        analyzer.Diagnostics.Should().NotContain(d => d.Id == "SEC002", 
            "no validation should occur when GenerateStreamConversion is not used");
    }

    [Fact]
    public void AnalyzeEntity_MultipleEntitiesWithStreamConversion_EachValidated()
    {
        // Arrange
        var source1 = @"
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    [GenerateStreamConversion]
    public partial class Entity1
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; } = string.Empty;
    }
}";

        var source2 = @"
using Oproto.FluentDynamoDb.Attributes;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    [GenerateStreamConversion]
    public partial class Entity2
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; } = string.Empty;
    }
}";

        var (classDecl1, semanticModel1) = ParseSourceWithoutLambdaPackage(source1);
        var (classDecl2, semanticModel2) = ParseSourceWithoutLambdaPackage(source2);
        
        var analyzer1 = new EntityAnalyzer();
        var analyzer2 = new EntityAnalyzer();

        // Act
        var result1 = analyzer1.AnalyzeEntity(classDecl1, semanticModel1);
        var result2 = analyzer2.AnalyzeEntity(classDecl2, semanticModel2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        
        // Both should report the missing package error
        analyzer1.Diagnostics.Should().Contain(d => d.Id == "SEC002");
        analyzer2.Diagnostics.Should().Contain(d => d.Id == "SEC002");
        
        analyzer1.Diagnostics.First(d => d.Id == "SEC002").GetMessage().Should().Contain("Entity1");
        analyzer2.Diagnostics.First(d => d.Id == "SEC002").GetMessage().Should().Contain("Entity2");
    }

    /// <summary>
    /// Helper method to parse source code without Lambda package reference.
    /// </summary>
    private (ClassDeclarationSyntax ClassDecl, SemanticModel SemanticModel) ParseSourceWithoutLambdaPackage(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            GetStandardReferences(), // Does not include Lambda package
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        return (classDecl, semanticModel);
    }

    /// <summary>
    /// Helper method to parse source code with Lambda package reference.
    /// </summary>
    private (ClassDeclarationSyntax ClassDecl, SemanticModel SemanticModel) ParseSourceWithLambdaPackage(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            GetReferencesWithLambdaPackage(), // Includes Lambda package
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        return (classDecl, semanticModel);
    }

    /// <summary>
    /// Gets standard metadata references without Lambda package.
    /// </summary>
    private IEnumerable<MetadataReference> GetStandardReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Amazon.DynamoDBv2.Model.AttributeValue).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Oproto.FluentDynamoDb.Attributes.DynamoDbTableAttribute).Assembly.Location)
        };

        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "netstandard.dll")));

        return references;
    }

    /// <summary>
    /// Gets metadata references including Lambda package.
    /// </summary>
    private IEnumerable<MetadataReference> GetReferencesWithLambdaPackage()
    {
        var references = GetStandardReferences().ToList();
        
        // Add Lambda package reference
        references.Add(MetadataReference.CreateFromFile(
            typeof(Amazon.Lambda.DynamoDBEvents.DynamoDBEvent).Assembly.Location));

        return references;
    }
}
