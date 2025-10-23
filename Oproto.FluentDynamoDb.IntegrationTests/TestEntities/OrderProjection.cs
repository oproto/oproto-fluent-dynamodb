using Oproto.FluentDynamoDb.Attributes;

namespace Oproto.FluentDynamoDb.IntegrationTests.TestEntities;

/// <summary>
/// Projection for OrderEntity to test sort key pattern discriminator validation.
/// </summary>
[DynamoDbProjection(typeof(OrderEntity))]
public partial class OrderProjection
{
    [DynamoDbAttribute("pk")]
    public string TenantId { get; set; } = string.Empty;
    
    [DynamoDbAttribute("order_number")]
    public string? OrderNumber { get; set; }
    
    [DynamoDbAttribute("total")]
    public decimal? Total { get; set; }
    
    [DynamoDbAttribute("status")]
    public string? Status { get; set; }
}
