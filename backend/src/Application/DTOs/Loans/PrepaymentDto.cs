using System;

namespace Application.DTOs.Loans;

public class PrepaymentDto
{
    public DateTime PrepaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? VoucherReference { get; set; }
    public string? Notes { get; set; }
    public bool ReduceEmi { get; set; } = false; // If true, reduce EMI amount; if false, reduce tenure
}




