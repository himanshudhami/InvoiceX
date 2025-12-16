using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class TaxRatesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        
        
        
        
        public string? Name { get; set; }
        
        
        
        public decimal? Rate { get; set; }
        
        
        
        
        
        
        
        
        
        
        
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            
            
            
            
            if (Name != null && !string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            
            
            
            if (Rate != null)
                filters.Add("rate", Rate);
            
            
            
            
            
            
            
            
            
            
            
            return filters;
        }
    }
}