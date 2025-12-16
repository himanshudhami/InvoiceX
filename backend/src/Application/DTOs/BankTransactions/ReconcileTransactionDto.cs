using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Data transfer object for reconciling a bank transaction
    /// </summary>
    public class ReconcileTransactionDto
    {
        /// <summary>
        /// Type of record being reconciled with: payment, expense, payroll, tax_payment, transfer, contractor
        /// </summary>
        [Required(ErrorMessage = "Reconciled type is required")]
        [StringLength(50, ErrorMessage = "Reconciled type cannot exceed 50 characters")]
        public string ReconciledType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the linked record
        /// </summary>
        [Required(ErrorMessage = "Reconciled ID is required")]
        public Guid ReconciledId { get; set; }

        /// <summary>
        /// User performing the reconciliation
        /// </summary>
        [StringLength(255, ErrorMessage = "Reconciled by cannot exceed 255 characters")]
        public string? ReconciledBy { get; set; }
    }

    /// <summary>
    /// Reconciliation suggestion result
    /// </summary>
    public class ReconciliationSuggestionDto
    {
        /// <summary>
        /// Payment ID
        /// </summary>
        public Guid PaymentId { get; set; }

        /// <summary>
        /// Payment date
        /// </summary>
        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Customer name (if available)
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Invoice number (if available)
        /// </summary>
        public string? InvoiceNumber { get; set; }

        /// <summary>
        /// Payment reference number
        /// </summary>
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Match score (0-100)
        /// </summary>
        public int MatchScore { get; set; }

        /// <summary>
        /// Amount difference from transaction
        /// </summary>
        public decimal AmountDifference { get; set; }

        /// <summary>
        /// Date difference in days
        /// </summary>
        public int DateDifferenceInDays { get; set; }
    }
}
