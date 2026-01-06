namespace Core.Entities.Inventory;

/// <summary>
/// Represents a unit of measure (e.g., Pieces, Kilograms, Boxes)
/// </summary>
public class UnitOfMeasure
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // NULL for system-wide units
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsSystemUnit { get; set; }

    // Tally migration fields
    public string? TallyUnitGuid { get; set; }
    public string? TallyUnitName { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
