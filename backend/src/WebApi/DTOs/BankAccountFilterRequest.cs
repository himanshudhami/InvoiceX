namespace WebApi.DTOs
{
    public class BankAccountFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Company filter
        public Guid? CompanyId { get; set; }

        // Account details filters
        public string? AccountType { get; set; }
        public string? Currency { get; set; }
        public string? BankName { get; set; }

        // Status filters
        public bool? IsActive { get; set; }
        public bool? IsPrimary { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (!string.IsNullOrWhiteSpace(AccountType))
                filters.Add("account_type", AccountType);

            if (!string.IsNullOrWhiteSpace(Currency))
                filters.Add("currency", Currency);

            if (!string.IsNullOrWhiteSpace(BankName))
                filters.Add("bank_name", BankName);

            if (IsActive.HasValue)
                filters.Add("is_active", IsActive.Value);

            if (IsPrimary.HasValue)
                filters.Add("is_primary", IsPrimary.Value);

            return filters;
        }
    }
}
