using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory;

public class CreateSerialNumberDto
{
    public Guid? CompanyId { get; set; }

    [Required(ErrorMessage = "Stock item is required")]
    public Guid StockItemId { get; set; }

    [Required(ErrorMessage = "Serial number is required")]
    [MaxLength(100)]
    public string SerialNo { get; set; } = string.Empty;

    public Guid? WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public string Status { get; set; } = "available";
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSerialNumberDto
{
    public Guid? WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public string Status { get; set; } = "available";
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public string? Notes { get; set; }
}

public class BulkCreateSerialNumberDto
{
    public Guid? CompanyId { get; set; }

    [Required(ErrorMessage = "Stock item is required")]
    public Guid StockItemId { get; set; }

    [Required(ErrorMessage = "Prefix is required")]
    [MaxLength(50)]
    public string Prefix { get; set; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "Count must be between 1 and 10000")]
    public int Count { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Start number must be at least 1")]
    public int StartNumber { get; set; } = 1;

    public Guid? WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public Guid? ProductionOrderId { get; set; }
}

public class MarkSerialAsSoldDto
{
    [Required(ErrorMessage = "Invoice is required")]
    public Guid InvoiceId { get; set; }
}

public class SerialNumberFilterParams
{
    public Guid? CompanyId { get; set; }
    public Guid? StockItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
