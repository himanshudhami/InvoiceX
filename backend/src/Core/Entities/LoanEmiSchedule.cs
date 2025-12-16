using System;

namespace Core.Entities;

public class LoanEmiSchedule
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public int EmiNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalEmi { get; set; }
    public decimal OutstandingPrincipalAfter { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? PaidDate { get; set; }
    public Guid? PaymentVoucherId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




