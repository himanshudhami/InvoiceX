namespace Core.Entities.Inventory;

public class SerialNumber
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid StockItemId { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public string Status { get; set; } = "available"; // available, sold, reserved, damaged
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public DateTime? SoldAt { get; set; }
    public Guid? SoldInvoiceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? StockItemName { get; set; }
    public string? StockItemSku { get; set; }
    public string? WarehouseName { get; set; }
    public string? BatchNumber { get; set; }
    public string? ProductionOrderNumber { get; set; }
}
