using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CreditNotes
{
    public class UpdateCreditNotesDto : CreateCreditNotesDto
    {
        [Required(ErrorMessage = "Id is required")]
        public Guid Id { get; set; }

        // Additional fields that can be updated
        public DateTime? IssuedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // E-invoicing fields
        public string? Irn { get; set; }
        public DateTime? IrnGeneratedAt { get; set; }
        public DateTime? IrnCancelledAt { get; set; }
        public string? QrCodeData { get; set; }
        public string? EInvoiceSignedJson { get; set; }
        public string? EInvoiceStatus { get; set; }

        // ITC Reversal Tracking
        public bool ItcReversalConfirmed { get; set; }
        public DateOnly? ItcReversalDate { get; set; }
        public string? ItcReversalCertificate { get; set; }

        // GSTR-1 Reporting
        public bool ReportedInGstr1 { get; set; }
        public string? Gstr1Period { get; set; }
        public DateOnly? Gstr1FilingDate { get; set; }
    }
}
