using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oproto.FluentDynamoDb.Logging;
using Oproto.FluentDynamoDb.SourceGenerator;
using Oproto.FluentDynamoDb.SourceGenerator.UnitTests.TestHelpers;
using System.Collections.Immutable;
using System.Reflection;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Integration;

/// <summary>
/// Integration tests for generated logging code.
/// Tests that the source generator produces code with proper logging calls.
/// </summary>
[Trait("Category", "Integration")]
public class GeneratedLoggingIntegrationTests
{
    /// <summary>
    /// Test entity source code with basic properties for testing logging generation.
    /// </summary>
    private const string TestEntitySource = @"
using System;
using System.Collections.Generic;
using Oproto.FluentDynamoDb.Attributes;
using Oproto.FluentDynamoDb.Storage;

namespace TestNamespace
{
    [DynamoDbTable(""test-table"")]
    public partial class TestEntity : IDynamoDbEntity
    {
        [PartitionKey]
        [DynamoDbAttribute(""pk"")]
        public string Id { get; set; } = string.Empty;
        
        [DynamoDbAttribute(""name"")]
        public string? Name { get; set; }
        
        [DynamoDbAttribute(""tags"")]
        public HashSet<string>? Tags { get; set; }
        
        [DynamoDbAttribute(""metadata"")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}";

    [Fact]
    public void GeneratedToDynamoDb_WithLogger_LogsEntryAndExit()
    {
        // Arrange
        var result = GenerateAndCompileCode(TestEntitySource);
        var assembly = result.Assembly;
        var testEntityType = assembly.GetType("TestNamespace.TestEntity");
        Assert.NotNull(testEntityType);

        var entity = Activator.CreateInstance(testEntityType);
        Assert.NotNull(entity);
        
        // Set properties
        testEntityType.GetProperty("Id")!.SetValue(entity, "test-123");
        testEntityType.GetProperty("Name")!.SetValue(entity, "Test Name");

        var logger = new TestLogger(LogLevel.Trace);

        // Act
        var toDynamoDbMethod = testEntityType.GetMethod("ToDynamoDb", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { testEntityType, typeof(IDynamoDbLogger) });
        Assert.NotNull(toDynamoDbMethod);

        var item = toDynamoDbMethod.Invoke(null, new[] { entity, logger });
        Assert.NotNull(item);

        // Assert - Entry logging
        var entryLog = logger.GetLogEntry(LogLevel.Trace, LogEventIds.MappingToDynamoDbStart);
        Assert.NotNull(entryLog);
        Assert.Contains("TestEntity", entryLog.FormattedMessage);
        Assert.Contains("Starting ToDynamoDb mapping", entryLog.FormattedMessage);

        // Assert - Exit logging
        var exitLog = logger.GetLogEntry(LogLevel.Trace, LogEventIds.MappingToDynamoDbComplete);
        Assert.NotNull(exitLog);
        Assert.Contains("TestEntity", exitLog.FormattedMessage);
        Assert.Contains("Completed ToDynamoDb mapping", exitLog.FormattedMessage);
        Assert.Contains("attributes", exitLog.FormattedMessage);
    }

    [Fact]
    public void GeneratedToDynamoDb_WithLogger_LogsPropertyMapping()
    {
        // Arrange
        var result = GenerateAndCompileCode(TestEntitySource);
        var assembly = result.Assembly;
        var testEntityType = assembly.GetType("TestNamespace.TestEntity");
        Assert.NotNull(testEntityType);

        var entity = Activator.CreateInstance(testEntityType);
        Assert.NotNull(entity);
        
        testEntityType.GetProperty("Id")!.SetValue(entity, "test-123");
        testEntityType.GetProperty("Name")!.SetValue(entity, "Test Name");

        var logger = new TestLogger(LogLevel.Debug);

        // Act
        var toDynamoDbMethod = testEntityType.GetMethod("ToDynamoDb", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { testEntityType, typeof(IDynamoDbLogger) });
        Assert.NotNull(toDynamoDbMethod);

        toDynamoDbMethod.Invoke(null, new[] { entity, logger });

        // Assert - Property mapping logs
        var propertyLogs = logger.LogEntries
            .Where(e => e.EventId == LogEventIds.MappingPropertyStart)
            .ToList();

        Assert.NotEmpty(propertyLogs);
        
        // Should log mapping for Id property
        Assert.Contains(propertyLogs, log => log.FormattedMessage.Contains("Id"));
        
        // Should log mapping for Name property
        Assert.Contains(propertyLogs, log => log.FormattedMessage.Contains("Name"));
    }

    [Fact]
    public void GeneratedToDynamoDb_WithLogger_LogsStructuredProperties()
    {
        // Arrange
        var result = GenerateAndCompileCode(TestEntitySource);
        var assembly = result.Assembly;
        var testEntityType = assembly.GetType("TestNamespace.TestEntity");
        Assert.NotNull(testEntityType);

        var entity = Activator.CreateInstance(testEntityType);
        Assert.NotNull(entity);
        
        testEntityType.GetProperty("Id")!.SetValue(entity, "test-123");

        var logger = new TestLogger(LogLevel.Trace);

        // Act
        var toDynamoDbMethod = testEntityType.GetMethod("ToDynamoDb", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { testEntityType, typeof(IDynamoDbLogger) });
        Assert.NotNull(toDynamoDbMethod);

        toDynamoDbMethod.Invoke(null, new[] { entity, logger });

        // Assert - Structured properties in logs
        var entryLog = logger.GetLogEntry(LogLevel.Trace, LogEventIds.MappingToDynamoDbStart);
        Assert.NotNull(entryLog);
        
        // Should have EntityType as a structured property
        Assert.Contains("TestEntity", entryLog.Args);
        
        var exitLog = logger.GetLogEntry(LogLevel.Trace, LogEventIds.MappingToDynamoDbComplete);
        Assert.NotNull(exitLog);
        
        // Should have EntityType and AttributeCount as structured properties
        Assert.Contains("TestEntity", exitLog.Args);
        Assert.Contains(exitLog.Args, arg => arg is int); // AttributeCount
    }

    [Fact]
    public void GeneratedToDynamoDb_WithCollections_LogsConversions()
    {
        // Arrange
        var result = GenerateAndCompileCode(TestEntitySource);
        var assembly = result.Assembly;
        var testEntityType = assembly.GetType("TestNamespace.TestEntity");
        Assert.NotNull(testEntityType);

        var entity = Activator.CreateInstance(testEntityType);
        Assert.NotNull(entity);
        
        testEntityType.GetProperty("Id")!.SetValue(entity, "test-123");
        
        // Set Tags (HashSet)
        var tagsType = typeof(HashSet<string>);
        var tags = Activator.CreateInstance(tagsType) as HashSet<string>;
        tags!.Add("tag1");
        tags.Add("tag2");
        testEntityType.GetProperty("Tags")!.SetValue(entity, tags);
        
        // Set Metadata (Dictionary)
        var metadataType = typeof(Dictionary<string, string>);
        var metadata = Activator.CreateInstance(metadataType) as Dictionary<string, string>;
        metadata!["key1"] = "value1";
        testEntityType.GetProperty("Metadata")!.SetValue(entity, metadata);

        var logger = new TestLogger(LogLevel.Debug);

        // Act
        var toDynamoDbMethod = testEntityType.GetMethod("ToDynamoDb", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { testEntityType, typeof(IDynamoDbLogger) });
        Assert.NotNull(toDynamoDbMethod);

        toDynamoDbMethod.Invoke(null, new[] { entity, logger });

        // Assert - Set conversion logging
        var setLog = logger.LogEntries
            .FirstOrDefault(e => e.EventId == LogEventIds.ConvertingSet);
        Assert.NotNull(setLog);
        Assert.Contains("Tags", setLog.FormattedMessage);
        
        // Assert - Map conversion logging
        var mapLog = logger.LogEntries
            .FirstOrDefault(e => e.EventId == LogEventIds.ConvertingMap);
        Assert.NotNull(mapLog);
        Assert.Contains("Metadata", mapLog.FormattedMessage);
    }

    private static CompilationResult GenerateAndCompileCode(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DynamoDbSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Compile to assembly
        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new Exception($"Compilation failed:\n{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        return new CompilationResult
        {
            Assembly = assembly,
            Diagnostics = diagnostics
        };
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IO.Stream).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Amazon.DynamoDBv2.Model.AttributeValue).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Oproto.FluentDynamoDb.Attributes.DynamoDbTableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Oproto.FluentDynamoDb.Storage.IDynamoDbEntity).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Oproto.FluentDynamoDb.Logging.IDynamoDbLogger).Assembly.Location),
        };

        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.AddRange(new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "netstandard.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Linq.Expressions.dll"))
        });

        return references;
    }
}

public class CompilationResult
{
    public required Assembly Assembly { get; set; }
    public required ImmutableArray<Diagnostic> Diagnostics { get; set; }
}
