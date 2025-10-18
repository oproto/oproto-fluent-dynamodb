using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oproto.FluentDynamoDb.SourceGenerator.Analysis;
using Oproto.FluentDynamoDb.SourceGenerator.Generators;
using Oproto.FluentDynamoDb.SourceGenerator.Models;
using System.Collections.Immutable;
using System.Text;

namespace Oproto.FluentDynamoDb.SourceGenerator.Performance;

/// <summary>
/// High-performance incremental source generator that minimizes work during compilation.
/// Uses advanced caching, change detection, and parallel processing for optimal build performance.
/// </summary>
[Generator]
public class IncrementalSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor PerformanceInfo = new(
        "DYNDB_PERF001",
        "Source Generator Performance",
        "Generated {0} entities in {1}ms with {2}% cache hit rate",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: false);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create incremental value provider for entity classes with advanced filtering
        var entityClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsPotentialDynamoDbEntity(node),
                transform: static (ctx, _) => TransformEntityClass(ctx))
            .Where(static result => result.EntityModel != null)
            .Collect();

        // Create incremental value provider for compilation-wide settings
        var compilationSettings = context.CompilationProvider
            .Select(static (compilation, _) => ExtractCompilationSettings(compilation));

        // Combine entity classes with compilation settings for context-aware generation
        var combinedProvider = entityClasses.Combine(compilationSettings);

        // Register source output with performance tracking
        context.RegisterSourceOutput(combinedProvider, ExecuteWithPerformanceTracking);
    }

    /// <summary>
    /// Fast predicate to identify potential DynamoDB entities without semantic analysis.
    /// Uses syntax-only checks for maximum performance.
    /// </summary>
    private static bool IsPotentialDynamoDbEntity(SyntaxNode node)
    {
        // Quick syntax-only checks to filter out obviously non-entity classes
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must be partial class (required for source generation)
        if (!classDecl.Modifiers.Any(m => m.ValueText == "partial"))
            return false;

        // Must have attributes (potential DynamoDbTable attribute)
        if (classDecl.AttributeLists.Count == 0)
            return false;

        // Quick check for DynamoDB-related attribute names
        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName.Contains("DynamoDb") || 
                    attributeName.Contains("Table") ||
                    attributeName.EndsWith("TableAttribute"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Transforms a syntax node into an entity transformation result with caching.
    /// </summary>
    private static EntityTransformResult TransformEntityClass(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        try
        {
            // Create a stable cache key for this entity
            var cacheKey = CreateEntityCacheKey(classDecl, semanticModel);
            
            // Try to get from cache first
            if (EntityTransformCache.TryGetCached(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // Perform full analysis
            var analyzer = new EntityAnalyzer();
            var entityModel = analyzer.AnalyzeEntity(classDecl, semanticModel);

            var result = new EntityTransformResult
            {
                EntityModel = entityModel,
                Diagnostics = analyzer.Diagnostics.ToImmutableArray(),
                CacheKey = cacheKey,
                LastModified = DateTime.UtcNow
            };

            // Cache the result for future use
            EntityTransformCache.Cache(cacheKey, result);

            return result;
        }
        catch (Exception ex)
        {
            // Return error result without crashing the generator
            return new EntityTransformResult
            {
                EntityModel = null,
                Diagnostics = ImmutableArray.Create(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "DYNDB_ERROR001",
                            "Entity Analysis Error",
                            "Failed to analyze entity {0}: {1}",
                            "Error",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        classDecl.Identifier.GetLocation(),
                        classDecl.Identifier.ValueText,
                        ex.Message)),
                CacheKey = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Extracts compilation-wide settings that affect code generation.
    /// </summary>
    private static CompilationSettings ExtractCompilationSettings(Compilation compilation)
    {
        return new CompilationSettings
        {
            TargetFramework = GetTargetFramework(compilation),
            OptimizationLevel = compilation.Options.OptimizationLevel,
            NullableContextOptions = compilation.Options.NullableContextOptions,
            AssemblyName = compilation.AssemblyName ?? "Unknown"
        };
    }

    /// <summary>
    /// Executes code generation with comprehensive performance tracking and parallel processing.
    /// </summary>
    private static void ExecuteWithPerformanceTracking(
        SourceProductionContext context,
        (ImmutableArray<EntityTransformResult> EntityResults, CompilationSettings Settings) input)
    {
        var startTime = DateTime.UtcNow;
        var (entityResults, settings) = input;
        
        var validEntities = entityResults
            .Where(result => result.EntityModel != null)
            .ToArray();

        if (validEntities.Length == 0)
            return;

        try
        {
            // Report all diagnostics first
            foreach (var result in entityResults)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Generate code for all valid entities in parallel
            var generationTasks = validEntities
                .AsParallel()
                .WithDegreeOfParallelism(4) // Fixed degree for analyzer compatibility
                .Select(result => GenerateEntityCode(result.EntityModel!, settings))
                .ToArray();

            // Add all generated sources
            foreach (var (fileName, sourceCode) in generationTasks.SelectMany(task => task))
            {
                context.AddSource(fileName, sourceCode);
            }

            // Calculate and report performance metrics
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            var cacheHitRate = EntityTransformCache.GetCacheHitRate();

            context.ReportDiagnostic(Diagnostic.Create(
                PerformanceInfo,
                Location.None,
                validEntities.Length,
                duration.ToString("F1"),
                (cacheHitRate * 100).ToString("F1")));

            // Perform cache maintenance if needed
            if (validEntities.Length > 10)
            {
                EntityTransformCache.PerformMaintenance();
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DYNDB_ERROR002",
                    "Code Generation Error",
                    "Failed to generate code: {0}",
                    "Error",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                ex.Message));
        }
    }

    /// <summary>
    /// Generates all code files for a single entity with optimized performance.
    /// </summary>
    private static IEnumerable<(string FileName, string SourceCode)> GenerateEntityCode(
        EntityModel entity, 
        CompilationSettings settings)
    {
        var results = new List<(string, string)>();

        try
        {
            // Generate Fields class
            var fieldsCode = FieldsGenerator.GenerateFieldsClass(entity);
            results.Add(($"{entity.ClassName}Fields.g.cs", fieldsCode));

            // Generate Keys class
            var keysCode = KeysGenerator.GenerateKeysClass(entity);
            results.Add(($"{entity.ClassName}Keys.g.cs", keysCode));

            // Generate optimized entity implementation
            var entityCode = GenerateOptimizedEntityImplementation(entity, settings);
            results.Add(($"{entity.ClassName}.g.cs", entityCode));

            return results;
        }
        catch (Exception ex)
        {
            // Return error placeholder to prevent build failure
            var errorCode = GenerateErrorPlaceholder(entity, ex);
            return new[] { ($"{entity.ClassName}.Error.g.cs", errorCode) };
        }
    }

    /// <summary>
    /// Generates optimized entity implementation using advanced performance optimizations.
    /// </summary>
    private static string GenerateOptimizedEntityImplementation(EntityModel entity, CompilationSettings settings)
    {
        // Use the advanced performance optimization system for maximum efficiency
        return AdvancedPerformanceOptimizations.GenerateOptimizedEntityCode(entity, settings);
    }

    /// <summary>
    /// Generates optimized GetPartitionKey method with minimal string operations.
    /// </summary>
    private static void GenerateOptimizedGetPartitionKeyMethod(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// High-performance partition key extraction with minimal allocations.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        public static string GetPartitionKey(Dictionary<string, AttributeValue> item)");
        sb.AppendLine("        {");
        
        var partitionKeyProperty = entity.PartitionKeyProperty;
        if (partitionKeyProperty != null)
        {
            sb.AppendLine($"            return item.TryGetValue(\"{partitionKeyProperty.AttributeName}\", out var pkValue) ? ");
            sb.AppendLine("                (pkValue.S ?? string.Empty) : string.Empty;");
        }
        else
        {
            sb.AppendLine("            return string.Empty;");
        }
        
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates optimized MatchesEntity method with efficient pattern matching.
    /// </summary>
    private static void GenerateOptimizedMatchesEntityMethod(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// High-performance entity type matching with optimized pattern detection.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        public static bool MatchesEntity(Dictionary<string, AttributeValue> item)");
        sb.AppendLine("        {");
        
        // Generate optimized matching logic
        if (!string.IsNullOrEmpty(entity.EntityDiscriminator))
        {
            sb.AppendLine($"            // Fast discriminator check");
            sb.AppendLine($"            if (item.TryGetValue(\"EntityType\", out var entityTypeValue))");
            sb.AppendLine("            {");
            sb.AppendLine($"                return string.Equals(entityTypeValue.S, \"{entity.EntityDiscriminator}\", StringComparison.Ordinal);");
            sb.AppendLine("            }");
        }
        
        // Check required attributes for fast validation
        var requiredAttributes = entity.Properties
            .Where(p => p.HasAttributeMapping && (p.IsPartitionKey || !p.IsNullable))
            .Take(3) // Limit to first 3 for performance
            .ToArray();
        
        if (requiredAttributes.Length > 0)
        {
            sb.AppendLine("            // Fast required attribute check");
            foreach (var property in requiredAttributes)
            {
                sb.AppendLine($"            if (!item.ContainsKey(\"{property.AttributeName}\"))");
                sb.AppendLine("                return false;");
            }
        }
        
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates optimized GetEntityMetadata method with caching.
    /// </summary>
    private static void GenerateOptimizedGetEntityMetadataMethod(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine();
        sb.AppendLine("        private static EntityMetadata? _cachedMetadata;");
        sb.AppendLine("        private static readonly object _metadataLock = new object();");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets cached entity metadata with thread-safe lazy initialization.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        public static EntityMetadata GetEntityMetadata()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_cachedMetadata != null)");
        sb.AppendLine("                return _cachedMetadata;");
        sb.AppendLine();
        sb.AppendLine("            lock (_metadataLock)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_cachedMetadata != null)");
        sb.AppendLine("                    return _cachedMetadata;");
        sb.AppendLine();
        sb.AppendLine("                _cachedMetadata = EntityMetadataCache.GetOrCreate(CreateEntityModel());");
        sb.AppendLine("                return _cachedMetadata;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private static EntityModel CreateEntityModel()");
        sb.AppendLine("        {");
        sb.AppendLine("            // This would contain the entity model creation logic");
        sb.AppendLine("            // Simplified for performance optimization example");
        sb.AppendLine($"            return new EntityModel {{ ClassName = \"{entity.ClassName}\", Namespace = \"{entity.Namespace}\" }};");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates error placeholder code when generation fails.
    /// </summary>
    private static string GenerateErrorPlaceholder(EntityModel entity, Exception ex)
    {
        return $@"// <auto-generated />
// ERROR: Failed to generate code for {entity.ClassName}
// {ex.Message}

#error Source generation failed for {entity.ClassName}: {ex.Message}
";
    }

    /// <summary>
    /// Creates a stable cache key for entity transformation results.
    /// </summary>
    private static string CreateEntityCacheKey(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var sb = new StringBuilder();
        
        // Include class name and namespace
        var symbol = semanticModel.GetDeclaredSymbol(classDecl);
        if (symbol != null)
        {
            sb.Append(symbol.ContainingNamespace.ToDisplayString());
            sb.Append('.');
            sb.Append(symbol.Name);
        }
        
        // Include attribute signatures for change detection
        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                sb.Append('|');
                sb.Append(attribute.ToString());
            }
        }
        
        // Include property signatures
        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            sb.Append('|');
            sb.Append(member.Identifier.ValueText);
            sb.Append(':');
            sb.Append(member.Type.ToString());
            
            // Include property attributes
            foreach (var attributeList in member.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    sb.Append('@');
                    sb.Append(attribute.ToString());
                }
            }
        }
        
        return sb.ToString();
    }

    private static string GetTargetFramework(Compilation compilation)
    {
        // Extract target framework from compilation references
        var frameworkReference = compilation.References
            .OfType<PortableExecutableReference>()
            .FirstOrDefault(r => r.Display?.Contains("System.Runtime") == true);
            
        return frameworkReference?.Display?.Contains("net8.0") == true ? "net8.0" : "unknown";
    }
}

/// <summary>
/// Result of entity transformation with caching information.
/// </summary>
public class EntityTransformResult
{
    public EntityModel? EntityModel { get; set; }
    public ImmutableArray<Diagnostic> Diagnostics { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}



/// <summary>
/// High-performance cache for entity transformation results.
/// </summary>
public static class EntityTransformCache
{
    private static readonly Dictionary<string, EntityTransformResult> _cache = new();
    private static readonly object _lock = new();
    private static int _hits = 0;
    private static int _misses = 0;

    public static bool TryGetCached(string cacheKey, out EntityTransformResult result)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out result!))
            {
                _hits++;
                return true;
            }
            
            _misses++;
            return false;
        }
    }

    public static void Cache(string cacheKey, EntityTransformResult result)
    {
        lock (_lock)
        {
            _cache[cacheKey] = result;
        }
    }

    public static double GetCacheHitRate()
    {
        lock (_lock)
        {
            var total = _hits + _misses;
            return total > 0 ? (double)_hits / total : 0.0;
        }
    }

    public static void PerformMaintenance()
    {
        lock (_lock)
        {
            // Remove old entries if cache gets too large
            if (_cache.Count > 1000)
            {
                var oldEntries = _cache
                    .Where(kvp => DateTime.UtcNow - kvp.Value.LastModified > TimeSpan.FromMinutes(30))
                    .Select(kvp => kvp.Key)
                    .ToArray();

                foreach (var key in oldEntries)
                {
                    _cache.Remove(key);
                }
            }
        }
    }
}