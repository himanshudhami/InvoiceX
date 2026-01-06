using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class VendorInvoicesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Basic filters
        public Guid? CompanyId { get; set; }
        public Guid? VendorId { get; set; }
        public string? Status { get; set; }
        public string? InvoiceNumber { get; set; }

        // Date filters
        public DateOnly? InvoiceDateFrom { get; set; }
        public DateOnly? InvoiceDateTo { get; set; }
        public DateOnly? DueDateFrom { get; set; }
        public DateOnly? DueDateTo { get; set; }

        // GST filters
        public string? InvoiceType { get; set; }
        public string? SupplyType { get; set; }
        public bool? ReverseCharge { get; set; }
        public bool? RcmApplicable { get; set; }

        // ITC filters
        public bool? ItcEligible { get; set; }
        public bool? MatchedWithGstr2B { get; set; }

        // TDS filters
        public bool? TdsApplicable { get; set; }
        public string? TdsSection { get; set; }

        // Posting filters
        public bool? IsPosted { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (VendorId.HasValue)
                filters.Add("vendor_id", VendorId.Value);

            if (!string.IsNullOrWhiteSpace(Status))
                filters.Add("status", Status);

            if (!string.IsNullOrWhiteSpace(InvoiceNumber))
                filters.Add("invoice_number", InvoiceNumber);

            if (!string.IsNullOrWhiteSpace(InvoiceType))
                filters.Add("invoice_type", InvoiceType);

            if (!string.IsNullOrWhiteSpace(SupplyType))
                filters.Add("supply_type", SupplyType);

            if (ReverseCharge.HasValue)
                filters.Add("reverse_charge", ReverseCharge.Value);

            if (RcmApplicable.HasValue)
                filters.Add("rcm_applicable", RcmApplicable.Value);

            if (ItcEligible.HasValue)
                filters.Add("itc_eligible", ItcEligible.Value);

            if (MatchedWithGstr2B.HasValue)
                filters.Add("matched_with_gstr2b", MatchedWithGstr2B.Value);

            if (TdsApplicable.HasValue)
                filters.Add("tds_applicable", TdsApplicable.Value);

            if (!string.IsNullOrWhiteSpace(TdsSection))
                filters.Add("tds_section", TdsSection);

            if (IsPosted.HasValue)
                filters.Add("is_posted", IsPosted.Value);

            // Date range filters - handled specially in repository
            if (InvoiceDateFrom.HasValue)
                filters.Add("invoice_date_from", InvoiceDateFrom.Value);
            if (InvoiceDateTo.HasValue)
                filters.Add("invoice_date_to", InvoiceDateTo.Value);
            if (DueDateFrom.HasValue)
                filters.Add("due_date_from", DueDateFrom.Value);
            if (DueDateTo.HasValue)
                filters.Add("due_date_to", DueDateTo.Value);

            return filters;
        }
    }
}
