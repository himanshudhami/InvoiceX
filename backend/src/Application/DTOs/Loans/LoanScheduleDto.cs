using System;
using System.Collections.Generic;

namespace Application.DTOs.Loans;

public class LoanScheduleDto
{
    public Guid LoanId { get; set; }
    public string LoanName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TenureMonths { get; set; }
    public decimal EmiAmount { get; set; }
    public List<LoanEmiScheduleItemDto> ScheduleItems { get; set; } = new();
}

public class LoanEmiScheduleItemDto
{
    public Guid Id { get; set; }
    public int EmiNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalEmi { get; set; }
    public decimal OutstandingPrincipalAfter { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
}




