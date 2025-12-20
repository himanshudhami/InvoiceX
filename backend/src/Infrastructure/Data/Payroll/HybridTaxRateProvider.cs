using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Hybrid tax rate provider that tries Tax Rule Packs first, then falls back to Legacy.
/// Use this as the default provider for safe migration from legacy to rule pack system.
/// </summary>
public class HybridTaxRateProvider : ITaxRateProvider
{
    private readonly TaxRulePackTaxRateProvider _rulePackProvider;
    private readonly LegacyTaxRateProvider _legacyProvider;
    private readonly ILogger<HybridTaxRateProvider>? _logger;
    private readonly bool _preferRulePacks;

    public HybridTaxRateProvider(
        TaxRulePackTaxRateProvider rulePackProvider,
        LegacyTaxRateProvider legacyProvider,
        ILogger<HybridTaxRateProvider>? logger = null,
        bool preferRulePacks = true)
    {
        _rulePackProvider = rulePackProvider ?? throw new ArgumentNullException(nameof(rulePackProvider));
        _legacyProvider = legacyProvider ?? throw new ArgumentNullException(nameof(legacyProvider));
        _logger = logger;
        _preferRulePacks = preferRulePacks;
    }

    public string ProviderName => _preferRulePacks ? "Hybrid(RulePack->Legacy)" : "Hybrid(Legacy->RulePack)";

    public async Task<Guid?> GetActiveRulePackIdAsync(string financialYear)
    {
        // Always try to get rule pack ID if available
        var rulePackId = await _rulePackProvider.GetActiveRulePackIdAsync(financialYear);
        return rulePackId;
    }

    public async Task<IEnumerable<TaxSlabInfo>> GetTaxSlabsAsync(string regime, string financialYear, string category = "all")
    {
        if (_preferRulePacks)
        {
            // Try rule packs first
            var slabs = await _rulePackProvider.GetTaxSlabsAsync(regime, financialYear, category);
            if (slabs.Any())
            {
                _logger?.LogDebug("Using Tax Rule Pack slabs for {Regime}/{FinancialYear}/{Category}", regime, financialYear, category);
                return slabs;
            }

            _logger?.LogInformation("No Tax Rule Pack found for {FinancialYear}, falling back to legacy tables", financialYear);
            return await _legacyProvider.GetTaxSlabsAsync(regime, financialYear, category);
        }
        else
        {
            // Try legacy first
            var slabs = await _legacyProvider.GetTaxSlabsAsync(regime, financialYear, category);
            if (slabs.Any())
            {
                _logger?.LogDebug("Using legacy slabs for {Regime}/{FinancialYear}/{Category}", regime, financialYear, category);
                return slabs;
            }

            return await _rulePackProvider.GetTaxSlabsAsync(regime, financialYear, category);
        }
    }

    public async Task<Dictionary<string, decimal>> GetTaxParametersAsync(string regime, string financialYear)
    {
        if (_preferRulePacks)
        {
            // Try rule packs first
            var rulePackId = await _rulePackProvider.GetActiveRulePackIdAsync(financialYear);
            if (rulePackId.HasValue)
            {
                var params_ = await _rulePackProvider.GetTaxParametersAsync(regime, financialYear);
                if (params_.Count > 0)
                {
                    _logger?.LogDebug("Using Tax Rule Pack parameters for {Regime}/{FinancialYear}", regime, financialYear);
                    return params_;
                }
            }

            _logger?.LogInformation("No Tax Rule Pack parameters for {FinancialYear}, using legacy", financialYear);
            return await _legacyProvider.GetTaxParametersAsync(regime, financialYear);
        }
        else
        {
            var params_ = await _legacyProvider.GetTaxParametersAsync(regime, financialYear);
            if (params_.Count > 0)
            {
                return params_;
            }

            return await _rulePackProvider.GetTaxParametersAsync(regime, financialYear);
        }
    }

    public async Task<TdsRateInfo?> GetTdsRateAsync(string sectionCode, string payeeType, bool hasPan, DateTime transactionDate)
    {
        if (_preferRulePacks)
        {
            // Try rule packs first
            var rate = await _rulePackProvider.GetTdsRateAsync(sectionCode, payeeType, hasPan, transactionDate);
            if (rate != null)
            {
                _logger?.LogDebug("Using Tax Rule Pack TDS rate for {SectionCode}", sectionCode);
                return rate;
            }

            _logger?.LogInformation("No TDS rate in Rule Pack for {SectionCode}, using legacy", sectionCode);
            return await _legacyProvider.GetTdsRateAsync(sectionCode, payeeType, hasPan, transactionDate);
        }
        else
        {
            var rate = await _legacyProvider.GetTdsRateAsync(sectionCode, payeeType, hasPan, transactionDate);
            if (rate != null)
            {
                return rate;
            }

            return await _rulePackProvider.GetTdsRateAsync(sectionCode, payeeType, hasPan, transactionDate);
        }
    }
}

/// <summary>
/// Factory for creating tax rate providers based on configuration
/// </summary>
public class TaxRateProviderFactory
{
    private readonly TaxRulePackTaxRateProvider _rulePackProvider;
    private readonly LegacyTaxRateProvider _legacyProvider;
    private readonly ILogger<HybridTaxRateProvider>? _logger;

    public TaxRateProviderFactory(
        TaxRulePackTaxRateProvider rulePackProvider,
        LegacyTaxRateProvider legacyProvider,
        ILogger<HybridTaxRateProvider>? logger = null)
    {
        _rulePackProvider = rulePackProvider;
        _legacyProvider = legacyProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create the recommended hybrid provider (Rule Packs preferred)
    /// </summary>
    public ITaxRateProvider CreateHybridProvider(bool preferRulePacks = true)
    {
        return new HybridTaxRateProvider(_rulePackProvider, _legacyProvider, _logger, preferRulePacks);
    }

    /// <summary>
    /// Create a pure Rule Pack provider (no fallback)
    /// </summary>
    public ITaxRateProvider CreateRulePackProvider()
    {
        return _rulePackProvider;
    }

    /// <summary>
    /// Create a pure Legacy provider
    /// </summary>
    public ITaxRateProvider CreateLegacyProvider()
    {
        return _legacyProvider;
    }
}
