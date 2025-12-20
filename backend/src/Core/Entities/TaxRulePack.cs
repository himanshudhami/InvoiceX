using System.Text.Json;

namespace Core.Entities;

/// <summary>
/// Tax Rule Pack - FY-versioned tax configurations
/// Enables tax rate updates without code changes
/// </summary>
public class TaxRulePack
{
    public Guid Id { get; set; }
    public string PackCode { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;  // e.g., "2025-26"
    public int Version { get; set; } = 1;
    public string? SourceNotification { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";  // draft, active, superseded, archived

    // Tax configurations stored as JSON
    public JsonDocument? IncomeTaxSlabs { get; set; }
    public JsonDocument? StandardDeductions { get; set; }
    public JsonDocument? RebateThresholds { get; set; }
    public JsonDocument? CessRates { get; set; }
    public JsonDocument? SurchargeRates { get; set; }
    public JsonDocument? TdsRates { get; set; }
    public JsonDocument? PfEsiRates { get; set; }
    public JsonDocument? ProfessionalTaxConfig { get; set; }
    public JsonDocument? GstRates { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivatedBy { get; set; }

    // Navigation
    public ICollection<TdsSectionRate>? TdsSectionRates { get; set; }
    public ICollection<RulePackUsageLog>? UsageLogs { get; set; }
}

/// <summary>
/// TDS Section Rates - Detailed TDS section-wise configurations
/// </summary>
public class TdsSectionRate
{
    public Guid Id { get; set; }
    public Guid RulePackId { get; set; }

    public string SectionCode { get; set; } = string.Empty;  // 194J, 194C, etc.
    public string SectionName { get; set; } = string.Empty;

    // Rate configuration
    public decimal RateIndividual { get; set; }
    public decimal? RateCompany { get; set; }
    public decimal? RateNoPan { get; set; }  // Usually 20%

    // Thresholds
    public decimal? ThresholdAmount { get; set; }
    public string ThresholdType { get; set; } = "per_transaction";  // per_transaction, annual

    // Applicability
    public string[]? PayeeTypes { get; set; }  // individual, company, partnership
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TaxRulePack? RulePack { get; set; }
}

/// <summary>
/// Rule Pack Usage Log - Audit trail for tax computations
/// </summary>
public class RulePackUsageLog
{
    public Guid Id { get; set; }
    public Guid RulePackId { get; set; }
    public Guid? CompanyId { get; set; }

    public string ComputationType { get; set; } = string.Empty;  // payroll_tds, contractor_tds, etc.
    public Guid ComputationId { get; set; }
    public DateTime ComputationDate { get; set; }

    // Snapshot of rules used (for audit immutability)
    public JsonDocument? RulesSnapshot { get; set; }

    // Computation results
    public decimal? InputAmount { get; set; }
    public decimal? ComputedTax { get; set; }
    public decimal? EffectiveRate { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public string? ComputedBy { get; set; }

    // Navigation
    public TaxRulePack? RulePack { get; set; }
}

/// <summary>
/// Income tax slab structure for JSON deserialization
/// </summary>
public class IncomeTaxSlab
{
    public decimal Min { get; set; }
    public decimal? Max { get; set; }
    public decimal Rate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// TDS rate configuration for a specific section
/// </summary>
public class TdsRateConfig
{
    public decimal Rate { get; set; }
    public decimal? RateIndividual { get; set; }
    public decimal? RateOther { get; set; }
    public decimal? Threshold { get; set; }
    public decimal? ThresholdSingle { get; set; }
    public decimal? ThresholdAggregate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// PF/ESI rate configuration
/// </summary>
public class PfEsiConfig
{
    public PfConfig? Pf { get; set; }
    public EsiConfig? Esi { get; set; }
}

public class PfConfig
{
    public decimal EmployeeContribution { get; set; }
    public decimal EmployerContribution { get; set; }
    public decimal EmployerEpf { get; set; }
    public decimal EmployerEps { get; set; }
    public decimal AdminCharges { get; set; }
    public decimal Edli { get; set; }
    public decimal WageCeiling { get; set; }
    public bool VoluntaryAboveCeiling { get; set; }
}

public class EsiConfig
{
    public decimal EmployeeContribution { get; set; }
    public decimal EmployerContribution { get; set; }
    public decimal WageCeiling { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

/// <summary>
/// Professional tax slab configuration
/// </summary>
public class ProfessionalTaxSlab
{
    public decimal Min { get; set; }
    public decimal? Max { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// State-wise professional tax configuration
/// </summary>
public class StateProfessionalTaxConfig
{
    public string Name { get; set; } = string.Empty;
    public List<ProfessionalTaxSlab> Slabs { get; set; } = new();
    public decimal? FebruaryAdditional { get; set; }
    public decimal? AnnualMax { get; set; }
    public string? Frequency { get; set; }  // monthly, half_yearly
}
