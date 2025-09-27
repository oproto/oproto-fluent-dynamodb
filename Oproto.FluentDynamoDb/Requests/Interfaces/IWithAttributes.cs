using Amazon.DynamoDBv2.Model;

namespace Oproto.FluentDynamoDb.Requests.Interfaces;

/// <summary>
/// Interface for request builders that support attribute name mappings.
/// Attribute name mappings are essential when attribute names conflict with DynamoDB reserved words
/// or contain special characters that cannot be used directly in expressions.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing this interface.</typeparam>
public interface IWithAttributeNames<out TBuilder>
{
    /// <summary>
    /// Adds multiple attribute name mappings for use in expressions.
    /// </summary>
    /// <param name="attributeNames">A dictionary mapping parameter names to actual attribute names.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithAttributes(Dictionary<string, string> attributeNames);

    /// <summary>
    /// Adds multiple attribute name mappings using a configuration action.
    /// </summary>
    /// <param name="attributeNameFunc">An action that configures the attribute name mappings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithAttributes(Action<Dictionary<string, string>> attributeNameFunc);

    /// <summary>
    /// Adds a single attribute name mapping for use in expressions.
    /// </summary>
    /// <param name="parameterName">The parameter name to use in expressions (e.g., "#name").</param>
    /// <param name="attributeName">The actual attribute name in the table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithAttribute(string parameterName, string attributeName);
}

/// <summary>
/// Interface for request builders that support attribute value mappings.
/// Attribute value mappings allow you to parameterize expressions with actual values,
/// providing type safety and preventing injection attacks.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing this interface.</typeparam>
public interface IWithAttributeValues<out TBuilder>
{
    /// <summary>
    /// Adds multiple attribute values for use in expressions.
    /// </summary>
    /// <param name="attributeValues">A dictionary mapping parameter names to AttributeValue objects.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValues(
        Dictionary<string, AttributeValue> attributeValues);

    /// <summary>
    /// Adds multiple attribute values using a configuration action.
    /// </summary>
    /// <param name="attributeValueFunc">An action that configures the attribute value mappings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValues(
        Action<Dictionary<string, AttributeValue>> attributeValueFunc);
    
    /// <summary>
    /// Adds a string attribute value for use in expressions.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":value").</param>
    /// <param name="attributeValue">The string value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValue(
        string attributeName, string? attributeValue, bool conditionalUse = true);
    
    /// <summary>
    /// Adds a boolean attribute value for use in expressions.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":active").</param>
    /// <param name="attributeValue">The boolean value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValue(
        string attributeName, bool? attributeValue, bool conditionalUse = true);
    
    /// <summary>
    /// Adds a numeric attribute value for use in expressions.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":amount").</param>
    /// <param name="attributeValue">The decimal value to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValue(
        string attributeName, decimal? attributeValue, bool conditionalUse = true);

    /// <summary>
    /// Adds a map attribute value (string dictionary) for use in expressions.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":metadata").</param>
    /// <param name="attributeValue">The string dictionary to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValue(string attributeName, Dictionary<string, string> attributeValue, bool conditionalUse = true);
    
    /// <summary>
    /// Adds a map attribute value (AttributeValue dictionary) for use in expressions.
    /// </summary>
    /// <param name="attributeName">The parameter name to use in expressions (e.g., ":complex").</param>
    /// <param name="attributeValue">The AttributeValue dictionary to associate with the parameter.</param>
    /// <param name="conditionalUse">If false, the value is not added when null. Defaults to true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithValue(string attributeName, Dictionary<string, AttributeValue> attributeValue, bool conditionalUse = true);
}