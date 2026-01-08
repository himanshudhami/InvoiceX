namespace Application.DTOs.Party
{
    // ==================== Party DTOs ====================

    public class PartyDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LegalName { get; set; }
        public string? PartyCode { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsVendor { get; set; }
        public bool IsEmployee { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Pincode { get; set; }
        public string Country { get; set; } = "India";
        public string? PanNumber { get; set; }
        public string? Gstin { get; set; }
        public bool IsGstRegistered { get; set; }
        public string? GstStateCode { get; set; }
        public string? PartyType { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string? TallyLedgerGuid { get; set; }
        public string? TallyLedgerName { get; set; }
        public string? TallyGroupName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Profile data
        public PartyVendorProfileDto? VendorProfile { get; set; }
        public PartyCustomerProfileDto? CustomerProfile { get; set; }
        public ICollection<PartyTagDto>? Tags { get; set; }
    }

    public class PartyListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? PartyCode { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsVendor { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Gstin { get; set; }
        public string? PanNumber { get; set; }
        public bool IsActive { get; set; }
        public string? PartyType { get; set; }
        public string? TallyGroupName { get; set; }
    }

    public class CreatePartyDto
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LegalName { get; set; }
        public string? PartyCode { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsVendor { get; set; }
        public bool IsEmployee { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Pincode { get; set; }
        public string Country { get; set; } = "India";
        public string? PanNumber { get; set; }
        public string? Gstin { get; set; }
        public bool IsGstRegistered { get; set; }
        public string? GstStateCode { get; set; }
        public string? PartyType { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }

        // Optional profiles to create alongside the party
        public CreatePartyVendorProfileDto? VendorProfile { get; set; }
        public CreatePartyCustomerProfileDto? CustomerProfile { get; set; }
        public ICollection<Guid>? TagIds { get; set; }
    }

    public class UpdatePartyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? LegalName { get; set; }
        public string? PartyCode { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsVendor { get; set; }
        public bool IsEmployee { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? Pincode { get; set; }
        public string Country { get; set; } = "India";
        public string? PanNumber { get; set; }
        public string? Gstin { get; set; }
        public bool IsGstRegistered { get; set; }
        public string? GstStateCode { get; set; }
        public string? PartyType { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    // ==================== Vendor Profile DTOs ====================

    public class PartyVendorProfileDto
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string? VendorType { get; set; }
        public bool TdsApplicable { get; set; }
        public string? DefaultTdsSection { get; set; }
        public decimal? DefaultTdsRate { get; set; }
        public string? TanNumber { get; set; }
        public string? LowerTdsCertificate { get; set; }
        public decimal? LowerTdsRate { get; set; }
        public DateOnly? LowerTdsValidFrom { get; set; }
        public DateOnly? LowerTdsValidTill { get; set; }
        public bool MsmeRegistered { get; set; }
        public string? MsmeRegistrationNumber { get; set; }
        public string? MsmeCategory { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankIfscCode { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankAccountType { get; set; }
        public Guid? DefaultExpenseAccountId { get; set; }
        public Guid? DefaultPayableAccountId { get; set; }
        public int? PaymentTermsDays { get; set; }
        public decimal? CreditLimit { get; set; }
    }

    public class CreatePartyVendorProfileDto
    {
        public string? VendorType { get; set; }
        public bool TdsApplicable { get; set; }
        public string? DefaultTdsSection { get; set; }
        public decimal? DefaultTdsRate { get; set; }
        public string? TanNumber { get; set; }
        public string? LowerTdsCertificate { get; set; }
        public decimal? LowerTdsRate { get; set; }
        public DateOnly? LowerTdsValidFrom { get; set; }
        public DateOnly? LowerTdsValidTill { get; set; }
        public bool MsmeRegistered { get; set; }
        public string? MsmeRegistrationNumber { get; set; }
        public string? MsmeCategory { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankIfscCode { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankAccountType { get; set; }
        public Guid? DefaultExpenseAccountId { get; set; }
        public Guid? DefaultPayableAccountId { get; set; }
        public int? PaymentTermsDays { get; set; }
        public decimal? CreditLimit { get; set; }
    }

    public class UpdatePartyVendorProfileDto : CreatePartyVendorProfileDto { }

    // ==================== Customer Profile DTOs ====================

    public class PartyCustomerProfileDto
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string? CustomerType { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? PaymentTermsDays { get; set; }
        public Guid? DefaultRevenueAccountId { get; set; }
        public Guid? DefaultReceivableAccountId { get; set; }
        public bool EInvoiceApplicable { get; set; }
        public bool EWayBillApplicable { get; set; }
        public decimal? DefaultDiscountPercent { get; set; }
        public Guid? PriceListId { get; set; }
    }

    public class CreatePartyCustomerProfileDto
    {
        public string? CustomerType { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? PaymentTermsDays { get; set; }
        public Guid? DefaultRevenueAccountId { get; set; }
        public Guid? DefaultReceivableAccountId { get; set; }
        public bool EInvoiceApplicable { get; set; }
        public bool EWayBillApplicable { get; set; }
        public decimal? DefaultDiscountPercent { get; set; }
        public Guid? PriceListId { get; set; }
    }

    public class UpdatePartyCustomerProfileDto : CreatePartyCustomerProfileDto { }

    // ==================== Party Tag DTOs ====================

    public class PartyTagDto
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public Guid TagId { get; set; }
        public string? TagName { get; set; }
        public string? TagGroup { get; set; }
        public string Source { get; set; } = "manual";
        public DateTime CreatedAt { get; set; }
    }

    public class AddPartyTagDto
    {
        public Guid TagId { get; set; }
        public string Source { get; set; } = "manual";
    }

    // ==================== TDS Detection DTOs ====================

    public class TdsConfigurationDto
    {
        public string TdsSection { get; set; } = string.Empty;
        public decimal TdsRate { get; set; }
        public decimal? TdsRateNoPan { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public decimal? SinglePaymentThreshold { get; set; }
        public string? MatchedBy { get; set; }  // "tag", "tally_group", "name_pattern"
        public string? MatchedRuleName { get; set; }
    }

    // ==================== TDS Section Rule DTOs ====================

    public class TdsSectionRuleDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? TagId { get; set; }
        public string? TagName { get; set; }
        public string? PartyNamePattern { get; set; }
        public string? TallyGroupName { get; set; }
        public string TdsSection { get; set; } = string.Empty;
        public decimal TdsRate { get; set; }
        public decimal? TdsRateNoPan { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public decimal? SinglePaymentThreshold { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTdsSectionRuleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? TagId { get; set; }
        public string? PartyNamePattern { get; set; }
        public string? TallyGroupName { get; set; }
        public string TdsSection { get; set; } = string.Empty;
        public decimal TdsRate { get; set; }
        public decimal? TdsRateNoPan { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public decimal? SinglePaymentThreshold { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 100;
    }

    public class UpdateTdsSectionRuleDto : CreateTdsSectionRuleDto { }

    // ==================== Tag-Driven TDS DTOs ====================

    /// <summary>
    /// Result of TDS detection for a party (tag-driven approach)
    /// </summary>
    public class TdsDetectionResultDto
    {
        public Guid PartyId { get; set; }
        public string? PartyName { get; set; }
        public string? Pan { get; set; }

        /// <summary>
        /// Whether TDS is applicable for this party
        /// </summary>
        public bool IsApplicable { get; set; }

        /// <summary>
        /// TDS Section code (194C, 194J, 194H, 194I, 194A, 194Q, 195, 194M, EXEMPT)
        /// </summary>
        public string? TdsSection { get; set; }

        /// <summary>
        /// Sub-clause if applicable (194J(a), 194J(ba), 194I(a), 194I(b))
        /// </summary>
        public string? TdsSectionClause { get; set; }

        /// <summary>
        /// Effective TDS rate based on PAN and entity type
        /// </summary>
        public decimal TdsRate { get; set; }

        /// <summary>
        /// Annual threshold for TDS applicability
        /// </summary>
        public decimal? ThresholdAnnual { get; set; }

        /// <summary>
        /// Single payment threshold
        /// </summary>
        public decimal? ThresholdSinglePayment { get; set; }

        /// <summary>
        /// Whether the payment is below threshold
        /// </summary>
        public bool IsBelowThreshold { get; set; }

        /// <summary>
        /// How TDS was detected: manual, manual_exempt, tag, tag_exempt, no_tag
        /// </summary>
        public string DetectionMethod { get; set; } = "none";

        /// <summary>
        /// ID of the matched TDS section tag
        /// </summary>
        public Guid? MatchedTagId { get; set; }

        /// <summary>
        /// Name of the matched TDS section tag
        /// </summary>
        public string? MatchedTagName { get; set; }

        /// <summary>
        /// Exemption notes from the TDS rule
        /// </summary>
        public string? ExemptionNotes { get; set; }

        /// <summary>
        /// Additional notes about the detection
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// TDS Tag Rule DTO (tag-driven TDS configuration)
    /// </summary>
    public class TdsTagRuleDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid TagId { get; set; }
        public string? TagName { get; set; }
        public string? TagCode { get; set; }
        public string? TagColor { get; set; }

        public string TdsSection { get; set; } = string.Empty;
        public string? TdsSectionClause { get; set; }

        public decimal TdsRateWithPan { get; set; }
        public decimal TdsRateWithoutPan { get; set; }
        public decimal? TdsRateIndividual { get; set; }
        public decimal? TdsRateCompany { get; set; }

        public decimal? ThresholdSinglePayment { get; set; }
        public decimal ThresholdAnnual { get; set; }

        public bool AppliesToIndividual { get; set; }
        public bool AppliesToHuf { get; set; }
        public bool AppliesToCompany { get; set; }
        public bool AppliesToFirm { get; set; }
        public bool AppliesToLlp { get; set; }
        public bool AppliesToTrust { get; set; }
        public bool AppliesToAopBoi { get; set; }
        public bool AppliesToGovernment { get; set; }

        public bool LowerCertificateAllowed { get; set; }
        public bool NilCertificateAllowed { get; set; }
        public string? ExemptionNotes { get; set; }

        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Create TDS Tag Rule DTO
    /// </summary>
    public class CreateTdsTagRuleDto
    {
        public Guid TagId { get; set; }
        public string TdsSection { get; set; } = string.Empty;
        public string? TdsSectionClause { get; set; }
        public decimal TdsRateWithPan { get; set; }
        public decimal? TdsRateWithoutPan { get; set; }
        public decimal? TdsRateIndividual { get; set; }
        public decimal? TdsRateCompany { get; set; }
        public decimal? ThresholdSinglePayment { get; set; }
        public decimal ThresholdAnnual { get; set; }
        public string? ExemptionNotes { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }

    /// <summary>
    /// Update TDS Tag Rule DTO
    /// </summary>
    public class UpdateTdsTagRuleDto
    {
        public string? TdsSection { get; set; }
        public string? TdsSectionClause { get; set; }
        public decimal? TdsRateWithPan { get; set; }
        public decimal? TdsRateWithoutPan { get; set; }
        public decimal? TdsRateIndividual { get; set; }
        public decimal? TdsRateCompany { get; set; }
        public decimal? ThresholdSinglePayment { get; set; }
        public decimal? ThresholdAnnual { get; set; }
        public string? ExemptionNotes { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool? IsActive { get; set; }
    }
}
