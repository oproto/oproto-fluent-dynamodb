using Oproto.FluentDynamoDb.Attributes;

namespace TestMultiTargeting;

[DynamoDbTable("test-table")]
public partial class TestEntity
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string Id { get; set; } = string.Empty;
    
    [DynamoDbAttribute("name")]
    public string Name { get; set; } = string.Empty;
    
    [DynamoDbAttribute("age")]
    public int Age { get; set; }
}