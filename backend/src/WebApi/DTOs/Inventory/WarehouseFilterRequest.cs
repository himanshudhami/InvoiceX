namespace WebApi.DTOs.Inventory
{
    public class WarehouseFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        public Guid? CompanyId { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsActive { get; set; }
        public Guid? ParentWarehouseId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            if (!string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            if (!string.IsNullOrWhiteSpace(Code))
                filters.Add("code", Code);
            if (!string.IsNullOrWhiteSpace(City))
                filters.Add("city", City);
            if (!string.IsNullOrWhiteSpace(State))
                filters.Add("state", State);
            if (IsDefault.HasValue)
                filters.Add("is_default", IsDefault.Value);
            if (IsActive.HasValue)
                filters.Add("is_active", IsActive.Value);
            if (ParentWarehouseId.HasValue)
                filters.Add("parent_warehouse_id", ParentWarehouseId.Value);

            return filters;
        }
    }
}
