namespace Core.Interfaces.Ledger
{
    /// <summary>
    /// Repository interface for ledger reporting queries
    /// Handles complex SQL for trial balance, income statement, balance sheet
    /// </summary>
    public interface ILedgerReportRepository
    {
        /// <summary>
        /// Get trial balance data for a company as of a specific date
        /// </summary>
        Task<IEnumerable<TrialBalanceData>> GetTrialBalanceDataAsync(
            Guid companyId,
            DateOnly asOfDate,
            bool includeZeroBalances = false);

        /// <summary>
        /// Get opening balance for an account before a specific date
        /// </summary>
        Task<decimal> GetAccountOpeningBalanceAsync(
            Guid accountId,
            DateOnly beforeDate,
            decimal initialOpeningBalance,
            string normalBalance);

        /// <summary>
        /// Get account ledger entries for a date range
        /// </summary>
        Task<IEnumerable<AccountLedgerData>> GetAccountLedgerDataAsync(
            Guid accountId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get income/expense data for income statement
        /// </summary>
        Task<IEnumerable<IncomeExpenseData>> GetIncomeExpenseDataAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get balance sheet data (assets, liabilities, equity)
        /// </summary>
        Task<IEnumerable<BalanceSheetData>> GetBalanceSheetDataAsync(
            Guid companyId,
            DateOnly asOfDate);

        /// <summary>
        /// Recalculate period balances for a financial year
        /// </summary>
        Task RecalculatePeriodBalancesAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get accounts with abnormal balances (opposite to their normal balance)
        /// </summary>
        Task<IEnumerable<AbnormalBalanceData>> GetAbnormalBalancesAsync(Guid companyId);

        /// <summary>
        /// Get count of accounts with abnormal balances (for dashboard alert)
        /// </summary>
        Task<AbnormalBalanceSummary> GetAbnormalBalanceSummaryAsync(Guid companyId);
    }

    // ==================== Data Transfer Objects for Repository ====================

    public class TrialBalanceData
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public int DepthLevel { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Debits { get; set; }
        public decimal Credits { get; set; }
        public decimal ClosingBalance { get; set; }
        public bool IsControlAccount { get; set; }
        public string? ControlAccountType { get; set; }
    }

    public class AccountLedgerData
    {
        public DateOnly Date { get; set; }
        public string JournalNumber { get; set; } = string.Empty;
        public Guid JournalEntryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class IncomeExpenseData
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubtype { get; set; }
        public decimal Amount { get; set; }
    }

    public class BalanceSheetData
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubtype { get; set; }
        public decimal Amount { get; set; }
    }

    public class AbnormalBalanceData
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubtype { get; set; }
        public string NormalBalance { get; set; } = string.Empty;
        public string ActualBalanceSide { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PossibleReason { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsContraAccount { get; set; }
    }

    public class AbnormalBalanceSummary
    {
        public int TotalAbnormalAccounts { get; set; }
        public int LiabilitiesWithDebit { get; set; }
        public int AssetsWithCredit { get; set; }
        public int ContraAccounts { get; set; }
        public decimal TotalAbnormalAmount { get; set; }
        public List<AbnormalBalanceCategory> Categories { get; set; } = new();
    }

    public class AbnormalBalanceCategory
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public string Severity { get; set; } = string.Empty;
    }
}
