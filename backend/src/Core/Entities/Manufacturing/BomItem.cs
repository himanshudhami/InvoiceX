namespace Core.Entities.Manufacturing;

public class BomItem
{
    public Guid Id { get; set; }
    public Guid BomId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public decimal ScrapPercentage { get; set; }
    public bool IsOptional { get; set; }
    public int Sequence { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? ComponentName { get; set; }
    public string? ComponentSku { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
}
