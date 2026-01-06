namespace WebApi.DTOs.Inventory
{
    public class StockMovementFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;

        public Guid? CompanyId { get; set; }
        public Guid? StockItemId { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? BatchId { get; set; }
        public string? MovementType { get; set; }
        public string? SourceType { get; set; }
        public Guid? SourceId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            if (StockItemId.HasValue)
                filters.Add("stock_item_id", StockItemId.Value);
            if (WarehouseId.HasValue)
                filters.Add("warehouse_id", WarehouseId.Value);
            if (BatchId.HasValue)
                filters.Add("batch_id", BatchId.Value);
            if (!string.IsNullOrWhiteSpace(MovementType))
                filters.Add("movement_type", MovementType);
            if (!string.IsNullOrWhiteSpace(SourceType))
                filters.Add("source_type", SourceType);
            if (SourceId.HasValue)
                filters.Add("source_id", SourceId.Value);

            return filters;
        }
    }
}
