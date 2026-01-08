namespace Application.DTOs.Vendors
{
    /// <summary>
    /// Summarizes total payments made to vendors
    /// </summary>
    public class VendorPaymentSummaryDto
    {
        /// <summary>
        /// Total amount paid across all vendors
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Count of vendors who have received payments
        /// </summary>
        public int VendorCount { get; set; }

        /// <summary>
        /// Count of total payments made
        /// </summary>
        public int PaymentCount { get; set; }

        /// <summary>
        /// List of per-vendor payment summaries
        /// </summary>
        public List<VendorPaymentDetailDto> Vendors { get; set; } = new();
    }

    /// <summary>
    /// Payment summary for a single vendor
    /// </summary>
    public class VendorPaymentDetailDto
    {
        /// <summary>
        /// Vendor/Party ID
        /// </summary>
        public Guid VendorId { get; set; }

        /// <summary>
        /// Vendor name
        /// </summary>
        public string VendorName { get; set; } = string.Empty;

        /// <summary>
        /// Total amount paid to this vendor
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Number of payments made to this vendor
        /// </summary>
        public int PaymentCount { get; set; }

        /// <summary>
        /// Date of last payment
        /// </summary>
        public DateTime? LastPaymentDate { get; set; }
    }
}
