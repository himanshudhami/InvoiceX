namespace Core.Entities.Inventory;

/// <summary>
/// Represents a unit conversion for a stock item (e.g., 1 Box = 12 Pieces)
/// </summary>
public class UnitConversion
{
    public Guid Id { get; set; }
    public Guid StockItemId { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }

    /// <summary>
    /// Conversion factor: 1 FromUnit = X ToUnit
    /// Example: 1 Box = 12 Pieces, ConversionFactor = 12
    /// </summary>
    public decimal ConversionFactor { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Navigation properties (service layer only, not DB mapped)
    public string? FromUnitName { get; set; }
    public string? FromUnitSymbol { get; set; }
    public string? ToUnitName { get; set; }
    public string? ToUnitSymbol { get; set; }
    public string? StockItemName { get; set; }
}
