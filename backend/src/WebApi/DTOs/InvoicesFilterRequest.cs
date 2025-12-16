using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class InvoicesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        
        
        
        
        
        public string? InvoiceNumber { get; set; }
        
        
        
        
        
        
        public string? Status { get; set; }
        
        
        public decimal? Subtotal { get; set; }
        
        
        
        
        
        public decimal? TotalAmount { get; set; }
        
        
        
        
        public string? Currency { get; set; }
        
        
        public string? Notes { get; set; }
        
        
        public string? Terms { get; set; }
        
        
        public string? PoNumber { get; set; }
        
        
        public string? ProjectName { get; set; }
        
        public Guid? CompanyId { get; set; }
        
        
        
        
        
        
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            
            if (InvoiceNumber != null && !string.IsNullOrWhiteSpace(InvoiceNumber))
                filters.Add("invoice_number", InvoiceNumber);
            
            
            
            
            if (Status != null && !string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);
            
            
            if (Subtotal != null)
                filters.Add("subtotal", Subtotal);
            
            
            
            
            if (TotalAmount != null)
                filters.Add("total_amount", TotalAmount);
            
            
            
            if (Currency != null && !string.IsNullOrWhiteSpace(Currency))
                filters.Add("currency", Currency);
            
            
            if (Notes != null && !string.IsNullOrWhiteSpace(Notes))
                filters.Add("notes", Notes);
            
            
            if (Terms != null && !string.IsNullOrWhiteSpace(Terms))
                filters.Add("terms", Terms);
            
            
            if (PoNumber != null && !string.IsNullOrWhiteSpace(PoNumber))
                filters.Add("po_number", PoNumber);
            
            
            if (ProjectName != null && !string.IsNullOrWhiteSpace(ProjectName))
                filters.Add("project_name", ProjectName);
            
            
            
            
            
            
            
            
            
            
            return filters;
        }
    }
}
