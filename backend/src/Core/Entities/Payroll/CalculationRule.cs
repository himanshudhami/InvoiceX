using Core.Entities;

namespace Core.Entities.Payroll;

/// <summary>
/// Represents a configurable calculation rule for payroll components.
/// Rules can be percentage-based, fixed amounts, slab-based, or custom formulas.
/// </summary>
public class CalculationRule
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    // Rule identification
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // What this rule calculates
    public string ComponentType { get; set; } = "earning"; // earning, deduction, employer_contribution
    public string ComponentCode { get; set; } = string.Empty; // PF_EMPLOYEE, HRA, GRATUITY, etc.
    public string? ComponentName { get; set; } // Display name for custom components

    // Calculation configuration (stored as JSON)
    public string RuleType { get; set; } = "percentage"; // percentage, fixed, slab, formula
    public string FormulaConfig { get; set; } = "{}"; // JSON configuration

    // Rule priority (lower = higher priority)
    public int Priority { get; set; } = 100;

    // Validity period
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveTo { get; set; }

    // Flags
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public bool IsTaxable { get; set; } = true;
    public bool AffectsPfWage { get; set; } = false;
    public bool AffectsEsiWage { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public Companies? Company { get; set; }
    public ICollection<CalculationRuleCondition> Conditions { get; set; } = new List<CalculationRuleCondition>();
}

/// <summary>
/// Represents a condition that determines when a calculation rule applies.
/// Multiple conditions can be grouped using AND/OR logic.
/// </summary>
public class CalculationRuleCondition
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }

    // Condition grouping (conditions in same group are AND'd, different groups are OR'd)
    public int ConditionGroup { get; set; } = 1;

    // Condition definition
    public string Field { get; set; } = string.Empty; // department, grade, basic_salary, age, etc.
    public string Operator { get; set; } = "equals"; // equals, greater_than, between, in, etc.
    public string Value { get; set; } = "{}"; // JSON value

    public DateTime CreatedAt { get; set; }

    // Navigation
    public CalculationRule? Rule { get; set; }
}

/// <summary>
/// Represents a variable available for use in calculation formulas.
/// </summary>
public class FormulaVariable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = "decimal"; // decimal, integer, boolean, string
    public string Source { get; set; } = string.Empty; // salary_structure, payroll_info, employee, etc.
    public string? SourceField { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Pre-built template for common calculation scenarios.
/// </summary>
public class CalculationRuleTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty; // statutory, allowance, deduction, benefit
    public string ComponentType { get; set; } = string.Empty;
    public string ComponentCode { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string FormulaConfig { get; set; } = "{}";
    public string? DefaultConditions { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 100;
    public DateTime CreatedAt { get; set; }
}
