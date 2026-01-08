namespace Core.Entities.Manufacturing;

public class ProductionOrder
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid BomId { get; set; }
    public Guid FinishedGoodId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string Status { get; set; } = "draft"; // draft, released, in_progress, completed, cancelled
    public string? Notes { get; set; }
    public Guid? ReleasedBy { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? StartedBy { get; set; }
    public DateTime? StartedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? BomName { get; set; }
    public string? FinishedGoodName { get; set; }
    public string? FinishedGoodSku { get; set; }
    public string? WarehouseName { get; set; }
    public string? ReleasedByName { get; set; }
    public string? StartedByName { get; set; }
    public string? CompletedByName { get; set; }
    public List<ProductionOrderItem>? Items { get; set; }
}
