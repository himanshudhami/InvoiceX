namespace Core.Entities
{
    /// <summary>
    /// Vendor Invoice Line Item entity - represents a line item on a purchase bill
    /// Mirrors InvoiceItems but for purchases
    /// </summary>
    public class VendorInvoiceItem
    {
        public Guid Id { get; set; }
        public Guid? VendorInvoiceId { get; set; }
        public Guid? ProductId { get; set; }

        // ==================== Item Details ====================

        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
        public int? SortOrder { get; set; }

        // ==================== GST Compliance ====================

        /// <summary>
        /// HSN code for goods, SAC code for services
        /// </summary>
        public string? HsnSacCode { get; set; }

        /// <summary>
        /// Whether this is a service (SAC) or goods (HSN)
        /// </summary>
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

        // ==================== ITC Eligibility ====================

        /// <summary>
        /// Whether ITC is eligible for this line item
        /// May be blocked for specific items under Section 17(5)
        /// </summary>
        public bool ItcEligible { get; set; } = true;

        /// <summary>
        /// ITC category: capital_goods, inputs, input_services
        /// </summary>
        public string? ItcCategory { get; set; }

        /// <summary>
        /// Reason if ITC is ineligible for this item
        /// </summary>
        public string? ItcIneligibleReason { get; set; }

        // ==================== Expense Account ====================

        /// <summary>
        /// Specific expense account for this line item
        /// Overrides invoice-level default
        /// </summary>
        public Guid? ExpenseAccountId { get; set; }

        // ==================== Cost Center ====================

        /// <summary>
        /// Cost center allocation for this line item
        /// </summary>
        public Guid? CostCenterId { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public VendorInvoice? VendorInvoice { get; set; }
        public Products? Product { get; set; }
    }
}
