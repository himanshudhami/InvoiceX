namespace Core.Entities.Inventory;

/// <summary>
/// Represents an inter-warehouse stock transfer
/// </summary>
public class StockTransfer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public DateOnly TransferDate { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }

    /// <summary>
    /// Status: draft, in_transit, completed, cancelled
    /// </summary>
    public string Status { get; set; } = "draft";

    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }

    public string? Notes { get; set; }

    // Tally migration field
    public string? TallyVoucherGuid { get; set; }

    // Audit fields
    public Guid? CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? CompanyName { get; set; }
    public string? CreatedByName { get; set; }
    public string? ApprovedByName { get; set; }
    public string? CompletedByName { get; set; }

    // Line items (populated by service)
    public List<StockTransferItem>? Items { get; set; }
}
