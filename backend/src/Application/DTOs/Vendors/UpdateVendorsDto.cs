using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Vendors
{
    /// <summary>
    /// Data transfer object for updating Vendors
    /// </summary>
    public class UpdateVendorsDto
    {
        /// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// CompanyName
        /// </summary>
        [StringLength(255, ErrorMessage = "CompanyName cannot exceed 255 characters")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        /// <summary>
        /// Phone
        /// </summary>
        [StringLength(255, ErrorMessage = "Phone cannot exceed 255 characters")]
        public string? Phone { get; set; }

        /// <summary>
        /// AddressLine1
        /// </summary>
        [StringLength(255, ErrorMessage = "AddressLine1 cannot exceed 255 characters")]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// AddressLine2
        /// </summary>
        [StringLength(255, ErrorMessage = "AddressLine2 cannot exceed 255 characters")]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [StringLength(255, ErrorMessage = "City cannot exceed 255 characters")]
        public string? City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        [StringLength(255, ErrorMessage = "State cannot exceed 255 characters")]
        public string? State { get; set; }

        /// <summary>
        /// ZipCode
        /// </summary>
        [StringLength(20, ErrorMessage = "ZipCode cannot exceed 20 characters")]
        public string? ZipCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string? Country { get; set; }

        /// <summary>
        /// TaxNumber
        /// </summary>
        [StringLength(50, ErrorMessage = "TaxNumber cannot exceed 50 characters")]
        public string? TaxNumber { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// CreditLimit
        /// </summary>
        public decimal? CreditLimit { get; set; }

        /// <summary>
        /// PaymentTerms (days)
        /// </summary>
        public int? PaymentTerms { get; set; }

        /// <summary>
        /// IsActive
        /// </summary>
        public bool? IsActive { get; set; }

        // ==================== Indian GST Compliance ====================

        /// <summary>
        /// GST Identification Number (15 characters)
        /// </summary>
        [StringLength(20, ErrorMessage = "GSTIN cannot exceed 20 characters")]
        public string? Gstin { get; set; }

        /// <summary>
        /// GST State Code (2 digits)
        /// </summary>
        [StringLength(5, ErrorMessage = "GST State Code cannot exceed 5 characters")]
        public string? GstStateCode { get; set; }

        /// <summary>
        /// Vendor type for GST classification (b2b, b2c, import, rcm_applicable)
        /// </summary>
        [StringLength(20, ErrorMessage = "Vendor Type cannot exceed 20 characters")]
        public string? VendorType { get; set; }

        /// <summary>
        /// Whether the vendor is GST registered
        /// </summary>
        public bool IsGstRegistered { get; set; }

        /// <summary>
        /// PAN Number (10 characters)
        /// </summary>
        [StringLength(15, ErrorMessage = "PAN cannot exceed 15 characters")]
        public string? PanNumber { get; set; }

        // ==================== TDS Compliance ====================

        /// <summary>
        /// TAN Number (Tax Deduction Account Number)
        /// </summary>
        [StringLength(15, ErrorMessage = "TAN cannot exceed 15 characters")]
        public string? TanNumber { get; set; }

        /// <summary>
        /// Default TDS Section for this vendor (194C, 194J, etc.)
        /// </summary>
        [StringLength(10, ErrorMessage = "TDS Section cannot exceed 10 characters")]
        public string? DefaultTdsSection { get; set; }

        /// <summary>
        /// Default TDS Rate for this vendor
        /// </summary>
        public decimal? DefaultTdsRate { get; set; }

        /// <summary>
        /// Whether TDS is applicable for payments to this vendor
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// Lower/Nil TDS Certificate Number
        /// </summary>
        [StringLength(50, ErrorMessage = "Lower TDS Certificate cannot exceed 50 characters")]
        public string? LowerTdsCertificate { get; set; }

        /// <summary>
        /// Lower TDS Rate from certificate
        /// </summary>
        public decimal? LowerTdsRate { get; set; }

        /// <summary>
        /// Certificate validity end date
        /// </summary>
        public DateOnly? LowerTdsCertificateValidTill { get; set; }

        // ==================== MSME Compliance ====================

        /// <summary>
        /// Whether the vendor is registered under MSME Act
        /// </summary>
        public bool MsmeRegistered { get; set; }

        /// <summary>
        /// MSME Registration Number (Udyam Number)
        /// </summary>
        [StringLength(50, ErrorMessage = "MSME Registration Number cannot exceed 50 characters")]
        public string? MsmeRegistrationNumber { get; set; }

        /// <summary>
        /// MSME Category: micro, small, medium
        /// </summary>
        [StringLength(20, ErrorMessage = "MSME Category cannot exceed 20 characters")]
        public string? MsmeCategory { get; set; }

        // ==================== Bank Details ====================

        /// <summary>
        /// Vendor's bank account number
        /// </summary>
        [StringLength(30, ErrorMessage = "Bank Account Number cannot exceed 30 characters")]
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// IFSC Code for NEFT/RTGS
        /// </summary>
        [StringLength(15, ErrorMessage = "IFSC Code cannot exceed 15 characters")]
        public string? BankIfscCode { get; set; }

        /// <summary>
        /// Bank name
        /// </summary>
        [StringLength(100, ErrorMessage = "Bank Name cannot exceed 100 characters")]
        public string? BankName { get; set; }

        /// <summary>
        /// Branch name
        /// </summary>
        [StringLength(100, ErrorMessage = "Bank Branch cannot exceed 100 characters")]
        public string? BankBranch { get; set; }

        /// <summary>
        /// Account holder name
        /// </summary>
        [StringLength(100, ErrorMessage = "Bank Account Holder Name cannot exceed 100 characters")]
        public string? BankAccountHolderName { get; set; }

        // ==================== Default Accounts ====================

        /// <summary>
        /// Default expense account for purchases from this vendor
        /// </summary>
        public Guid? DefaultExpenseAccountId { get; set; }

        /// <summary>
        /// Default payables account
        /// </summary>
        public Guid? DefaultPayableAccountId { get; set; }

        // ==================== Tally Migration Fields ====================

        /// <summary>
        /// Original Tally Ledger GUID
        /// </summary>
        [StringLength(50, ErrorMessage = "Tally Ledger GUID cannot exceed 50 characters")]
        public string? TallyLedgerGuid { get; set; }

        /// <summary>
        /// Original Tally Ledger Name
        /// </summary>
        [StringLength(255, ErrorMessage = "Tally Ledger Name cannot exceed 255 characters")]
        public string? TallyLedgerName { get; set; }
    }
}
