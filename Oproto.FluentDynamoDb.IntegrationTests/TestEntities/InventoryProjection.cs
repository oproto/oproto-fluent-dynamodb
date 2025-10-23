using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

/// <summary>
/// Projection for InventoryEntity queried through GSI to test GSI-specific discriminator.
/// </summary>
[DynamoDbProjection(typeof(InventoryEntity))]
public partial class InventoryProjection
{
    [DynamoDbAttribute("pk")]
    public string WarehouseId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("item_name")]
    public string? ItemName { get; set; }
    
    [DynamoDbAttribute("quantity")]
    public int? Quantity { get; set; }
    
    [DynamoDbAttribute("gsi1_pk")]
    public string? Status { get; set; }
}
