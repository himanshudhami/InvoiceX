using Core.Entities.Payroll;
using Core.Interfaces.Payroll;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Tax rate provider using legacy database tables (tax_slabs, tax_parameters).
/// Used for backward compatibility and fallback when Tax Rule Packs are not available.
/// </summary>
public class LegacyTaxRateProvider : ITaxRateProvider
{
    private readonly ITaxSlabRepository _taxSlabRepository;
    private readonly ITaxParameterRepository _taxParameterRepository;

    public LegacyTaxRateProvider(
        ITaxSlabRepository taxSlabRepository,
        ITaxParameterRepository taxParameterRepository)
    {
        _taxSlabRepository = taxSlabRepository ?? throw new ArgumentNullException(nameof(taxSlabRepository));
        _taxParameterRepository = taxParameterRepository ?? throw new ArgumentNullException(nameof(taxParameterRepository));
    }

    public string ProviderName => "Legacy";

    /// <summary>
    /// Legacy provider does not use rule packs
    /// </summary>
    public Task<Guid?> GetActiveRulePackIdAsync(string financialYear)
    {
        return Task.FromResult<Guid?>(null);
    }

    public async Task<IEnumerable<TaxSlabInfo>> GetTaxSlabsAsync(string regime, string financialYear, string category = "all")
    {
        var slabs = await _taxSlabRepository.GetByRegimeYearAndCategoryAsync(regime, financialYear, category);

        return slabs.Select(s => new TaxSlabInfo
        {
            MinIncome = s.MinIncome,
            MaxIncome = s.MaxIncome,
            Rate = s.Rate,
            Description = null
        }).OrderBy(s => s.MinIncome);
    }

    public async Task<Dictionary<string, decimal>> GetTaxParametersAsync(string regime, string financialYear)
    {
        return await _taxParameterRepository.GetAllParametersForRegimeAsync(regime, financialYear);
    }

    public Task<TdsRateInfo?> GetTdsRateAsync(string sectionCode, string payeeType, bool hasPan, DateTime transactionDate)
    {
        // Legacy system uses hardcoded TDS rates
        var baseRate = sectionCode.ToUpperInvariant() switch
        {
            "194C" => payeeType.ToLower() == "individual" || payeeType.ToLower() == "huf" ? 1.0m : 2.0m,
            "194J" => 10.0m,
            "194H" => 5.0m, // Note: Changed to 2% for FY 2025-26 in Rule Packs
            "194A" => 10.0m,
            "194I" => 10.0m, // Rent
            "194T" => 10.0m, // New section for partner payments
            _ => 10.0m
        };

        // Section 206AA: Higher TDS rate of 20% if PAN not provided
        var effectiveRate = hasPan ? baseRate : 20.0m;

        // Legacy thresholds
        var threshold = sectionCode.ToUpperInvariant() switch
        {
            "194C" => 30000m, // Per transaction for 194C
            "194J" => 30000m, // Note: Changed to 50000 for FY 2025-26 in Rule Packs
            "194H" => 15000m,
            "194A" => 40000m, // Interest other than bank
            "194I" => 240000m, // Annual rent limit
            _ => null as decimal?
        };

        return Task.FromResult<TdsRateInfo?>(new TdsRateInfo
        {
            SectionCode = sectionCode,
            SectionName = $"Section {sectionCode}",
            Rate = effectiveRate,
            ThresholdAmount = threshold,
            ThresholdType = sectionCode == "194I" ? "annual" : "per_transaction",
            IsExemptBelowThreshold = threshold.HasValue
        });
    }
}
