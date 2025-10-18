using Oproto.FluentDynamoDb.Attributes;

namespace TestGenerator;

[DynamoDbTable("test-table")]
public partial class TestEntity
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string Id { get; set; } = string.Empty;

    [DynamoDbAttribute("data")]
    public string Data { get; set; } = string.Empty;
}