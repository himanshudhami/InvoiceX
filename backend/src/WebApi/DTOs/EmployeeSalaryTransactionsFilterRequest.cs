using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class EmployeeSalaryTransactionsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        
        public Guid? EmployeeId { get; set; }
        public int? SalaryMonth { get; set; }
        public int? SalaryYear { get; set; }
        public string? Status { get; set; }
        public string? TransactionType { get; set; }
        public string? ContractType { get; set; }
        public string? Department { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDateFrom { get; set; }
        public DateTime? PaymentDateTo { get; set; }
        public decimal? MinNetSalary { get; set; }
        public decimal? MaxNetSalary { get; set; }
        
        public string? Company { get; set; }
        public Guid? CompanyId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            
            if (EmployeeId.HasValue)
                filters.Add("employee_id", EmployeeId.Value);
            
            if (SalaryMonth.HasValue)
                filters.Add("salary_month", SalaryMonth.Value);
            
            if (SalaryYear.HasValue)
                filters.Add("salary_year", SalaryYear.Value);
            
            if (Status != null && !string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);
            
            if (TransactionType != null && !string.IsNullOrWhiteSpace(TransactionType))
                filters.Add("transaction_type", TransactionType);
            
            if (ContractType != null && !string.IsNullOrWhiteSpace(ContractType))
                filters.Add("contract_type", ContractType);
            
            if (Department != null && !string.IsNullOrWhiteSpace(Department))
                filters.Add("department", Department);
            
            if (PaymentMethod != null && !string.IsNullOrWhiteSpace(PaymentMethod))
                filters.Add("payment_method", PaymentMethod);
            
            if (PaymentDateFrom.HasValue)
                filters.Add("payment_date_from", PaymentDateFrom.Value);
            
            if (PaymentDateTo.HasValue)
                filters.Add("payment_date_to", PaymentDateTo.Value);
            
            if (MinNetSalary.HasValue)
                filters.Add("min_net_salary", MinNetSalary.Value);
            
            if (MaxNetSalary.HasValue)
                filters.Add("max_net_salary", MaxNetSalary.Value);
            
            if (Company != null && !string.IsNullOrWhiteSpace(Company))
                filters.Add("company", Company);
            
            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);
            
            return filters;
        }
    }
}