namespace Application.DTOs.Payroll;

/// <summary>
/// TDS calculation breakdown for an employee
/// </summary>
public class TaxCalculationDto
{
    public Guid EmployeeId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = "new";

    // Income
    public decimal AnnualGrossIncome { get; set; }
    public decimal AnnualBasic { get; set; }
    public decimal AnnualHra { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal PreviousEmployerIncome { get; set; }
    public decimal TotalGrossIncome { get; set; }

    // Standard Deduction (New Regime)
    public decimal StandardDeduction { get; set; }

    // Old Regime Deductions
    public OldRegimeDeductionsDto? OldRegimeDeductions { get; set; }

    // Taxable Income Calculation
    public decimal TotalDeductions { get; set; }
    public decimal TaxableIncome { get; set; }

    // Tax Calculation
    public List<TaxSlabBreakdownDto> TaxSlabs { get; set; } = new();
    public decimal TaxOnIncome { get; set; }
    public decimal Section87aRebate { get; set; }
    public decimal TaxOnIncomeAfterRebate { get; set; }
    public decimal Surcharge { get; set; }
    public decimal Cess { get; set; }
    public decimal TotalTaxLiability { get; set; }

    // Credits
    public decimal PreviousEmployerTds { get; set; }
    public decimal TdsPaidYtd { get; set; }

    // Final Calculation
    public decimal RemainingTaxLiability { get; set; }
    public int RemainingMonths { get; set; }
    public decimal MonthlyTds { get; set; }
}

public class OldRegimeDeductionsDto
{
    // Section 80C
    public decimal Section80cTotal { get; set; }
    public decimal Section80cLimit { get; set; } = 150000m;
    public decimal Section80cAllowed { get; set; }

    // Section 80CCD(1B)
    public decimal Section80ccdNps { get; set; }
    public decimal Section80ccdLimit { get; set; } = 50000m;
    public decimal Section80ccdAllowed { get; set; }

    // Section 80D
    public decimal Section80dTotal { get; set; }
    public decimal Section80dLimit { get; set; }
    public decimal Section80dAllowed { get; set; }

    // HRA Exemption
    public decimal HraReceived { get; set; }
    public decimal HraExemption { get; set; }
    public HraCalculationDto? HraCalculation { get; set; }

    // Section 80E
    public decimal Section80eEducationLoan { get; set; }

    // Section 24
    public decimal Section24HomeLoanInterest { get; set; }
    public decimal Section24Limit { get; set; } = 200000m;
    public decimal Section24Allowed { get; set; }

    // Section 80G
    public decimal Section80gDonations { get; set; }

    // Section 80TTA
    public decimal Section80ttaSavingsInterest { get; set; }
    public decimal Section80ttaLimit { get; set; } = 10000m;
    public decimal Section80ttaAllowed { get; set; }
}

public class HraCalculationDto
{
    public decimal ActualHraReceived { get; set; }
    public decimal RentPaid { get; set; }
    public decimal TenPercentBasic { get; set; }
    public decimal RentMinusTenPercentBasic { get; set; }
    public decimal FiftyOrFortyPercentBasic { get; set; }
    public bool IsMetroCity { get; set; }
    public decimal HraExemption { get; set; }
}

public class TaxSlabBreakdownDto
{
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

/// <summary>
/// PF calculation result
/// </summary>
public class PfCalculationDto
{
    public decimal BasicSalary { get; set; }

    /// <summary>
    /// Actual PF wage before any ceiling is applied (Basic + DA + optional Special Allowance)
    /// </summary>
    public decimal ActualPfWage { get; set; }

    /// <summary>
    /// PF wage base used for calculation (may be capped in ceiling_based mode)
    /// </summary>
    public decimal PfWageBase { get; set; }
    public decimal PfWageCeiling { get; set; }
    public bool CeilingApplied { get; set; }

    /// <summary>
    /// PF calculation mode: ceiling_based, actual_wage, or restricted_pf
    /// </summary>
    public string CalculationMode { get; set; } = "ceiling_based";

    // Employee Contribution
    public decimal EmployeeRate { get; set; }
    public decimal EmployeeContribution { get; set; }

    // Employer Contribution
    public decimal EmployerRate { get; set; }
    public decimal EmployerContribution { get; set; }
    public decimal EmployerPensionFund { get; set; }
    public decimal EmployerEpfContribution { get; set; }

    // Admin Charges
    public decimal AdminCharges { get; set; }
    public decimal EdliCharges { get; set; }

    public decimal TotalEmployerCost { get; set; }
}

/// <summary>
/// ESI calculation result
/// </summary>
public class EsiCalculationDto
{
    public decimal GrossSalary { get; set; }
    public decimal EsiWageCeiling { get; set; }
    public bool IsApplicable { get; set; }

    public decimal EmployeeRate { get; set; }
    public decimal EmployeeContribution { get; set; }

    public decimal EmployerRate { get; set; }
    public decimal EmployerContribution { get; set; }
}

/// <summary>
/// Professional Tax calculation result
/// </summary>
public class PtCalculationDto
{
    public string State { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public decimal TaxAmount { get; set; }
    public int PaymentMonth { get; set; }
}
