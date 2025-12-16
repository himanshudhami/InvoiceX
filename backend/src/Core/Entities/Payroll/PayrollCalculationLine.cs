namespace Core.Entities.Payroll;

/// <summary>
/// Stores detailed calculation breakdown for each payroll transaction.
/// Provides complete auditability per Rule 9.
/// </summary>
public class PayrollCalculationLine
{
    public Guid Id { get; set; }

    /// <summary>
    /// Link to the parent payroll transaction
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Type: earning, deduction, employer_contribution, statutory
    /// </summary>
    public string LineType { get; set; } = "earning";

    /// <summary>
    /// Sequence for ordering within a type
    /// </summary>
    public int LineSequence { get; set; }

    // ==================== Rule Identification ====================

    /// <summary>
    /// Unique rule code like 'TDS_192', 'PF_EMPLOYEE_12', 'ESI_EMPLOYEE_075', 'PT_KARNATAKA'
    /// </summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // ==================== Calculation Details ====================

    /// <summary>
    /// Base amount the calculation was applied to (e.g., PF wage base)
    /// </summary>
    public decimal? BaseAmount { get; set; }

    /// <summary>
    /// Rate/percentage applied (e.g., 12.0000 for 12%)
    /// NULL for fixed amounts
    /// </summary>
    public decimal? Rate { get; set; }

    /// <summary>
    /// The calculated result
    /// </summary>
    public decimal ComputedAmount { get; set; }

    // ==================== Configuration Audit Trail ====================

    /// <summary>
    /// Version identifier for the configuration (e.g., 'FY_2024-25')
    /// </summary>
    public string? ConfigVersion { get; set; }

    /// <summary>
    /// JSON snapshot of parameters used for reproducibility
    /// </summary>
    public string? ConfigSnapshot { get; set; }

    /// <summary>
    /// Additional notes about the calculation
    /// </summary>
    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Navigation property
    public PayrollTransaction? Transaction { get; set; }
}
