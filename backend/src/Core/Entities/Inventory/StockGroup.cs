namespace Core.Entities.Inventory;

/// <summary>
/// Represents a hierarchical stock group for categorizing stock items
/// </summary>
public class StockGroup
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentStockGroupId { get; set; }

    // Tally migration fields
    public string? TallyStockGroupGuid { get; set; }
    public string? TallyStockGroupName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? ParentStockGroupName { get; set; }
    public string? CompanyName { get; set; }
    public string? FullPath { get; set; } // e.g., "Electronics > Mobile Phones"
}
