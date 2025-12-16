namespace Core.Entities
{
    public class Invoices
    {
public Guid Id { get; set; }
public Guid? CompanyId { get; set; }
public Guid? CustomerId { get; set; }
public string InvoiceNumber { get; set; } = string.Empty;
public DateOnly InvoiceDate { get; set; }
public DateOnly DueDate { get; set; }
public string? Status { get; set; }
public decimal Subtotal { get; set; }
public decimal? TaxAmount { get; set; }
public decimal? DiscountAmount { get; set; }
public decimal TotalAmount { get; set; }
public decimal? PaidAmount { get; set; }
public string? Currency { get; set; }
public string? Notes { get; set; }
public string? Terms { get; set; }
public string? PaymentInstructions { get; set; }
public string? PoNumber { get; set; }
public string? ProjectName { get; set; }
public DateTime? SentAt { get; set; }
public DateTime? ViewedAt { get; set; }
public DateTime? PaidAt { get; set; }
public DateTime? CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
}
}