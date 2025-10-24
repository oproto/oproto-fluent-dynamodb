using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Test entity for AOT compatibility tests.
/// </summary>
[DynamoDbEntity]
public class TestEntity
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string PartitionKey { get; set; } = string.Empty;

    [SortKey]
    [DynamoDbAttribute("sk")]
    public string SortKey { get; set; } = string.Empty;

    [DynamoDbAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [DynamoDbAttribute("age")]
    public int Age { get; set; }

    [DynamoDbAttribute("email")]
    public string? Email { get; set; }

    [DynamoDbAttribute("status")]
    public EntityStatus Status { get; set; }

    [DynamoDbAttribute("createdAt")]
    public DateTime CreatedAt { get; set; }

    [DynamoDbAttribute("tags")]
    public List<string>? Tags { get; set; }
}

public enum EntityStatus
{
    Active,
    Inactive,
    Pending
}
