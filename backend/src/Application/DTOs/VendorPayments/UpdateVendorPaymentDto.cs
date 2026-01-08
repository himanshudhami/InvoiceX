using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.VendorPayments
{
    /// <summary>
    /// Data transfer object for updating Vendor Payments
    /// </summary>
    public class UpdateVendorPaymentDto
    {
        public Guid? CompanyId { get; set; }
        public Guid? PartyId { get; set; }
        public Guid? BankAccountId { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        public DateOnly PaymentDate { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        public decimal Amount { get; set; }

        public decimal? GrossAmount { get; set; }

        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
        public string? Currency { get; set; }

        [StringLength(30, ErrorMessage = "Payment method cannot exceed 30 characters")]
        public string? PaymentMethod { get; set; }

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters")]
        public string? ReferenceNumber { get; set; }

        [StringLength(30, ErrorMessage = "Cheque number cannot exceed 30 characters")]
        public string? ChequeNumber { get; set; }

        public DateOnly? ChequeDate { get; set; }

        public string? Notes { get; set; }
        public string? Description { get; set; }

        [StringLength(30, ErrorMessage = "Payment type cannot exceed 30 characters")]
        public string? PaymentType { get; set; }

        [StringLength(30, ErrorMessage = "Status cannot exceed 30 characters")]
        public string? Status { get; set; }

        // TDS
        public bool TdsApplicable { get; set; }

        [StringLength(10, ErrorMessage = "TDS section cannot exceed 10 characters")]
        public string? TdsSection { get; set; }

        public decimal? TdsRate { get; set; }
        public decimal? TdsAmount { get; set; }
    }
}
