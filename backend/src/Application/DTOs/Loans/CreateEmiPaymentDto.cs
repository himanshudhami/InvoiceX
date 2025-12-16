using System;

namespace Application.DTOs.Loans;

public class CreateEmiPaymentDto
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? VoucherReference { get; set; }
    public string? Notes { get; set; }
    public int? EmiNumber { get; set; } // Optional: if paying specific EMI
}



