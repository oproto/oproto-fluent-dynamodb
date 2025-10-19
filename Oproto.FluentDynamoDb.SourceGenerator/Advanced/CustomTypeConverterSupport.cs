using Oproto.FluentDynamoDb.SourceGenerator.Models;
using System.Text;

namespace Oproto.FluentDynamoDb.SourceGenerator.Advanced;

/// <summary>
/// Provides support for custom type converters in generated entity mapping code.
/// Enables extensible type conversion for complex or custom types.
/// </summary>
public static class CustomTypeConverterSupport
{
    /// <summary>
    /// Generates custom type converter support code for entities with complex types.
    /// </summary>
    public static void GenerateCustomConverterSupport(StringBuilder sb, EntityModel entity)
    {
        var customTypeProperties = entity.Properties
            .Where(p => RequiresCustomConverter(p.PropertyType))
            .ToArray();

        if (customTypeProperties.Length == 0)
            return;

        sb.AppendLine();
        sb.AppendLine("        #region Custom Type Converter Support");
        sb.AppendLine();

        // Generate converter interface
        GenerateConverterInterface(sb);

        // Generate converter registry
        GenerateConverterRegistry(sb, customTypeProperties);

        // Generate converter methods
        GenerateConverterMethods(sb, customTypeProperties);

        sb.AppendLine("        #endregion");
    }

    /// <summary>
    /// Generates the custom type converter interface.
    /// </summary>
    private static void GenerateConverterInterface(StringBuilder sb)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Interface for custom type converters that handle complex type conversions.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public interface ICustomTypeConverter<T>");
        sb.AppendLine("        {");
        sb.AppendLine("            AttributeValue ToAttributeValue(T value);");
        sb.AppendLine("            T FromAttributeValue(AttributeValue attributeValue);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the converter registry for managing custom converters.
    /// </summary>
    private static void GenerateConverterRegistry(StringBuilder sb, PropertyModel[] customTypeProperties)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Registry for custom type converters.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static readonly Dictionary<Type, object> _customConverters = new()");
        sb.AppendLine("        {");

        foreach (var property in customTypeProperties)
        {
            var converterType = GetDefaultConverterType(property.PropertyType);
            if (converterType != null)
            {
                sb.AppendLine($"            {{ typeof({GetBaseType(property.PropertyType)}), new {converterType}() }},");
            }
        }

        sb.AppendLine("        };");
        sb.AppendLine();

        // Generate converter registration method
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Registers a custom converter for a specific type.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static void RegisterConverter<T>(ICustomTypeConverter<T> converter)");
        sb.AppendLine("        {");
        sb.AppendLine("            _customConverters[typeof(T)] = converter;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates converter methods for custom types.
    /// </summary>
    private static void GenerateConverterMethods(StringBuilder sb, PropertyModel[] customTypeProperties)
    {
        foreach (var property in customTypeProperties)
        {
            GenerateConverterMethodsForType(sb, property);
        }
    }

    /// <summary>
    /// Generates converter methods for a specific property type.
    /// </summary>
    private static void GenerateConverterMethodsForType(StringBuilder sb, PropertyModel property)
    {
        var baseType = GetBaseType(property.PropertyType);
        var methodSuffix = baseType.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "");

        // To AttributeValue converter
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts {baseType} to AttributeValue using custom converter.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        private static AttributeValue Convert{methodSuffix}ToAttributeValue({baseType} value)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (_customConverters.TryGetValue(typeof({baseType}), out var converter))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return ((ICustomTypeConverter<{baseType}>)converter).ToAttributeValue(value);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            // Fallback to default conversion");
        sb.AppendLine($"            return {GetDefaultToAttributeValueConversion(property, "value")};");
        sb.AppendLine("        }");
        sb.AppendLine();

        // From AttributeValue converter
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts AttributeValue to {baseType} using custom converter.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        private static {baseType} Convert{methodSuffix}FromAttributeValue(AttributeValue attributeValue)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (_customConverters.TryGetValue(typeof({baseType}), out var converter))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return ((ICustomTypeConverter<{baseType}>)converter).FromAttributeValue(attributeValue);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            // Fallback to default conversion");
        sb.AppendLine($"            return {GetDefaultFromAttributeValueConversion(property, "attributeValue")};");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Determines if a property type requires a custom converter.
    /// </summary>
    private static bool RequiresCustomConverter(string propertyType)
    {
        var baseType = GetBaseType(propertyType);
        
        return baseType switch
        {
            "Uri" or "System.Uri" => true,
            "TimeSpan" or "System.TimeSpan" => true,
            "Version" or "System.Version" => true,
            _ when baseType.StartsWith("Dictionary<") => true,
            _ when baseType.StartsWith("HashSet<") => true,
            _ when baseType.StartsWith("SortedSet<") => true,
            _ when baseType.Contains("JsonElement") => true,
            _ when baseType.Contains("JsonDocument") => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the default converter type for a property type.
    /// </summary>
    private static string? GetDefaultConverterType(string propertyType)
    {
        var baseType = GetBaseType(propertyType);
        
        return baseType switch
        {
            "Uri" or "System.Uri" => "UriConverter",
            "TimeSpan" or "System.TimeSpan" => "TimeSpanConverter",
            "Version" or "System.Version" => "VersionConverter",
            _ when baseType.StartsWith("Dictionary<") => "DictionaryConverter",
            _ => null
        };
    }

    /// <summary>
    /// Gets the default to AttributeValue conversion for fallback scenarios.
    /// </summary>
    private static string GetDefaultToAttributeValueConversion(PropertyModel property, string valueExpression)
    {
        var baseType = GetBaseType(property.PropertyType);
        
        return baseType switch
        {
            "Uri" or "System.Uri" => $"new AttributeValue {{ S = {valueExpression}?.ToString() ?? string.Empty }}",
            "TimeSpan" or "System.TimeSpan" => $"new AttributeValue {{ S = {valueExpression}.ToString() }}",
            "Version" or "System.Version" => $"new AttributeValue {{ S = {valueExpression}?.ToString() ?? string.Empty }}",
            _ when baseType.StartsWith("Dictionary<") => $"new AttributeValue {{ S = System.Text.Json.JsonSerializer.Serialize({valueExpression}) }}",
            _ => $"new AttributeValue {{ S = {valueExpression}?.ToString() ?? string.Empty }}"
        };
    }

    /// <summary>
    /// Gets the default from AttributeValue conversion for fallback scenarios.
    /// </summary>
    private static string GetDefaultFromAttributeValueConversion(PropertyModel property, string valueExpression)
    {
        var baseType = GetBaseType(property.PropertyType);
        
        return baseType switch
        {
            "Uri" or "System.Uri" => $"new Uri({valueExpression}.S)",
            "TimeSpan" or "System.TimeSpan" => $"TimeSpan.Parse({valueExpression}.S)",
            "Version" or "System.Version" => $"new Version({valueExpression}.S)",
            _ when baseType.StartsWith("Dictionary<") => $"System.Text.Json.JsonSerializer.Deserialize<{baseType}>({valueExpression}.S) ?? new {baseType}()",
            _ => $"{valueExpression}.S"
        };
    }

    /// <summary>
    /// Gets the base type without nullable annotation.
    /// </summary>
    private static string GetBaseType(string typeName)
    {
        return typeName.TrimEnd('?');
    }
}