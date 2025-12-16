using System;

namespace Core.Entities;

public class LoanTransaction
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public string? Description { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? VoucherReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



