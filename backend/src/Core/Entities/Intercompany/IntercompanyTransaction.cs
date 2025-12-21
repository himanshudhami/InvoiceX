namespace Core.Entities.Intercompany
{
    /// <summary>
    /// Represents a transaction between group companies
    /// Used for intercompany reconciliation
    /// </summary>
    public class IntercompanyTransaction
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Company recording this transaction
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Other company in the transaction
        /// </summary>
        public Guid CounterpartyCompanyId { get; set; }

        public DateOnly TransactionDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Transaction type: 'invoice', 'payment', 'allocation', 'journal', 'recharge'
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Direction: 'receivable' or 'payable'
        /// </summary>
        public string TransactionDirection { get; set; } = string.Empty;

        public string? SourceDocumentType { get; set; }
        public Guid? SourceDocumentId { get; set; }
        public string? SourceDocumentNumber { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal ExchangeRate { get; set; } = 1.0m;
        public decimal? AmountInInr { get; set; }

        public decimal GstAmount { get; set; } = 0;
        public bool IsGstApplicable { get; set; } = false;

        public Guid? JournalEntryId { get; set; }

        // Reconciliation
        public bool IsReconciled { get; set; } = false;
        public DateTime? ReconciledAt { get; set; }
        public Guid? ReconciledBy { get; set; }

        /// <summary>
        /// Matching entry in counterparty's books
        /// </summary>
        public Guid? CounterpartyTransactionId { get; set; }
        public string? ReconciliationNotes { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}
