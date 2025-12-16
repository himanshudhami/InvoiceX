using Application.DTOs.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Employee State Insurance (ESI) calculations
/// Indian ESI rules:
/// - Applicable when gross salary <= ₹21,000/month
/// - Employee contribution: 0.75%
/// - Employer contribution: 3.25%
/// - Total: 4%
/// </summary>
public class EsiCalculationService
{
    /// <summary>
    /// Calculate ESI contributions for an employee
    /// </summary>
    public EsiCalculationDto Calculate(
        decimal grossSalary,
        decimal esiWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        bool isEsiApplicable)
    {
        var result = new EsiCalculationDto
        {
            GrossSalary = grossSalary,
            EsiWageCeiling = esiWageCeiling,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate
        };

        // ESI is only applicable if:
        // 1. Company has ESI enabled
        // 2. Employee is marked as ESI applicable
        // 3. Gross salary is within the ceiling
        result.IsApplicable = isEsiApplicable && grossSalary <= esiWageCeiling;

        if (!result.IsApplicable)
        {
            return result;
        }

        // Employee contribution
        result.EmployeeContribution = Math.Round(grossSalary * employeeRate / 100, 0, MidpointRounding.AwayFromZero);

        // Employer contribution
        result.EmployerContribution = Math.Round(grossSalary * employerRate / 100, 0, MidpointRounding.AwayFromZero);

        return result;
    }

    /// <summary>
    /// Calculate ESI with prorated amounts for partial month
    /// </summary>
    public EsiCalculationDto CalculateProrated(
        decimal fullGrossSalary,
        decimal esiWageCeiling,
        decimal employeeRate,
        decimal employerRate,
        bool isEsiApplicable,
        int workingDays,
        int presentDays)
    {
        if (workingDays <= 0 || presentDays <= 0)
        {
            return new EsiCalculationDto
            {
                GrossSalary = fullGrossSalary,
                EsiWageCeiling = esiWageCeiling,
                EmployeeRate = employeeRate,
                EmployerRate = employerRate,
                IsApplicable = false
            };
        }

        // ESI applicability is based on full month gross, not prorated
        // But contributions are calculated on actual gross earned
        var proratedGross = Math.Round(fullGrossSalary * presentDays / workingDays, 2, MidpointRounding.AwayFromZero);

        var result = new EsiCalculationDto
        {
            GrossSalary = proratedGross,
            EsiWageCeiling = esiWageCeiling,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            IsApplicable = isEsiApplicable && fullGrossSalary <= esiWageCeiling
        };

        if (!result.IsApplicable)
        {
            return result;
        }

        result.EmployeeContribution = Math.Round(proratedGross * employeeRate / 100, 0, MidpointRounding.AwayFromZero);
        result.EmployerContribution = Math.Round(proratedGross * employerRate / 100, 0, MidpointRounding.AwayFromZero);

        return result;
    }

    /// <summary>
    /// Check if ESI is applicable based on gross salary and ceiling
    /// </summary>
    public bool IsEsiApplicable(decimal grossSalary, decimal esiWageCeiling)
    {
        return grossSalary <= esiWageCeiling;
    }

    /// <summary>
    /// Calculate annual ESI contributions for budgeting
    /// </summary>
    public (decimal AnnualEmployeeContribution, decimal AnnualEmployerContribution) CalculateAnnual(
        decimal monthlyGross,
        decimal esiWageCeiling,
        decimal employeeRate,
        decimal employerRate)
    {
        if (monthlyGross > esiWageCeiling)
        {
            return (0, 0);
        }

        var monthly = Calculate(monthlyGross, esiWageCeiling, employeeRate, employerRate, true);
        return (monthly.EmployeeContribution * 12, monthly.EmployerContribution * 12);
    }
}
