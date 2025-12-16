using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    /// <summary>
    /// Filter request for QuoteItems
    /// </summary>
    public class QuoteItemsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Filter by quote ID
        /// </summary>
        public Guid? QuoteId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (QuoteId != null)
                filters.Add("quote_id", QuoteId);

            return filters;
        }
    }
}
