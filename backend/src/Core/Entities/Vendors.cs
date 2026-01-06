namespace Core.Entities
{
    /// <summary>
    /// Vendor/Supplier entity - represents a vendor with Indian GST/TDS compliance fields
    /// Mirrors Customers entity but for payables (Sundry Creditors in Tally)
    /// </summary>
    public class Vendors
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? TaxNumber { get; set; }
        public string? Notes { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? PaymentTerms { get; set; }
        public bool? IsActive { get; set; }

        // ==================== Indian GST Compliance ====================

        /// <summary>
        /// GST Identification Number (15 characters)
        /// Required for B2B vendors in India
        /// </summary>
        public string? Gstin { get; set; }

        /// <summary>
        /// GST State Code (2 digits)
        /// Used for determining CGST/SGST vs IGST
        /// </summary>
        public string? GstStateCode { get; set; }

        /// <summary>
        /// Vendor type for GST classification
        /// Values: 'b2b' (GST registered), 'b2c' (unregistered), 'import' (overseas supplier), 'rcm_applicable' (reverse charge)
        /// </summary>
        public string? VendorType { get; set; }

        /// <summary>
        /// Whether the vendor is GST registered
        /// </summary>
        public bool IsGstRegistered { get; set; }

        /// <summary>
        /// PAN Number (10 characters)
        /// Required when TDS is applicable
        /// </summary>
        public string? PanNumber { get; set; }

        // ==================== TDS Compliance ====================

        /// <summary>
        /// TAN Number (Tax Deduction Account Number)
        /// Required for TDS certificate issuance
        /// </summary>
        public string? TanNumber { get; set; }

        /// <summary>
        /// Default TDS Section for this vendor
        /// Values: '194C' (Contractors), '194J' (Professional/Technical), '194H' (Commission),
        /// '194I' (Rent), '194A' (Interest), '194Q' (Purchase of goods)
        /// </summary>
        public string? DefaultTdsSection { get; set; }

        /// <summary>
        /// Default TDS Rate for this vendor
        /// Based on TdsSection and vendor's PAN availability
        /// </summary>
        public decimal? DefaultTdsRate { get; set; }

        /// <summary>
        /// Whether TDS is applicable for payments to this vendor
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// Lower/Nil TDS Certificate Number (if vendor has one)
        /// </summary>
        public string? LowerTdsCertificate { get; set; }

        /// <summary>
        /// Lower TDS Rate from certificate (if applicable)
        /// </summary>
        public decimal? LowerTdsRate { get; set; }

        /// <summary>
        /// Certificate validity end date
        /// </summary>
        public DateOnly? LowerTdsCertificateValidTill { get; set; }

        // ==================== MSME Compliance ====================

        /// <summary>
        /// Whether the vendor is registered under MSME Act
        /// Required for MSME payment tracking and reporting
        /// </summary>
        public bool MsmeRegistered { get; set; }

        /// <summary>
        /// MSME Registration Number (Udyam Number)
        /// Format: UDYAM-XX-00-0000000
        /// </summary>
        public string? MsmeRegistrationNumber { get; set; }

        /// <summary>
        /// MSME Category: micro, small, medium
        /// Affects payment terms under MSME Act (45 days limit)
        /// </summary>
        public string? MsmeCategory { get; set; }

        // ==================== Bank Details (for payments) ====================

        /// <summary>
        /// Vendor's bank account number
        /// </summary>
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// IFSC Code for NEFT/RTGS
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
        public string? BankAccountHolderName { get; set; }

        // ==================== Default Accounts (for auto-posting) ====================

        /// <summary>
        /// Default expense account for purchases from this vendor
        /// </summary>
        public Guid? DefaultExpenseAccountId { get; set; }

        /// <summary>
        /// Default payables account (usually Trade Payables)
        /// </summary>
        public Guid? DefaultPayableAccountId { get; set; }

        // ==================== Tally Migration Fields ====================

        /// <summary>
        /// Original Tally Ledger GUID for migration tracking
        /// </summary>
        public string? TallyLedgerGuid { get; set; }

        /// <summary>
        /// Original Tally Ledger Name
        /// </summary>
        public string? TallyLedgerName { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
