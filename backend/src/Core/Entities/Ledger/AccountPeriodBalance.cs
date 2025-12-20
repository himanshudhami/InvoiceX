namespace Core.Entities.Ledger
{
    /// <summary>
    /// Pre-computed account balances per period
    /// Used for efficient trial balance and financial statement generation
    /// </summary>
    public class AccountPeriodBalance
    {
        public Guid Id { get; set; }

        // ==================== Keys ====================

        /// <summary>
        /// Company
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Account
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Financial year (e.g., "2024-25")
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Period month (1-12, where April = 1)
        /// </summary>
        public int PeriodMonth { get; set; }

        // ==================== Balances ====================

        /// <summary>
        /// Opening balance for this period
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Total debits during this period
        /// </summary>
        public decimal TotalDebits { get; set; }

        /// <summary>
        /// Total credits during this period
        /// </summary>
        public decimal TotalCredits { get; set; }

        /// <summary>
        /// Net movement (debits - credits for debit-normal, credits - debits for credit-normal)
        /// </summary>
        public decimal NetMovement { get; set; }

        /// <summary>
        /// Closing balance for this period (becomes opening for next)
        /// </summary>
        public decimal ClosingBalance { get; set; }

        // ==================== Audit ====================

        public DateTime LastUpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public ChartOfAccount? Account { get; set; }
    }
}
