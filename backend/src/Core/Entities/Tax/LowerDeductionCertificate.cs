namespace Core.Entities.Tax
{
    /// <summary>
    /// Form 13 Lower/NIL TDS Deduction Certificate tracking per Section 197.
    /// Deductees can obtain certificates from AO for lower or nil TDS deduction.
    /// Must be verified before applying reduced rates on payments.
    /// </summary>
    public class LowerDeductionCertificate
    {
        public Guid Id { get; set; }

        // ==================== Company Linking ====================

        public Guid CompanyId { get; set; }

        // ==================== Certificate Details ====================

        /// <summary>
        /// Certificate number issued by IT department
        /// </summary>
        public string CertificateNumber { get; set; } = string.Empty;

        public DateOnly CertificateDate { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }

        /// <summary>
        /// Financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        // ==================== Certificate Type ====================

        /// <summary>
        /// Certificate type: 'lower' (reduced rate) or 'nil' (no deduction)
        /// </summary>
        public string CertificateType { get; set; } = string.Empty;

        // ==================== Deductee Details ====================

        /// <summary>
        /// Deductee type: vendor, contractor, landlord, other
        /// </summary>
        public string DeducteeType { get; set; } = string.Empty;

        /// <summary>
        /// Reference to vendor/contractor table
        /// </summary>
        public Guid? DeducteeId { get; set; }

        public string DeducteeName { get; set; } = string.Empty;

        /// <summary>
        /// PAN of deductee - required for certificate validation
        /// </summary>
        public string DeducteePan { get; set; } = string.Empty;

        public string? DeducteeAddress { get; set; }

        // ==================== TDS Section Details ====================

        /// <summary>
        /// TDS section for which certificate is issued: 194C, 194J, 194I, etc.
        /// </summary>
        public string TdsSection { get; set; } = string.Empty;

        /// <summary>
        /// Normal TDS rate without certificate (percentage)
        /// </summary>
        public decimal NormalRate { get; set; }

        /// <summary>
        /// Rate as per certificate (0 for NIL)
        /// </summary>
        public decimal CertificateRate { get; set; }

        // ==================== Limits ====================

        /// <summary>
        /// Maximum amount covered by the certificate
        /// </summary>
        public decimal? ThresholdAmount { get; set; }

        /// <summary>
        /// Amount already utilized against this certificate
        /// </summary>
        public decimal UtilizedAmount { get; set; }

        // ==================== Issuing Authority ====================

        public string? AssessingOfficer { get; set; }
        public string? AoDesignation { get; set; }
        public string? AoOfficeAddress { get; set; }

        // ==================== Document Storage ====================

        /// <summary>
        /// Reference to attachments table for certificate document
        /// </summary>
        public Guid? CertificateDocumentId { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Status: active, expired, revoked, exhausted
        /// </summary>
        public string Status { get; set; } = "active";

        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }

        // ==================== Notes ====================

        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Remaining threshold amount (if threshold exists)
        /// </summary>
        public decimal? RemainingThreshold => ThresholdAmount.HasValue
            ? ThresholdAmount.Value - UtilizedAmount
            : null;

        /// <summary>
        /// Utilization percentage (if threshold exists)
        /// </summary>
        public decimal? UtilizationPercentage => ThresholdAmount.HasValue && ThresholdAmount.Value > 0
            ? Math.Round((UtilizedAmount / ThresholdAmount.Value) * 100, 2)
            : null;

        /// <summary>
        /// Check if certificate is currently valid
        /// </summary>
        public bool IsValid(DateOnly asOfDate) =>
            Status == LdcStatus.Active &&
            asOfDate >= ValidFrom &&
            asOfDate <= ValidTo &&
            (!ThresholdAmount.HasValue || UtilizedAmount < ThresholdAmount.Value);
    }

    /// <summary>
    /// LDC certificate types
    /// </summary>
    public static class LdcCertificateType
    {
        public const string Lower = "lower";
        public const string Nil = "nil";
    }

    /// <summary>
    /// LDC deductee types
    /// </summary>
    public static class LdcDeducteeType
    {
        public const string Vendor = "vendor";
        public const string Contractor = "contractor";
        public const string Landlord = "landlord";
        public const string Other = "other";
    }

    /// <summary>
    /// LDC status values
    /// </summary>
    public static class LdcStatus
    {
        public const string Active = "active";
        public const string Expired = "expired";
        public const string Revoked = "revoked";
        public const string Exhausted = "exhausted";
    }
}
