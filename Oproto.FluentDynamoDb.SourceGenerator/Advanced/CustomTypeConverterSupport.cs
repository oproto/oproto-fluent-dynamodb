using Microsoft.CodeAnalysis;
using Oproto.FluentDynamoDb.SourceGenerator.Models;
using System.Text;

namespace Oproto.FluentDynamoDb.SourceGenerator.Advanced;

/// <summary>
/// Provides support for custom type converters in DynamoDB entity mapping.
/// Enables advanced mapping scenarios beyond the built-in type conversions.
/// </summary>
public static class CustomTypeConverterSupport
{
    /// <summary>
    /// Generates code that supports custom type converters for complex property types.
    /// </summary>
    public static void GenerateCustomConverterSupport(StringBuilder sb, EntityModel entity)
    {
        var customProperties = entity.Properties
            .Where(p => RequiresCustomConverter(p))
            .ToArray();

        if (customProperties.Length == 0)
            return;

        sb.AppendLine();
        sb.AppendLine("        #region Custom Type Converter Support");
        sb.AppendLine();

        // Generate converter registry
        GenerateConverterRegistry(sb, entity, customProperties);

        // Generate converter methods
        foreach (var property in customProperties)
        {
            GeneratePropertyConverter(sb, property);
        }

        sb.AppendLine("        #endregion");
    }

    /// <summary>
    /// Generates a converter registry for managing custom type converters.
    /// </summary>
    private static void GenerateConverterRegistry(StringBuilder sb, EntityModel entity, PropertyModel[] customProperties)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Registry for custom type converters used by this entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static readonly Dictionary<Type, ICustomTypeConverter> _customConverters = new()");
        sb.AppendLine("        {");

        foreach (var property in customProperties)
        {
            var converterType = GetConverterTypeName(property);
            sb.AppendLine($"            {{ typeof({GetPropertyTypeName(property)}), new {converterType}() }},");
        }

        sb.AppendLine("        };");
        sb.AppendLine();

        // Generate converter lookup method
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets a custom converter for the specified type, if available.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static ICustomTypeConverter? GetCustomConverter(Type type)");
        sb.AppendLine("        {");
        sb.AppendLine("            _customConverters.TryGetValue(type, out var converter);");
        sb.AppendLine("            return converter;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates converter methods for a specific property type.
    /// </summary>
    private static void GeneratePropertyConverter(StringBuilder sb, PropertyModel property)
    {
        var propertyTypeName = GetPropertyTypeName(property);
        var converterMethodName = $"Convert{property.PropertyName}";

        // Generate ToAttributeValue converter
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts {property.PropertyName} to DynamoDB AttributeValue using custom converter.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        private static AttributeValue {converterMethodName}ToAttributeValue({propertyTypeName} value)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var converter = GetCustomConverter(typeof({propertyTypeName}));");
        sb.AppendLine("            if (converter != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                return converter.ToAttributeValue(value);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Fallback to default conversion");
        sb.AppendLine($"            return {GetFallbackToAttributeValueExpression(property, "value")};");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FromAttributeValue converter
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts DynamoDB AttributeValue to {property.PropertyName} using custom converter.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        private static {propertyTypeName} {converterMethodName}FromAttributeValue(AttributeValue attributeValue)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var converter = GetCustomConverter(typeof({propertyTypeName}));");
        sb.AppendLine("            if (converter != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return ({propertyTypeName})converter.FromAttributeValue(attributeValue, typeof({propertyTypeName}));");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Fallback to default conversion");
        sb.AppendLine($"            return {GetFallbackFromAttributeValueExpression(property, "attributeValue")};");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the interface definition for custom type converters.
    /// </summary>
    public static string GenerateCustomTypeConverterInterface()
    {
        return @"
/// <summary>
/// Interface for custom type converters that handle complex type mappings to/from DynamoDB.
/// </summary>
public interface ICustomTypeConverter
{
    /// <summary>
    /// Converts a .NET object to a DynamoDB AttributeValue.
    /// </summary>
    /// <param name=""value"">The .NET object to convert.</param>
    /// <returns>The corresponding DynamoDB AttributeValue.</returns>
    AttributeValue ToAttributeValue(object? value);

    /// <summary>
    /// Converts a DynamoDB AttributeValue to a .NET object.
    /// </summary>
    /// <param name=""attributeValue"">The DynamoDB AttributeValue to convert.</param>
    /// <param name=""targetType"">The target .NET type.</param>
    /// <returns>The converted .NET object.</returns>
    object? FromAttributeValue(AttributeValue attributeValue, Type targetType);

    /// <summary>
    /// Gets the supported .NET types for this converter.
    /// </summary>
    Type[] SupportedTypes { get; }
}";
    }

    /// <summary>
    /// Generates built-in converter implementations for common complex types.
    /// </summary>
    public static string GenerateBuiltInConverters()
    {
        var sb = new StringBuilder();

        // URI converter
        sb.AppendLine(GenerateUriConverter());
        sb.AppendLine();

        // TimeSpan converter
        sb.AppendLine(GenerateTimeSpanConverter());
        sb.AppendLine();

        // Dictionary converter
        sb.AppendLine(GenerateDictionaryConverter());

        return sb.ToString();
    }



    private static string GenerateUriConverter()
    {
        return @"/// <summary>
/// High-performance converter for Uri objects.
/// </summary>
public class UriTypeConverter : ICustomTypeConverter
{
    public Type[] SupportedTypes => new[] { typeof(Uri) };

    public AttributeValue ToAttributeValue(object? value)
    {
        if (value is not Uri uri)
            return new AttributeValue { NULL = true };

        return new AttributeValue { S = uri.ToString() };
    }

    public object? FromAttributeValue(AttributeValue attributeValue, Type targetType)
    {
        if (attributeValue.NULL || string.IsNullOrEmpty(attributeValue.S))
            return null;

        if (Uri.TryCreate(attributeValue.S, UriKind.RelativeOrAbsolute, out var uri))
            return uri;

        throw new InvalidOperationException($""Invalid URI format: {attributeValue.S}"");
    }
}";
    }

    private static string GenerateTimeSpanConverter()
    {
        return @"/// <summary>
/// High-performance converter for TimeSpan objects using ticks for precision.
/// </summary>
public class TimeSpanTypeConverter : ICustomTypeConverter
{
    public Type[] SupportedTypes => new[] { typeof(TimeSpan) };

    public AttributeValue ToAttributeValue(object? value)
    {
        if (value is not TimeSpan timeSpan)
            return new AttributeValue { NULL = true };

        // Store as ticks for maximum precision and efficient sorting
        return new AttributeValue { N = timeSpan.Ticks.ToString() };
    }

    public object? FromAttributeValue(AttributeValue attributeValue, Type targetType)
    {
        if (attributeValue.NULL || string.IsNullOrEmpty(attributeValue.N))
            return null;

        if (long.TryParse(attributeValue.N, out var ticks))
            return new TimeSpan(ticks);

        throw new InvalidOperationException($""Invalid TimeSpan ticks format: {attributeValue.N}"");
    }
}";
    }

    private static string GenerateDictionaryConverter()
    {
        return @"/// <summary>
/// High-performance converter for Dictionary objects using DynamoDB Map type.
/// </summary>
public class DictionaryTypeConverter : ICustomTypeConverter
{
    public Type[] SupportedTypes => new[] { typeof(Dictionary<string, object>), typeof(IDictionary<string, object>) };

    public AttributeValue ToAttributeValue(object? value)
    {
        if (value is not IDictionary<string, object> dictionary)
            return new AttributeValue { NULL = true };

        var map = new Dictionary<string, AttributeValue>();
        
        foreach (var kvp in dictionary)
        {
            if (kvp.Value == null)
            {
                map[kvp.Key] = new AttributeValue { NULL = true };
            }
            else
            {
                // Convert common types to appropriate AttributeValue
                map[kvp.Key] = kvp.Value switch
                {
                    string s => new AttributeValue { S = s },
                    int i => new AttributeValue { N = i.ToString() },
                    long l => new AttributeValue { N = l.ToString() },
                    double d => new AttributeValue { N = d.ToString(""G17"") },
                    bool b => new AttributeValue { BOOL = b },
                    _ => new AttributeValue { S = kvp.Value.ToString() ?? """" }
                };
            }
        }

        return new AttributeValue { M = map };
    }

    public object? FromAttributeValue(AttributeValue attributeValue, Type targetType)
    {
        if (attributeValue.NULL || attributeValue.M == null)
            return null;

        var dictionary = new Dictionary<string, object>();
        
        foreach (var kvp in attributeValue.M)
        {
            var value = kvp.Value;
            
            if (value.NULL)
            {
                dictionary[kvp.Key] = null!;
            }
            else if (!string.IsNullOrEmpty(value.S))
            {
                dictionary[kvp.Key] = value.S;
            }
            else if (!string.IsNullOrEmpty(value.N))
            {
                // Try to parse as different numeric types
                if (int.TryParse(value.N, out var intVal))
                    dictionary[kvp.Key] = intVal;
                else if (long.TryParse(value.N, out var longVal))
                    dictionary[kvp.Key] = longVal;
                else if (double.TryParse(value.N, out var doubleVal))
                    dictionary[kvp.Key] = doubleVal;
                else
                    dictionary[kvp.Key] = value.N;
            }
            else if (value.BOOL.HasValue)
            {
                dictionary[kvp.Key] = value.BOOL.Value;
            }
            else
            {
                dictionary[kvp.Key] = value.ToString() ?? """";
            }
        }

        return dictionary;
    }
}";
    }

    /// <summary>
    /// Determines if a property requires a custom converter.
    /// Only generates converters for specific well-known types that we have implementations for.
    /// </summary>
    private static bool RequiresCustomConverter(PropertyModel property)
    {
        var propertyType = property.PropertyType;
        
        // Only generate converters for specific types we have implementations for
        if (propertyType.Contains("Uri") || 
            propertyType.Contains("TimeSpan") ||
            propertyType.Contains("Dictionary") ||
            propertyType.Contains("IDictionary"))
            return true;

        return false;
    }



    private static string GetPropertyTypeName(PropertyModel property)
    {
        return property.PropertyType;
    }

    private static string GetConverterTypeName(PropertyModel property)
    {
        var propertyType = property.PropertyType;
        
        if (propertyType.Contains("Uri"))
            return "UriTypeConverter";
        if (propertyType.Contains("TimeSpan"))
            return "TimeSpanTypeConverter";
        if (propertyType.Contains("Dictionary") || propertyType.Contains("IDictionary"))
            return "DictionaryTypeConverter";
        
        // For other complex types, we shouldn't generate a converter - let them handle it manually
        return ""; // This will prevent generating a converter for unsupported types
    }

    private static string GetFallbackToAttributeValueExpression(PropertyModel property, string valueExpression)
    {
        // Provide fallback conversion for when custom converter is not available
        return $"new AttributeValue {{ S = {valueExpression}?.ToString() ?? \"\" }}";
    }

    private static string GetFallbackFromAttributeValueExpression(PropertyModel property, string valueExpression)
    {
        // Provide fallback conversion for when custom converter is not available
        var propertyType = GetPropertyTypeName(property);
        
        if (propertyType == "string")
            return $"{valueExpression}.S";
        
        return $"({propertyType})Convert.ChangeType({valueExpression}.S, typeof({propertyType}))";
    }
}