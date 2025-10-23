using System;

namespace Oproto.FluentDynamoDb.Attributes;

/// <summary>
/// Marks a class as a DynamoDB entity and specifies the table name.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DynamoDbTableAttribute : Attribute
{
    /// <summary>
    /// Gets the DynamoDB table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets or sets the property name containing the discriminator (e.g., "entity_type", "SK", "PK").
    /// If null, no discriminator validation is performed.
    /// </summary>
    /// <remarks>
    /// The discriminator property is used to identify which entity type a DynamoDB item represents
    /// when multiple entity types share the same table. Common patterns:
    /// <list type="bullet">
    /// <item><description>"entity_type" - Dedicated attribute for entity type</description></item>
    /// <item><description>"SK" - Sort key contains entity type (e.g., "USER#123")</description></item>
    /// <item><description>"PK" - Partition key contains entity type</description></item>
    /// </list>
    /// </remarks>
    public string? DiscriminatorProperty { get; set; }

    /// <summary>
    /// Gets or sets the exact value to match for this entity type.
    /// Mutually exclusive with <see cref="DiscriminatorPattern"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// [DynamoDbTable("MyTable", 
    ///     DiscriminatorProperty = "entity_type",
    ///     DiscriminatorValue = "USER")]
    /// </code>
    /// </example>
    public string? DiscriminatorValue { get; set; }

    /// <summary>
    /// Gets or sets a pattern to match for this entity type (supports * wildcard).
    /// Mutually exclusive with <see cref="DiscriminatorValue"/>.
    /// </summary>
    /// <remarks>
    /// Pattern matching supports the * wildcard character:
    /// <list type="bullet">
    /// <item><description>"USER#*" - Matches any value starting with "USER#"</description></item>
    /// <item><description>"*#USER#*" - Matches any value containing "#USER#"</description></item>
    /// <item><description>"*USER" - Matches any value ending with "USER"</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// [DynamoDbTable("MyTable",
    ///     DiscriminatorProperty = "SK",
    ///     DiscriminatorPattern = "USER#*")]
    /// </code>
    /// </example>
    public string? DiscriminatorPattern { get; set; }

    /// <summary>
    /// Gets or sets an optional entity discriminator for multi-type tables.
    /// </summary>
    /// <remarks>
    /// <strong>Legacy property for backward compatibility.</strong>
    /// Equivalent to setting <c>DiscriminatorProperty = "entity_type"</c> and <c>DiscriminatorValue</c>.
    /// New code should use <see cref="DiscriminatorProperty"/> and <see cref="DiscriminatorValue"/> instead.
    /// </remarks>
    [Obsolete("Use DiscriminatorProperty and DiscriminatorValue instead. This property will be removed in a future version.")]
    public string? EntityDiscriminator { get; set; }

    /// <summary>
    /// Initializes a new instance of the DynamoDbTableAttribute class.
    /// </summary>
    /// <param name="tableName">The DynamoDB table name.</param>
    public DynamoDbTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}