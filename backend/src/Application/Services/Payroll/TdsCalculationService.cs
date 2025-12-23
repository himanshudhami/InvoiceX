using Application.DTOs.Payroll;
using Core.Interfaces.Payroll;
using Core.Interfaces.Tax;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Tax Deducted at Source (TDS) calculations
/// Supports both Old and New tax regimes for Indian income tax
/// Tax parameters are loaded from ITaxRateProvider (supports both Rule Packs and Legacy tables)
/// Integrates with Lower Deduction Certificates (Form 13) for reduced rate TDS
/// </summary>
public class TdsCalculationService
{
    private readonly ITaxRateProvider _taxRateProvider;
    private readonly ILowerDeductionCertificateRepository? _ldcRepository;

    // Default fallback values (used if provider returns no data)
    private const decimal DEFAULT_STANDARD_DEDUCTION_NEW = 75000m;
    private const decimal DEFAULT_STANDARD_DEDUCTION_OLD = 50000m;
    private const decimal DEFAULT_CESS_RATE = 4m;
    private const decimal DEFAULT_REBATE_THRESHOLD_NEW = 700000m;
    private const decimal DEFAULT_REBATE_THRESHOLD_OLD = 500000m;
    private const decimal DEFAULT_REBATE_MAX_NEW = 25000m;
    private const decimal DEFAULT_REBATE_MAX_OLD = 12500m;

    /// <summary>
    /// Create TdsCalculationService with ITaxRateProvider (recommended)
    /// </summary>
    public TdsCalculationService(ITaxRateProvider taxRateProvider)
    {
        _taxRateProvider = taxRateProvider ?? throw new ArgumentNullException(nameof(taxRateProvider));
    }

    /// <summary>
    /// Create TdsCalculationService with ITaxRateProvider and LDC Repository for Form 13 integration
    /// </summary>
    public TdsCalculationService(ITaxRateProvider taxRateProvider, ILowerDeductionCertificateRepository ldcRepository)
    {
        _taxRateProvider = taxRateProvider ?? throw new ArgumentNullException(nameof(taxRateProvider));
        _ldcRepository = ldcRepository ?? throw new ArgumentNullException(nameof(ldcRepository));
    }

    /// <summary>
    /// Get the active rule pack ID for the given financial year (for audit logging)
    /// Returns null if using legacy provider
    /// </summary>
    public async Task<Guid?> GetActiveRulePackIdAsync(string financialYear)
    {
        return await _taxRateProvider.GetActiveRulePackIdAsync(financialYear);
    }

    /// <summary>
    /// Get the tax rate provider name for logging
    /// </summary>
    public string ProviderName => _taxRateProvider.ProviderName;

    /// <summary>
    /// Calculate TDS for an employee based on projected annual income
    /// </summary>
    /// <param name="dateOfBirth">Employee's date of birth for senior citizen slab determination (Old Regime only)</param>
    public async Task<TaxCalculationDto> CalculateAsync(
        Guid employeeId,
        string financialYear,
        string taxRegime,
        decimal annualGrossIncome,
        decimal annualBasic,
        decimal annualHra,
        decimal otherIncome,
        decimal previousEmployerIncome,
        decimal previousEmployerTds,
        decimal tdsPaidYtd,
        int remainingMonths,
        EmployeeTaxDeclarationDto? taxDeclaration = null,
        DateTime? dateOfBirth = null)
    {
        var result = new TaxCalculationDto
        {
            EmployeeId = employeeId,
            FinancialYear = financialYear,
            TaxRegime = taxRegime,
            AnnualGrossIncome = annualGrossIncome,
            AnnualBasic = annualBasic,
            AnnualHra = annualHra,
            OtherIncome = otherIncome,
            PreviousEmployerIncome = previousEmployerIncome,
            PreviousEmployerTds = previousEmployerTds,
            TdsPaidYtd = tdsPaidYtd,
            RemainingMonths = remainingMonths
        };

        // Total gross income
        result.TotalGrossIncome = annualGrossIncome + otherIncome + previousEmployerIncome;

        // Fetch tax parameters from provider (Rule Packs or Legacy)
        var taxParams = await _taxRateProvider.GetTaxParametersAsync(taxRegime, financialYear);

        // Calculate deductions based on regime
        if (taxRegime == "new")
        {
            result.StandardDeduction = taxParams.GetValueOrDefault("STANDARD_DEDUCTION", DEFAULT_STANDARD_DEDUCTION_NEW);
            result.TotalDeductions = result.StandardDeduction;
        }
        else
        {
            result.StandardDeduction = taxParams.GetValueOrDefault("STANDARD_DEDUCTION", DEFAULT_STANDARD_DEDUCTION_OLD);
            result.OldRegimeDeductions = CalculateOldRegimeDeductions(taxDeclaration, annualBasic, annualHra);
            result.TotalDeductions = result.StandardDeduction + (result.OldRegimeDeductions?.TotalAllowedDeductions() ?? 0);
        }

        // Taxable income
        result.TaxableIncome = Math.Max(0, result.TotalGrossIncome - result.TotalDeductions);

        // Determine taxpayer category for senior citizen slabs (Old Regime only)
        var taxCategory = DetermineTaxCategory(dateOfBirth, financialYear);

        // Get tax slabs from provider (uses category-based lookup)
        var taxSlabInfos = await _taxRateProvider.GetTaxSlabsAsync(taxRegime, financialYear, taxCategory);
        var taxSlabs = taxSlabInfos.Select(s => new Core.Entities.Payroll.TaxSlab
        {
            MinIncome = s.MinIncome,
            MaxIncome = s.MaxIncome,
            Rate = s.Rate
        }).ToList();

        result.TaxSlabs = CalculateTaxBySlabs(result.TaxableIncome, taxSlabs);
        result.TaxOnIncome = result.TaxSlabs.Sum(s => s.TaxAmount);

        // Apply Section 87A rebate (parameterized)
        result.Section87aRebate = CalculateSection87aRebate(result.TaxableIncome, taxRegime, result.TaxOnIncome, taxParams);
        result.TaxOnIncomeAfterRebate = Math.Max(0, result.TaxOnIncome - result.Section87aRebate);

        // Apply surcharge with marginal relief if applicable (on tax after rebate)
        result.Surcharge = CalculateSurchargeWithMarginalRelief(
            result.TaxableIncome,
            result.TaxOnIncomeAfterRebate,
            taxParams,
            taxSlabs.ToList());

        // Apply Section 87A marginal relief BEFORE calculating cess
        // This caps tax (before cess) at excess income when income slightly exceeds rebate threshold
        // Per Indian tax law, cess is calculated AFTER marginal relief is applied
        var taxBeforeCess = result.TaxOnIncomeAfterRebate + result.Surcharge;
        var taxAfterMarginalRelief = ApplySection87aMarginalRelief(
            result.TaxableIncome,
            taxBeforeCess,
            taxRegime,
            taxParams);

        // Calculate cess (parameterized) on tax AFTER marginal relief
        var cessRate = taxParams.GetValueOrDefault("CESS_RATE", DEFAULT_CESS_RATE) / 100m;
        result.Cess = Math.Round(taxAfterMarginalRelief * cessRate, 0, MidpointRounding.AwayFromZero);

        // Total tax liability (after marginal relief and cess)
        result.TotalTaxLiability = taxAfterMarginalRelief + result.Cess;

        // Remaining tax after credits
        result.RemainingTaxLiability = Math.Max(0, result.TotalTaxLiability - previousEmployerTds - tdsPaidYtd);

        // Monthly TDS
        if (remainingMonths > 0)
        {
            result.MonthlyTds = Math.Round(result.RemainingTaxLiability / remainingMonths, 0, MidpointRounding.AwayFromZero);
        }

        return result;
    }

    /// <summary>
    /// Calculate old regime specific deductions
    /// </summary>
    private OldRegimeDeductionsDto CalculateOldRegimeDeductions(
        EmployeeTaxDeclarationDto? declaration,
        decimal annualBasic,
        decimal annualHra)
    {
        var result = new OldRegimeDeductionsDto();

        if (declaration == null)
            return result;

        // Section 80C (max ₹1,50,000)
        result.Section80cTotal = declaration.Sec80cPpf + declaration.Sec80cElss + declaration.Sec80cLifeInsurance +
                                 declaration.Sec80cHomeLoanPrincipal + declaration.Sec80cChildrenTuition +
                                 declaration.Sec80cNsc + declaration.Sec80cSukanyaSamriddhi +
                                 declaration.Sec80cFixedDeposit + declaration.Sec80cOthers;
        result.Section80cAllowed = Math.Min(result.Section80cTotal, result.Section80cLimit);

        // Section 80CCD(1B) NPS (max ₹50,000)
        result.Section80ccdNps = declaration.Sec80ccdNps;
        result.Section80ccdAllowed = Math.Min(result.Section80ccdNps, result.Section80ccdLimit);

        // Section 80D Health Insurance
        var selfFamilyLimit = declaration.Sec80dSelfSeniorCitizen ? 50000m : 25000m;
        var parentsLimit = declaration.Sec80dParentsSeniorCitizen ? 50000m : 25000m;
        result.Section80dTotal = declaration.Sec80dSelfFamily + declaration.Sec80dParents + declaration.Sec80dPreventiveCheckup;
        result.Section80dLimit = selfFamilyLimit + parentsLimit;
        result.Section80dAllowed = Math.Min(declaration.Sec80dSelfFamily, selfFamilyLimit) +
                                   Math.Min(declaration.Sec80dParents, parentsLimit) +
                                   Math.Min(declaration.Sec80dPreventiveCheckup, 5000m);

        // HRA Exemption
        if (declaration.HraRentPaidAnnual > 0 && annualHra > 0)
        {
            result.HraReceived = annualHra;
            result.HraCalculation = CalculateHraExemption(
                annualHra,
                annualBasic,
                declaration.HraRentPaidAnnual,
                declaration.HraMetroCity);
            result.HraExemption = result.HraCalculation.HraExemption;
        }

        // Section 80E Education Loan Interest (no limit)
        result.Section80eEducationLoan = declaration.Sec80eEducationLoan;

        // Section 24 Home Loan Interest (max ₹2,00,000)
        result.Section24HomeLoanInterest = declaration.Sec24HomeLoanInterest;
        result.Section24Allowed = Math.Min(result.Section24HomeLoanInterest, result.Section24Limit);

        // Section 80G Donations (assuming 50% deduction)
        result.Section80gDonations = declaration.Sec80gDonations * 0.5m;

        // Section 80TTA Savings Interest (max ₹10,000)
        result.Section80ttaSavingsInterest = declaration.Sec80ttaSavingsInterest;
        result.Section80ttaAllowed = Math.Min(result.Section80ttaSavingsInterest, result.Section80ttaLimit);

        return result;
    }

    /// <summary>
    /// Calculate HRA exemption
    /// Minimum of:
    /// 1. Actual HRA received
    /// 2. Rent paid - 10% of basic
    /// 3. 50% (metro) or 40% (non-metro) of basic
    /// </summary>
    private HraCalculationDto CalculateHraExemption(decimal hraReceived, decimal annualBasic, decimal rentPaid, bool isMetroCity)
    {
        var tenPercentBasic = annualBasic * 0.10m;
        var rentMinusTenPercent = Math.Max(0, rentPaid - tenPercentBasic);
        var percentageBasic = annualBasic * (isMetroCity ? 0.50m : 0.40m);

        var hraExemption = Math.Min(Math.Min(hraReceived, rentMinusTenPercent), percentageBasic);

        return new HraCalculationDto
        {
            ActualHraReceived = hraReceived,
            RentPaid = rentPaid,
            TenPercentBasic = tenPercentBasic,
            RentMinusTenPercentBasic = rentMinusTenPercent,
            FiftyOrFortyPercentBasic = percentageBasic,
            IsMetroCity = isMetroCity,
            HraExemption = hraExemption
        };
    }

    /// <summary>
    /// Calculate tax by slabs
    /// </summary>
    private List<TaxSlabBreakdownDto> CalculateTaxBySlabs(decimal taxableIncome, List<Core.Entities.Payroll.TaxSlab> slabs)
    {
        var breakdown = new List<TaxSlabBreakdownDto>();
        var remainingIncome = taxableIncome;

        foreach (var slab in slabs.OrderBy(s => s.MinIncome))
        {
            if (remainingIncome <= 0)
                break;

            var slabMin = slab.MinIncome;
            var slabMax = slab.MaxIncome ?? decimal.MaxValue;
            var slabRange = slabMax - slabMin;

            var taxableInSlab = Math.Min(remainingIncome, slabRange);

            if (taxableInSlab > 0)
            {
                var taxAmount = Math.Round(taxableInSlab * slab.Rate / 100, 0, MidpointRounding.AwayFromZero);

                breakdown.Add(new TaxSlabBreakdownDto
                {
                    MinIncome = slabMin,
                    MaxIncome = slab.MaxIncome,
                    Rate = slab.Rate,
                    TaxableAmount = taxableInSlab,
                    TaxAmount = taxAmount
                });

                remainingIncome -= taxableInSlab;
            }
        }

        return breakdown;
    }

    /// <summary>
    /// Calculate Section 87A rebate (parameterized from database)
    /// New Regime: If taxable income <= threshold, tax liability is NIL (100% rebate up to max)
    /// Old Regime: If taxable income <= threshold, tax liability is NIL (100% rebate up to max)
    /// </summary>
    private decimal CalculateSection87aRebate(decimal taxableIncome, string taxRegime, decimal taxOnIncome, Dictionary<string, decimal> taxParams)
    {
        if (taxRegime == "new")
        {
            var threshold = taxParams.GetValueOrDefault("REBATE_87A_THRESHOLD", DEFAULT_REBATE_THRESHOLD_NEW);
            var maxRebate = taxParams.GetValueOrDefault("REBATE_87A_MAX", DEFAULT_REBATE_MAX_NEW);
            if (taxableIncome <= threshold)
            {
                return Math.Min(taxOnIncome, maxRebate);
            }
        }
        else
        {
            var threshold = taxParams.GetValueOrDefault("REBATE_87A_THRESHOLD", DEFAULT_REBATE_THRESHOLD_OLD);
            var maxRebate = taxParams.GetValueOrDefault("REBATE_87A_MAX", DEFAULT_REBATE_MAX_OLD);
            if (taxableIncome <= threshold)
            {
                return Math.Min(taxOnIncome, maxRebate);
            }
        }

        return 0;
    }

    /// <summary>
    /// Apply marginal relief for Section 87A rebate threshold.
    /// When income slightly exceeds the rebate threshold, the total tax payable
    /// should not exceed the amount by which income exceeds the threshold.
    /// This prevents the "tax cliff" effect at the rebate boundary.
    ///
    /// Example (FY 2025-26 New Regime, threshold ₹12L):
    /// - At ₹12,00,000: Tax = ₹60,000, Rebate = ₹60,000, Net = ₹0
    /// - At ₹12,69,000: Tax = ₹70,350 + Cess = ₹73,164
    /// - Excess income = ₹69,000
    /// - With marginal relief: Tax capped at ₹69,000
    /// </summary>
    /// <param name="taxableIncome">Total taxable income</param>
    /// <param name="totalTaxLiability">Total tax including cess (before marginal relief)</param>
    /// <param name="taxRegime">Tax regime (new/old)</param>
    /// <param name="taxParams">Tax parameters from database</param>
    /// <returns>Tax liability after applying marginal relief (may be reduced)</returns>
    private decimal ApplySection87aMarginalRelief(
        decimal taxableIncome,
        decimal totalTaxLiability,
        string taxRegime,
        Dictionary<string, decimal> taxParams)
    {
        var threshold = taxRegime == "new"
            ? taxParams.GetValueOrDefault("REBATE_87A_THRESHOLD", DEFAULT_REBATE_THRESHOLD_NEW)
            : taxParams.GetValueOrDefault("REBATE_87A_THRESHOLD", DEFAULT_REBATE_THRESHOLD_OLD);

        // Marginal relief only applies when income exceeds threshold
        if (taxableIncome <= threshold)
            return totalTaxLiability;

        // Calculate excess income above threshold
        var excessIncome = taxableIncome - threshold;

        // If total tax exceeds excess income, cap it at excess income
        // This ensures taxpayer doesn't pay more tax than their excess income
        if (totalTaxLiability > excessIncome)
        {
            return excessIncome;
        }

        return totalTaxLiability;
    }

    /// <summary>
    /// Calculate surcharge with marginal relief.
    /// Marginal relief ensures that tax + surcharge doesn't exceed:
    /// (Tax at threshold) + (Income exceeding threshold)
    /// This prevents the "tax cliff" effect where crossing a threshold by ₹1
    /// could result in disproportionately higher total tax.
    /// </summary>
    /// <param name="taxableIncome">Total taxable income</param>
    /// <param name="taxAmount">Tax amount before surcharge (after rebate)</param>
    /// <param name="taxParams">Tax parameters from database</param>
    /// <param name="taxSlabs">Tax slabs for calculating tax at threshold</param>
    /// <returns>Surcharge amount after applying marginal relief</returns>
    private decimal CalculateSurchargeWithMarginalRelief(
        decimal taxableIncome,
        decimal taxAmount,
        Dictionary<string, decimal> taxParams,
        List<Core.Entities.Payroll.TaxSlab> taxSlabs)
    {
        // Get surcharge thresholds (defaults to FY 2024-25 values)
        var threshold50L = taxParams.GetValueOrDefault("SURCHARGE_THRESHOLD_50L", 5000000m);  // 50 lakh
        var threshold1Cr = taxParams.GetValueOrDefault("SURCHARGE_THRESHOLD_1CR", 10000000m); // 1 crore
        var threshold2Cr = taxParams.GetValueOrDefault("SURCHARGE_THRESHOLD_2CR", 20000000m); // 2 crore
        var threshold5Cr = taxParams.GetValueOrDefault("SURCHARGE_THRESHOLD_5CR", 50000000m); // 5 crore

        // Get surcharge rates (as percentages)
        var rate50L = taxParams.GetValueOrDefault("SURCHARGE_RATE_50L", 10m) / 100m;
        var rate1Cr = taxParams.GetValueOrDefault("SURCHARGE_RATE_1CR", 15m) / 100m;
        var rate2Cr = taxParams.GetValueOrDefault("SURCHARGE_RATE_2CR", 25m) / 100m;
        var rateMax = taxParams.GetValueOrDefault("SURCHARGE_MAX_RATE", 25m) / 100m; // Regime-specific cap

        // No surcharge if below lowest threshold
        if (taxableIncome <= threshold50L)
            return 0;

        // Determine applicable threshold and rate
        decimal applicableThreshold;
        decimal surchargeRate;

        if (taxableIncome <= threshold1Cr)
        {
            applicableThreshold = threshold50L;
            surchargeRate = rate50L;
        }
        else if (taxableIncome <= threshold2Cr)
        {
            applicableThreshold = threshold1Cr;
            surchargeRate = rate1Cr;
        }
        else if (taxableIncome <= threshold5Cr)
        {
            applicableThreshold = threshold2Cr;
            surchargeRate = rate2Cr;
        }
        else
        {
            applicableThreshold = threshold5Cr;
            surchargeRate = rateMax;
        }

        // Calculate normal surcharge (without marginal relief)
        var normalSurcharge = Math.Round(taxAmount * surchargeRate, 0, MidpointRounding.AwayFromZero);

        // Calculate marginal relief
        // Tax at threshold (no surcharge applies at threshold)
        var taxAtThreshold = CalculateTaxForIncome(applicableThreshold, taxSlabs);
        var incomeAboveThreshold = taxableIncome - applicableThreshold;

        // Maximum allowed: Total tax + surcharge should not exceed (Tax at threshold + excess income)
        // This ensures that the marginal rate on excess income is never > 100%
        var maxAllowedTotalTax = taxAtThreshold + incomeAboveThreshold;
        var totalTaxWithNormalSurcharge = taxAmount + normalSurcharge;

        if (totalTaxWithNormalSurcharge > maxAllowedTotalTax)
        {
            // Apply marginal relief: reduce surcharge so total doesn't exceed max allowed
            var reliefAmount = totalTaxWithNormalSurcharge - maxAllowedTotalTax;
            return Math.Max(0, normalSurcharge - reliefAmount);
        }

        return normalSurcharge;
    }

    /// <summary>
    /// Calculate tax for a specific income amount using tax slabs.
    /// Helper method for marginal relief calculation.
    /// </summary>
    private decimal CalculateTaxForIncome(decimal income, List<Core.Entities.Payroll.TaxSlab> slabs)
    {
        decimal totalTax = 0;
        var remainingIncome = income;

        foreach (var slab in slabs.OrderBy(s => s.MinIncome))
        {
            if (remainingIncome <= 0)
                break;

            var slabMin = slab.MinIncome;
            var slabMax = slab.MaxIncome ?? decimal.MaxValue;
            var slabRange = slabMax - slabMin;

            var taxableInSlab = Math.Min(remainingIncome, slabRange);

            if (taxableInSlab > 0)
            {
                totalTax += taxableInSlab * slab.Rate / 100;
                remainingIncome -= taxableInSlab;
            }
        }

        return Math.Round(totalTax, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Determine taxpayer category based on age at end of financial year.
    /// Senior citizens (60-79 years) and super senior citizens (80+ years)
    /// get higher exemption limits under the Old Regime.
    /// New Regime has no age-based differentiation.
    /// </summary>
    /// <param name="dateOfBirth">Employee's date of birth</param>
    /// <param name="financialYear">Financial year in format '2024-25'</param>
    /// <returns>'all' (general), 'senior' (60-79), or 'super_senior' (80+)</returns>
    private string DetermineTaxCategory(DateTime? dateOfBirth, string financialYear)
    {
        if (dateOfBirth == null)
            return "all";

        // Parse financial year end date (March 31 of end year)
        // Format: '2024-25' means FY ends on March 31, 2025
        var fyParts = financialYear.Split('-');
        if (fyParts.Length != 2 || !int.TryParse(fyParts[0], out var startYear))
            return "all";

        var endYear = startYear + 1;
        var fyEndDate = new DateTime(endYear, 3, 31);

        // Calculate age at end of financial year
        var age = fyEndDate.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > fyEndDate.AddYears(-age))
            age--;

        // Determine category based on age
        if (age >= 80)
            return "super_senior";
        if (age >= 60)
            return "senior";

        return "all";
    }

    /// <summary>
    /// Simple TDS calculation for contractors (backward compatible)
    /// </summary>
    public decimal CalculateContractorTds(decimal grossAmount, decimal tdsRate = 10.0m)
    {
        return Math.Round(grossAmount * tdsRate / 100, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculate contractor TDS based on section and PAN availability.
    /// Section 194C: Contractors/sub-contractors (2% for individuals, 1% for transporters)
    /// Section 194J: Professional/technical services (10%)
    /// No PAN: 20% higher rate applies if PAN is not provided (Section 206AA)
    /// Now uses ITaxRateProvider for FY-versioned rates.
    /// </summary>
    /// <param name="grossAmount">Gross payment amount</param>
    /// <param name="section">TDS section: '194C' or '194J'</param>
    /// <param name="contractorPan">Contractor's PAN number (null/empty triggers 20% higher rate)</param>
    /// <param name="payeeType">Type of payee: "individual", "company", etc.</param>
    /// <param name="transactionDate">Transaction date for FY determination</param>
    /// <returns>Tuple of (TDS amount, effective TDS rate, threshold amount, is exempt)</returns>
    public async Task<ContractorTdsResult> CalculateContractorTdsWithSectionAsync(
        decimal grossAmount,
        string section = "194J",
        string? contractorPan = null,
        string payeeType = "individual",
        DateTime? transactionDate = null)
    {
        var date = transactionDate ?? DateTime.Today;
        var hasPan = !string.IsNullOrWhiteSpace(contractorPan);

        // Try to get rate from provider (Rule Packs or Legacy)
        var tdsRateInfo = await _taxRateProvider.GetTdsRateAsync(section, payeeType, hasPan, date);

        if (tdsRateInfo != null)
        {
            var isExempt = tdsRateInfo.ThresholdAmount.HasValue && grossAmount <= tdsRateInfo.ThresholdAmount.Value;
            var tdsAmount = isExempt
                ? 0m
                : Math.Round(grossAmount * tdsRateInfo.Rate / 100, 0, MidpointRounding.AwayFromZero);

            return new ContractorTdsResult
            {
                TdsAmount = tdsAmount,
                EffectiveRate = isExempt ? 0m : tdsRateInfo.Rate,
                ThresholdAmount = tdsRateInfo.ThresholdAmount,
                IsExempt = isExempt,
                SectionCode = section,
                RulePackId = await _taxRateProvider.GetActiveRulePackIdAsync(GetFinancialYear(date))
            };
        }

        // Fallback to hardcoded rates
        var baseRate = section.ToUpperInvariant() switch
        {
            "194C" => payeeType.ToLower() == "individual" || payeeType.ToLower() == "huf" ? 1.0m : 2.0m,
            "194J" => 10.0m,
            "194H" => 2.0m, // Updated for FY 2025-26
            _ => 10.0m
        };

        var effectiveRate = hasPan ? baseRate : 20.0m;
        var amount = Math.Round(grossAmount * effectiveRate / 100, 0, MidpointRounding.AwayFromZero);

        return new ContractorTdsResult
        {
            TdsAmount = amount,
            EffectiveRate = effectiveRate,
            ThresholdAmount = null,
            IsExempt = false,
            SectionCode = section,
            RulePackId = null
        };
    }

    /// <summary>
    /// Synchronous version for backward compatibility (uses hardcoded rates)
    /// Deprecated: Use CalculateContractorTdsWithSectionAsync instead
    /// </summary>
    [Obsolete("Use CalculateContractorTdsWithSectionAsync for FY-versioned rates")]
    public (decimal TdsAmount, decimal EffectiveRate) CalculateContractorTdsWithSection(
        decimal grossAmount,
        string section = "194J",
        string? contractorPan = null)
    {
        // Default rates by section
        var baseRate = section.ToUpperInvariant() switch
        {
            "194C" => 2.0m,   // Contractors (individuals)
            "194J" => 10.0m,  // Professional/Technical services
            _ => 10.0m        // Default to 194J rate
        };

        // Section 206AA: Higher TDS rate of 20% if PAN not provided
        var effectiveRate = string.IsNullOrWhiteSpace(contractorPan) ? 20.0m : baseRate;

        var tdsAmount = Math.Round(grossAmount * effectiveRate / 100, 0, MidpointRounding.AwayFromZero);

        return (tdsAmount, effectiveRate);
    }

    private static string GetFinancialYear(DateTime date)
    {
        var year = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{year}-{(year + 1) % 100:D2}";
    }

    /// <summary>
    /// Get the default TDS rate for a given section
    /// </summary>
    public static decimal GetDefaultTdsRateForSection(string section)
    {
        return section.ToUpperInvariant() switch
        {
            "194C" => 2.0m,
            "194J" => 10.0m,
            _ => 10.0m
        };
    }

    // ==================== LDC Integration Methods ====================

    /// <summary>
    /// Calculate contractor TDS with automatic Lower Deduction Certificate (Form 13) check.
    /// If a valid LDC exists for the deductee, applies the certificate rate instead of normal rate.
    /// This is the recommended method for all contractor/vendor payments.
    /// </summary>
    /// <param name="companyId">Company ID (deductor)</param>
    /// <param name="grossAmount">Gross payment amount</param>
    /// <param name="section">TDS section: '194C', '194J', '194I', etc.</param>
    /// <param name="deducteePan">Deductee's PAN number</param>
    /// <param name="payeeType">Type of payee: "individual", "company", etc.</param>
    /// <param name="transactionDate">Transaction date for FY and LDC validation</param>
    /// <param name="recordUsage">If true, records LDC usage for audit trail</param>
    /// <param name="transactionType">Transaction type for usage record (e.g., 'contractor_payment')</param>
    /// <param name="transactionId">Transaction ID for usage record linking</param>
    /// <returns>TDS calculation result with LDC information</returns>
    public async Task<ContractorTdsWithLdcResult> CalculateContractorTdsWithCertificateAsync(
        Guid companyId,
        decimal grossAmount,
        string section = "194J",
        string? deducteePan = null,
        string payeeType = "individual",
        DateTime? transactionDate = null,
        bool recordUsage = false,
        string? transactionType = null,
        Guid? transactionId = null)
    {
        var date = transactionDate ?? DateTime.Today;
        var dateOnly = DateOnly.FromDateTime(date);
        var hasPan = !string.IsNullOrWhiteSpace(deducteePan);

        var result = new ContractorTdsWithLdcResult
        {
            SectionCode = section,
            GrossAmount = grossAmount,
            TransactionDate = date
        };

        // Check for valid LDC if repository is available and PAN is provided
        LdcValidationResult? ldcValidation = null;
        if (_ldcRepository != null && hasPan)
        {
            ldcValidation = await _ldcRepository.ValidateCertificateAsync(
                companyId,
                deducteePan!,
                section,
                dateOnly,
                grossAmount);

            if (ldcValidation.IsValid)
            {
                result.LdcApplied = true;
                result.LdcCertificateId = ldcValidation.CertificateId;
                result.LdcCertificateNumber = ldcValidation.CertificateNumber;
                result.LdcCertificateType = ldcValidation.CertificateType;
                result.NormalRate = ldcValidation.NormalRate;
                result.EffectiveRate = ldcValidation.CertificateRate;
                result.RemainingThreshold = ldcValidation.RemainingThreshold;
                result.ValidationMessage = ldcValidation.ValidationMessage;

                // Calculate TDS at certificate rate
                result.TdsAmount = Math.Round(grossAmount * ldcValidation.CertificateRate / 100, 0, MidpointRounding.AwayFromZero);
                result.TdsSavings = Math.Round(grossAmount * ldcValidation.NormalRate / 100, 0, MidpointRounding.AwayFromZero) - result.TdsAmount;
                result.IsExempt = ldcValidation.CertificateType == "nil";

                // Record LDC usage if requested
                if (recordUsage && ldcValidation.CertificateId.HasValue)
                {
                    var usageRecord = new LdcUsageRecord
                    {
                        CertificateId = ldcValidation.CertificateId.Value,
                        CompanyId = companyId,
                        TransactionDate = dateOnly,
                        TransactionType = transactionType ?? "contractor_payment",
                        TransactionId = transactionId,
                        GrossAmount = grossAmount,
                        NormalTdsAmount = Math.Round(grossAmount * ldcValidation.NormalRate / 100, 0, MidpointRounding.AwayFromZero),
                        ActualTdsAmount = result.TdsAmount,
                        TdsSavings = result.TdsSavings,
                        CreatedAt = DateTime.UtcNow
                    };

                    result.LdcUsageRecordId = await _ldcRepository.RecordUsageAsync(usageRecord);
                    await _ldcRepository.UpdateUtilizedAmountAsync(ldcValidation.CertificateId.Value, grossAmount);
                }

                result.RulePackId = await _taxRateProvider.GetActiveRulePackIdAsync(GetFinancialYear(date));
                return result;
            }
        }

        // No valid LDC - use standard TDS calculation
        var standardResult = await CalculateContractorTdsWithSectionAsync(
            grossAmount,
            section,
            deducteePan,
            payeeType,
            transactionDate);

        result.TdsAmount = standardResult.TdsAmount;
        result.EffectiveRate = standardResult.EffectiveRate;
        result.NormalRate = standardResult.EffectiveRate;
        result.ThresholdAmount = standardResult.ThresholdAmount;
        result.IsExempt = standardResult.IsExempt;
        result.RulePackId = standardResult.RulePackId;
        result.LdcApplied = false;
        result.TdsSavings = 0;

        if (ldcValidation != null && !ldcValidation.IsValid)
        {
            result.ValidationMessage = ldcValidation.ValidationMessage;
        }

        return result;
    }

    /// <summary>
    /// Check if a valid Lower Deduction Certificate exists for a deductee
    /// </summary>
    /// <param name="companyId">Company ID (deductor)</param>
    /// <param name="deducteePan">Deductee's PAN number</param>
    /// <param name="section">TDS section</param>
    /// <param name="transactionDate">Transaction date</param>
    /// <returns>True if valid certificate exists</returns>
    public async Task<bool> HasValidLdcAsync(
        Guid companyId,
        string deducteePan,
        string section,
        DateTime? transactionDate = null)
    {
        if (_ldcRepository == null)
            return false;

        var date = transactionDate ?? DateTime.Today;
        var dateOnly = DateOnly.FromDateTime(date);

        return await _ldcRepository.HasValidCertificateAsync(companyId, deducteePan, section, dateOnly);
    }

    /// <summary>
    /// Get LDC validation details for a potential transaction
    /// Useful for pre-checking certificate availability before making payment
    /// </summary>
    public async Task<LdcValidationResult?> ValidateLdcForTransactionAsync(
        Guid companyId,
        string deducteePan,
        string section,
        decimal amount,
        DateTime? transactionDate = null)
    {
        if (_ldcRepository == null)
            return null;

        var date = transactionDate ?? DateTime.Today;
        var dateOnly = DateOnly.FromDateTime(date);

        return await _ldcRepository.ValidateCertificateAsync(companyId, deducteePan, section, dateOnly, amount);
    }
}

/// <summary>
/// Extension methods for OldRegimeDeductionsDto
/// </summary>
public static class OldRegimeDeductionsExtensions
{
    public static decimal TotalAllowedDeductions(this OldRegimeDeductionsDto deductions)
    {
        return deductions.Section80cAllowed +
               deductions.Section80ccdAllowed +
               deductions.Section80dAllowed +
               deductions.HraExemption +
               deductions.Section80eEducationLoan +
               deductions.Section24Allowed +
               deductions.Section80gDonations +
               deductions.Section80ttaAllowed;
    }
}

/// <summary>
/// Result of contractor TDS calculation with Rule Pack tracking
/// </summary>
public class ContractorTdsResult
{
    public decimal TdsAmount { get; set; }
    public decimal EffectiveRate { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public bool IsExempt { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public Guid? RulePackId { get; set; }
}

/// <summary>
/// Result of contractor TDS calculation with Lower Deduction Certificate (Form 13) integration
/// </summary>
public class ContractorTdsWithLdcResult
{
    // ==================== Basic TDS Info ====================
    public decimal TdsAmount { get; set; }
    public decimal EffectiveRate { get; set; }
    public decimal NormalRate { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public bool IsExempt { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public Guid? RulePackId { get; set; }
    public decimal GrossAmount { get; set; }
    public DateTime TransactionDate { get; set; }

    // ==================== LDC Information ====================

    /// <summary>
    /// True if a valid Lower Deduction Certificate was applied
    /// </summary>
    public bool LdcApplied { get; set; }

    /// <summary>
    /// Certificate ID if LDC was applied
    /// </summary>
    public Guid? LdcCertificateId { get; set; }

    /// <summary>
    /// Certificate number from IT department
    /// </summary>
    public string? LdcCertificateNumber { get; set; }

    /// <summary>
    /// Certificate type: 'lower' or 'nil'
    /// </summary>
    public string? LdcCertificateType { get; set; }

    /// <summary>
    /// TDS savings due to LDC (normal TDS minus actual TDS)
    /// </summary>
    public decimal TdsSavings { get; set; }

    /// <summary>
    /// Remaining threshold on the certificate (if applicable)
    /// </summary>
    public decimal? RemainingThreshold { get; set; }

    /// <summary>
    /// Usage record ID if usage was recorded
    /// </summary>
    public Guid? LdcUsageRecordId { get; set; }

    /// <summary>
    /// Validation message (e.g., why LDC was not applied)
    /// </summary>
    public string? ValidationMessage { get; set; }

    // ==================== Summary Properties ====================

    /// <summary>
    /// Rate reduction percentage due to LDC
    /// </summary>
    public decimal RateReduction => LdcApplied ? NormalRate - EffectiveRate : 0;

    /// <summary>
    /// Savings percentage (TDS savings as percentage of normal TDS)
    /// </summary>
    public decimal SavingsPercentage
    {
        get
        {
            var normalTds = Math.Round(GrossAmount * NormalRate / 100, 0);
            return normalTds > 0 ? Math.Round(TdsSavings / normalTds * 100, 2) : 0;
        }
    }
}
