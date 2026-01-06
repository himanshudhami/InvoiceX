namespace Core.Entities.Inventory;

/// <summary>
/// Represents a stock ledger entry (movement in/out of stock)
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid StockItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BatchId { get; set; }

    public DateOnly MovementDate { get; set; }

    /// <summary>
    /// Movement type: purchase, sale, transfer_in, transfer_out, adjustment, opening, return_in, return_out
    /// </summary>
    public string MovementType { get; set; } = string.Empty;

    /// <summary>
    /// Quantity: positive for inward, negative for outward
    /// </summary>
    public decimal Quantity { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Value { get; set; }

    /// <summary>
    /// Source type: sales_invoice, purchase_invoice, stock_journal, stock_transfer, credit_note, debit_note
    /// </summary>
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceNumber { get; set; }

    // Link to journal entry for value posting
    public Guid? JournalEntryId { get; set; }

    // Tally migration field
    public string? TallyVoucherGuid { get; set; }

    // Running totals for stock ledger report
    public decimal? RunningQuantity { get; set; }
    public decimal? RunningValue { get; set; }

    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? StockItemName { get; set; }
    public string? StockItemSku { get; set; }
    public string? WarehouseName { get; set; }
    public string? BatchNumber { get; set; }
    public string? UnitSymbol { get; set; }
    public string? CompanyName { get; set; }
}
