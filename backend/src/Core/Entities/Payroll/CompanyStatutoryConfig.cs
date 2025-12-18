namespace Core.Entities.Payroll
{
    /// <summary>
    /// Company-specific statutory configuration for PF, ESI, PT, and TDS
    /// </summary>
    public class CompanyStatutoryConfig
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== PF Configuration ====================

        /// <summary>
        /// Whether PF is enabled for this company
        /// </summary>
        public bool PfEnabled { get; set; } = true;

        /// <summary>
        /// PF establishment registration number
        /// </summary>
        public string? PfRegistrationNumber { get; set; }

        /// <summary>
        /// Employee PF contribution rate (default 12%)
        /// </summary>
        public decimal PfEmployeeRate { get; set; } = 12.00m;

        /// <summary>
        /// Employer PF contribution rate (default 12%)
        /// </summary>
        public decimal PfEmployerRate { get; set; } = 12.00m;

        /// <summary>
        /// PF admin charges rate
        /// </summary>
        public decimal PfAdminChargesRate { get; set; } = 0.50m;

        /// <summary>
        /// EDLI contribution rate
        /// </summary>
        public decimal PfEdliRate { get; set; } = 0.50m;

        /// <summary>
        /// PF wage ceiling (currently ₹15,000)
        /// </summary>
        public decimal PfWageCeiling { get; set; } = 15000.00m;

        /// <summary>
        /// Whether to include special allowance in PF wage calculation
        /// </summary>
        public bool PfIncludeSpecialAllowance { get; set; } = false;

        /// <summary>
        /// PF calculation mode:
        /// - ceiling_based: 12% of PF wage capped at ceiling (default)
        /// - actual_wage: 12% of actual PF wage (no ceiling)
        /// - restricted_pf: For employees earning >15k who opt for lower PF
        /// </summary>
        public string PfCalculationMode { get; set; } = "ceiling_based";

        /// <summary>
        /// PF trust type:
        /// - epfo: Government EPFO (default)
        /// - private_trust: Private PF trust
        /// </summary>
        public string PfTrustType { get; set; } = "epfo";

        /// <summary>
        /// Name of the private PF trust (if pf_trust_type = 'private_trust')
        /// </summary>
        public string? PfTrustName { get; set; }

        /// <summary>
        /// Registration number of the private PF trust
        /// </summary>
        public string? PfTrustRegistrationNumber { get; set; }

        /// <summary>
        /// Maximum wage for restricted PF option (used when pf_calculation_mode = 'restricted_pf')
        /// </summary>
        public decimal RestrictedPfMaxWage { get; set; } = 15000.00m;

        // ==================== ESI Configuration ====================

        /// <summary>
        /// Whether ESI is enabled for this company
        /// </summary>
        public bool EsiEnabled { get; set; } = false;

        /// <summary>
        /// ESI registration number
        /// </summary>
        public string? EsiRegistrationNumber { get; set; }

        /// <summary>
        /// Employee ESI contribution rate (default 0.75%)
        /// </summary>
        public decimal EsiEmployeeRate { get; set; } = 0.75m;

        /// <summary>
        /// Employer ESI contribution rate (default 3.25%)
        /// </summary>
        public decimal EsiEmployerRate { get; set; } = 3.25m;

        /// <summary>
        /// Gross salary ceiling for ESI applicability (currently ₹21,000)
        /// </summary>
        public decimal EsiGrossCeiling { get; set; } = 21000.00m;

        // ==================== PT Configuration ====================

        /// <summary>
        /// Whether Professional Tax is enabled
        /// </summary>
        public bool PtEnabled { get; set; } = true;

        /// <summary>
        /// State for PT slab lookup
        /// </summary>
        public string PtState { get; set; } = "Karnataka";

        /// <summary>
        /// PT registration number
        /// </summary>
        public string? PtRegistrationNumber { get; set; }

        // ==================== TDS Configuration ====================

        /// <summary>
        /// Company TAN number for TDS
        /// </summary>
        public string? TanNumber { get; set; }

        /// <summary>
        /// Default tax regime for new employees
        /// </summary>
        public string DefaultTaxRegime { get; set; } = "new";

        // ==================== Gratuity Configuration ====================

        /// <summary>
        /// Whether gratuity provision is enabled
        /// </summary>
        public bool GratuityEnabled { get; set; } = true;

        /// <summary>
        /// Gratuity provision rate (default 4.81%)
        /// </summary>
        public decimal GratuityRate { get; set; } = 4.81m;

        // ==================== Effective Dating ====================

        /// <summary>
        /// Date from which this configuration is effective
        /// </summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Date until which this configuration is effective (null = current)
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        // ==================== Metadata ====================

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation property
        public Companies? Company { get; set; }
    }
}
