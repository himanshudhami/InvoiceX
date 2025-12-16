using System;

namespace Application.DTOs.Subscriptions;

public class CancelSubscriptionDto
{
    public DateTime? CancelledOn { get; set; }
    public string? Notes { get; set; }
}




