namespace Core.Entities.Ledger
{
    /// <summary>
    /// Journal Entry Line - individual debit or credit within a journal entry
    /// Each line affects one account
    /// </summary>
    public class JournalEntryLine
    {
        public Guid Id { get; set; }

        // ==================== References ====================

        /// <summary>
        /// Parent journal entry
        /// </summary>
        public Guid JournalEntryId { get; set; }

        /// <summary>
        /// Account being debited or credited
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Line number within the journal entry (for ordering)
        /// </summary>
        public int LineNumber { get; set; }

        // ==================== Amounts ====================

        /// <summary>
        /// Debit amount (0 if credit line)
        /// </summary>
        public decimal DebitAmount { get; set; }

        /// <summary>
        /// Credit amount (0 if debit line)
        /// </summary>
        public decimal CreditAmount { get; set; }

        // ==================== Currency ====================

        /// <summary>
        /// Currency code (default INR)
        /// </summary>
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Exchange rate to INR (1.0 for INR transactions)
        /// </summary>
        public decimal ExchangeRate { get; set; } = 1;

        /// <summary>
        /// Amount in INR (for multi-currency)
        /// </summary>
        public decimal? AmountInInr { get; set; }

        // ==================== Subledger ====================

        /// <summary>
        /// Subledger type: customer, vendor, employee, bank_account
        /// </summary>
        public string? SubledgerType { get; set; }

        /// <summary>
        /// Subledger ID (customer_id, vendor_id, etc.)
        /// </summary>
        public Guid? SubledgerId { get; set; }

        // ==================== Description ====================

        /// <summary>
        /// Line-specific description/narration
        /// </summary>
        public string? Description { get; set; }

        // ==================== Cost Center (Future) ====================

        /// <summary>
        /// Cost center ID for departmental accounting
        /// </summary>
        public Guid? CostCenterId { get; set; }

        /// <summary>
        /// Project ID for project-based accounting
        /// </summary>
        public Guid? ProjectId { get; set; }

        // ==================== Navigation Properties ====================

        public JournalEntry? JournalEntry { get; set; }
        public ChartOfAccount? Account { get; set; }
    }
}
