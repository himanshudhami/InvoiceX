using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class EmployeesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Status { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ContractType { get; set; }
        public string? Company { get; set; }
        public Guid? CompanyId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (EmployeeName != null && !string.IsNullOrWhiteSpace(EmployeeName))
                filters.Add("employee_name", EmployeeName);
            
            if (Email != null && !string.IsNullOrWhiteSpace(Email))
                filters.Add("email", Email);
            
            if (EmployeeId != null && !string.IsNullOrWhiteSpace(EmployeeId))
                filters.Add("employee_id", EmployeeId);
            
            if (Department != null && !string.IsNullOrWhiteSpace(Department))
                filters.Add("department", Department);
            
            if (Designation != null && !string.IsNullOrWhiteSpace(Designation))
                filters.Add("designation", Designation);
            
            if (Status != null && !string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);
            
            if (City != null && !string.IsNullOrWhiteSpace(City))
                filters.Add("city", City);
            
            if (State != null && !string.IsNullOrWhiteSpace(State))
                filters.Add("state", State);
            
            if (Country != null && !string.IsNullOrWhiteSpace(Country))
                filters.Add("country", Country);

            if (ContractType != null && !string.IsNullOrWhiteSpace(ContractType))
                filters.Add("contract_type", ContractType);

            if (Company != null && !string.IsNullOrWhiteSpace(Company))
                filters.Add("company", Company);
            
            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            
            return filters;
        }
    }
}