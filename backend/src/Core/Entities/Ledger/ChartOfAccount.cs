namespace Core.Entities.Ledger
{
    /// <summary>
    /// Chart of Accounts - follows Indian Schedule III standard
    /// Represents the hierarchical structure of accounts for double-entry bookkeeping
    /// </summary>
    public class ChartOfAccount
    {
        public Guid Id { get; set; }

        // ==================== Company & Hierarchy ====================

        /// <summary>
        /// Company this account belongs to (null for system-wide template accounts)
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Unique account code within the company (e.g., "1110", "4120")
        /// Follows Schedule III numbering: 1xxx=Assets, 2xxx=Liabilities, 3xxx=Equity, 4xxx=Income, 5xxx=Expenses
        /// </summary>
        public string AccountCode { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the account
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Parent account for hierarchical grouping (null for top-level accounts)
        /// </summary>
        public Guid? ParentAccountId { get; set; }

        /// <summary>
        /// Depth in hierarchy (0 = top level, 1 = first child, etc.)
        /// </summary>
        public int DepthLevel { get; set; }

        /// <summary>
        /// Order for display within same parent
        /// </summary>
        public int SortOrder { get; set; }

        // ==================== Account Classification ====================

        /// <summary>
        /// Main type: asset, liability, equity, income, expense
        /// </summary>
        public string AccountType { get; set; } = string.Empty;

        /// <summary>
        /// Sub-classification (e.g., current_asset, fixed_asset, long_term_liability)
        /// </summary>
        public string? AccountSubtype { get; set; }

        /// <summary>
        /// Reference to Indian Schedule III (e.g., "III(I)(a)" for share capital)
        /// </summary>
        public string? ScheduleReference { get; set; }

        /// <summary>
        /// Normal balance side: debit or credit
        /// Assets/Expenses = debit, Liabilities/Equity/Income = credit
        /// </summary>
        public string NormalBalance { get; set; } = "debit";

        // ==================== GST & Tax ====================

        /// <summary>
        /// GST treatment: taxable, exempt, nil_rated, non_gst, input_credit, output_tax
        /// </summary>
        public string? GstTreatment { get; set; }

        /// <summary>
        /// Whether this account is used for subledger reconciliation (AR, AP, etc.)
        /// </summary>
        public bool IsControlAccount { get; set; }

        /// <summary>
        /// System-created account that cannot be modified
        /// </summary>
        public bool IsSystemAccount { get; set; }

        // ==================== Balances ====================

        /// <summary>
        /// Opening balance (typically set at beginning of FY)
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Current running balance (updated by triggers)
        /// </summary>
        public decimal CurrentBalance { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Whether account is active and can receive postings
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public ChartOfAccount? ParentAccount { get; set; }
        public ICollection<ChartOfAccount>? ChildAccounts { get; set; }
        public ICollection<JournalEntryLine>? JournalLines { get; set; }
    }
}
