using System.Text.Json;
using Core.Entities;
using Core.Interfaces;

namespace Application.Services;

/// <summary>
/// Service for managing Tax Rule Packs and performing tax calculations
/// </summary>
public class TaxRulePackService
{
    private readonly ITaxRulePackRepository _repository;

    public TaxRulePackService(ITaxRulePackRepository repository)
    {
        _repository = repository;
    }

    #region Rule Pack Management

    public async Task<IEnumerable<TaxRulePack>> GetAllPacksAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<TaxRulePack?> GetPackByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<TaxRulePack?> GetActivePackForDateAsync(DateTime date)
    {
        var fy = GetFinancialYear(date);
        return await _repository.GetActivePackForFyAsync(fy);
    }

    public async Task<TaxRulePack?> GetActivePackAsync(string financialYear)
    {
        return await _repository.GetActivePackForFyAsync(financialYear);
    }

    public async Task<TaxRulePack> CreatePackAsync(TaxRulePack pack, string createdBy)
    {
        // Get latest version for this FY
        var latest = await _repository.GetLatestVersionAsync(pack.FinancialYear);
        pack.Version = (latest?.Version ?? 0) + 1;
        pack.CreatedBy = createdBy;
        pack.Status = "draft";

        return await _repository.CreateAsync(pack);
    }

    public async Task<TaxRulePack> UpdatePackAsync(TaxRulePack pack, string updatedBy)
    {
        pack.UpdatedBy = updatedBy;
        return await _repository.UpdateAsync(pack);
    }

    public async Task<bool> ActivatePackAsync(Guid id, string activatedBy)
    {
        return await _repository.ActivatePackAsync(id, activatedBy);
    }

    public async Task<bool> DeletePackAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    #endregion

    #region TDS Calculations

    /// <summary>
    /// Get TDS rate for a specific section and payee type
    /// </summary>
    public async Task<TdsCalculationResult> GetTdsRateAsync(
        string sectionCode,
        string payeeType,
        decimal amount,
        bool hasPan,
        DateTime transactionDate)
    {
        var pack = await GetActivePackForDateAsync(transactionDate);
        if (pack == null)
        {
            return new TdsCalculationResult
            {
                Success = false,
                ErrorMessage = $"No active tax rule pack found for {GetFinancialYear(transactionDate)}"
            };
        }

        // Try to get from TDS section rates first
        var sectionRate = await _repository.GetTdsSectionRateAsync(pack.Id, sectionCode);
        if (sectionRate != null && sectionRate.IsActive)
        {
            var rate = payeeType.ToLower() switch
            {
                "company" => sectionRate.RateCompany ?? sectionRate.RateIndividual,
                _ => sectionRate.RateIndividual
            };

            if (!hasPan && sectionRate.RateNoPan.HasValue)
            {
                rate = sectionRate.RateNoPan.Value;
            }

            // Check threshold
            var isExempt = sectionRate.ThresholdAmount.HasValue && amount <= sectionRate.ThresholdAmount.Value;

            return new TdsCalculationResult
            {
                Success = true,
                RulePackId = pack.Id,
                SectionCode = sectionCode,
                ApplicableRate = isExempt ? 0 : rate,
                ThresholdAmount = sectionRate.ThresholdAmount,
                IsExempt = isExempt,
                TdsAmount = isExempt ? 0 : Math.Round(amount * rate / 100, 2)
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
                var isExempt = threshold.HasValue && amount <= threshold.Value;

                return new TdsCalculationResult
                {
                    Success = true,
                    RulePackId = pack.Id,
                    SectionCode = sectionCode,
                    ApplicableRate = isExempt ? 0 : rate,
                    ThresholdAmount = threshold,
                    IsExempt = isExempt,
                    TdsAmount = isExempt ? 0 : Math.Round(amount * rate / 100, 2)
                };
            }
        }

        return new TdsCalculationResult
        {
            Success = false,
            ErrorMessage = $"TDS section {sectionCode} not found in rule pack"
        };
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

    #endregion

    #region Income Tax Calculations

    /// <summary>
    /// Calculate income tax based on taxable income and regime
    /// </summary>
    public async Task<IncomeTaxCalculationResult> CalculateIncomeTaxAsync(
        decimal taxableIncome,
        string regime, // "new" or "old"
        string? ageCategory, // null, "senior", "super_senior"
        string financialYear)
    {
        var pack = await _repository.GetActivePackForFyAsync(financialYear);
        if (pack == null)
        {
            return new IncomeTaxCalculationResult
            {
                Success = false,
                ErrorMessage = $"No active tax rule pack found for {financialYear}"
            };
        }

        if (pack.IncomeTaxSlabs == null)
        {
            return new IncomeTaxCalculationResult
            {
                Success = false,
                ErrorMessage = "Income tax slabs not configured in rule pack"
            };
        }

        var slabsElement = pack.IncomeTaxSlabs.RootElement;

        // Determine which slab set to use
        var slabKey = regime.ToLower() == "old" && !string.IsNullOrEmpty(ageCategory)
            ? ageCategory switch
            {
                "senior" => "senior_citizen_old",
                "super_senior" => "super_senior_old",
                _ => "old"
            }
            : regime.ToLower();

        if (!slabsElement.TryGetProperty(slabKey, out var slabsArray))
        {
            slabKey = regime.ToLower(); // Fallback
            if (!slabsElement.TryGetProperty(slabKey, out slabsArray))
            {
                return new IncomeTaxCalculationResult
                {
                    Success = false,
                    ErrorMessage = $"Tax slabs for regime '{regime}' not found"
                };
            }
        }

        // Calculate tax
        decimal totalTax = 0;
        var slabBreakdown = new List<TaxSlabBreakdown>();

        foreach (var slab in slabsArray.EnumerateArray())
        {
            var min = slab.GetProperty("min").GetDecimal();
            var max = slab.TryGetProperty("max", out var maxProp) && maxProp.ValueKind != JsonValueKind.Null
                ? maxProp.GetDecimal()
                : decimal.MaxValue;
            var rate = slab.GetProperty("rate").GetDecimal();

            if (taxableIncome <= min) break;

            var taxableInSlab = Math.Min(taxableIncome, max) - min;
            var taxInSlab = Math.Round(taxableInSlab * rate / 100, 2);

            if (taxableInSlab > 0)
            {
                slabBreakdown.Add(new TaxSlabBreakdown
                {
                    SlabMin = min,
                    SlabMax = max == decimal.MaxValue ? null : max,
                    Rate = rate,
                    TaxableAmount = taxableInSlab,
                    TaxAmount = taxInSlab
                });
                totalTax += taxInSlab;
            }
        }

        // Apply rebate (Section 87A)
        decimal rebate = 0;
        if (pack.RebateThresholds != null)
        {
            var rebateConfig = pack.RebateThresholds.RootElement;
            if (rebateConfig.TryGetProperty(regime.ToLower(), out var regimeRebate))
            {
                var threshold = regimeRebate.GetProperty("income_threshold").GetDecimal();
                var maxRebate = regimeRebate.GetProperty("max_rebate").GetDecimal();

                if (taxableIncome <= threshold)
                {
                    rebate = Math.Min(totalTax, maxRebate);
                }
            }
        }

        var taxAfterRebate = totalTax - rebate;

        // Apply cess
        decimal cessRate = 4; // Default 4%
        if (pack.CessRates != null)
        {
            var cessConfig = pack.CessRates.RootElement;
            if (cessConfig.TryGetProperty("health_education_cess", out var cess))
            {
                cessRate = cess.GetDecimal();
            }
        }
        var cessAmount = Math.Round(taxAfterRebate * cessRate / 100, 2);

        return new IncomeTaxCalculationResult
        {
            Success = true,
            RulePackId = pack.Id,
            TaxableIncome = taxableIncome,
            Regime = regime,
            GrossTax = totalTax,
            Rebate = rebate,
            TaxAfterRebate = taxAfterRebate,
            CessRate = cessRate,
            CessAmount = cessAmount,
            TotalTax = taxAfterRebate + cessAmount,
            SlabBreakdown = slabBreakdown
        };
    }

    #endregion

    #region PF/ESI Calculations

    /// <summary>
    /// Get PF/ESI rates from active rule pack
    /// </summary>
    public async Task<PfEsiRates?> GetPfEsiRatesAsync(string financialYear)
    {
        var pack = await _repository.GetActivePackForFyAsync(financialYear);
        if (pack?.PfEsiRates == null) return null;

        var config = pack.PfEsiRates.RootElement;

        var rates = new PfEsiRates();

        if (config.TryGetProperty("pf", out var pf))
        {
            rates.EmployeePfRate = pf.TryGetProperty("employee_contribution", out var empPf) ? empPf.GetDecimal() : 12;
            rates.EmployerPfRate = pf.TryGetProperty("employer_contribution", out var emplrPf) ? emplrPf.GetDecimal() : 12;
            rates.PfWageCeiling = pf.TryGetProperty("wage_ceiling", out var ceiling) ? ceiling.GetDecimal() : 15000;
        }

        if (config.TryGetProperty("esi", out var esi))
        {
            rates.EmployeeEsiRate = esi.TryGetProperty("employee_contribution", out var empEsi) ? empEsi.GetDecimal() : 0.75m;
            rates.EmployerEsiRate = esi.TryGetProperty("employer_contribution", out var emplrEsi) ? emplrEsi.GetDecimal() : 3.25m;
            rates.EsiWageCeiling = esi.TryGetProperty("wage_ceiling", out var esiCeiling) ? esiCeiling.GetDecimal() : 21000;
        }

        return rates;
    }

    #endregion

    #region Usage Logging

    public async Task LogUsageAsync(
        Guid rulePackId,
        Guid? companyId,
        string computationType,
        Guid computationId,
        DateTime computationDate,
        object rulesSnapshot,
        decimal? inputAmount,
        decimal? computedTax,
        string? computedBy)
    {
        var log = new RulePackUsageLog
        {
            RulePackId = rulePackId,
            CompanyId = companyId,
            ComputationType = computationType,
            ComputationId = computationId,
            ComputationDate = computationDate,
            RulesSnapshot = JsonDocument.Parse(JsonSerializer.Serialize(rulesSnapshot)),
            InputAmount = inputAmount,
            ComputedTax = computedTax,
            EffectiveRate = inputAmount > 0 ? computedTax / inputAmount * 100 : null,
            ComputedBy = computedBy
        };

        await _repository.LogUsageAsync(log);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get Indian financial year from date (Apr-Mar)
    /// </summary>
    public static string GetFinancialYear(DateTime date)
    {
        var year = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{year}-{(year + 1) % 100:D2}";
    }

    /// <summary>
    /// Get financial year start date
    /// </summary>
    public static DateTime GetFyStartDate(string financialYear)
    {
        var startYear = int.Parse(financialYear.Split('-')[0]);
        return new DateTime(startYear, 4, 1);
    }

    /// <summary>
    /// Get financial year end date
    /// </summary>
    public static DateTime GetFyEndDate(string financialYear)
    {
        var startYear = int.Parse(financialYear.Split('-')[0]);
        return new DateTime(startYear + 1, 3, 31);
    }

    #endregion
}

#region Result Classes

public class TdsCalculationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RulePackId { get; set; }
    public string? SectionCode { get; set; }
    public decimal ApplicableRate { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public bool IsExempt { get; set; }
    public decimal TdsAmount { get; set; }
}

public class IncomeTaxCalculationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RulePackId { get; set; }
    public decimal TaxableIncome { get; set; }
    public string? Regime { get; set; }
    public decimal GrossTax { get; set; }
    public decimal Rebate { get; set; }
    public decimal TaxAfterRebate { get; set; }
    public decimal CessRate { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalTax { get; set; }
    public List<TaxSlabBreakdown>? SlabBreakdown { get; set; }
}

public class TaxSlabBreakdown
{
    public decimal SlabMin { get; set; }
    public decimal? SlabMax { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class PfEsiRates
{
    public decimal EmployeePfRate { get; set; }
    public decimal EmployerPfRate { get; set; }
    public decimal PfWageCeiling { get; set; }
    public decimal EmployeeEsiRate { get; set; }
    public decimal EmployerEsiRate { get; set; }
    public decimal EsiWageCeiling { get; set; }
}

#endregion
