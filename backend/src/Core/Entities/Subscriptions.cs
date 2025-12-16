using System;

namespace Core.Entities;

public class Subscriptions
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string? PlanName { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? StartDate { get; set; }
    public DateTime? RenewalDate { get; set; }
    public string RenewalPeriod { get; set; } = "monthly";
    public int? SeatsTotal { get; set; }
    public int? SeatsUsed { get; set; }
    public string? LicenseKey { get; set; }
    public decimal? CostPerPeriod { get; set; }
    public decimal? CostPerSeat { get; set; }
    public string? Currency { get; set; }
    public DateTime? BillingCycleStart { get; set; }
    public DateTime? BillingCycleEnd { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public DateTime? PausedOn { get; set; }
    public DateTime? ResumedOn { get; set; }
    public DateTime? CancelledOn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




