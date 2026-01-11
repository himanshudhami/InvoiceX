namespace Application.Interfaces.Ledger
{
    /// <summary>
    /// Service for subledger reports - party-wise breakdown of control accounts.
    /// Part of COA Modernization (Task 16).
    /// </summary>
    public interface ISubledgerReportService
    {
        /// <summary>
        /// Get AP aging by vendor (subledger breakdown of Trade Payables)
        /// </summary>
        Task<SubledgerAgingReport> GetApAgingAsync(
            Guid companyId,
            DateOnly asOfDate,
            AgingBuckets? customBuckets = null);

        /// <summary>
        /// Get AR aging by customer (subledger breakdown of Trade Receivables)
        /// </summary>
        Task<SubledgerAgingReport> GetArAgingAsync(
            Guid companyId,
            DateOnly asOfDate,
            AgingBuckets? customBuckets = null);

        /// <summary>
        /// Get party ledger (transaction history for a specific party)
        /// </summary>
        Task<PartyLedgerReport> GetPartyLedgerAsync(
            Guid companyId,
            string partyType,
            Guid partyId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get control account reconciliation (verify subledger sum = control account balance)
        /// </summary>
        Task<ControlAccountReconciliation> GetControlAccountReconciliationAsync(
            Guid companyId,
            DateOnly asOfDate);

        /// <summary>
        /// Get subledger drill-down for a control account
        /// </summary>
        Task<SubledgerDrilldown> GetSubledgerDrilldownAsync(
            Guid companyId,
            Guid controlAccountId,
            DateOnly asOfDate);
    }

    // ==================== Report DTOs ====================

    public class SubledgerAgingReport
    {
        public Guid CompanyId { get; set; }
        public string ReportType { get; set; } = string.Empty; // "AP" or "AR"
        public DateOnly AsOfDate { get; set; }
        public List<SubledgerAgingRow> Rows { get; set; } = new();
        public AgingBuckets Buckets { get; set; } = new();
        public SubledgerAgingTotals Totals { get; set; } = new();
    }

    public class SubledgerAgingRow
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string? PartyCode { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class SubledgerAgingTotals
    {
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class AgingBuckets
    {
        public int Bucket1Days { get; set; } = 30;
        public int Bucket2Days { get; set; } = 60;
        public int Bucket3Days { get; set; } = 90;
    }

    public class PartyLedgerReport
    {
        public Guid CompanyId { get; set; }
        public Guid PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string PartyType { get; set; } = string.Empty;
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<PartyLedgerEntry> Entries { get; set; } = new();
        public decimal ClosingBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
    }

    public class PartyLedgerEntry
    {
        public DateOnly Date { get; set; }
        public Guid JournalEntryId { get; set; }
        public string JournalNumber { get; set; } = string.Empty;
        public string? SourceType { get; set; }
        public string? SourceNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public class ControlAccountReconciliation
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public List<ControlAccountReconciliationRow> Rows { get; set; } = new();
        public bool AllReconciled => Rows.All(r => r.IsReconciled);
    }

    public class ControlAccountReconciliationRow
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? ControlAccountType { get; set; }
        public decimal ControlAccountBalance { get; set; }
        public decimal SubledgerSum { get; set; }
        public decimal Difference { get; set; }
        public bool IsReconciled => Math.Abs(Difference) < 0.01m;
        public int PartyCount { get; set; }
    }

    public class SubledgerDrilldown
    {
        public Guid CompanyId { get; set; }
        public Guid ControlAccountId { get; set; }
        public string ControlAccountCode { get; set; } = string.Empty;
        public string ControlAccountName { get; set; } = string.Empty;
        public DateOnly AsOfDate { get; set; }
        public decimal ControlAccountBalance { get; set; }
        public List<SubledgerDrilldownRow> Parties { get; set; } = new();
        public decimal SubledgerSum { get; set; }
        public bool IsReconciled => Math.Abs(ControlAccountBalance - SubledgerSum) < 0.01m;
    }

    public class SubledgerDrilldownRow
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string? PartyCode { get; set; }
        public string PartyType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public int TransactionCount { get; set; }
        public DateOnly? LastTransactionDate { get; set; }
    }
}
