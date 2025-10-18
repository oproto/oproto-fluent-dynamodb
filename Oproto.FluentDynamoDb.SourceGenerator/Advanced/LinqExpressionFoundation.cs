using Oproto.FluentDynamoDb.SourceGenerator.Models;
using System.Text;
using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.SourceGenerator.Advanced;

/// <summary>
/// Provides the foundation for future LINQ expression support by generating comprehensive metadata
/// and expression-friendly structures for DynamoDB entities.
/// </summary>
public static class LinqExpressionFoundation
{
    /// <summary>
    /// Generates LINQ-ready metadata structures for an entity.
    /// </summary>
    public static void GenerateLinqFoundation(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine();
        sb.AppendLine("        #region LINQ Expression Foundation");
        sb.AppendLine();

        // Generate expression metadata
        GenerateExpressionMetadata(sb, entity);

        // Generate property accessors for expression trees
        GeneratePropertyAccessors(sb, entity);

        // Generate query capability metadata
        GenerateQueryCapabilities(sb, entity);

        // Generate index optimization hints
        GenerateIndexOptimizationHints(sb, entity);

        sb.AppendLine("        #endregion");
    }

    /// <summary>
    /// Generates comprehensive expression metadata for LINQ support.
    /// </summary>
    private static void GenerateExpressionMetadata(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Expression metadata for LINQ query translation.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static class ExpressionMetadata");
        sb.AppendLine("        {");

        // Generate property expression mappings
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Maps property expressions to DynamoDB attribute names.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static readonly Dictionary<string, string> PropertyToAttributeMap = new()");
        sb.AppendLine("            {");

        foreach (var property in entity.Properties.Where(p => p.HasAttributeMapping))
        {
            sb.AppendLine($"                {{ nameof({entity.ClassName}.{property.PropertyName}), \"{property.AttributeName}\" }},");
        }

        sb.AppendLine("            };");
        sb.AppendLine();

        // Generate operation support mappings
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Maps properties to their supported DynamoDB operations.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static readonly Dictionary<string, DynamoDbOperation[]> PropertyOperations = new()");
        sb.AppendLine("            {");

        foreach (var property in entity.Properties.Where(p => p.Queryable != null))
        {
            var operations = property.Queryable!.SupportedOperations ?? Array.Empty<DynamoDbOperation>();
            var operationList = string.Join(", ", operations.Select(op => $"DynamoDbOperation.{op}"));
            sb.AppendLine($"                {{ nameof({entity.ClassName}.{property.PropertyName}), new[] {{ {operationList} }} }},");
        }

        sb.AppendLine("            };");
        sb.AppendLine();

        // Generate index availability mappings
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Maps properties to the indexes where they are available.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static readonly Dictionary<string, string[]> PropertyIndexes = new()");
        sb.AppendLine("            {");

        foreach (var property in entity.Properties.Where(p => p.Queryable?.AvailableInIndexes?.Length > 0))
        {
            var indexes = string.Join(", ", property.Queryable!.AvailableInIndexes!.Select(idx => $"\"{idx}\""));
            sb.AppendLine($"                {{ nameof({entity.ClassName}.{property.PropertyName}), new[] {{ {indexes} }} }},");
        }

        sb.AppendLine("            };");

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates property accessors optimized for expression tree compilation.
    /// </summary>
    private static void GeneratePropertyAccessors(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Property accessors for efficient expression tree compilation.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static class PropertyAccessors");
        sb.AppendLine("        {");

        foreach (var property in entity.Properties.Where(p => p.HasAttributeMapping))
        {
            GeneratePropertyAccessor(sb, entity, property);
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a property accessor for a specific property.
    /// </summary>
    private static void GeneratePropertyAccessor(StringBuilder sb, EntityModel entity, PropertyModel property)
    {
        var propertyType = property.PropertyType;
        var accessorName = $"Get{property.PropertyName}";
        var setterName = $"Set{property.PropertyName}";

        // Generate getter
        sb.AppendLine($"            /// <summary>");
        sb.AppendLine($"            /// Gets the {property.PropertyName} property value.");
        sb.AppendLine($"            /// </summary>");
        sb.AppendLine($"            public static readonly Func<{entity.ClassName}, {propertyType}> {accessorName} = ");
        sb.AppendLine($"                entity => entity.{property.PropertyName};");
        sb.AppendLine();

        // Generate setter for non-readonly properties
        if (!property.IsReadOnly)
        {
            sb.AppendLine($"            /// <summary>");
            sb.AppendLine($"            /// Sets the {property.PropertyName} property value.");
            sb.AppendLine($"            /// </summary>");
            sb.AppendLine($"            public static readonly Action<{entity.ClassName}, {propertyType}> {setterName} = ");
            sb.AppendLine($"                (entity, value) => entity.{property.PropertyName} = value;");
            sb.AppendLine();
        }

        // Generate expression tree for the property
        sb.AppendLine($"            /// <summary>");
        sb.AppendLine($"            /// Expression tree for the {property.PropertyName} property.");
        sb.AppendLine($"            /// </summary>");
        sb.AppendLine($"            public static readonly Expression<Func<{entity.ClassName}, {propertyType}>> {property.PropertyName}Expression = ");
        sb.AppendLine($"                entity => entity.{property.PropertyName};");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates query capability metadata for optimization.
    /// </summary>
    private static void GenerateQueryCapabilities(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Query capabilities and optimization metadata.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static class QueryCapabilities");
        sb.AppendLine("        {");

        // Generate partition key information
        var partitionKey = entity.PartitionKeyProperty;
        if (partitionKey != null)
        {
            sb.AppendLine("            /// <summary>");
            sb.AppendLine("            /// Partition key property information for query optimization.");
            sb.AppendLine("            /// </summary>");
            sb.AppendLine("            public static readonly PartitionKeyInfo PartitionKey = new()");
            sb.AppendLine("            {");
            sb.AppendLine($"                PropertyName = nameof({entity.ClassName}.{partitionKey.PropertyName}),");
            sb.AppendLine($"                AttributeName = \"{partitionKey.AttributeName}\",");
            sb.AppendLine($"                PropertyType = typeof({partitionKey.PropertyType}),");
            sb.AppendLine($"                IsRequired = true,");
            sb.AppendLine($"                KeyFormat = {GenerateKeyFormatLiteral(partitionKey.KeyFormat)}");
            sb.AppendLine("            };");
            sb.AppendLine();
        }

        // Generate sort key information
        var sortKey = entity.SortKeyProperty;
        if (sortKey != null)
        {
            sb.AppendLine("            /// <summary>");
            sb.AppendLine("            /// Sort key property information for query optimization.");
            sb.AppendLine("            /// </summary>");
            sb.AppendLine("            public static readonly SortKeyInfo SortKey = new()");
            sb.AppendLine("            {");
            sb.AppendLine($"                PropertyName = nameof({entity.ClassName}.{sortKey.PropertyName}),");
            sb.AppendLine($"                AttributeName = \"{sortKey.AttributeName}\",");
            sb.AppendLine($"                PropertyType = typeof({sortKey.PropertyType}),");
            sb.AppendLine($"                IsRequired = false,");
            sb.AppendLine($"                KeyFormat = {GenerateKeyFormatLiteral(sortKey.KeyFormat)}");
            sb.AppendLine("            };");
            sb.AppendLine();
        }

        // Generate queryable properties
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Properties that support efficient querying.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static readonly QueryablePropertyInfo[] QueryableProperties = new[]");
        sb.AppendLine("            {");

        foreach (var property in entity.Properties.Where(p => p.Queryable != null))
        {
            sb.AppendLine("                new QueryablePropertyInfo");
            sb.AppendLine("                {");
            sb.AppendLine($"                    PropertyName = nameof({entity.ClassName}.{property.PropertyName}),");
            sb.AppendLine($"                    AttributeName = \"{property.AttributeName}\",");
            sb.AppendLine($"                    PropertyType = typeof({property.PropertyType}),");
            sb.AppendLine($"                    SupportedOperations = new[] {{ {string.Join(", ", property.Queryable!.SupportedOperations?.Select(op => $"DynamoDbOperation.{op}") ?? Array.Empty<string>())} }},");
            sb.AppendLine($"                    AvailableInIndexes = new[] {{ {string.Join(", ", property.Queryable.AvailableInIndexes?.Select(idx => $"\"{idx}\"") ?? Array.Empty<string>())} }}");
            sb.AppendLine("                },");
        }

        sb.AppendLine("            };");

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates index optimization hints for query planning.
    /// </summary>
    private static void GenerateIndexOptimizationHints(StringBuilder sb, EntityModel entity)
    {
        if (entity.Indexes.Length == 0)
            return;

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Index optimization hints for query planning.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static class IndexOptimization");
        sb.AppendLine("        {");

        foreach (var index in entity.Indexes)
        {
            GenerateIndexOptimizationHint(sb, entity, index);
        }

        // Generate index selection logic
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Selects the optimal index for a given query pattern.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static string? SelectOptimalIndex(string[] queryProperties, DynamoDbOperation[] operations)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Simple heuristic: prefer indexes that cover the most query properties");
        sb.AppendLine("                var bestIndex = string.Empty;");
        sb.AppendLine("                var bestScore = 0;");
        sb.AppendLine();

        foreach (var index in entity.Indexes)
        {
            sb.AppendLine($"                // Check {index.IndexName}");
            sb.AppendLine($"                var {GetSafeVariableName(index.IndexName)}Score = 0;");
            sb.AppendLine($"                if (queryProperties.Contains(\"{index.PartitionKeyProperty}\")) {GetSafeVariableName(index.IndexName)}Score += 10;");
            
            if (!string.IsNullOrEmpty(index.SortKeyProperty))
            {
                sb.AppendLine($"                if (queryProperties.Contains(\"{index.SortKeyProperty}\")) {GetSafeVariableName(index.IndexName)}Score += 5;");
            }

            sb.AppendLine($"                if ({GetSafeVariableName(index.IndexName)}Score > bestScore)");
            sb.AppendLine("                {");
            sb.AppendLine($"                    bestScore = {GetSafeVariableName(index.IndexName)}Score;");
            sb.AppendLine($"                    bestIndex = \"{index.IndexName}\";");
            sb.AppendLine("                }");
            sb.AppendLine();
        }

        sb.AppendLine("                return bestScore > 0 ? bestIndex : null;");
        sb.AppendLine("            }");

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates optimization hints for a specific index.
    /// </summary>
    private static void GenerateIndexOptimizationHint(StringBuilder sb, EntityModel entity, IndexModel index)
    {
        var indexVarName = GetSafeVariableName(index.IndexName);

        sb.AppendLine($"            /// <summary>");
        sb.AppendLine($"            /// Optimization hints for {index.IndexName} index.");
        sb.AppendLine($"            /// </summary>");
        sb.AppendLine($"            public static readonly IndexOptimizationHint {indexVarName} = new()");
        sb.AppendLine("            {");
        sb.AppendLine($"                IndexName = \"{index.IndexName}\",");
        sb.AppendLine($"                PartitionKeyProperty = \"{index.PartitionKeyProperty}\",");
        sb.AppendLine($"                SortKeyProperty = {(string.IsNullOrEmpty(index.SortKeyProperty) ? "null" : $"\"{index.SortKeyProperty}\"")},");
        sb.AppendLine($"                ProjectedProperties = new[] {{ {string.Join(", ", index.ProjectedProperties?.Select(p => $"\"{p}\"") ?? Array.Empty<string>())} }},");
        sb.AppendLine($"                OptimalForOperations = new[] {{ DynamoDbOperation.Equals, DynamoDbOperation.BeginsWith }},");
        sb.AppendLine($"                EstimatedSelectivity = {EstimateIndexSelectivity(index)},");
        sb.AppendLine($"                RecommendedForQueries = new[] {{ {GenerateRecommendedQueryPatterns(index)} }}");
        sb.AppendLine("            };");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the supporting classes for LINQ expression foundation.
    /// </summary>
    public static string GenerateLinqSupportClasses()
    {
        return @"
/// <summary>
/// Information about a partition key property for query optimization.
/// </summary>
public class PartitionKeyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public bool IsRequired { get; set; }
    public KeyFormatMetadata? KeyFormat { get; set; }
}

/// <summary>
/// Information about a sort key property for query optimization.
/// </summary>
public class SortKeyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public bool IsRequired { get; set; }
    public KeyFormatMetadata? KeyFormat { get; set; }
}

/// <summary>
/// Information about a queryable property for LINQ expression translation.
/// </summary>
public class QueryablePropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public DynamoDbOperation[] SupportedOperations { get; set; } = Array.Empty<DynamoDbOperation>();
    public string[] AvailableInIndexes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Optimization hints for a specific index.
/// </summary>
public class IndexOptimizationHint
{
    public string IndexName { get; set; } = string.Empty;
    public string PartitionKeyProperty { get; set; } = string.Empty;
    public string? SortKeyProperty { get; set; }
    public string[] ProjectedProperties { get; set; } = Array.Empty<string>();
    public DynamoDbOperation[] OptimalForOperations { get; set; } = Array.Empty<DynamoDbOperation>();
    public double EstimatedSelectivity { get; set; }
    public string[] RecommendedForQueries { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Key format metadata for LINQ expression support.
/// </summary>
public class KeyFormatMetadata
{
    public string? Prefix { get; set; }
    public string Separator { get; set; } = ""#"";
    public bool HasCustomFormat { get; set; }
    public string? FormatString { get; set; }
}";
    }

    private static string GenerateKeyFormatLiteral(KeyFormatModel? keyFormat)
    {
        if (keyFormat == null)
            return "null";

        return $@"new KeyFormatMetadata 
            {{ 
                Prefix = {(string.IsNullOrEmpty(keyFormat.Prefix) ? "null" : $"\"{keyFormat.Prefix}\"")},
                Separator = ""{keyFormat.Separator}"",
                HasCustomFormat = false
            }}";
    }

    private static string GetSafeVariableName(string name)
    {
        // Convert to safe C# variable name
        var safeName = new StringBuilder();
        
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsLetterOrDigit(c))
            {
                safeName.Append(c);
            }
            else if (c == '-' || c == '_')
            {
                safeName.Append('_');
            }
        }
        
        var result = safeName.ToString();
        
        // Ensure it starts with a letter
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "Index" + result;
        }
        
        return string.IsNullOrEmpty(result) ? "Index" : result;
    }

    private static double EstimateIndexSelectivity(IndexModel index)
    {
        // Simple heuristic for index selectivity
        // In a real implementation, this could be based on actual data analysis
        return 0.1; // Assume 10% selectivity as default
    }

    private static string GenerateRecommendedQueryPatterns(IndexModel index)
    {
        var patterns = new List<string>
        {
            $"\"{index.PartitionKeyProperty} = value\""
        };

        if (!string.IsNullOrEmpty(index.SortKeyProperty))
        {
            patterns.Add($"\"{index.PartitionKeyProperty} = value AND {index.SortKeyProperty} begins_with prefix\"");
            patterns.Add($"\"{index.PartitionKeyProperty} = value AND {index.SortKeyProperty} BETWEEN low AND high\"");
        }

        return string.Join(", ", patterns);
    }
}