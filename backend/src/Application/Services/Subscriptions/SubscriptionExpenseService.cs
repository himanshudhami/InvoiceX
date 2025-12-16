using Application.DTOs.Subscriptions;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubscriptionsEntity = Core.Entities.Subscriptions;

namespace Application.Services.Subscriptions;

public class SubscriptionExpenseService
{
    private readonly ISubscriptionsRepository _repository;
    private const decimal UsdToInrRate = 86m; // Fixed rate as per existing pattern

    public SubscriptionExpenseService(ISubscriptionsRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<IEnumerable<SubscriptionMonthlyExpenseDto>>> GetMonthlyExpensesAsync(
        int year,
        int? month = null,
        Guid? companyId = null)
    {
        var subscriptions = await _repository.GetAllAsync(companyId);
        
        // Filter only active subscriptions
        var activeSubscriptions = subscriptions.Where(s => s.Status?.ToLowerInvariant() == "active").ToList();

        var monthlyExpenses = new Dictionary<(int Year, int Month), SubscriptionMonthlyExpenseDto>();

        foreach (var subscription in activeSubscriptions)
        {
            if (!subscription.CostPerPeriod.HasValue || subscription.CostPerPeriod.Value <= 0)
                continue;

            var cost = subscription.CostPerPeriod.Value;
            var currency = subscription.Currency ?? "USD";
            var renewalPeriod = subscription.RenewalPeriod?.ToLowerInvariant() ?? "monthly";

            // Calculate monthly cost based on renewal period
            decimal monthlyCost = renewalPeriod switch
            {
                "monthly" => cost,
                "quarterly" => cost / 3,
                "yearly" => cost / 12,
                _ => cost // Default to full cost for custom periods
            };

            // Determine the months this subscription should be active
            var activeMonths = GetActiveMonths(subscription, year, month);

            foreach (var (activeYear, activeMonth) in activeMonths)
            {
                var key = (activeYear, activeMonth);
                if (!monthlyExpenses.ContainsKey(key))
                {
                    monthlyExpenses[key] = new SubscriptionMonthlyExpenseDto
                    {
                        Year = activeYear,
                        Month = activeMonth,
                        TotalCost = 0,
                        Currency = "INR",
                        TotalCostInInr = 0,
                        ActiveSubscriptionCount = 0
                    };
                }

                // Calculate prorated cost for partial months
                var proratedCost = CalculateProratedCost(subscription, monthlyCost, activeYear, activeMonth);
                var costInInr = ConvertToInr(proratedCost, currency);

                monthlyExpenses[key].TotalCost += proratedCost;
                monthlyExpenses[key].TotalCostInInr += costInInr;
                monthlyExpenses[key].ActiveSubscriptionCount++;
            }
        }

        var result = monthlyExpenses.Values
            .OrderBy(e => e.Year)
            .ThenBy(e => e.Month)
            .ToList();

        return Result<IEnumerable<SubscriptionMonthlyExpenseDto>>.Success(result);
    }

    public async Task<Result<SubscriptionCostReportDto>> GetCostReportAsync(Guid? companyId = null)
    {
        var subscriptions = await _repository.GetAllAsync(companyId);
        var activeSubscriptions = subscriptions.Where(s => s.Status?.ToLowerInvariant() == "active").ToList();
        var allSubscriptions = subscriptions.ToList();

        var currentYear = DateTime.UtcNow.Year;
        var monthlyExpensesResult = await GetMonthlyExpensesAsync(currentYear, null, companyId);
        
        if (monthlyExpensesResult.IsFailure)
            return monthlyExpensesResult.Error!;

        var monthlyExpenses = monthlyExpensesResult.Value!.ToList();
        var totalMonthlyCost = monthlyExpenses.Sum(e => e.TotalCostInInr);
        var totalYearlyCost = totalMonthlyCost * 12; // Approximate yearly cost

        var report = new SubscriptionCostReportDto
        {
            TotalMonthlyCost = totalMonthlyCost,
            TotalYearlyCost = totalYearlyCost,
            TotalCostInInr = totalYearlyCost,
            ActiveSubscriptionCount = activeSubscriptions.Count,
            TotalSubscriptionCount = allSubscriptions.Count,
            MonthlyExpenses = monthlyExpenses
        };

        return Result<SubscriptionCostReportDto>.Success(report);
    }

    private List<(int Year, int Month)> GetActiveMonths(SubscriptionsEntity subscription, int targetYear, int? targetMonth)
    {
        var months = new List<(int Year, int Month)>();

        // If subscription has billing cycle dates, use those
        if (subscription.BillingCycleStart.HasValue && subscription.BillingCycleEnd.HasValue)
        {
            var start = subscription.BillingCycleStart.Value;
            var end = subscription.BillingCycleEnd.Value;

            var current = new DateTime(start.Year, start.Month, 1);
            var endDate = new DateTime(end.Year, end.Month, 1);

            while (current <= endDate)
            {
                if (current.Year == targetYear && (!targetMonth.HasValue || current.Month == targetMonth.Value))
                {
                    months.Add((current.Year, current.Month));
                }
                current = current.AddMonths(1);
            }
        }
        // Otherwise, use start date and renewal date
        else if (subscription.StartDate.HasValue)
        {
            var start = subscription.StartDate.Value;
            var end = subscription.RenewalDate ?? DateTime.UtcNow;

            var current = new DateTime(start.Year, start.Month, 1);
            var endDate = new DateTime(end.Year, end.Month, 1);

            while (current <= endDate)
            {
                if (current.Year == targetYear && (!targetMonth.HasValue || current.Month == targetMonth.Value))
                {
                    months.Add((current.Year, current.Month));
                }
                current = current.AddMonths(1);
            }
        }
        // If no dates, assume active for the entire target year/month
        else
        {
            if (!targetMonth.HasValue)
            {
                for (int m = 1; m <= 12; m++)
                {
                    months.Add((targetYear, m));
                }
            }
            else
            {
                months.Add((targetYear, targetMonth.Value));
            }
        }

        return months;
    }

    private decimal CalculateProratedCost(SubscriptionsEntity subscription, decimal monthlyCost, int year, int month)
    {
        // If subscription was paused/resumed/cancelled during this month, prorate
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        DateTime? activeStart = subscription.BillingCycleStart ?? subscription.StartDate ?? monthStart;
        DateTime? activeEnd = subscription.BillingCycleEnd ?? subscription.RenewalDate ?? monthEnd;

        // Check if subscription was paused during this month
        if (subscription.PausedOn.HasValue && subscription.PausedOn.Value >= monthStart && subscription.PausedOn.Value <= monthEnd)
        {
            activeEnd = subscription.PausedOn.Value;
        }

        // Check if subscription was resumed during this month
        if (subscription.ResumedOn.HasValue && subscription.ResumedOn.Value >= monthStart && subscription.ResumedOn.Value <= monthEnd)
        {
            activeStart = subscription.ResumedOn.Value;
        }

        // Check if subscription was cancelled during this month
        if (subscription.CancelledOn.HasValue && subscription.CancelledOn.Value >= monthStart && subscription.CancelledOn.Value <= monthEnd)
        {
            activeEnd = subscription.CancelledOn.Value;
        }

        // Calculate active days in the month
        var actualStart = activeStart > monthStart ? activeStart.Value : monthStart;
        var actualEnd = activeEnd < monthEnd ? activeEnd.Value : monthEnd;

        if (actualStart > actualEnd)
            return 0;

        var activeDays = (actualEnd - actualStart).Days + 1;
        var proratedCost = (monthlyCost / daysInMonth) * activeDays;

        return proratedCost;
    }

    private decimal ConvertToInr(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.ToUpperInvariant() == "INR")
            return amount;

        if (currency.ToUpperInvariant() == "USD")
            return amount * UsdToInrRate;

        // For other currencies, treat as INR for now (can be extended)
        return amount;
    }
}

