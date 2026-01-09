namespace Core.Entities
{
    /// <summary>
    /// Bank transaction record - tracks entries from bank statements
    /// Supports import from CSV/manual entry and reconciliation with payments
    /// </summary>
    public class BankTransaction
    {
        public Guid Id { get; set; }

        // ==================== Bank Account Linking ====================

        /// <summary>
        /// Bank account this transaction belongs to
        /// </summary>
        public Guid BankAccountId { get; set; }

        // ==================== Transaction Details ====================

        /// <summary>
        /// Date of the transaction
        /// </summary>
        public DateOnly TransactionDate { get; set; }

        /// <summary>
        /// Value date (settlement date)
        /// </summary>
        public DateOnly? ValueDate { get; set; }

        /// <summary>
        /// Transaction description/narration from bank
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bank reference number (UTR, NEFT ref, etc.)
        /// </summary>
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Cheque number if applicable
        /// </summary>
        public string? ChequeNumber { get; set; }

        // ==================== Amount ====================

        /// <summary>
        /// Transaction type: credit (money in) or debit (money out)
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Transaction amount (always positive)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Balance after this transaction
        /// </summary>
        public decimal? BalanceAfter { get; set; }

        // ==================== Categorization ====================

        /// <summary>
        /// Category: customer_payment, vendor_payment, salary, tax, bank_charges, transfer, other
        /// </summary>
        public string? Category { get; set; }

        // ==================== Reconciliation ====================

        /// <summary>
        /// Whether this transaction has been reconciled
        /// </summary>
        public bool IsReconciled { get; set; }

        /// <summary>
        /// Type of record this was reconciled with: payment, expense, payroll, tax_payment, transfer, contractor
        /// </summary>
        public string? ReconciledType { get; set; }

        /// <summary>
        /// ID of the linked record (payment ID, expense ID, etc.)
        /// </summary>
        public Guid? ReconciledId { get; set; }

        /// <summary>
        /// When the transaction was reconciled
        /// </summary>
        public DateTime? ReconciledAt { get; set; }

        /// <summary>
        /// User who performed the reconciliation
        /// </summary>
        public string? ReconciledBy { get; set; }

        // ==================== Reconciliation Difference Handling ====================

        /// <summary>
        /// Amount difference between bank transaction and reconciled record
        /// Positive = bank received more, Negative = bank received less
        /// </summary>
        public decimal? ReconciliationDifferenceAmount { get; set; }

        /// <summary>
        /// Classification of the difference: bank_interest, bank_charges, tds_deducted,
        /// round_off, forex_gain, forex_loss, other_income, other_expense, suspense
        /// </summary>
        public string? ReconciliationDifferenceType { get; set; }

        /// <summary>
        /// Notes explaining the reconciliation difference
        /// </summary>
        public string? ReconciliationDifferenceNotes { get; set; }

        /// <summary>
        /// TDS section if difference type is tds_deducted (e.g., 194C, 194J)
        /// </summary>
        public string? ReconciliationTdsSection { get; set; }

        /// <summary>
        /// Journal entry ID created to account for the difference
        /// </summary>
        public Guid? ReconciliationAdjustmentJournalId { get; set; }

        // ==================== Journal Entry Linking (Hybrid Reconciliation) ====================

        /// <summary>
        /// Journal Entry ID for the source document's JE (enables BRS from ledger perspective)
        /// </summary>
        public Guid? ReconciledJournalEntryId { get; set; }

        /// <summary>
        /// Specific JE line ID that affects the bank account (for complete audit trail)
        /// </summary>
        public Guid? ReconciledJeLineId { get; set; }

        // ==================== Reversal Pairing ====================

        /// <summary>
        /// ID of the paired transaction (for reversal pairs)
        /// Original transaction points to reversal, reversal points to original
        /// </summary>
        public Guid? PairedTransactionId { get; set; }

        /// <summary>
        /// Type of pair: 'original' (the failed transaction) or 'reversal' (the return credit)
        /// </summary>
        public string? PairType { get; set; }

        /// <summary>
        /// If the original was already posted to ledger, this is the reversal journal entry ID
        /// </summary>
        public Guid? ReversalJournalEntryId { get; set; }

        /// <summary>
        /// Whether this is detected as a reversal transaction (starts with REV-, REVERSAL, etc.)
        /// </summary>
        public bool IsReversalTransaction { get; set; }

        // ==================== Import Tracking ====================

        /// <summary>
        /// Source of the transaction: manual, csv, pdf, api
        /// </summary>
        public string ImportSource { get; set; } = "manual";

        /// <summary>
        /// Batch ID for bulk imports (to track and potentially rollback imports)
        /// </summary>
        public Guid? ImportBatchId { get; set; }

        /// <summary>
        /// Original raw data from import (JSON)
        /// </summary>
        public string? RawData { get; set; }

        // ==================== Duplicate Detection ====================

        /// <summary>
        /// SHA256 hash of date+amount+description for duplicate detection
        /// </summary>
        public string? TransactionHash { get; set; }

        // ==================== Tally Import Matching ====================

        /// <summary>
        /// Tally voucher type that created this transaction: payment, receipt, contra, journal, sales, purchase
        /// Enables tracking the origin of each bank book entry
        /// </summary>
        public string? SourceVoucherType { get; set; }

        /// <summary>
        /// Source entity type from Tally import: vendor_payments, contractor_payments, statutory_payments, journal_entries
        /// Distinct from ReconciledType which is for bank statement reconciliation
        /// </summary>
        public string? MatchedEntityType { get; set; }

        /// <summary>
        /// Source entity ID from Tally import - the business entity this bank transaction was created from
        /// Distinct from ReconciledId which is for bank statement reconciliation
        /// </summary>
        public Guid? MatchedEntityId { get; set; }

        /// <summary>
        /// Tally voucher GUID for deduplication
        /// </summary>
        public string? TallyVoucherGuid { get; set; }

        /// <summary>
        /// Original Tally voucher number
        /// </summary>
        public string? TallyVoucherNumber { get; set; }

        /// <summary>
        /// Migration batch ID for rollback support
        /// </summary>
        public Guid? TallyMigrationBatchId { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public BankAccount? BankAccount { get; set; }
    }
}
