using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class VendorPaymentsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Basic filters
        public Guid? CompanyId { get; set; }
        public Guid? VendorId { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentType { get; set; }

        // Date filters
        public DateOnly? PaymentDateFrom { get; set; }
        public DateOnly? PaymentDateTo { get; set; }

        // TDS filters
        public bool? TdsApplicable { get; set; }
        public string? TdsSection { get; set; }
        public bool? TdsDeposited { get; set; }

        // Financial year
        public string? FinancialYear { get; set; }

        // Reconciliation filters
        public bool? IsReconciled { get; set; }
        public bool? IsPosted { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (VendorId.HasValue)
                filters.Add("vendor_id", VendorId.Value);

            if (BankAccountId.HasValue)
                filters.Add("bank_account_id", BankAccountId.Value);

            if (!string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);

            if (!string.IsNullOrWhiteSpace(PaymentMethod))
                filters.Add("payment_method", PaymentMethod);

            if (!string.IsNullOrWhiteSpace(PaymentType))
                filters.Add("payment_type", PaymentType);

            if (TdsApplicable.HasValue)
                filters.Add("tds_applicable", TdsApplicable.Value);

            if (!string.IsNullOrWhiteSpace(TdsSection))
                filters.Add("tds_section", TdsSection);

            if (TdsDeposited.HasValue)
                filters.Add("tds_deposited", TdsDeposited.Value);

            if (!string.IsNullOrWhiteSpace(FinancialYear))
                filters.Add("financial_year", FinancialYear);

            if (IsReconciled.HasValue)
                filters.Add("is_reconciled", IsReconciled.Value);

            if (IsPosted.HasValue)
                filters.Add("is_posted", IsPosted.Value);

            // Date range filters
            if (PaymentDateFrom.HasValue)
                filters.Add("payment_date_from", PaymentDateFrom.Value);
            if (PaymentDateTo.HasValue)
                filters.Add("payment_date_to", PaymentDateTo.Value);

            return filters;
        }
    }
}
