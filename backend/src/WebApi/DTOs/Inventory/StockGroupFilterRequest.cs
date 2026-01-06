namespace WebApi.DTOs.Inventory
{
    public class StockGroupFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        public Guid? CompanyId { get; set; }
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public Guid? ParentStockGroupId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            if (!string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            if (IsActive.HasValue)
                filters.Add("is_active", IsActive.Value);
            if (ParentStockGroupId.HasValue)
                filters.Add("parent_stock_group_id", ParentStockGroupId.Value);

            return filters;
        }
    }
}
