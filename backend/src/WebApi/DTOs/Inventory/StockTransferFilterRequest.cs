namespace WebApi.DTOs.Inventory
{
    public class StockTransferFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;

        public Guid? CompanyId { get; set; }
        public string? TransferNumber { get; set; }
        public Guid? FromWarehouseId { get; set; }
        public Guid? ToWarehouseId { get; set; }
        public string? Status { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            if (!string.IsNullOrWhiteSpace(TransferNumber))
                filters.Add("transfer_number", TransferNumber);
            if (FromWarehouseId.HasValue)
                filters.Add("from_warehouse_id", FromWarehouseId.Value);
            if (ToWarehouseId.HasValue)
                filters.Add("to_warehouse_id", ToWarehouseId.Value);
            if (!string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);

            return filters;
        }
    }
}
