using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll;

/// <summary>
/// Repository interface for calculation rules
/// </summary>
public interface ICalculationRuleRepository
{
    Task<CalculationRule?> GetByIdAsync(Guid id);
    Task<CalculationRule?> GetByIdWithConditionsAsync(Guid id);
    Task<IEnumerable<CalculationRule>> GetAllAsync();
    Task<IEnumerable<CalculationRule>> GetByCompanyIdAsync(Guid companyId);
    Task<IEnumerable<CalculationRule>> GetActiveRulesForCompanyAsync(Guid companyId, DateTime? asOfDate = null);
    Task<IEnumerable<CalculationRule>> GetActiveRulesByComponentAsync(Guid companyId, string componentCode, DateTime? asOfDate = null);
    Task<(IEnumerable<CalculationRule> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<CalculationRule> AddAsync(CalculationRule entity);
    Task UpdateAsync(CalculationRule entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

    // Conditions
    Task<IEnumerable<CalculationRuleCondition>> GetConditionsByRuleIdAsync(Guid ruleId);
    Task AddConditionAsync(CalculationRuleCondition condition);
    Task DeleteConditionsByRuleIdAsync(Guid ruleId);
}

/// <summary>
/// Repository interface for formula variables
/// </summary>
public interface IFormulaVariableRepository
{
    Task<IEnumerable<FormulaVariable>> GetAllAsync();
    Task<IEnumerable<FormulaVariable>> GetActiveAsync();
    Task<FormulaVariable?> GetByCodeAsync(string code);
}

/// <summary>
/// Repository interface for calculation rule templates
/// </summary>
public interface ICalculationRuleTemplateRepository
{
    Task<IEnumerable<CalculationRuleTemplate>> GetAllAsync();
    Task<IEnumerable<CalculationRuleTemplate>> GetByCategoryAsync(string category);
    Task<CalculationRuleTemplate?> GetByIdAsync(Guid id);
}
