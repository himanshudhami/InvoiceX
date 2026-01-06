namespace WebApi.DTOs.Inventory
{
    public class StockItemFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        public Guid? CompanyId { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public Guid? StockGroupId { get; set; }
        public Guid? BaseUnitId { get; set; }
        public string? HsnSacCode { get; set; }
        public bool? IsBatchEnabled { get; set; }
        public string? ValuationMethod { get; set; }
        public bool? IsActive { get; set; }
        public bool? LowStock { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            if (!string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            if (!string.IsNullOrWhiteSpace(Sku))
                filters.Add("sku", Sku);
            if (StockGroupId.HasValue)
                filters.Add("stock_group_id", StockGroupId.Value);
            if (BaseUnitId.HasValue)
                filters.Add("base_unit_id", BaseUnitId.Value);
            if (!string.IsNullOrWhiteSpace(HsnSacCode))
                filters.Add("hsn_sac_code", HsnSacCode);
            if (IsBatchEnabled.HasValue)
                filters.Add("is_batch_enabled", IsBatchEnabled.Value);
            if (!string.IsNullOrWhiteSpace(ValuationMethod))
                filters.Add("valuation_method", ValuationMethod);
            if (IsActive.HasValue)
                filters.Add("is_active", IsActive.Value);

            return filters;
        }
    }
}
