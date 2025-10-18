using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oproto.FluentDynamoDb.SourceGenerator;
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var source = @"
using System;
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

        var attributeSource = @"
using System;

namespace Oproto.FluentDynamoDb.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DynamoDbTableAttribute : Attribute
    {
        public string TableName { get; }
        public DynamoDbTableAttribute(string tableName) => TableName = tableName;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DynamoDbAttributeAttribute : Attribute
    {
        public string AttributeName { get; }
        public DynamoDbAttributeAttribute(string attributeName) => AttributeName = attributeName;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PartitionKeyAttribute : Attribute
    {
        public string? Prefix { get; set; }
        public string? Separator { get; set; } = ""#"";
    }
}";

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { 
                CSharpSyntaxTree.ParseText(source),
                CSharpSyntaxTree.ParseText(attributeSource)
            },
            new[] { 
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DynamoDbSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Console.WriteLine($"Diagnostics count: {diagnostics.Length}");
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine($"Diagnostic: {diagnostic}");
        }

        var generatedSources = outputCompilation.SyntaxTrees
            .Skip(compilation.SyntaxTrees.Count())
            .ToArray();

        Console.WriteLine($"Generated sources count: {generatedSources.Length}");
        
        foreach (var tree in generatedSources)
        {
            Console.WriteLine($"Generated file: {tree.FilePath}");
            Console.WriteLine(tree.GetText());
            Console.WriteLine("---");
        }
    }
}