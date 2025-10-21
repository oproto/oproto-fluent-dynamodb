using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

/// <summary>
/// Test entity with various HashSet properties for integration testing.
/// </summary>
[DynamoDbTable("test-hashset-entity")]
public partial class HashSetTestEntity
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string Id { get; set; } = string.Empty;
    
    [DynamoDbAttribute("category_ids")]
    public HashSet<int>? CategoryIds { get; set; }
    
    [DynamoDbAttribute("tags")]
    public HashSet<string>? Tags { get; set; }
    
    [DynamoDbAttribute("binary_data")]
    public HashSet<byte[]>? BinaryData { get; set; }
}
