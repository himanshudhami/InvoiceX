using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.VendorPayments
{
    /// <summary>
    /// Data transfer object for creating Vendor Payments
    /// </summary>
    public class CreateVendorPaymentDto
    {
        public Guid? CompanyId { get; set; }

        [Required(ErrorMessage = "Vendor ID is required")]
        public Guid VendorId { get; set; }

        public Guid? BankAccountId { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// Net amount paid (after TDS deduction)
        /// </summary>
        [Required(ErrorMessage = "Amount is required")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Gross amount before TDS
        /// </summary>
        public decimal? GrossAmount { get; set; }

        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
        public string? Currency { get; set; } = "INR";

        [StringLength(30, ErrorMessage = "Payment method cannot exceed 30 characters")]
        public string? PaymentMethod { get; set; }

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters")]
        public string? ReferenceNumber { get; set; }

        [StringLength(30, ErrorMessage = "Cheque number cannot exceed 30 characters")]
        public string? ChequeNumber { get; set; }

        public DateOnly? ChequeDate { get; set; }

        public string? Notes { get; set; }
        public string? Description { get; set; }

        // Payment classification
        [StringLength(30, ErrorMessage = "Payment type cannot exceed 30 characters")]
        public string? PaymentType { get; set; } = "bill_payment";

        [StringLength(30, ErrorMessage = "Status cannot exceed 30 characters")]
        public string? Status { get; set; } = "draft";

        // TDS
        public bool TdsApplicable { get; set; }

        [StringLength(10, ErrorMessage = "TDS section cannot exceed 10 characters")]
        public string? TdsSection { get; set; }

        public decimal? TdsRate { get; set; }
        public decimal? TdsAmount { get; set; }

        // Allocations (optional - can be added separately)
        public List<CreateVendorPaymentAllocationDto>? Allocations { get; set; }
    }

    /// <summary>
    /// Data transfer object for creating Vendor Payment Allocations
    /// </summary>
    public class CreateVendorPaymentAllocationDto
    {
        public Guid? VendorInvoiceId { get; set; }

        [Required(ErrorMessage = "Allocated amount is required")]
        public decimal AllocatedAmount { get; set; }

        public decimal? TdsAllocated { get; set; }

        [StringLength(30, ErrorMessage = "Allocation type cannot exceed 30 characters")]
        public string? AllocationType { get; set; } = "bill_settlement";

        public string? Notes { get; set; }
    }
}
