namespace Core.Entities.Intercompany
{
    /// <summary>
    /// Running balance between company pairs for quick reconciliation
    /// </summary>
    public class IntercompanyBalance
    {
        public Guid Id { get; set; }
        public Guid FromCompanyId { get; set; }
        public Guid ToCompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Balance amount: Positive = receivable, Negative = payable
        /// </summary>
        public decimal BalanceAmount { get; set; } = 0;
        public string Currency { get; set; } = "INR";
        public decimal? BalanceInInr { get; set; }

        // Activity summary
        public decimal OpeningBalance { get; set; } = 0;
        public decimal TotalDebits { get; set; } = 0;
        public decimal TotalCredits { get; set; } = 0;
        public int TransactionCount { get; set; } = 0;
        public DateOnly? LastTransactionDate { get; set; }

        // Reconciliation
        public bool IsReconciled { get; set; } = false;

        /// <summary>
        /// Balance as per counterparty (should be opposite sign)
        /// </summary>
        public decimal? CounterpartyBalance { get; set; }

        /// <summary>
        /// Difference for reconciliation
        /// </summary>
        public decimal? Difference { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
