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
        /// Filter by party ID
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by valid until date from
        /// </summary>
        public DateOnly? ValidUntilFrom { get; set; }

        /// <summary>
        /// Filter by valid until date to
        /// </summary>
        public DateOnly? ValidUntilTo { get; set; }

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

            if (PartyId != null)
                filters.Add("partyId", PartyId);

            if (Status != null && !string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);

            if (ValidUntilFrom != null)
                filters.Add("valid_until_from", ValidUntilFrom);

            if (ValidUntilTo != null)
                filters.Add("valid_until_to", ValidUntilTo);

            if (TotalAmountFrom != null)
                filters.Add("total_amount_from", TotalAmountFrom);

            if (TotalAmountTo != null)
                filters.Add("total_amount_to", TotalAmountTo);

            return filters;
        }
    }
}
