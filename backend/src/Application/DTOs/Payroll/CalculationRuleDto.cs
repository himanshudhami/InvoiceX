namespace Application.DTOs.Payroll;

/// <summary>
/// DTO for reading calculation rules
/// </summary>
public class CalculationRuleDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public string ComponentCode { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string FormulaConfig { get; set; } = "{}";
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public bool IsTaxable { get; set; }
    public bool AffectsPfWage { get; set; }
    public bool AffectsEsiWage { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public string? CompanyName { get; set; }
    public List<CalculationRuleConditionDto> Conditions { get; set; } = new();
}

/// <summary>
/// DTO for creating calculation rules
/// </summary>
public class CreateCalculationRuleDto
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ComponentType { get; set; } = "earning";
    public string ComponentCode { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string RuleType { get; set; } = "percentage";
    public string FormulaConfig { get; set; } = "{}";
    public int Priority { get; set; } = 100;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool AffectsPfWage { get; set; } = false;
    public bool AffectsEsiWage { get; set; } = false;
    public List<CreateCalculationRuleConditionDto> Conditions { get; set; } = new();
}

/// <summary>
/// DTO for updating calculation rules
/// </summary>
public class UpdateCalculationRuleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ComponentType { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string? RuleType { get; set; }
    public string? FormulaConfig { get; set; }
    public int? Priority { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsTaxable { get; set; }
    public bool? AffectsPfWage { get; set; }
    public bool? AffectsEsiWage { get; set; }
    public List<CreateCalculationRuleConditionDto>? Conditions { get; set; }
}

/// <summary>
/// DTO for calculation rule conditions
/// </summary>
public class CalculationRuleConditionDto
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public int ConditionGroup { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = "{}";
}

/// <summary>
/// DTO for creating calculation rule conditions
/// </summary>
public class CreateCalculationRuleConditionDto
{
    public int ConditionGroup { get; set; } = 1;
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public string Value { get; set; } = "{}";
}

/// <summary>
/// DTO for formula variables
/// </summary>
public class FormulaVariableDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}

/// <summary>
/// DTO for calculation rule templates
/// </summary>
public class CalculationRuleTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string ComponentCode { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string FormulaConfig { get; set; } = "{}";
    public string? DefaultConditions { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for validating a formula expression
/// </summary>
public class ValidateFormulaDto
{
    public string Expression { get; set; } = string.Empty;
    public Dictionary<string, decimal>? SampleValues { get; set; }
}

/// <summary>
/// Result of formula validation
/// </summary>
public class FormulaValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? SampleResult { get; set; }
    public List<string> UsedVariables { get; set; } = new();
    public List<string> UnknownVariables { get; set; } = new();
}

/// <summary>
/// DTO for previewing a rule calculation
/// </summary>
public class PreviewRuleCalculationDto
{
    public Guid? RuleId { get; set; }
    public CreateCalculationRuleDto? Rule { get; set; }
    public Guid? EmployeeId { get; set; }
    public Dictionary<string, decimal>? CustomValues { get; set; }
}

/// <summary>
/// Result of rule calculation preview
/// </summary>
public class RuleCalculationPreviewDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Result { get; set; }
    public Dictionary<string, decimal> InputValues { get; set; } = new();
    public List<CalculationStepDto> Steps { get; set; } = new();
}

/// <summary>
/// A step in the calculation process for debugging/preview
/// </summary>
public class CalculationStepDto
{
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
