using System;
using System.Collections.Generic;
using System.Linq;
using Application.DTOs.Loans;

namespace Application.Services.Loans;

/// <summary>
/// Service for calculating EMI schedules using reducing balance method (Indian standard)
/// Formula: EMI = [P × R × (1+R)^N] / [(1+R)^N - 1]
/// Where:
/// P = Principal
/// R = Monthly interest rate (Annual rate / 12 / 100)
/// N = Number of months
/// </summary>
public class EmiCalculationService
{
    /// <summary>
    /// Calculate EMI amount using reducing balance method
    /// </summary>
    public decimal CalculateEmi(decimal principal, decimal annualInterestRate, int tenureMonths)
    {
        if (principal <= 0 || tenureMonths <= 0)
            return 0;

        if (annualInterestRate == 0)
            return principal / tenureMonths;

        // Monthly interest rate
        var monthlyRate = annualInterestRate / 12 / 100;
        
        // EMI formula: [P × R × (1+R)^N] / [(1+R)^N - 1]
        var power = Math.Pow((double)(1 + monthlyRate), tenureMonths);
        var emi = principal * monthlyRate * (decimal)power / ((decimal)power - 1);

        return Math.Round(emi, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Generate complete EMI schedule for a loan
    /// </summary>
    public List<LoanEmiScheduleItemDto> GenerateSchedule(
        decimal principal,
        decimal annualInterestRate,
        int tenureMonths,
        DateTime startDate,
        decimal emiAmount)
    {
        var schedule = new List<LoanEmiScheduleItemDto>();
        var outstandingPrincipal = principal;
        var monthlyRate = annualInterestRate / 12 / 100;

        for (int i = 1; i <= tenureMonths; i++)
        {
            // Calculate interest for this month (on outstanding principal)
            var interestAmount = outstandingPrincipal * monthlyRate;
            interestAmount = Math.Round(interestAmount, 2, MidpointRounding.AwayFromZero);

            // Principal component = EMI - Interest
            var principalAmount = emiAmount - interestAmount;
            
            // For the last EMI, adjust to ensure outstanding becomes zero
            if (i == tenureMonths)
            {
                principalAmount = outstandingPrincipal;
                emiAmount = principalAmount + interestAmount;
            }

            principalAmount = Math.Round(principalAmount, 2, MidpointRounding.AwayFromZero);

            // Update outstanding principal
            outstandingPrincipal -= principalAmount;
            outstandingPrincipal = Math.Max(0, Math.Round(outstandingPrincipal, 2, MidpointRounding.AwayFromZero));

            var dueDate = startDate.AddMonths(i - 1);

            schedule.Add(new LoanEmiScheduleItemDto
            {
                EmiNumber = i,
                DueDate = dueDate,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                TotalEmi = emiAmount,
                OutstandingPrincipalAfter = outstandingPrincipal,
                Status = "pending"
            });
        }

        return schedule;
    }

    /// <summary>
    /// Calculate outstanding principal after a given number of EMIs
    /// </summary>
    public decimal CalculateOutstandingPrincipal(
        decimal principal,
        decimal annualInterestRate,
        int tenureMonths,
        int emisPaid)
    {
        if (emisPaid <= 0)
            return principal;

        if (emisPaid >= tenureMonths)
            return 0;

        var monthlyRate = annualInterestRate / 12 / 100;
        var emiAmount = CalculateEmi(principal, annualInterestRate, tenureMonths);
        var outstanding = principal;

        for (int i = 0; i < emisPaid; i++)
        {
            var interest = outstanding * monthlyRate;
            var principalPaid = emiAmount - interest;
            outstanding -= principalPaid;
        }

        return Math.Max(0, Math.Round(outstanding, 2, MidpointRounding.AwayFromZero));
    }
}





