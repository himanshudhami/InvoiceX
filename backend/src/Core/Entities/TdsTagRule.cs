namespace Core.Entities
{
    /// <summary>
    /// TDS configuration linked to tags (tag-driven TDS system)
    /// Tags drive TDS behavior instead of hard-coded vendor types
    /// Rates are per FY 2024-25 (CBDT circular)
    /// </summary>
    public class TdsTagRule
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid TagId { get; set; }

        // ==================== TDS Section Info ====================

        /// <summary>
        /// TDS Section code per Income Tax Act, 1961
        /// Values: 194C, 194J, 194H, 194I, 194A, 194Q, 195, 194M, EXEMPT
        /// </summary>
        public string TdsSection { get; set; } = string.Empty;

        /// <summary>
        /// Sub-clause if applicable
        /// e.g., 194J(a), 194J(ba), 194I(a), 194I(b)
        /// </summary>
        public string? TdsSectionClause { get; set; }

        // ==================== Rate Configuration ====================

        /// <summary>
        /// TDS rate when valid PAN is provided
        /// </summary>
        public decimal TdsRateWithPan { get; set; }

        /// <summary>
        /// TDS rate when PAN not provided (Section 206AA)
        /// Typically 20% for most sections
        /// </summary>
        public decimal TdsRateWithoutPan { get; set; } = 20.00m;

        /// <summary>
        /// Specific rate for individuals (PAN 4th char = P)
        /// e.g., 194C: 1% for individuals
        /// </summary>
        public decimal? TdsRateIndividual { get; set; }

        /// <summary>
        /// Specific rate for companies (PAN 4th char = C)
        /// e.g., 194C: 2% for companies
        /// </summary>
        public decimal? TdsRateCompany { get; set; }

        // ==================== Thresholds ====================

        /// <summary>
        /// Per payment threshold
        /// e.g., 194C: Rs 30,000 per payment
        /// </summary>
        public decimal? ThresholdSinglePayment { get; set; }

        /// <summary>
        /// Annual aggregate threshold
        /// e.g., 194C: Rs 1,00,000/year, 194J: Rs 50,000/year
        /// </summary>
        public decimal ThresholdAnnual { get; set; }

        // ==================== Applicability by Entity Type ====================

        /// <summary>
        /// Applies to Individual (PAN 4th char = P)
        /// </summary>
        public bool AppliesToIndividual { get; set; } = true;

        /// <summary>
        /// Applies to HUF (PAN 4th char = H)
        /// </summary>
        public bool AppliesToHuf { get; set; } = true;

        /// <summary>
        /// Applies to Company (PAN 4th char = C)
        /// </summary>
        public bool AppliesToCompany { get; set; } = true;

        /// <summary>
        /// Applies to Firm (PAN 4th char = F)
        /// </summary>
        public bool AppliesToFirm { get; set; } = true;

        /// <summary>
        /// Applies to LLP (PAN 4th char = L)
        /// </summary>
        public bool AppliesToLlp { get; set; } = true;

        /// <summary>
        /// Applies to Trust (PAN 4th char = T)
        /// </summary>
        public bool AppliesToTrust { get; set; } = true;

        /// <summary>
        /// Applies to AOP/BOI (PAN 4th char = A/B)
        /// </summary>
        public bool AppliesToAopBoi { get; set; } = true;

        /// <summary>
        /// Applies to Government (PAN 4th char = G)
        /// Usually exempt from TDS
        /// </summary>
        public bool AppliesToGovernment { get; set; } = false;

        // ==================== Special Provisions ====================

        /// <summary>
        /// Whether Form 13 lower TDS certificate is allowed
        /// </summary>
        public bool LowerCertificateAllowed { get; set; } = true;

        /// <summary>
        /// Whether nil deduction certificate is allowed
        /// </summary>
        public bool NilCertificateAllowed { get; set; } = true;

        /// <summary>
        /// Exemption notes and special provisions
        /// </summary>
        public string? ExemptionNotes { get; set; }

        // ==================== Validity Period ====================

        /// <summary>
        /// Rule effective from date (typically FY start: 2024-04-01)
        /// </summary>
        public DateOnly EffectiveFrom { get; set; } = new DateOnly(2024, 4, 1);

        /// <summary>
        /// Rule effective to date (NULL = currently effective)
        /// </summary>
        public DateOnly? EffectiveTo { get; set; }

        // ==================== Status ====================

        public bool IsActive { get; set; } = true;

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation ====================

        public Tags.Tag? Tag { get; set; }

        // ==================== Helper Methods ====================

        /// <summary>
        /// Get the appropriate TDS rate based on PAN entity type
        /// </summary>
        public decimal GetRateForPan(string? pan)
        {
            if (string.IsNullOrEmpty(pan) || pan.Length < 4)
                return TdsRateWithoutPan;

            var entityType = pan[3];
            return entityType switch
            {
                'P' => TdsRateIndividual ?? TdsRateWithPan,  // Individual
                'H' => TdsRateIndividual ?? TdsRateWithPan,  // HUF
                'C' => TdsRateCompany ?? TdsRateWithPan,     // Company
                'F' => TdsRateCompany ?? TdsRateWithPan,     // Firm
                'L' => TdsRateCompany ?? TdsRateWithPan,     // LLP
                'T' => TdsRateCompany ?? TdsRateWithPan,     // Trust
                'A' => TdsRateCompany ?? TdsRateWithPan,     // AOP
                'B' => TdsRateCompany ?? TdsRateWithPan,     // BOI
                'G' => 0,                                     // Government (usually exempt)
                _ => TdsRateWithPan
            };
        }

        /// <summary>
        /// Check if rule applies to given PAN entity type
        /// </summary>
        public bool AppliesToPan(string? pan)
        {
            if (string.IsNullOrEmpty(pan) || pan.Length < 4)
                return true; // Default: applies

            var entityType = pan[3];
            return entityType switch
            {
                'P' => AppliesToIndividual,
                'H' => AppliesToHuf,
                'C' => AppliesToCompany,
                'F' => AppliesToFirm,
                'L' => AppliesToLlp,
                'T' => AppliesToTrust,
                'A' => AppliesToAopBoi,
                'B' => AppliesToAopBoi,
                'G' => AppliesToGovernment,
                _ => true
            };
        }

        /// <summary>
        /// Check if the rule is currently effective
        /// </summary>
        public bool IsCurrentlyEffective()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return IsActive &&
                   EffectiveFrom <= today &&
                   (EffectiveTo == null || EffectiveTo >= today);
        }
    }
}
