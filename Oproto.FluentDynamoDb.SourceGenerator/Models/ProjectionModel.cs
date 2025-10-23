using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Oproto.FluentDynamoDb.SourceGenerator.Models;

/// <summary>
/// Represents a projection model during source generation.
/// </summary>
public class ProjectionModel
{
    /// <summary>
    /// Gets or sets the projection class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the namespace of the projection class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source entity type that this projection derives from.
    /// </summary>
    public string SourceEntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the properties included in the projection.
    /// </summary>
    public ProjectionPropertyModel[] Properties { get; set; } = Array.Empty<ProjectionPropertyModel>();
    
    /// <summary>
    /// Gets or sets the generated projection expression string.
    /// Example: "id, amount, created_date, entity_type"
    /// </summary>
    public string ProjectionExpression { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the discriminator property name if source entity uses discriminators.
    /// </summary>
    public string? DiscriminatorProperty { get; set; }
    
    /// <summary>
    /// Gets or sets the discriminator value for the source entity.
    /// </summary>
    public string? DiscriminatorValue { get; set; }
    
    /// <summary>
    /// Gets or sets the original class declaration syntax node.
    /// </summary>
    public ClassDeclarationSyntax? ClassDeclaration { get; set; }
}
