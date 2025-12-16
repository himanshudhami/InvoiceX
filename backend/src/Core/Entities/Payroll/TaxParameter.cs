namespace Core.Entities.Payroll;

/// <summary>
/// Tax calculation parameter for a specific financial year and regime.
/// Enables parameterization of tax rules without code changes (Rule 5 - Parameterization).
/// </summary>
public class TaxParameter
{
    public Guid Id { get; set; }

    /// <summary>
    /// Financial year e.g., '2024-25'
    /// </summary>
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>
    /// Tax regime: 'old', 'new', or 'both' (applies to both regimes)
    /// </summary>
    public string Regime { get; set; } = "new";

    /// <summary>
    /// Parameter code e.g., 'STANDARD_DEDUCTION', 'CESS_RATE', 'REBATE_87A_THRESHOLD'
    /// </summary>
    public string ParameterCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable parameter name
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// The parameter value (amount, rate, or threshold)
    /// </summary>
    public decimal ParameterValue { get; set; }

    /// <summary>
    /// Type of parameter: 'amount', 'percentage', 'threshold'
    /// </summary>
    public string ParameterType { get; set; } = "amount";

    /// <summary>
    /// Description of the parameter
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Legal reference (e.g., Section number)
    /// </summary>
    public string? LegalReference { get; set; }

    /// <summary>
    /// Date from which this parameter is effective
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date until which this parameter is effective (null = current)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Whether this parameter is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
