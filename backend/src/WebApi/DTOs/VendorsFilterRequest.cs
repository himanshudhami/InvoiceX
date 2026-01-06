using System;
using System.Collections.Generic;

namespace WebApi.DTOs
{
    public class VendorsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        // Basic filters
        public Guid? CompanyId { get; set; }
        public string? Name { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public bool? IsActive { get; set; }

        // GST filters
        public string? Gstin { get; set; }
        public string? GstStateCode { get; set; }
        public string? VendorType { get; set; }
        public bool? IsGstRegistered { get; set; }
        public string? PanNumber { get; set; }

        // TDS filters
        public bool? TdsApplicable { get; set; }
        public string? DefaultTdsSection { get; set; }

        // MSME filters
        public bool? MsmeRegistered { get; set; }
        public string? MsmeCategory { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (CompanyId.HasValue)
                filters.Add("company_id", CompanyId.Value);

            if (!string.IsNullOrWhiteSpace(Name))
                filters.Add("name", Name);

            if (!string.IsNullOrWhiteSpace(CompanyName))
                filters.Add("company_name", CompanyName);

            if (!string.IsNullOrWhiteSpace(Email))
                filters.Add("email", Email);

            if (!string.IsNullOrWhiteSpace(Phone))
                filters.Add("phone", Phone);

            if (!string.IsNullOrWhiteSpace(City))
                filters.Add("city", City);

            if (!string.IsNullOrWhiteSpace(State))
                filters.Add("state", State);

            if (!string.IsNullOrWhiteSpace(Country))
                filters.Add("country", Country);

            if (IsActive.HasValue)
                filters.Add("is_active", IsActive.Value);

            // GST filters
            if (!string.IsNullOrWhiteSpace(Gstin))
                filters.Add("gstin", Gstin);

            if (!string.IsNullOrWhiteSpace(GstStateCode))
                filters.Add("gst_state_code", GstStateCode);

            if (!string.IsNullOrWhiteSpace(VendorType))
                filters.Add("vendor_type", VendorType);

            if (IsGstRegistered.HasValue)
                filters.Add("is_gst_registered", IsGstRegistered.Value);

            if (!string.IsNullOrWhiteSpace(PanNumber))
                filters.Add("pan_number", PanNumber);

            // TDS filters
            if (TdsApplicable.HasValue)
                filters.Add("tds_applicable", TdsApplicable.Value);

            if (!string.IsNullOrWhiteSpace(DefaultTdsSection))
                filters.Add("default_tds_section", DefaultTdsSection);

            // MSME filters
            if (MsmeRegistered.HasValue)
                filters.Add("msme_registered", MsmeRegistered.Value);

            if (!string.IsNullOrWhiteSpace(MsmeCategory))
                filters.Add("msme_category", MsmeCategory);

            return filters;
        }
    }
}
