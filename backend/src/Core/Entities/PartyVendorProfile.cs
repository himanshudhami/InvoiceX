namespace Core.Entities
{
    /// <summary>
    /// Vendor-specific profile extension for Party
    /// Contains TDS, MSME, Bank details specific to vendor role
    /// </summary>
    public class PartyVendorProfile
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Vendor Classification ====================

        /// <summary>
        /// Vendor type for GST/compliance: b2b, b2c, import, rcm_applicable
        /// </summary>
        public string? VendorType { get; set; }

        // ==================== TDS Configuration ====================

        /// <summary>
        /// Whether TDS is applicable for payments to this vendor
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// Default TDS Section for this vendor
        /// Values: 194C, 194J, 194H, 194I, 194IA, 194IB, 194A, 194Q, 194O, 194N, 194M, 195
        /// </summary>
        public string? DefaultTdsSection { get; set; }

        /// <summary>
        /// Default TDS Rate (percentage with PAN)
        /// </summary>
        public decimal? DefaultTdsRate { get; set; }

        /// <summary>
        /// Vendor's TAN (Tax Deduction Account Number)
        /// </summary>
        public string? TanNumber { get; set; }

        // ==================== Lower/Nil TDS Certificate ====================

        /// <summary>
        /// Lower/Nil TDS Certificate Number
        /// </summary>
        public string? LowerTdsCertificate { get; set; }

        /// <summary>
        /// Lower TDS Rate from certificate
        /// </summary>
        public decimal? LowerTdsRate { get; set; }

        /// <summary>
        /// Certificate validity start date
        /// </summary>
        public DateOnly? LowerTdsValidFrom { get; set; }

        /// <summary>
        /// Certificate validity end date
        /// </summary>
        public DateOnly? LowerTdsValidTill { get; set; }

        // ==================== MSME Compliance ====================

        /// <summary>
        /// Whether vendor is registered under MSME Act
        /// Required for 45-day payment tracking
        /// </summary>
        public bool MsmeRegistered { get; set; }

        /// <summary>
        /// MSME Registration Number (Udyam Number)
        /// Format: UDYAM-XX-00-0000000
        /// </summary>
        public string? MsmeRegistrationNumber { get; set; }

        /// <summary>
        /// MSME Category: micro, small, medium
        /// </summary>
        public string? MsmeCategory { get; set; }

        // ==================== Bank Details (for payments) ====================

        /// <summary>
        /// Vendor's bank account number
        /// </summary>
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// IFSC Code for NEFT/RTGS transfers
        /// </summary>
        public string? BankIfscCode { get; set; }

        /// <summary>
        /// Bank name
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Branch name
        /// </summary>
        public string? BankBranch { get; set; }

        /// <summary>
        /// Account holder name (for verification)
        /// </summary>
        public string? BankAccountHolder { get; set; }

        /// <summary>
        /// Account type: savings, current, cc (cash credit), od (overdraft)
        /// </summary>
        public string? BankAccountType { get; set; }

        // ==================== Default Accounts (for auto-posting) ====================

        /// <summary>
        /// Default expense account for purchases from this vendor
        /// </summary>
        public Guid? DefaultExpenseAccountId { get; set; }

        /// <summary>
        /// Default payables account (usually Trade Payables)
        /// </summary>
        public Guid? DefaultPayableAccountId { get; set; }

        // ==================== Payment Terms ====================

        /// <summary>
        /// Payment terms in days (Net 30, Net 45, etc.)
        /// </summary>
        public int? PaymentTermsDays { get; set; }

        /// <summary>
        /// Credit limit for this vendor
        /// </summary>
        public decimal? CreditLimit { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Property ====================

        /// <summary>
        /// Parent Party entity
        /// </summary>
        public Party? Party { get; set; }
    }
}
