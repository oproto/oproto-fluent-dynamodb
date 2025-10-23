using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

/// <summary>
/// Projection for UserEntity to test discriminator validation during hydration.
/// </summary>
[DynamoDbProjection(typeof(UserEntity))]
public partial class UserProjection
{
    [DynamoDbAttribute("pk")]
    public string Id { get; set; } = string.Empty;
    
    [DynamoDbAttribute("name")]
    public string? Name { get; set; }
    
    [DynamoDbAttribute("email")]
    public string? Email { get; set; }
}
