using Application.DTOs.Payroll;
using Core.Interfaces.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Tax Deducted at Source (TDS) calculations
/// Supports both Old and New tax regimes for Indian income tax
/// Tax parameters are loaded from database for configurability
/// </summary>
public class TdsCalculationService
{
    private readonly ITaxSlabRepository _taxSlabRepository;
    private readonly ITaxParameterRepository _taxParameterRepository;

    // Default fallback values (used if DB parameters not found)
    private const decimal DEFAULT_STANDARD_DEDUCTION_NEW = 75000m;
    private const decimal DEFAULT_STANDARD_DEDUCTION_OLD = 50000m;
    private const decimal DEFAULT_CESS_RATE = 4m;
    private const decimal DEFAULT_REBATE_THRESHOLD_NEW = 700000m;
    private const decimal DEFAULT_REBATE_THRESHOLD_OLD = 500000m;
    private const decimal DEFAULT_REBATE_MAX_NEW = 25000m;
    private const decimal DEFAULT_REBATE_MAX_OLD = 12500m;

    public TdsCalculationService(
        ITaxSlabRepository taxSlabRepository,
        ITaxParameterRepository taxParameterRepository)
    {
        _taxSlabRepository = taxSlabRepository ?? throw new ArgumentNullException(nameof(taxSlabRepository));
        _taxParameterRepository = taxParameterRepository ?? throw new ArgumentNullException(nameof(taxParameterRepository));
    }

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

        // Fetch tax parameters from database
        var taxParams = await _taxParameterRepository.GetAllParametersForRegimeAsync(taxRegime, financialYear);

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

        // Get tax slabs and calculate tax (uses category-based lookup)
        var taxSlabs = await _taxSlabRepository.GetByRegimeYearAndCategoryAsync(taxRegime, financialYear, taxCategory);
        result.TaxSlabs = CalculateTaxBySlabs(result.TaxableIncome, taxSlabs.ToList());
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

        // Calculate cess (parameterized) on tax after rebate and surcharge
        var cessRate = taxParams.GetValueOrDefault("CESS_RATE", DEFAULT_CESS_RATE) / 100m;
        result.Cess = Math.Round((result.TaxOnIncomeAfterRebate + result.Surcharge) * cessRate, 0, MidpointRounding.AwayFromZero);

        // Total tax liability (after rebate, surcharge, and cess)
        result.TotalTaxLiability = result.TaxOnIncomeAfterRebate + result.Surcharge + result.Cess;

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
    /// </summary>
    /// <param name="grossAmount">Gross payment amount</param>
    /// <param name="section">TDS section: '194C' or '194J'</param>
    /// <param name="contractorPan">Contractor's PAN number (null/empty triggers 20% higher rate)</param>
    /// <returns>Tuple of (TDS amount, effective TDS rate)</returns>
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
