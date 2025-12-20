using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Payroll;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Tax rate provider implementation using Tax Rule Packs (JSONB-based FY-versioned configs).
/// This is the recommended provider for FY 2025-26 onwards as it contains updated rates.
/// </summary>
public class TaxRulePackTaxRateProvider : ITaxRateProvider
{
    private readonly ITaxRulePackRepository _repository;

    public TaxRulePackTaxRateProvider(ITaxRulePackRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public string ProviderName => "TaxRulePack";

    public async Task<Guid?> GetActiveRulePackIdAsync(string financialYear)
    {
        var pack = await _repository.GetActivePackForFyAsync(financialYear);
        return pack?.Id;
    }

    public async Task<IEnumerable<TaxSlabInfo>> GetTaxSlabsAsync(string regime, string financialYear, string category = "all")
    {
        var pack = await _repository.GetActivePackForFyAsync(financialYear);
        if (pack?.IncomeTaxSlabs == null)
            return Enumerable.Empty<TaxSlabInfo>();

        var slabsElement = pack.IncomeTaxSlabs.RootElement;

        // Determine which slab set to use
        var slabKey = regime.ToLower() == "old" && category != "all"
            ? category switch
            {
                "senior" => "senior_citizen_old",
                "super_senior" => "super_senior_old",
                _ => "old"
            }
            : regime.ToLower();

        // Try to get the specific slab set
        if (!slabsElement.TryGetProperty(slabKey, out var slabsArray))
        {
            // Fallback to base regime
            slabKey = regime.ToLower();
            if (!slabsElement.TryGetProperty(slabKey, out slabsArray))
            {
                return Enumerable.Empty<TaxSlabInfo>();
            }
        }

        var slabs = new List<TaxSlabInfo>();
        foreach (var slab in slabsArray.EnumerateArray())
        {
            slabs.Add(new TaxSlabInfo
            {
                MinIncome = slab.GetProperty("min").GetDecimal(),
                MaxIncome = slab.TryGetProperty("max", out var maxProp) && maxProp.ValueKind != JsonValueKind.Null
                    ? maxProp.GetDecimal()
                    : null,
                Rate = slab.GetProperty("rate").GetDecimal(),
                Description = slab.TryGetProperty("description", out var desc) ? desc.GetString() : null
            });
        }

        return slabs.OrderBy(s => s.MinIncome);
    }

    public async Task<Dictionary<string, decimal>> GetTaxParametersAsync(string regime, string financialYear)
    {
        var pack = await _repository.GetActivePackForFyAsync(financialYear);
        if (pack == null)
            return new Dictionary<string, decimal>();

        var parameters = new Dictionary<string, decimal>();

        // Standard deductions
        if (pack.StandardDeductions != null)
        {
            var stdDed = pack.StandardDeductions.RootElement;
            if (stdDed.TryGetProperty(regime.ToLower(), out var regimeDeduction))
            {
                parameters["STANDARD_DEDUCTION"] = regimeDeduction.GetDecimal();
            }
            else if (stdDed.TryGetProperty("default", out var defaultDeduction))
            {
                parameters["STANDARD_DEDUCTION"] = defaultDeduction.GetDecimal();
            }
        }

        // Rebate thresholds (Section 87A)
        if (pack.RebateThresholds != null)
        {
            var rebate = pack.RebateThresholds.RootElement;
            if (rebate.TryGetProperty(regime.ToLower(), out var regimeRebate))
            {
                if (regimeRebate.TryGetProperty("income_threshold", out var threshold))
                    parameters["REBATE_87A_THRESHOLD"] = threshold.GetDecimal();
                if (regimeRebate.TryGetProperty("max_rebate", out var maxRebate))
                    parameters["REBATE_87A_MAX"] = maxRebate.GetDecimal();
            }
        }

        // Cess rates
        if (pack.CessRates != null)
        {
            var cess = pack.CessRates.RootElement;
            if (cess.TryGetProperty("health_education_cess", out var cessRate))
            {
                parameters["CESS_RATE"] = cessRate.GetDecimal();
            }
            else if (cess.TryGetProperty("health_education", out var cessRate2))
            {
                parameters["CESS_RATE"] = cessRate2.GetDecimal();
            }
        }

        // Surcharge thresholds and rates
        if (pack.SurchargeRates != null)
        {
            var surcharge = pack.SurchargeRates.RootElement;

            // Parse surcharge configuration
            if (surcharge.TryGetProperty("thresholds", out var thresholds))
            {
                if (thresholds.TryGetProperty("50L", out var t50L))
                    parameters["SURCHARGE_THRESHOLD_50L"] = t50L.GetDecimal();
                if (thresholds.TryGetProperty("1Cr", out var t1Cr))
                    parameters["SURCHARGE_THRESHOLD_1CR"] = t1Cr.GetDecimal();
                if (thresholds.TryGetProperty("2Cr", out var t2Cr))
                    parameters["SURCHARGE_THRESHOLD_2CR"] = t2Cr.GetDecimal();
                if (thresholds.TryGetProperty("5Cr", out var t5Cr))
                    parameters["SURCHARGE_THRESHOLD_5CR"] = t5Cr.GetDecimal();
            }

            if (surcharge.TryGetProperty("rates", out var rates))
            {
                if (rates.TryGetProperty("50L_1Cr", out var r50L))
                    parameters["SURCHARGE_RATE_50L"] = r50L.GetDecimal();
                if (rates.TryGetProperty("1Cr_2Cr", out var r1Cr))
                    parameters["SURCHARGE_RATE_1CR"] = r1Cr.GetDecimal();
                if (rates.TryGetProperty("2Cr_5Cr", out var r2Cr))
                    parameters["SURCHARGE_RATE_2CR"] = r2Cr.GetDecimal();
                if (rates.TryGetProperty("above_5Cr", out var rMax))
                    parameters["SURCHARGE_MAX_RATE"] = rMax.GetDecimal();
            }

            // Flat key format fallback
            if (surcharge.TryGetProperty("50L_1Cr", out var rate50L))
                parameters["SURCHARGE_RATE_50L"] = rate50L.GetDecimal();
            if (surcharge.TryGetProperty("1Cr_2Cr", out var rate1Cr))
                parameters["SURCHARGE_RATE_1CR"] = rate1Cr.GetDecimal();
        }

        return parameters;
    }

    public async Task<TdsRateInfo?> GetTdsRateAsync(string sectionCode, string payeeType, bool hasPan, DateTime transactionDate)
    {
        var fy = GetFinancialYear(transactionDate);
        var pack = await _repository.GetActivePackForFyAsync(fy);
        if (pack == null)
            return null;

        // Try to get from TDS section rates table first
        var sectionRate = await _repository.GetTdsSectionRateAsync(pack.Id, sectionCode);
        if (sectionRate != null && sectionRate.IsActive)
        {
            var rate = payeeType.ToLower() switch
            {
                "company" => sectionRate.RateCompany ?? sectionRate.RateIndividual,
                "partnership" => sectionRate.RateCompany ?? sectionRate.RateIndividual,
                _ => sectionRate.RateIndividual
            };

            if (!hasPan && sectionRate.RateNoPan.HasValue)
            {
                rate = sectionRate.RateNoPan.Value;
            }

            return new TdsRateInfo
            {
                SectionCode = sectionCode,
                SectionName = sectionRate.SectionName ?? $"Section {sectionCode}",
                Rate = rate,
                ThresholdAmount = sectionRate.ThresholdAmount,
                ThresholdType = sectionRate.ThresholdType,
                IsExemptBelowThreshold = sectionRate.ThresholdAmount.HasValue
            };
        }

        // Fallback to JSON TDS rates
        if (pack.TdsRates != null)
        {
            var tdsRates = pack.TdsRates.RootElement;
            if (tdsRates.TryGetProperty(sectionCode, out var sectionConfig))
            {
                var rate = GetRateFromJson(sectionConfig, payeeType, hasPan);
                var threshold = GetThresholdFromJson(sectionConfig);

                return new TdsRateInfo
                {
                    SectionCode = sectionCode,
                    SectionName = sectionConfig.TryGetProperty("name", out var name)
                        ? name.GetString() ?? $"Section {sectionCode}"
                        : $"Section {sectionCode}",
                    Rate = rate,
                    ThresholdAmount = threshold,
                    ThresholdType = sectionConfig.TryGetProperty("threshold_type", out var tt)
                        ? tt.GetString()
                        : "per_transaction",
                    IsExemptBelowThreshold = threshold.HasValue
                };
            }
        }

        return null;
    }

    private static decimal GetRateFromJson(JsonElement config, string payeeType, bool hasPan)
    {
        if (!hasPan && config.TryGetProperty("rate_no_pan", out var noPanRate))
        {
            return noPanRate.GetDecimal();
        }

        if (payeeType.ToLower() == "company" && config.TryGetProperty("rate_company", out var companyRate))
        {
            return companyRate.GetDecimal();
        }

        if (config.TryGetProperty("rate_individual", out var individualRate))
        {
            return individualRate.GetDecimal();
        }

        if (config.TryGetProperty("rate", out var rate))
        {
            return rate.GetDecimal();
        }

        return 0;
    }

    private static decimal? GetThresholdFromJson(JsonElement config)
    {
        if (config.TryGetProperty("threshold", out var threshold))
        {
            return threshold.GetDecimal();
        }
        if (config.TryGetProperty("threshold_single", out var thresholdSingle))
        {
            return thresholdSingle.GetDecimal();
        }
        return null;
    }

    private static string GetFinancialYear(DateTime date)
    {
        var year = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{year}-{(year + 1) % 100:D2}";
    }
}
