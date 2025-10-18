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
    /// Gets or sets an optional entity discriminator for multi-type tables.
    /// </summary>
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