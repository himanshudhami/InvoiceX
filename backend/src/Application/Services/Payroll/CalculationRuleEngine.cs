using System.Text.Json;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Engine for evaluating calculation rules against employee data.
/// Finds applicable rules, evaluates them in priority order, and returns results.
/// </summary>
public class CalculationRuleEngine
{
    private readonly ICalculationRuleRepository _ruleRepository;
    private readonly IFormulaVariableRepository _variableRepository;

    public CalculationRuleEngine(
        ICalculationRuleRepository ruleRepository,
        IFormulaVariableRepository variableRepository)
    {
        _ruleRepository = ruleRepository;
        _variableRepository = variableRepository;
    }

    /// <summary>
    /// Calculate a specific component for an employee using the rules engine
    /// </summary>
    public async Task<RuleCalculationResult> CalculateComponentAsync(
        Guid companyId,
        string componentCode,
        Dictionary<string, decimal> variables,
        EmployeeContext? employeeContext = null)
    {
        // Get all active rules for this component
        var rules = await _ruleRepository.GetActiveRulesByComponentAsync(companyId, componentCode);
        var rulesList = rules.ToList();

        if (!rulesList.Any())
        {
            return new RuleCalculationResult
            {
                Success = true,
                Result = 0,
                RuleUsed = null,
                Message = $"No active rules found for component {componentCode}"
            };
        }

        // Find the first matching rule (rules are already sorted by priority)
        foreach (var rule in rulesList)
        {
            // Load conditions if not already loaded
            if (rule.Conditions == null || !rule.Conditions.Any())
            {
                var conditions = await _ruleRepository.GetConditionsByRuleIdAsync(rule.Id);
                rule.Conditions = conditions.ToList();
            }

            // Check if conditions match
            if (EvaluateConditions(rule.Conditions, variables, employeeContext))
            {
                try
                {
                    var result = EvaluateRule(rule, variables);
                    return new RuleCalculationResult
                    {
                        Success = true,
                        Result = result.Value,
                        RuleUsed = rule,
                        Steps = result.Steps,
                        InputValues = variables
                    };
                }
                catch (Exception ex)
                {
                    return new RuleCalculationResult
                    {
                        Success = false,
                        Result = 0,
                        RuleUsed = rule,
                        Message = $"Error evaluating rule '{rule.Name}': {ex.Message}"
                    };
                }
            }
        }

        return new RuleCalculationResult
        {
            Success = true,
            Result = 0,
            Message = "No matching rules found for the given conditions"
        };
    }

    /// <summary>
    /// Preview a rule calculation without saving
    /// </summary>
    public RuleEvaluationResult PreviewRule(CalculationRule rule, Dictionary<string, decimal> variables)
    {
        return EvaluateRule(rule, variables);
    }

    /// <summary>
    /// Validate a formula expression
    /// </summary>
    public FormulaValidationResult ValidateFormula(string expression, Dictionary<string, decimal>? sampleValues = null)
    {
        var evaluator = new FormulaEvaluator(sampleValues ?? new Dictionary<string, decimal>());
        return evaluator.Validate(expression);
    }

    /// <summary>
    /// Get all available variables with their descriptions
    /// </summary>
    public async Task<IEnumerable<FormulaVariable>> GetAvailableVariablesAsync()
    {
        return await _variableRepository.GetActiveAsync();
    }

    // ===================== Private Methods =====================

    private bool EvaluateConditions(
        IEnumerable<CalculationRuleCondition> conditions,
        Dictionary<string, decimal> variables,
        EmployeeContext? employeeContext)
    {
        var conditionsList = conditions.ToList();

        // No conditions = always matches
        if (!conditionsList.Any())
            return true;

        // Group conditions by condition_group
        var groups = conditionsList.GroupBy(c => c.ConditionGroup).ToList();

        // Each group is AND'd internally, groups are OR'd together
        // If ANY group passes, the rule matches
        foreach (var group in groups)
        {
            var allConditionsInGroupMatch = true;

            foreach (var condition in group)
            {
                if (!EvaluateSingleCondition(condition, variables, employeeContext))
                {
                    allConditionsInGroupMatch = false;
                    break;
                }
            }

            if (allConditionsInGroupMatch)
                return true;
        }

        return false;
    }

    private bool EvaluateSingleCondition(
        CalculationRuleCondition condition,
        Dictionary<string, decimal> variables,
        EmployeeContext? employeeContext)
    {
        try
        {
            var valueDoc = JsonDocument.Parse(condition.Value);
            var root = valueDoc.RootElement;

            // Get the field value to compare
            var fieldValue = GetFieldValue(condition.Field, variables, employeeContext);
            if (fieldValue == null)
                return false;

            return condition.Operator.ToLowerInvariant() switch
            {
                "equals" => CompareEquals(fieldValue, root),
                "not_equals" => !CompareEquals(fieldValue, root),
                "greater_than" => CompareGreaterThan(fieldValue, root),
                "less_than" => CompareLessThan(fieldValue, root),
                "greater_than_or_equals" => CompareGreaterThanOrEquals(fieldValue, root),
                "less_than_or_equals" => CompareLessThanOrEquals(fieldValue, root),
                "between" => CompareBetween(fieldValue, root),
                "in" => CompareIn(fieldValue, root),
                "not_in" => !CompareIn(fieldValue, root),
                "contains" => CompareContains(fieldValue, root),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private object? GetFieldValue(
        string field,
        Dictionary<string, decimal> variables,
        EmployeeContext? employeeContext)
    {
        // First check numeric variables
        if (variables.TryGetValue(field, out var numericValue))
            return numericValue;

        // Then check employee context
        if (employeeContext != null)
        {
            return field.ToLowerInvariant() switch
            {
                "department" => employeeContext.Department,
                "designation" => employeeContext.Designation,
                "grade" => employeeContext.Grade,
                "location" => employeeContext.Location,
                "payroll_type" => employeeContext.PayrollType,
                "age" => employeeContext.Age,
                "tenure_years" => employeeContext.TenureYears,
                "tenure_months" => employeeContext.TenureMonths,
                _ => null
            };
        }

        return null;
    }

    private bool CompareEquals(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is decimal d && valueToCompare.ValueKind == JsonValueKind.Number)
            return d == valueToCompare.GetDecimal();

        if (fieldValue is string s && valueToCompare.ValueKind == JsonValueKind.String)
            return s.Equals(valueToCompare.GetString(), StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private bool CompareGreaterThan(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is decimal d && valueToCompare.ValueKind == JsonValueKind.Number)
            return d > valueToCompare.GetDecimal();

        return false;
    }

    private bool CompareLessThan(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is decimal d && valueToCompare.ValueKind == JsonValueKind.Number)
            return d < valueToCompare.GetDecimal();

        return false;
    }

    private bool CompareGreaterThanOrEquals(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is decimal d && valueToCompare.ValueKind == JsonValueKind.Number)
            return d >= valueToCompare.GetDecimal();

        return false;
    }

    private bool CompareLessThanOrEquals(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is decimal d && valueToCompare.ValueKind == JsonValueKind.Number)
            return d <= valueToCompare.GetDecimal();

        return false;
    }

    private bool CompareBetween(object fieldValue, JsonElement conditionValue)
    {
        if (fieldValue is not decimal d)
            return false;

        if (conditionValue.TryGetProperty("min", out var min) &&
            conditionValue.TryGetProperty("max", out var max))
        {
            return d >= min.GetDecimal() && d <= max.GetDecimal();
        }

        return false;
    }

    private bool CompareIn(object fieldValue, JsonElement conditionValue)
    {
        var values = conditionValue.TryGetProperty("values", out var v) ? v : conditionValue;

        if (values.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var item in values.EnumerateArray())
        {
            if (fieldValue is decimal d && item.ValueKind == JsonValueKind.Number && d == item.GetDecimal())
                return true;

            if (fieldValue is string s && item.ValueKind == JsonValueKind.String &&
                s.Equals(item.GetString(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool CompareContains(object fieldValue, JsonElement conditionValue)
    {
        var valueToCompare = conditionValue.TryGetProperty("value", out var v) ? v : conditionValue;

        if (fieldValue is string s && valueToCompare.ValueKind == JsonValueKind.String)
            return s.Contains(valueToCompare.GetString()!, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private RuleEvaluationResult EvaluateRule(CalculationRule rule, Dictionary<string, decimal> variables)
    {
        var steps = new List<CalculationStep>();
        var config = JsonDocument.Parse(rule.FormulaConfig).RootElement;

        decimal result = rule.RuleType.ToLowerInvariant() switch
        {
            "percentage" => EvaluatePercentageRule(config, variables, steps),
            "fixed" => EvaluateFixedRule(config, variables, steps),
            "slab" => EvaluateSlabRule(config, variables, steps),
            "formula" => EvaluateFormulaRule(config, variables, steps),
            _ => throw new InvalidOperationException($"Unknown rule type: {rule.RuleType}")
        };

        // Round to 2 decimal places
        result = Math.Round(result, 2, MidpointRounding.AwayFromZero);

        steps.Add(new CalculationStep
        {
            Description = "Final Result",
            Expression = "ROUND(result, 2)",
            Value = result
        });

        return new RuleEvaluationResult
        {
            Value = result,
            Steps = steps
        };
    }

    private decimal EvaluatePercentageRule(JsonElement config, Dictionary<string, decimal> variables, List<CalculationStep> steps)
    {
        var rate = config.GetProperty("rate").GetDecimal();
        var ofField = config.GetProperty("of").GetString() ?? "basic";

        if (!variables.TryGetValue(ofField, out var baseValue))
            throw new InvalidOperationException($"Variable '{ofField}' not found");

        steps.Add(new CalculationStep
        {
            Description = $"Get {ofField}",
            Expression = ofField,
            Value = baseValue
        });

        // Check for ceiling
        if (config.TryGetProperty("ceiling", out var ceilingElement))
        {
            var ceiling = ceilingElement.GetDecimal();
            var cappedValue = Math.Min(baseValue, ceiling);
            steps.Add(new CalculationStep
            {
                Description = $"Apply ceiling of {ceiling:N0}",
                Expression = $"MIN({baseValue:N2}, {ceiling:N0})",
                Value = cappedValue
            });
            baseValue = cappedValue;
        }

        var result = baseValue * rate / 100;
        steps.Add(new CalculationStep
        {
            Description = $"Calculate {rate}% of {baseValue:N2}",
            Expression = $"{baseValue:N2} * {rate} / 100",
            Value = result
        });

        return result;
    }

    private decimal EvaluateFixedRule(JsonElement config, Dictionary<string, decimal> variables, List<CalculationStep> steps)
    {
        var amount = config.GetProperty("amount").GetDecimal();

        steps.Add(new CalculationStep
        {
            Description = "Fixed amount",
            Expression = amount.ToString("N2"),
            Value = amount
        });

        // Check for pro-rata
        if (config.TryGetProperty("proRata", out var proRata) && proRata.GetBoolean())
        {
            if (variables.TryGetValue("payable_days", out var payableDays) &&
                variables.TryGetValue("working_days", out var workingDays) &&
                workingDays > 0)
            {
                var proratedAmount = amount * payableDays / workingDays;
                steps.Add(new CalculationStep
                {
                    Description = "Pro-rate for payable days",
                    Expression = $"{amount:N2} * {payableDays} / {workingDays}",
                    Value = proratedAmount
                });
                return proratedAmount;
            }
        }

        return amount;
    }

    private decimal EvaluateSlabRule(JsonElement config, Dictionary<string, decimal> variables, List<CalculationStep> steps)
    {
        var ofField = config.GetProperty("of").GetString() ?? "gross_earnings";

        if (!variables.TryGetValue(ofField, out var baseValue))
            throw new InvalidOperationException($"Variable '{ofField}' not found");

        steps.Add(new CalculationStep
        {
            Description = $"Get {ofField} for slab lookup",
            Expression = ofField,
            Value = baseValue
        });

        var slabs = config.GetProperty("slabs");
        foreach (var slab in slabs.EnumerateArray())
        {
            var min = slab.GetProperty("min").GetDecimal();
            var max = slab.GetProperty("max").GetDecimal();
            var value = slab.GetProperty("value").GetDecimal();

            if (baseValue >= min && baseValue <= max)
            {
                steps.Add(new CalculationStep
                {
                    Description = $"Matched slab: {min:N0} - {max:N0}",
                    Expression = $"Slab value",
                    Value = value
                });
                return value;
            }
        }

        steps.Add(new CalculationStep
        {
            Description = "No matching slab found",
            Expression = "0",
            Value = 0
        });

        return 0;
    }

    private decimal EvaluateFormulaRule(JsonElement config, Dictionary<string, decimal> variables, List<CalculationStep> steps)
    {
        var expression = config.GetProperty("expression").GetString()
            ?? throw new InvalidOperationException("Formula expression is missing");

        steps.Add(new CalculationStep
        {
            Description = "Formula expression",
            Expression = expression,
            Value = 0
        });

        var evaluator = new FormulaEvaluator(variables);
        var result = evaluator.Evaluate(expression);

        steps.Add(new CalculationStep
        {
            Description = "Formula result",
            Expression = $"= {result:N2}",
            Value = result
        });

        return result;
    }
}

/// <summary>
/// Result of a rule calculation
/// </summary>
public class RuleCalculationResult
{
    public bool Success { get; set; }
    public decimal Result { get; set; }
    public CalculationRule? RuleUsed { get; set; }
    public string? Message { get; set; }
    public List<CalculationStep> Steps { get; set; } = new();
    public Dictionary<string, decimal> InputValues { get; set; } = new();
}

/// <summary>
/// Result of evaluating a single rule
/// </summary>
public class RuleEvaluationResult
{
    public decimal Value { get; set; }
    public List<CalculationStep> Steps { get; set; } = new();
}

/// <summary>
/// A step in the calculation process
/// </summary>
public class CalculationStep
{
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>
/// Context information about the employee for condition evaluation
/// </summary>
public class EmployeeContext
{
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string? Grade { get; set; }
    public string? Location { get; set; }
    public string? PayrollType { get; set; }
    public int? Age { get; set; }
    public decimal? TenureYears { get; set; }
    public int? TenureMonths { get; set; }
}
