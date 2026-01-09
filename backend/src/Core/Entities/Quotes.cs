namespace Core.Entities
{
    public class Quotes
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? PartyId { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateOnly QuoteDate { get; set; }
        public DateOnly? ValidUntil { get; set; }
        public string? Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public Guid? ConvertedToInvoiceId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
