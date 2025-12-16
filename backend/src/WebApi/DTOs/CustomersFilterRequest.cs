using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class CustomersFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        
        
        
        
        public string? Name { get; set; }
        
        
        
        public string? CompanyName { get; set; }
        
        
        
        public string? Email { get; set; }
        
        
        
        public string? Phone { get; set; }
        
        
        
        public string? AddressLine1 { get; set; }
        
        
        
        public string? AddressLine2 { get; set; }
        
        
        
        public string? City { get; set; }
        
        
        
        public string? State { get; set; }
        
        
        
        public string? ZipCode { get; set; }
        
        
        
        public string? Country { get; set; }
        
        
        
        public string? TaxNumber { get; set; }
        
        
        
        public string? Notes { get; set; }
        
        public Guid? CompanyId { get; set; }
        
        
        
        
        
        
        
        
        
        
        
        
        
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            
            if (Name != null && !string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);
            
            
            
            if (CompanyName != null && !string.IsNullOrWhiteSpace(CompanyName))
                filters.Add("company_name", CompanyName);
            
            
            
            if (Email != null && !string.IsNullOrWhiteSpace(Email))
                filters.Add("email", Email);
            
            
            
            if (Phone != null && !string.IsNullOrWhiteSpace(Phone))
                filters.Add("phone", Phone);
            
            
            
            if (AddressLine1 != null && !string.IsNullOrWhiteSpace(AddressLine1))
                filters.Add("address_line1", AddressLine1);
            
            
            
            if (AddressLine2 != null && !string.IsNullOrWhiteSpace(AddressLine2))
                filters.Add("address_line2", AddressLine2);
            
            
            
            if (City != null && !string.IsNullOrWhiteSpace(City))
                filters.Add("city", City);
            
            
            
            if (State != null && !string.IsNullOrWhiteSpace(State))
                filters.Add("state", State);
            
            
            
            if (ZipCode != null && !string.IsNullOrWhiteSpace(ZipCode))
                filters.Add("zip_code", ZipCode);
            
            
            
            if (Country != null && !string.IsNullOrWhiteSpace(Country))
                filters.Add("country", Country);
            
            
            
            if (TaxNumber != null && !string.IsNullOrWhiteSpace(TaxNumber))
                filters.Add("tax_number", TaxNumber);
            
            
            
            if (Notes != null && !string.IsNullOrWhiteSpace(Notes))
                filters.Add("notes", Notes);
            
            
            
            
            
            
            
            
            
            
            
            
            
            return filters;
        }
    }
}