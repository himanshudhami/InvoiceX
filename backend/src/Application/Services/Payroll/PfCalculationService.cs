using Application.DTOs.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Provident Fund (PF) calculations
/// Indian PF rules:
/// - Employee contribution: 12% of Basic + DA (capped at ₹15,000 in ceiling mode)
/// - Employer contribution: 12% of Basic + DA (split into EPF and EPS)
///   - 8.33% to EPS (Pension Fund, capped at ₹15,000 wage base)
///   - Remaining to EPF
/// - Admin charges: 0.5% of PF wage (minimum ₹500 per month applies at establishment level, not per employee)
/// - EDLI charges: 0.5% of PF wage (capped at ₹15,000 wage base)
///
/// Calculation Modes:
/// - ceiling_based: 12% of PF wage capped at ceiling (default, statutory minimum)
/// - actual_wage: 12% of actual PF wage (no ceiling, full contribution)
/// - restricted_pf: For employees earning above ceiling who opt for lower PF
/// </summary>
public class PfCalculationService
{
    /// <summary>
    /// Calculate PF contributions for an employee
    /// </summary>
    /// <param name="basicSalary">Basic salary component</param>
    /// <param name="dearnessAllowance">Dearness allowance component</param>
    /// <param name="specialAllowance">Special allowance component (included in PF wage if flag is set)</param>
    /// <param name="includeSpecialAllowance">Whether to include special allowance in PF wage calculation</param>
    /// <param name="pfWageCeiling">PF wage ceiling (typically ₹15,000)</param>
    /// <param name="employeeRate">Employee contribution rate (typically 12%)</param>
    /// <param name="employerRate">Employer contribution rate (typically 12%)</param>
    /// <param name="isPfApplicable">Whether PF is applicable for this employee</param>
    /// <param name="calculationMode">PF calculation mode: ceiling_based, actual_wage, or restricted_pf</param>
    /// <param name="restrictedPfMaxWage">Maximum wage for restricted PF mode (default ₹15,000)</param>
    /// <param name="employeeOptedForRestrictedPf">Whether employee opted for restricted PF (for restricted_pf mode)</param>
    public PfCalculationDto Calculate(
        decimal basicSalary,
        decimal dearnessAllowance,
        decimal specialAllowance,
        bool includeSpecialAllowance,
        decimal pfWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        bool isPfApplicable,
        string calculationMode = "ceiling_based",
        decimal restrictedPfMaxWage = 15000m,
        bool employeeOptedForRestrictedPf = false)
    {
        var result = new PfCalculationDto
        {
            BasicSalary = basicSalary,
            PfWageCeiling = pfWageCeiling,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            CalculationMode = calculationMode
        };

        if (!isPfApplicable)
        {
            return result;
        }

        // PF wage = Basic + DA (+ Special Allowance if configured)
        var pfWage = basicSalary + dearnessAllowance;
        if (includeSpecialAllowance)
        {
            pfWage += specialAllowance;
        }

        // Calculate PF wage base based on calculation mode
        var pfWageBase = CalculatePfWageBase(pfWage, pfWageCeiling, calculationMode, restrictedPfMaxWage, employeeOptedForRestrictedPf);
        result.PfWageBase = pfWageBase;
        result.ActualPfWage = pfWage;
        result.CeilingApplied = pfWageBase < pfWage;

        // Employee contribution
        result.EmployeeContribution = Math.Round(pfWageBase * employeeRate / 100, 0, MidpointRounding.AwayFromZero);

        // Employer contribution (same rate as employee)
        var employerTotal = Math.Round(pfWageBase * employerRate / 100, 0, MidpointRounding.AwayFromZero);
        result.EmployerContribution = employerTotal;

        // Split employer contribution into EPS and EPF
        // EPS is 8.33% capped at ₹15,000 wage base
        var epsWageBase = Math.Min(pfWageBase, 15000m);
        result.EmployerPensionFund = Math.Round(epsWageBase * 8.33m / 100, 0, MidpointRounding.AwayFromZero);
        result.EmployerEpfContribution = employerTotal - result.EmployerPensionFund;

        // Admin charges (0.5% of PF wage base)
        // Note: The ₹500 minimum applies at the establishment/company level for all employees combined,
        // not per individual employee. This proportional calculation is correct per employee.
        result.AdminCharges = Math.Round(pfWageBase * 0.5m / 100, 0, MidpointRounding.AwayFromZero);

        // EDLI charges (0.5% of PF wage base, capped at wage base of ₹15,000)
        var edliWageBase = Math.Min(pfWageBase, 15000m);
        result.EdliCharges = Math.Round(edliWageBase * 0.5m / 100, 0, MidpointRounding.AwayFromZero);

        // Total employer cost
        result.TotalEmployerCost = result.EmployerContribution + result.AdminCharges + result.EdliCharges;

        return result;
    }

    /// <summary>
    /// Calculate PF with prorated amounts for LOP days
    /// </summary>
    public PfCalculationDto CalculateProrated(
        decimal basicSalary,
        decimal dearnessAllowance,
        decimal specialAllowance,
        bool includeSpecialAllowance,
        decimal pfWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        bool isPfApplicable,
        int workingDays,
        int presentDays,
        string calculationMode = "ceiling_based",
        decimal restrictedPfMaxWage = 15000m,
        bool employeeOptedForRestrictedPf = false)
    {
        if (workingDays <= 0 || presentDays <= 0)
        {
            return new PfCalculationDto
            {
                BasicSalary = basicSalary,
                PfWageCeiling = pfWageCeiling,
                EmployeeRate = employeeRate,
                EmployerRate = employerRate,
                CalculationMode = calculationMode
            };
        }

        // Prorate the basic, DA, and special allowance based on attendance
        var proratedBasic = Math.Round(basicSalary * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);
        var proratedDa = Math.Round(dearnessAllowance * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);
        var proratedSpecial = Math.Round(specialAllowance * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);

        return Calculate(
            proratedBasic,
            proratedDa,
            proratedSpecial,
            includeSpecialAllowance,
            pfWageCeiling,
            employeeRate,
            employerRate,
            isPfApplicable,
            calculationMode,
            restrictedPfMaxWage,
            employeeOptedForRestrictedPf);
    }

    /// <summary>
    /// Calculate annual PF contributions for budgeting/projections
    /// </summary>
    public (decimal AnnualEmployeeContribution, decimal AnnualEmployerContribution, decimal AnnualTotalCost) CalculateAnnual(
        decimal monthlyBasic,
        decimal monthlyDa,
        decimal monthlySpecialAllowance,
        bool includeSpecialAllowance,
        decimal pfWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        string calculationMode = "ceiling_based",
        decimal restrictedPfMaxWage = 15000m,
        bool employeeOptedForRestrictedPf = false)
    {
        var monthly = Calculate(
            monthlyBasic,
            monthlyDa,
            monthlySpecialAllowance,
            includeSpecialAllowance,
            pfWageCeiling,
            employeeRate,
            employerRate,
            true,
            calculationMode,
            restrictedPfMaxWage,
            employeeOptedForRestrictedPf);

        return (
            monthly.EmployeeContribution * 12,
            monthly.EmployerContribution * 12,
            monthly.TotalEmployerCost * 12
        );
    }

    /// <summary>
    /// Calculate PF wage base based on calculation mode
    /// </summary>
    /// <param name="pfWage">Total PF wage (Basic + DA + optional Special Allowance)</param>
    /// <param name="pfWageCeiling">PF wage ceiling (typically ₹15,000)</param>
    /// <param name="calculationMode">PF calculation mode</param>
    /// <param name="restrictedPfMaxWage">Maximum wage for restricted PF mode</param>
    /// <param name="employeeOptedForRestrictedPf">Whether employee opted for restricted PF</param>
    /// <returns>PF wage base to use for contribution calculation</returns>
    private static decimal CalculatePfWageBase(
        decimal pfWage,
        decimal pfWageCeiling,
        string calculationMode,
        decimal restrictedPfMaxWage,
        bool employeeOptedForRestrictedPf)
    {
        return calculationMode switch
        {
            // Actual wage mode: No ceiling applied - 12% of full PF wage
            "actual_wage" => pfWage,

            // Restricted PF mode: Only for employees who opt for lower PF contribution
            // If employee hasn't opted, use full PF wage
            "restricted_pf" when employeeOptedForRestrictedPf =>
                Math.Min(pfWage, restrictedPfMaxWage),
            "restricted_pf" => pfWage,

            // Ceiling-based mode (default): Apply wage ceiling
            _ => pfWageCeiling > 0 ? Math.Min(pfWage, pfWageCeiling) : pfWage
        };
    }
}
