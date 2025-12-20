using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// Repository interface for Tax Rule Packs
/// </summary>
public interface ITaxRulePackRepository
{
    // Basic CRUD
    Task<TaxRulePack?> GetByIdAsync(Guid id);
    Task<IEnumerable<TaxRulePack>> GetAllAsync();
    Task<TaxRulePack> CreateAsync(TaxRulePack rulePack);
    Task<TaxRulePack> UpdateAsync(TaxRulePack rulePack);
    Task<bool> DeleteAsync(Guid id);

    // Specialized queries
    Task<TaxRulePack?> GetActivePackForFyAsync(string financialYear);
    Task<IEnumerable<TaxRulePack>> GetByFinancialYearAsync(string financialYear);
    Task<IEnumerable<TaxRulePack>> GetByStatusAsync(string status);
    Task<TaxRulePack?> GetLatestVersionAsync(string financialYear);

    // TDS Section Rates
    Task<IEnumerable<TdsSectionRate>> GetTdsSectionRatesAsync(Guid rulePackId);
    Task<TdsSectionRate?> GetTdsSectionRateAsync(Guid rulePackId, string sectionCode);
    Task<TdsSectionRate> CreateTdsSectionRateAsync(TdsSectionRate rate);
    Task<TdsSectionRate> UpdateTdsSectionRateAsync(TdsSectionRate rate);

    // Usage Logging
    Task<RulePackUsageLog> LogUsageAsync(RulePackUsageLog log);
    Task<IEnumerable<RulePackUsageLog>> GetUsageLogsAsync(Guid rulePackId, int limit = 100);
    Task<IEnumerable<RulePackUsageLog>> GetUsageLogsByCompanyAsync(Guid companyId, int limit = 100);

    // Activation
    Task<bool> ActivatePackAsync(Guid id, string activatedBy);
    Task<bool> SupersedePackAsync(Guid id);
}
