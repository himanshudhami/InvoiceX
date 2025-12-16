using System;

namespace Core.Entities;

public class Loan
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string LoanName { get; set; } = string.Empty;
    public string LenderName { get; set; } = string.Empty;
    public string LoanType { get; set; } = "secured";
    public Guid? AssetId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime LoanStartDate { get; set; }
    public DateTime? LoanEndDate { get; set; }
    public int TenureMonths { get; set; }
    public decimal EmiAmount { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public string InterestType { get; set; } = "fixed";
    public string CompoundingFrequency { get; set; } = "monthly";
    public string Status { get; set; } = "active";
    public string? LoanAccountNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




