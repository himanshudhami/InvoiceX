namespace Core.Entities.Manufacturing;

public class ProductionOrderItem
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? ComponentName { get; set; }
    public string? ComponentSku { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public string? BatchNumber { get; set; }
    public string? WarehouseName { get; set; }
}
