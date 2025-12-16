using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class ProductsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        
        
        
        
        public string? Name { get; set; }
        
        
        
        public string? Description { get; set; }
        
        
        
        public string? Sku { get; set; }
        
        
        
        public string? Category { get; set; }
        
        
        
        public string? Type { get; set; }
        
        
        
        public decimal? UnitPrice { get; set; }
        
        
        
        public string? Unit { get; set; }
        
        public Guid? CompanyId { get; set; }
        
        
        
        
        
        
        
        
        
        
        
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            
            if (Name != null && !string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            
            
            
            if (Description != null && !string.IsNullOrWhiteSpace(Description))
                filters.Add("description", Description);
            
            
            
            if (Sku != null && !string.IsNullOrWhiteSpace(Sku))
                filters.Add("sku", Sku);
            
            
            
            if (Category != null && !string.IsNullOrWhiteSpace(Category))
                filters.Add("category", Category);
            
            
            
            if (Type != null && !string.IsNullOrWhiteSpace(Type))
                filters.Add("type", Type);
            
            
            
            if (UnitPrice != null)
                filters.Add("unit_price", UnitPrice);
            
            
            
            if (Unit != null && !string.IsNullOrWhiteSpace(Unit))
                filters.Add("unit", Unit);
            
            
            
            
            
            
            
            
            
            
            
            return filters;
        }
    }
}