namespace Core.Entities.Payroll;

/// <summary>
/// Defines salary components and their contribution to PF/ESI/Tax wage bases.
/// Supports company-specific overrides of default components.
/// </summary>
public class SalaryComponent
{
    public Guid Id { get; set; }

    /// <summary>
    /// NULL for default/global components, company UUID for company-specific overrides
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Component code like 'BASIC', 'HRA', 'SPECIAL_ALLOWANCE'
    /// </summary>
    public string ComponentCode { get; set; } = string.Empty;

    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Type: earning, deduction, employer_contribution
    /// </summary>
    public string ComponentType { get; set; } = "earning";

    // ==================== Calculation Flags ====================

    /// <summary>
    /// If TRUE, this component is included in PF wage base calculation
    /// </summary>
    public bool IsPfWage { get; set; }

    /// <summary>
    /// If TRUE, this component is included in ESI wage base calculation
    /// </summary>
    public bool IsEsiWage { get; set; }

    /// <summary>
    /// If TRUE, this component is included in taxable income
    /// </summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// If TRUE, this component is included in PT wage base
    /// </summary>
    public bool IsPtWage { get; set; }

    // ==================== Proration Rules ====================

    /// <summary>
    /// If TRUE, component amount is prorated for LOP/partial months
    /// </summary>
    public bool ApplyProration { get; set; } = true;

    /// <summary>
    /// Basis for proration: calendar_days, working_days, fixed
    /// </summary>
    public string ProrationBasis { get; set; } = "calendar_days";

    // ==================== Display & Payslip ====================

    public int DisplayOrder { get; set; } = 100;

    public bool ShowOnPayslip { get; set; } = true;

    /// <summary>
    /// Group on payslip: 'Earnings', 'Deductions', 'Employer Contributions'
    /// </summary>
    public string? PayslipGroup { get; set; }

    // ==================== Status ====================

    public bool IsActive { get; set; } = true;

    // ==================== Audit ====================

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
