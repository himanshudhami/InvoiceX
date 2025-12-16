namespace Core.Entities
{
    public class InvoiceItems
    {
        public Guid Id { get; set; }
        public Guid? InvoiceId { get; set; }
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

        // GST Compliance fields
        public string? HsnSacCode { get; set; }
        public bool IsService { get; set; } = true;

        // CGST (Central GST) - for intra-state supplies
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }

        // SGST (State GST) - for intra-state supplies
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }

        // IGST (Integrated GST) - for inter-state supplies
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }

        // Cess - for specific goods
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
    }
}