using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Manufacturing;

public class CreateBomDto
{
    public Guid? CompanyId { get; set; }

    [Required(ErrorMessage = "Finished good is required")]
    public Guid FinishedGoodId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Output quantity must be greater than 0")]
    public decimal OutputQuantity { get; set; } = 1;

    public Guid? OutputUnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    [Required(ErrorMessage = "At least one component is required")]
    [MinLength(1, ErrorMessage = "At least one component is required")]
    public List<BomItemDto> Items { get; set; } = new();
}

public class UpdateBomDto
{
    [Required(ErrorMessage = "Finished good is required")]
    public Guid FinishedGoodId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Output quantity must be greater than 0")]
    public decimal OutputQuantity { get; set; } = 1;

    public Guid? OutputUnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    [Required(ErrorMessage = "At least one component is required")]
    [MinLength(1, ErrorMessage = "At least one component is required")]
    public List<BomItemDto> Items { get; set; } = new();
}

public class BomItemDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Component is required")]
    public Guid ComponentId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    public Guid? UnitId { get; set; }

    [Range(0, 100, ErrorMessage = "Scrap percentage must be between 0 and 100")]
    public decimal ScrapPercentage { get; set; }

    public bool IsOptional { get; set; }
    public int Sequence { get; set; }
    public string? Notes { get; set; }
}

public class CopyBomDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(20)]
    public string Version { get; set; } = "1.0";
}
