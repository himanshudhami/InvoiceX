using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class InvoiceItemsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        /// <summary>
        /// Filter by Invoice ID
        /// </summary>
        public Guid? InvoiceId { get; set; }
        
        public string? Description { get; set; }
        
        
        
        public decimal? Quantity { get; set; }
        
        
        
        public decimal? UnitPrice { get; set; }
        
        
        
        
        
        
        
        public decimal? LineTotal { get; set; }
        
        
        
        
        
        
        
        
        
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (InvoiceId.HasValue)
                filters.Add("invoice_id", InvoiceId.Value);
            
            if (Description != null && !string.IsNullOrWhiteSpace(Description))
                filters.Add("description", Description);
            
            
            
            if (Quantity != null)
                filters.Add("quantity", Quantity);
            
            
            
            if (UnitPrice != null)
                filters.Add("unit_price", UnitPrice);
            
            
            
            
            
            
            
            if (LineTotal != null)
                filters.Add("line_total", LineTotal);
            
            
            
            
            
            
            
            
            
            return filters;
        }
    }
}