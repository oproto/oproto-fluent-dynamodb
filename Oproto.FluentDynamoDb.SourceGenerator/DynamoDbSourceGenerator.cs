using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oproto.FluentDynamoDb.SourceGenerator.Analysis;
using Oproto.FluentDynamoDb.SourceGenerator.Generators;
using Oproto.FluentDynamoDb.SourceGenerator.Models;
using Oproto.FluentDynamoDb.SourceGenerator.Performance;
using Oproto.FluentDynamoDb.SourceGenerator.Advanced;
using System.Collections.Immutable;

namespace Oproto.FluentDynamoDb.SourceGenerator;

/// <summary>
/// High-performance DynamoDB source generator with advanced optimizations and features.
/// Uses incremental generation, caching, and performance optimizations for fast builds.
/// </summary>
[Generator]
public class DynamoDbSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Use the high-performance incremental generator for better build performance
        var incrementalGenerator = new IncrementalSourceGenerator();
        incrementalGenerator.Initialize(context);
    }

    private static bool IsDynamoDbEntity(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        return classDecl.AttributeLists.Any(al =>
            al.Attributes.Any(a =>
            {
                var attributeName = a.Name.ToString();
                return attributeName.Contains("DynamoDbTable") ||
                       attributeName.Contains("DynamoDbTableAttribute");
            }));
    }

    private static (EntityModel? Model, IReadOnlyList<Diagnostic> Diagnostics) GetEntityModel(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl)
            return (null, Array.Empty<Diagnostic>());

        try
        {
            var analyzer = new EntityAnalyzer();
            var entityModel = analyzer.AnalyzeEntity(classDecl, context.SemanticModel);

            return (entityModel, analyzer.Diagnostics);
        }
        catch (Exception)
        {
            // If there's an exception during analysis, return null to skip this entity
            return (null, Array.Empty<Diagnostic>());
        }
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<(EntityModel? Model, IReadOnlyList<Diagnostic> Diagnostics)> entities)
    {
        foreach (var (entity, diagnostics) in entities)
        {
            // Report diagnostics
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (entity == null) continue;

            // Generate Fields class with field name constants
            var fieldsCode = FieldsGenerator.GenerateFieldsClass(entity);
            context.AddSource($"{entity.ClassName}Fields.g.cs", fieldsCode);

            // Generate Keys class with key builder methods
            var keysCode = KeysGenerator.GenerateKeysClass(entity);
            context.AddSource($"{entity.ClassName}Keys.g.cs", keysCode);

            // Generate optimized entity implementation with mapping methods
            var sourceCode = GenerateOptimizedEntityImplementation(entity);
            context.AddSource($"{entity.ClassName}.g.cs", sourceCode);
        }
    }

    /// <summary>
    /// Generates optimized entity implementation using advanced performance optimizations.
    /// </summary>
    private static string GenerateOptimizedEntityImplementation(EntityModel entity)
    {
        // Create default compilation settings for the legacy generator
        var settings = new CompilationSettings
        {
            TargetFramework = "net8.0",
            OptimizationLevel = Microsoft.CodeAnalysis.OptimizationLevel.Release,
            NullableContextOptions = Microsoft.CodeAnalysis.NullableContextOptions.Enable,
            AssemblyName = "Generated"
        };

        // Use the advanced performance optimization system
        return AdvancedPerformanceOptimizations.GenerateOptimizedEntityCode(entity, settings);
    }
}