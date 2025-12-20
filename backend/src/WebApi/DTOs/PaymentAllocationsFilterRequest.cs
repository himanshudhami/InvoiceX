namespace WebApi.DTOs
{
    public class PaymentAllocationsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Linking filters
        public Guid? CompanyId { get; set; }
        public Guid? PaymentId { get; set; }
        public Guid? InvoiceId { get; set; }

        // Allocation type filter
        public string? AllocationType { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (PaymentId.HasValue)
                filters.Add("payment_id", PaymentId.Value);

            if (InvoiceId.HasValue)
                filters.Add("invoice_id", InvoiceId.Value);

            if (!string.IsNullOrWhiteSpace(AllocationType))
                filters.Add("allocation_type", AllocationType);

            return filters;
        }
    }
}
