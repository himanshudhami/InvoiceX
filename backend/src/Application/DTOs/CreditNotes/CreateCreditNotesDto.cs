using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CreditNotes
{
    public class CreateCreditNotesDto
    {
        public Guid? CompanyId { get; set; }
        public Guid? PartyId { get; set; }

        [Required(ErrorMessage = "CreditNoteNumber is required")]
        [StringLength(50, ErrorMessage = "CreditNoteNumber cannot exceed 50 characters")]
        public string CreditNoteNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "CreditNoteDate is required")]
        public DateOnly CreditNoteDate { get; set; }

        // Original invoice reference (MANDATORY as per GST)
        [Required(ErrorMessage = "OriginalInvoiceId is required")]
        public Guid OriginalInvoiceId { get; set; }

        // Reason for credit note (required for GST compliance)
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(50, ErrorMessage = "Reason cannot exceed 50 characters")]
        public string Reason { get; set; } = string.Empty;

        public string? ReasonDescription { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string? Status { get; set; } = "draft";

        [Required(ErrorMessage = "Subtotal is required")]
        public decimal Subtotal { get; set; }

        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }

        [Required(ErrorMessage = "TotalAmount is required")]
        public decimal TotalAmount { get; set; }

        [StringLength(3, ErrorMessage = "Currency cannot exceed 3 characters")]
        public string? Currency { get; set; } = "INR";

        public string? Notes { get; set; }
        public string? Terms { get; set; }

        // GST Classification
        [StringLength(30, ErrorMessage = "InvoiceType cannot exceed 30 characters")]
        public string? InvoiceType { get; set; }

        [StringLength(30, ErrorMessage = "SupplyType cannot exceed 30 characters")]
        public string? SupplyType { get; set; }

        [StringLength(10, ErrorMessage = "PlaceOfSupply cannot exceed 10 characters")]
        public string? PlaceOfSupply { get; set; }

        public bool ReverseCharge { get; set; }

        // GST Totals
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }

        // Forex
        [StringLength(3, ErrorMessage = "ForeignCurrency cannot exceed 3 characters")]
        public string? ForeignCurrency { get; set; }

        public decimal? ExchangeRate { get; set; }

        // E-invoicing
        public bool EInvoiceApplicable { get; set; }

        // ITC Reversal Tracking (2025 Amendment)
        public bool ItcReversalRequired { get; set; }
    }
}
