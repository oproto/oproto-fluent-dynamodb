using Oproto.FluentDynamoDb.Attributes;

namespace BasicUsage.Models;

/// <summary>
/// Example transaction entity demonstrating source generator usage
/// </summary>
[DynamoDbTable("transactions")]
public partial class Transaction
{
    [PartitionKey]
    [DynamoDbAttribute("pk")]
    public string TenantId { get; set; } = string.Empty;

    [SortKey]
    [DynamoDbAttribute("sk")]
    public string TransactionId { get; set; } = string.Empty;

    [DynamoDbAttribute("amount")]
    public decimal Amount { get; set; }

    [DynamoDbAttribute("description")]
    public string Description { get; set; } = string.Empty;

    [GlobalSecondaryIndex("StatusIndex", IsPartitionKey = true)]
    [DynamoDbAttribute("status")]
    public string Status { get; set; } = string.Empty;

    [GlobalSecondaryIndex("StatusIndex", IsSortKey = true)]
    [DynamoDbAttribute("created_date")]
    public DateTime CreatedDate { get; set; }

    [DynamoDbAttribute("updated_date")]
    public DateTime? UpdatedDate { get; set; }

    [DynamoDbAttribute("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}