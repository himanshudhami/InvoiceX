namespace Core.Entities.Inventory;

/// <summary>
/// Represents a batch/lot of a stock item with optional expiry tracking
/// </summary>
public class StockBatch
{
    public Guid Id { get; set; }
    public Guid StockItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    // Current quantity and value in this batch
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public decimal? CostRate { get; set; }

    // Tally migration field
    public string? TallyBatchGuid { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? StockItemName { get; set; }
    public string? WarehouseName { get; set; }
}
