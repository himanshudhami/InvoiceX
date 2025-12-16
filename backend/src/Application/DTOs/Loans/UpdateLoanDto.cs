using System;

namespace Application.DTOs.Loans;

public class UpdateLoanDto
{
    public string? LoanName { get; set; }
    public string? LenderName { get; set; }
    public string? LoanType { get; set; }
    public Guid? AssetId { get; set; }
    public decimal? PrincipalAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime? LoanStartDate { get; set; }
    public DateTime? LoanEndDate { get; set; }
    public int? TenureMonths { get; set; }
    public decimal? EmiAmount { get; set; }
    public string? InterestType { get; set; }
    public string? CompoundingFrequency { get; set; }
    public string? Status { get; set; }
    public string? LoanAccountNumber { get; set; }
    public string? Notes { get; set; }
}



