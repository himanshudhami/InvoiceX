namespace Core.Entities
{
    /// <summary>
    /// Unified Party entity - single source of truth for customers, vendors, and employees
    /// Inspired by SAP S/4HANA Business Partner and Odoo res.partner models
    /// A party can have multiple roles (is_customer, is_vendor, is_employee)
    /// </summary>
    public class Party
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Core Identity ====================

        /// <summary>
        /// Primary name of the party
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short name for display purposes
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Registered legal name (as per PAN/GST records)
        /// </summary>
        public string? LegalName { get; set; }

        /// <summary>
        /// Internal reference code (auto-generated or manual)
        /// </summary>
        public string? PartyCode { get; set; }

        // ==================== Role Flags ====================

        /// <summary>
        /// Party can receive invoices (customer role)
        /// </summary>
        public bool IsCustomer { get; set; }

        /// <summary>
        /// Party can send invoices (vendor role)
        /// </summary>
        public bool IsVendor { get; set; }

        /// <summary>
        /// Party is an employee (for payroll)
        /// </summary>
        public bool IsEmployee { get; set; }

        // ==================== Contact Information ====================

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }

        // ==================== Address (Primary) ====================

        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        /// <summary>
        /// GST State Code (e.g., '29' for Karnataka)
        /// </summary>
        public string? StateCode { get; set; }

        public string? Pincode { get; set; }
        public string Country { get; set; } = "India";

        // ==================== Indian Tax Identifiers ====================

        /// <summary>
        /// PAN Number (10 characters)
        /// Format: AAAAA1234A
        /// </summary>
        public string? PanNumber { get; set; }

        /// <summary>
        /// GST Identification Number (15 characters)
        /// Format: 29AAAAA1234A1Z5
        /// </summary>
        public string? Gstin { get; set; }

        /// <summary>
        /// Whether the party is GST registered
        /// </summary>
        public bool IsGstRegistered { get; set; }

        /// <summary>
        /// First 2 characters of GSTIN (state code)
        /// </summary>
        public string? GstStateCode { get; set; }

        // ==================== Classification ====================

        /// <summary>
        /// Legal entity type: individual, company, firm, llp, trust, huf, government, foreign
        /// </summary>
        public string? PartyType { get; set; }

        // ==================== Status ====================

        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }

        // ==================== Tally Migration Tracking ====================

        /// <summary>
        /// Original Tally Ledger GUID from migration
        /// </summary>
        public string? TallyLedgerGuid { get; set; }

        /// <summary>
        /// Original Tally Ledger Name at time of import
        /// </summary>
        public string? TallyLedgerName { get; set; }

        /// <summary>
        /// Original Tally ledger group (e.g., 'CONSULTANTS', 'Sundry Creditors')
        /// Used for TDS rule matching
        /// </summary>
        public string? TallyGroupName { get; set; }

        /// <summary>
        /// Migration batch that imported this record
        /// </summary>
        public Guid? TallyMigrationBatchId { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Vendor-specific profile (TDS, MSME, Bank details)
        /// Only populated when IsVendor = true
        /// </summary>
        public PartyVendorProfile? VendorProfile { get; set; }

        /// <summary>
        /// Customer-specific profile (Credit terms, E-invoicing)
        /// Only populated when IsCustomer = true
        /// </summary>
        public PartyCustomerProfile? CustomerProfile { get; set; }

        /// <summary>
        /// Tags associated with this party for classification
        /// </summary>
        public ICollection<PartyTag>? Tags { get; set; }
    }
}
