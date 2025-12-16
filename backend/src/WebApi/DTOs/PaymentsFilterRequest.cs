using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class PaymentsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Linking filters
        public Guid? InvoiceId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? CustomerId { get; set; }

        // Payment details filters
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? Currency { get; set; }

        // Classification filters
        public string? PaymentType { get; set; }
        public string? IncomeCategory { get; set; }

        // TDS filters
        public bool? TdsApplicable { get; set; }
        public string? TdsSection { get; set; }

        // Financial year filter
        public string? FinancialYear { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            // Linking filters
            if (InvoiceId.HasValue)
                filters.Add("invoice_id", InvoiceId.Value);

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (CustomerId.HasValue)
                filters.Add("customer_id", CustomerId.Value);

            // Payment details filters
            if (Amount != null)
                filters.Add("amount", Amount);

            if (!string.IsNullOrWhiteSpace(PaymentMethod))
                filters.Add("payment_method", PaymentMethod);

            if (!string.IsNullOrWhiteSpace(ReferenceNumber))
                filters.Add("reference_number", ReferenceNumber);

            if (!string.IsNullOrWhiteSpace(Notes))
                filters.Add("notes", Notes);

            if (!string.IsNullOrWhiteSpace(Currency))
                filters.Add("currency", Currency);

            // Classification filters
            if (!string.IsNullOrWhiteSpace(PaymentType))
                filters.Add("payment_type", PaymentType);

            if (!string.IsNullOrWhiteSpace(IncomeCategory))
                filters.Add("income_category", IncomeCategory);

            // TDS filters
            if (TdsApplicable.HasValue)
                filters.Add("tds_applicable", TdsApplicable.Value);

            if (!string.IsNullOrWhiteSpace(TdsSection))
                filters.Add("tds_section", TdsSection);

            // Financial year filter
            if (!string.IsNullOrWhiteSpace(FinancialYear))
                filters.Add("financial_year", FinancialYear);

            return filters;
        }
    }
}