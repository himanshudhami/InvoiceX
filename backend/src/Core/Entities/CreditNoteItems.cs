namespace Core.Entities
{
    /// <summary>
    /// Credit Note line item entity.
    /// </summary>
    public class CreditNoteItems
    {
        public Guid Id { get; set; }
        public Guid CreditNoteId { get; set; }
        public Guid? OriginalInvoiceItemId { get; set; }
        public Guid? ProductId { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
        public int? SortOrder { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // GST Compliance fields
        public string? HsnSacCode { get; set; }
        public bool IsService { get; set; }
        public decimal? CgstRate { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstRate { get; set; }
        public decimal? SgstAmount { get; set; }
        public decimal? IgstRate { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CessRate { get; set; }
        public decimal? CessAmount { get; set; }
    }
}
