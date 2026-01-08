namespace Core.Entities.Manufacturing;

public class BillOfMaterials
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedGoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Version { get; set; } = "1.0";
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal OutputQuantity { get; set; } = 1;
    public Guid? OutputUnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? FinishedGoodName { get; set; }
    public string? FinishedGoodSku { get; set; }
    public string? OutputUnitName { get; set; }
    public string? OutputUnitSymbol { get; set; }
    public List<BomItem>? Items { get; set; }
}
