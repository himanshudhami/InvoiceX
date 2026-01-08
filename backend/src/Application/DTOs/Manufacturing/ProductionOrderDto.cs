using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Manufacturing;

public class CreateProductionOrderDto
{
    public Guid? CompanyId { get; set; }
    public string? OrderNumber { get; set; }

    [Required(ErrorMessage = "BOM is required")]
    public Guid BomId { get; set; }

    [Required(ErrorMessage = "Warehouse is required")]
    public Guid WarehouseId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Planned quantity must be greater than 0")]
    public decimal PlannedQuantity { get; set; }

    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public string? Notes { get; set; }

    // Optional: Override items from BOM (if not provided, items are copied from BOM)
    public List<ProductionOrderItemDto>? Items { get; set; }
}

public class UpdateProductionOrderDto
{
    [Required(ErrorMessage = "BOM is required")]
    public Guid BomId { get; set; }

    [Required(ErrorMessage = "Warehouse is required")]
    public Guid WarehouseId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Planned quantity must be greater than 0")]
    public decimal PlannedQuantity { get; set; }

    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public string? Notes { get; set; }
    public List<ProductionOrderItemDto>? Items { get; set; }
}

public class ProductionOrderItemDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Component is required")]
    public Guid ComponentId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Planned quantity must be greater than 0")]
    public decimal PlannedQuantity { get; set; }

    public decimal ConsumedQuantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
}

public class ReleaseProductionOrderDto
{
    public Guid? UserId { get; set; }
}

public class StartProductionOrderDto
{
    public Guid? UserId { get; set; }
}

public class CompleteProductionOrderDto
{
    public Guid? UserId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Actual quantity must be greater than 0")]
    public decimal ActualQuantity { get; set; }
}

public class CancelProductionOrderDto
{
    public Guid? UserId { get; set; }
    public string? Reason { get; set; }
}

public class ConsumeItemDto
{
    [Required(ErrorMessage = "Item is required")]
    public Guid ItemId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    public Guid? BatchId { get; set; }
}
