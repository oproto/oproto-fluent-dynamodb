using Amazon.DynamoDBv2.Model;

namespace Oproto.FluentDynamoDb.Requests.Interfaces;

/// <summary>
/// Interface for request builders that support key specification.
/// Provides methods to specify primary keys and sort keys for DynamoDB operations.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing this interface.</typeparam>
public interface IWithKey<out TBuilder>
{
    /// <summary>
    /// Specifies the primary key and optional sort key using AttributeValue objects.
    /// </summary>
    /// <param name="primaryKeyName">The name of the primary key attribute.</param>
    /// <param name="primaryKeyValue">The value of the primary key attribute.</param>
    /// <param name="sortKeyName">The name of the sort key attribute (optional).</param>
    /// <param name="sortKeyValue">The value of the sort key attribute (optional).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithKey(
        string primaryKeyName,
        AttributeValue primaryKeyValue,
        string? sortKeyName = null,
        AttributeValue? sortKeyValue = null);

    /// <summary>
    /// Specifies a single key attribute using a string value.
    /// </summary>
    /// <param name="keyName">The name of the key attribute.</param>
    /// <param name="keyValue">The string value of the key attribute.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithKey(string keyName, string keyValue);
    
    /// <summary>
    /// Specifies both primary key and sort key using string values.
    /// </summary>
    /// <param name="primaryKeyName">The name of the primary key attribute.</param>
    /// <param name="primaryKeyValue">The string value of the primary key attribute.</param>
    /// <param name="sortKeyName">The name of the sort key attribute.</param>
    /// <param name="sortKeyValue">The string value of the sort key attribute.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TBuilder WithKey(string primaryKeyName, string primaryKeyValue, string sortKeyName, string sortKeyValue);
}