namespace Core.Entities
{
    /// <summary>
    /// Bank account record - tracks company bank accounts for reconciliation
    /// Supports multiple account types (current, savings, CC, foreign)
    /// </summary>
    public class BankAccount
    {
        public Guid Id { get; set; }

        // ==================== Company Linking ====================

        /// <summary>
        /// Company that owns this bank account
        /// </summary>
        public Guid? CompanyId { get; set; }

        // ==================== Account Details ====================

        /// <summary>
        /// Display name for the account (e.g., "HDFC Current Account")
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Bank account number
        /// </summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Name of the bank
        /// </summary>
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// IFSC code for Indian bank accounts
        /// </summary>
        public string? IfscCode { get; set; }

        /// <summary>
        /// Branch name/location
        /// </summary>
        public string? BranchName { get; set; }

        // ==================== Account Classification ====================

        /// <summary>
        /// Account type: current, savings, cc (cash credit), foreign
        /// </summary>
        public string AccountType { get; set; } = "current";

        /// <summary>
        /// Currency of the account (default INR)
        /// </summary>
        public string Currency { get; set; } = "INR";

        // ==================== Balance Tracking ====================

        /// <summary>
        /// Opening balance when account was added to the system
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Current balance as per last reconciliation
        /// </summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Date of the balance snapshot
        /// </summary>
        public DateOnly? AsOfDate { get; set; }

        // ==================== Status Flags ====================

        /// <summary>
        /// Whether this is the primary account for the company
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ==================== Additional Info ====================

        /// <summary>
        /// Additional notes about the account
        /// </summary>
        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
    }
}
