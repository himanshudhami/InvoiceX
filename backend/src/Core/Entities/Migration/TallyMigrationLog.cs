namespace Core.Entities.Migration
{
    /// <summary>
    /// Detailed log entry for each record processed during Tally import.
    /// Tracks success, failure, skips, and suspense mappings at the record level.
    /// </summary>
    public class TallyMigrationLog
    {
        public Guid Id { get; set; }
        public Guid BatchId { get; set; }

        // ==================== Record Identification ====================

        /// <summary>
        /// Type of record being processed.
        /// Values: ledger, stock_item, stock_group, godown, unit, cost_center,
        ///         voucher_sales, voucher_purchase, voucher_receipt, voucher_payment,
        ///         voucher_journal, voucher_contra, voucher_credit_note, voucher_debit_note,
        ///         voucher_stock_journal, voucher_physical_stock, opening_balance, cost_allocation
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// Original Tally GUID
        /// </summary>
        public string? TallyGuid { get; set; }

        /// <summary>
        /// Original Tally name/identifier
        /// </summary>
        public string? TallyName { get; set; }

        /// <summary>
        /// Tally parent name (for hierarchical items like stock groups)
        /// </summary>
        public string? TallyParentName { get; set; }

        /// <summary>
        /// Tally date for vouchers
        /// </summary>
        public DateOnly? TallyDate { get; set; }

        // ==================== Target Entity ====================

        /// <summary>
        /// Target entity type in our system.
        /// Values: customers, vendors, chart_of_accounts, bank_accounts, invoices,
        ///         payments, vendor_invoices, vendor_payments, journal_entries,
        ///         stock_items, stock_groups, warehouses, tags, etc.
        /// </summary>
        public string? TargetEntity { get; set; }

        /// <summary>
        /// ID of the created/updated record in target entity
        /// </summary>
        public Guid? TargetId { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Processing status.
        /// Values: pending, success, skipped, failed, mapped_to_suspense, duplicate
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Reason for skipping (if status = skipped)
        /// </summary>
        public string? SkipReason { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code for categorization
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Non-blocking validation warnings (JSONB array)
        /// </summary>
        public string ValidationWarnings { get; set; } = "[]";

        // ==================== Raw Data ====================

        /// <summary>
        /// Original Tally data (JSONB) for debugging and re-processing
        /// </summary>
        public string? RawData { get; set; }

        // ==================== Amount Tracking ====================

        /// <summary>
        /// Amount from Tally data
        /// </summary>
        public decimal? TallyAmount { get; set; }

        /// <summary>
        /// Amount imported into our system
        /// </summary>
        public decimal? ImportedAmount { get; set; }

        /// <summary>
        /// Difference for reconciliation
        /// </summary>
        public decimal? AmountDifference { get; set; }

        // ==================== Processing Info ====================

        /// <summary>
        /// Processing order within batch
        /// </summary>
        public int? ProcessingOrder { get; set; }

        /// <summary>
        /// Time taken to process this record (milliseconds)
        /// </summary>
        public int? ProcessingDurationMs { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==================== Navigation ====================

        public TallyMigrationBatch? Batch { get; set; }
    }
}
