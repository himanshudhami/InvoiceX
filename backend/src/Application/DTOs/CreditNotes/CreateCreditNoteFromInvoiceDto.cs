using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CreditNotes
{
    /// <summary>
    /// DTO for creating a credit note directly from an invoice.
    /// This is the recommended way as it ensures proper linking and field population.
    /// </summary>
    public class CreateCreditNoteFromInvoiceDto
    {
        [Required(ErrorMessage = "InvoiceId is required")]
        public Guid InvoiceId { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(50, ErrorMessage = "Reason cannot exceed 50 characters")]
        public string Reason { get; set; } = string.Empty;

        public string? ReasonDescription { get; set; }

        /// <summary>
        /// If true, creates a credit note for the full invoice amount.
        /// If false, items must be specified.
        /// </summary>
        public bool IsFullCreditNote { get; set; } = true;

        /// <summary>
        /// Items to include in partial credit note.
        /// Required if IsFullCreditNote is false.
        /// </summary>
        public List<PartialCreditNoteItem>? Items { get; set; }

        public string? Notes { get; set; }
    }

    public class PartialCreditNoteItem
    {
        /// <summary>
        /// Original invoice item ID to create credit for
        /// </summary>
        [Required(ErrorMessage = "OriginalItemId is required")]
        public Guid OriginalItemId { get; set; }

        /// <summary>
        /// Quantity to credit (must be <= original quantity)
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Override unit price if different from original
        /// </summary>
        public decimal? UnitPrice { get; set; }
    }
}
