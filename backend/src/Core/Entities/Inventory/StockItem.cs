namespace Core.Entities.Inventory;

/// <summary>
/// Represents an inventory item with quantity and value tracking
/// </summary>
public class StockItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public Guid? StockGroupId { get; set; }
    public Guid BaseUnitId { get; set; }

    // GST/Tax fields
    public string? HsnSacCode { get; set; }
    public decimal GstRate { get; set; } = 18;

    // Opening balances
    public decimal OpeningQuantity { get; set; }
    public decimal OpeningValue { get; set; }

    // Current running totals (updated by stock movements)
    public decimal CurrentQuantity { get; set; }
    public decimal CurrentValue { get; set; }

    // Inventory control
    public decimal? ReorderLevel { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }

    // Pricing
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? Mrp { get; set; }

    // Batch/Lot tracking
    public bool IsBatchEnabled { get; set; }

    // Serial number tracking for high-value items
    public bool IsSerialEnabled { get; set; }

    // Valuation method: fifo, lifo, weighted_avg
    public string ValuationMethod { get; set; } = "weighted_avg";

    // Tally migration fields
    public string? TallyStockItemGuid { get; set; }
    public string? TallyStockItemName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? StockGroupName { get; set; }
    public string? BaseUnitName { get; set; }
    public string? BaseUnitSymbol { get; set; }
    public string? CompanyName { get; set; }
}
