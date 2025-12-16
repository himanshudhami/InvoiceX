using Application.DTOs.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Provident Fund (PF) calculations
/// Indian PF rules:
/// - Employee contribution: 12% of Basic + DA (capped at ₹15,000)
/// - Employer contribution: 12% of Basic + DA (split into EPF and EPS)
///   - 8.33% to EPS (Pension Fund, capped at ₹15,000 wage base)
///   - Remaining to EPF
/// - Admin charges: 0.5% of PF wage (minimum ₹500 per month applies at establishment level, not per employee)
/// - EDLI charges: 0.5% of PF wage (capped at ₹15,000 wage base)
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
    public PfCalculationDto Calculate(
        decimal basicSalary,
        decimal dearnessAllowance,
        decimal specialAllowance,
        bool includeSpecialAllowance,
        decimal pfWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        bool isPfApplicable)
    {
        var result = new PfCalculationDto
        {
            BasicSalary = basicSalary,
            PfWageCeiling = pfWageCeiling,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate
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

        // Apply ceiling if configured
        var pfWageBase = pfWageCeiling > 0 ? Math.Min(pfWage, pfWageCeiling) : pfWage;
        result.PfWageBase = pfWageBase;
        result.CeilingApplied = pfWageCeiling > 0 && pfWage > pfWageCeiling;

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
        int presentDays)
    {
        if (workingDays <= 0 || presentDays <= 0)
        {
            return new PfCalculationDto
            {
                BasicSalary = basicSalary,
                PfWageCeiling = pfWageCeiling,
                EmployeeRate = employeeRate,
                EmployerRate = employerRate
            };
        }

        // Prorate the basic, DA, and special allowance based on attendance
        var proratedBasic = Math.Round(basicSalary * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);
        var proratedDa = Math.Round(dearnessAllowance * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);
        var proratedSpecial = Math.Round(specialAllowance * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);

        return Calculate(proratedBasic, proratedDa, proratedSpecial, includeSpecialAllowance, pfWageCeiling, employeeRate, employerRate, isPfApplicable);
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
        decimal employerRate)
    {
        var monthly = Calculate(monthlyBasic, monthlyDa, monthlySpecialAllowance, includeSpecialAllowance, pfWageCeiling, employeeRate, employerRate, true);

        return (
            monthly.EmployeeContribution * 12,
            monthly.EmployerContribution * 12,
            monthly.TotalEmployerCost * 12
        );
    }
}
