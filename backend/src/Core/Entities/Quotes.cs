namespace Core.Entities
{
    public class Quotes
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? CustomerId { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateOnly QuoteDate { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public string? Status { get; set; }
        public decimal Subtotal { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string? PaymentInstructions { get; set; }
        public string? PoNumber { get; set; }
        public string? ProjectName { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
