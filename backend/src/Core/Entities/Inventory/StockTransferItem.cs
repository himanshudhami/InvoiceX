namespace Core.Entities.Inventory;

/// <summary>
/// Represents a line item in a stock transfer
/// </summary>
public class StockTransferItem
{
    public Guid Id { get; set; }
    public Guid StockTransferId { get; set; }
    public Guid StockItemId { get; set; }
    public Guid? BatchId { get; set; }

    public decimal Quantity { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Value { get; set; }

    /// <summary>
    /// Received quantity for partial receipts
    /// </summary>
    public decimal? ReceivedQuantity { get; set; }

    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? StockItemName { get; set; }
    public string? StockItemSku { get; set; }
    public string? BatchNumber { get; set; }
    public string? UnitSymbol { get; set; }
}
