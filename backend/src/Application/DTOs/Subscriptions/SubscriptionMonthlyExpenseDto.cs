using System;

namespace Application.DTOs.Subscriptions;

public class SubscriptionMonthlyExpenseDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TotalCostInInr { get; set; }
    public int ActiveSubscriptionCount { get; set; }
}

public class SubscriptionCostReportDto
{
    public decimal TotalMonthlyCost { get; set; }
    public decimal TotalYearlyCost { get; set; }
    public decimal TotalCostInInr { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public int TotalSubscriptionCount { get; set; }
    public IEnumerable<SubscriptionMonthlyExpenseDto> MonthlyExpenses { get; set; } = Array.Empty<SubscriptionMonthlyExpenseDto>();
}





