namespace Application.Interfaces.Ledger
{
    /// <summary>
    /// Service for generating trial balance and financial statements
    /// </summary>
    public interface ITrialBalanceService
    {
        /// <summary>
        /// Get trial balance for a company as of a specific date
        /// </summary>
        Task<TrialBalanceReport> GetTrialBalanceAsync(
            Guid companyId,
            DateOnly asOfDate,
            bool includeZeroBalances = false);

        /// <summary>
        /// Get trial balance for a specific period
        /// </summary>
        Task<TrialBalanceReport> GetTrialBalanceByPeriodAsync(
            Guid companyId,
            string financialYear,
            int? periodMonth = null);

        /// <summary>
        /// Get account ledger (all transactions for an account)
        /// </summary>
        Task<AccountLedgerReport> GetAccountLedgerAsync(
            Guid accountId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get income statement (P&L)
        /// </summary>
        Task<IncomeStatementReport> GetIncomeStatementAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get balance sheet
        /// </summary>
        Task<BalanceSheetReport> GetBalanceSheetAsync(
            Guid companyId,
            DateOnly asOfDate);

        /// <summary>
        /// Recalculate period balances (maintenance operation)
        /// </summary>
        Task RecalculatePeriodBalancesAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get accounts with abnormal balances (for data quality report)
        /// </summary>
        Task<AbnormalBalanceReport> GetAbnormalBalancesAsync(Guid companyId);

        /// <summary>
        /// Get summary of abnormal balances (for dashboard alert)
        /// </summary>
        Task<AbnormalBalanceAlertSummary> GetAbnormalBalanceAlertAsync(Guid companyId);
    }

    // ==================== Report DTOs ====================

    public class TrialBalanceReport
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public List<TrialBalanceRow> Rows { get; set; } = new();
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;
    }

    public class TrialBalanceRow
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
        public decimal DebitBalance => ClosingBalance > 0 ? ClosingBalance : 0;
        public decimal CreditBalance => ClosingBalance < 0 ? Math.Abs(ClosingBalance) : 0;
        public bool IsControlAccount { get; set; }
        public string? ControlAccountType { get; set; }
    }

    public class AccountLedgerReport
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<AccountLedgerEntry> Entries { get; set; } = new();
        public decimal ClosingBalance { get; set; }
    }

    public class AccountLedgerEntry
    {
        public DateOnly Date { get; set; }
        public string JournalNumber { get; set; } = string.Empty;
        public Guid JournalEntryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public class IncomeStatementReport
    {
        public Guid CompanyId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public List<IncomeStatementSection> IncomeSections { get; set; } = new();
        public List<IncomeStatementSection> ExpenseSections { get; set; } = new();
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit => TotalIncome - TotalExpenses;
    }

    public class IncomeStatementSection
    {
        public string SectionName { get; set; } = string.Empty;
        public List<IncomeStatementRow> Rows { get; set; } = new();
        public decimal SectionTotal { get; set; }
    }

    public class IncomeStatementRow
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BalanceSheetReport
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public List<BalanceSheetSection> AssetSections { get; set; } = new();
        public List<BalanceSheetSection> LiabilitySections { get; set; } = new();
        public List<BalanceSheetSection> EquitySections { get; set; } = new();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public bool IsBalanced => Math.Abs(TotalAssets - (TotalLiabilities + TotalEquity)) < 0.01m;
    }

    public class BalanceSheetSection
    {
        public string SectionName { get; set; } = string.Empty;
        public List<BalanceSheetRow> Rows { get; set; } = new();
        public decimal SectionTotal { get; set; }
    }

    public class BalanceSheetRow
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class AbnormalBalanceReport
    {
        public Guid CompanyId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int TotalAbnormalAccounts { get; set; }
        public int ActionableIssues { get; set; }
        public decimal TotalAbnormalAmount { get; set; }
        public List<AbnormalBalanceItem> Items { get; set; } = new();
        public List<AbnormalBalanceCategorySummary> CategorySummary { get; set; } = new();
    }

    public class AbnormalBalanceItem
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubtype { get; set; }
        public string ExpectedBalanceSide { get; set; } = string.Empty;
        public string ActualBalanceSide { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PossibleReason { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsContraAccount { get; set; }
        public string Severity => IsContraAccount ? "info" : "warning";
    }

    public class AbnormalBalanceCategorySummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public class AbnormalBalanceAlertSummary
    {
        public Guid CompanyId { get; set; }
        public bool HasIssues { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public decimal TotalAmount { get; set; }
        public string AlertMessage { get; set; } = string.Empty;
        public string AlertSeverity { get; set; } = "info";
        public List<AbnormalBalanceCategorySummary> TopCategories { get; set; } = new();
    }
}
