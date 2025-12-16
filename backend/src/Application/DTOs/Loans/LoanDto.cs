using System;

namespace Application.DTOs.Loans;

public class LoanDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string LoanName { get; set; } = string.Empty;
    public string LenderName { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public Guid? AssetId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime LoanStartDate { get; set; }
    public DateTime? LoanEndDate { get; set; }
    public int TenureMonths { get; set; }
    public decimal EmiAmount { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public string InterestType { get; set; } = string.Empty;
    public string CompoundingFrequency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LoanAccountNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




