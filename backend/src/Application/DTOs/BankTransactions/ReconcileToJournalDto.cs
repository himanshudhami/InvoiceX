using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Data transfer object for reconciling a bank transaction directly to a journal entry.
    /// Used for manual JE reconciliation (opening entries, adjustments) without source documents.
    /// </summary>
    public class ReconcileToJournalDto
    {
        /// <summary>
        /// Journal Entry ID to reconcile with
        /// </summary>
        [Required(ErrorMessage = "Journal Entry ID is required")]
        public Guid JournalEntryId { get; set; }

        /// <summary>
        /// Specific Journal Entry Line ID that affects the bank account
        /// </summary>
        [Required(ErrorMessage = "Journal Entry Line ID is required")]
        public Guid JournalEntryLineId { get; set; }

        /// <summary>
        /// User performing the reconciliation
        /// </summary>
        [StringLength(255, ErrorMessage = "Reconciled by cannot exceed 255 characters")]
        public string? ReconciledBy { get; set; }

        /// <summary>
        /// Optional notes about the reconciliation
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Amount of difference between bank transaction and JE line (can be positive or negative)
        /// </summary>
        public decimal? DifferenceAmount { get; set; }

        /// <summary>
        /// Classification of the difference for accounting treatment:
        /// - bank_interest: Interest income credited by bank
        /// - bank_charges: Fees/charges deducted by bank
        /// - tds_deducted: TDS deducted by customer (receivable)
        /// - round_off: Minor rounding difference
        /// - forex_gain: Foreign exchange gain
        /// - forex_loss: Foreign exchange loss
        /// - other_income: Other miscellaneous income
        /// - other_expense: Other miscellaneous expense
        /// </summary>
        [StringLength(50)]
        public string? DifferenceType { get; set; }

        /// <summary>
        /// Notes explaining the difference
        /// </summary>
        [StringLength(500)]
        public string? DifferenceNotes { get; set; }

        /// <summary>
        /// TDS section if difference is TDS (e.g., 194C, 194J)
        /// </summary>
        [StringLength(20)]
        public string? TdsSection { get; set; }
    }
}
