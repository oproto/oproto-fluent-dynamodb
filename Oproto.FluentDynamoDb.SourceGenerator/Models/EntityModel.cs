using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Oproto.FluentDynamoDb.SourceGenerator.Models;

/// <summary>
/// Represents a complete entity model extracted from source analysis.
/// </summary>
public class EntityModel
{
    /// <summary>
    /// Gets or sets the class name of the entity.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the entity.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DynamoDB table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional entity discriminator for multi-type tables.
    /// </summary>
    [Obsolete("Use Discriminator property instead")]
    public string? EntityDiscriminator { get; set; }

    /// <summary>
    /// Gets or sets the discriminator configuration for this entity.
    /// </summary>
    public DiscriminatorConfig? Discriminator { get; set; }

    /// <summary>
    /// Gets or sets the properties of the entity.
    /// </summary>
    public PropertyModel[] Properties { get; set; } = Array.Empty<PropertyModel>();

    /// <summary>
    /// Gets or sets the Global Secondary Indexes defined for the entity.
    /// </summary>
    public IndexModel[] Indexes { get; set; } = Array.Empty<IndexModel>();

    /// <summary>
    /// Gets or sets the related entity relationships.
    /// </summary>
    public RelationshipModel[] Relationships { get; set; } = Array.Empty<RelationshipModel>();

    /// <summary>
    /// Gets or sets a value indicating whether this entity spans multiple DynamoDB items.
    /// </summary>
    public bool IsMultiItemEntity { get; set; }

    /// <summary>
    /// Gets or sets the original class declaration syntax node.
    /// </summary>
    public ClassDeclarationSyntax? ClassDeclaration { get; set; }

    /// <summary>
    /// Gets or sets the semantic model for accessing compilation information.
    /// </summary>
    public SemanticModel? SemanticModel { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer configuration information.
    /// </summary>
    public Oproto.FluentDynamoDb.SourceGenerator.Analysis.JsonSerializerInfo? JsonSerializerInfo { get; set; }

    /// <summary>
    /// Gets the partition key property, if any.
    /// </summary>
    public PropertyModel? PartitionKeyProperty => Properties.FirstOrDefault(p => p.IsPartitionKey);

    /// <summary>
    /// Gets the sort key property, if any.
    /// </summary>
    public PropertyModel? SortKeyProperty => Properties.FirstOrDefault(p => p.IsSortKey);

    /// <summary>
    /// Gets a value indicating whether this entity has a valid key structure.
    /// </summary>
    public bool HasValidKeyStructure => PartitionKeyProperty != null;
}