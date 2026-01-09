namespace Application.DTOs.Payroll;

public class ContractorPaymentDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid CompanyId { get; set; }
    public int PaymentMonth { get; set; }
    public int PaymentYear { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? ContractReference { get; set; }
    public decimal GrossAmount { get; set; }
    public string TdsSection { get; set; } = "194J";
    public decimal TdsRate { get; set; }
    public decimal TdsAmount { get; set; }
    public string? ContractorPan { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetPayable { get; set; }
    public bool GstApplicable { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal? TotalInvoiceAmount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? Description { get; set; }
    public string? Remarks { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Bank reconciliation - populated from contractor_payments fields
    public Guid? BankTransactionId { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public bool IsReconciled { get; set; }

    // Navigation - from party join
    public string? PartyName { get; set; }
    public string? CompanyName { get; set; }
    public string MonthName => new DateTime(PaymentYear, PaymentMonth, 1).ToString("MMMM");
}

public class CreateContractorPaymentDto
{
    public Guid PartyId { get; set; }
    public Guid CompanyId { get; set; }
    public int PaymentMonth { get; set; }
    public int PaymentYear { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? ContractReference { get; set; }
    public decimal GrossAmount { get; set; }
    public string TdsSection { get; set; } = "194J";
    public decimal TdsRate { get; set; } = 10.00m;
    public decimal OtherDeductions { get; set; }
    public bool GstApplicable { get; set; } = false;
    public decimal GstRate { get; set; } = 18.00m;
    public string? Description { get; set; }
    public string? Remarks { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateContractorPaymentDto
{
    public string? InvoiceNumber { get; set; }
    public string? ContractReference { get; set; }
    public decimal? GrossAmount { get; set; }
    public string? TdsSection { get; set; }
    public decimal? TdsRate { get; set; }
    public decimal? OtherDeductions { get; set; }
    public bool? GstApplicable { get; set; }
    public decimal? GstRate { get; set; }
    public string? Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? Description { get; set; }
    public string? Remarks { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Bank account ID for payment tracking and journal entries
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

public class ContractorPaymentSummaryDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public decimal TotalGross { get; set; }
    public decimal TotalTds { get; set; }
    public decimal TotalGst { get; set; }
    public decimal TotalNet { get; set; }
    public int PaymentCount { get; set; }
}
