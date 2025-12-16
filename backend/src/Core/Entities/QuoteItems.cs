namespace Core.Entities
{
    public class QuoteItems
    {
        public Guid Id { get; set; }
        public Guid? QuoteId { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
        public int? SortOrder { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
