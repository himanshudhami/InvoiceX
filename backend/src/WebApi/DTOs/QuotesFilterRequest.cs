using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    /// <summary>
    /// Filter request for Quotes
    /// </summary>
    public class QuotesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Filter by company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Filter by customer ID
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by expiry date from
        /// </summary>
        public DateOnly? ExpiryDateFrom { get; set; }

        /// <summary>
        /// Filter by expiry date to
        /// </summary>
        public DateOnly? ExpiryDateTo { get; set; }

        /// <summary>
        /// Filter by total amount from
        /// </summary>
        public decimal? TotalAmountFrom { get; set; }

        /// <summary>
        /// Filter by total amount to
        /// </summary>
        public decimal? TotalAmountTo { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId != null)
                filters.Add("companyId", CompanyId);

            if (CustomerId != null)
                filters.Add("customerId", CustomerId);

            if (Status != null && !string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);

            if (ExpiryDateFrom != null)
                filters.Add("expiry_date_from", ExpiryDateFrom);

            if (ExpiryDateTo != null)
                filters.Add("expiry_date_to", ExpiryDateTo);

            if (TotalAmountFrom != null)
                filters.Add("total_amount_from", TotalAmountFrom);

            if (TotalAmountTo != null)
                filters.Add("total_amount_to", TotalAmountTo);

            return filters;
        }
    }
}
