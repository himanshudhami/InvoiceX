namespace Core.Entities.Inventory;

/// <summary>
/// Represents a warehouse/godown for stock storage
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public bool IsDefault { get; set; }
    public Guid? ParentWarehouseId { get; set; }

    // Tally migration fields
    public string? TallyGodownGuid { get; set; }
    public string? TallyGodownName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? ParentWarehouseName { get; set; }
    public string? CompanyName { get; set; }
}
