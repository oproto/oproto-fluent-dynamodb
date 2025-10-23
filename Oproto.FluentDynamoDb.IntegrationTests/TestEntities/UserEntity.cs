using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

/// <summary>
/// Test entity using attribute-based discriminator with entity_type property.
/// </summary>
[DynamoDbTable("test-multi-entity", 
    DiscriminatorProperty = "entity_type",
    DiscriminatorValue = "USER")]
public partial class UserEntity
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string Id { get; set; } = string.Empty;
    
    [SortKey]
    [DynamoDbAttribute("sk")]
    public string SortKey { get; set; } = "METADATA";
    
    [DynamoDbAttribute("entity_type")]
    public string EntityType { get; set; } = "USER";
    
    [DynamoDbAttribute("name")]
    public string? Name { get; set; }
    
    [DynamoDbAttribute("email")]
    public string? Email { get; set; }
    
    [DynamoDbAttribute("created_at")]
    public DateTime? CreatedAt { get; set; }
}
