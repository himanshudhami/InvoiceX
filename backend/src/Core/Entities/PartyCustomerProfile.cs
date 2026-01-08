namespace Core.Entities
{
    /// <summary>
    /// Customer-specific profile extension for Party
    /// Contains credit terms, e-invoicing settings specific to customer role
    /// </summary>
    public class PartyCustomerProfile
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Customer Classification ====================

        /// <summary>
        /// Customer type for GST classification
        /// Values: b2b (GST registered), b2c (unregistered/consumer), overseas (export), sez (SEZ unit)
        /// </summary>
        public string? CustomerType { get; set; }

        // ==================== Credit Terms ====================

        /// <summary>
        /// Credit limit for this customer
        /// </summary>
        public decimal? CreditLimit { get; set; }

        /// <summary>
        /// Payment terms in days (Net 30, Net 45, etc.)
        /// </summary>
        public int? PaymentTermsDays { get; set; }

        // ==================== Default Accounts (for auto-posting) ====================

        /// <summary>
        /// Default revenue account for sales to this customer
        /// </summary>
        public Guid? DefaultRevenueAccountId { get; set; }

        /// <summary>
        /// Default receivables account (usually Trade Receivables)
        /// </summary>
        public Guid? DefaultReceivableAccountId { get; set; }

        // ==================== E-Invoicing (GST India) ====================

        /// <summary>
        /// Whether e-invoice generation is applicable
        /// Required for B2B invoices above threshold
        /// </summary>
        public bool EInvoiceApplicable { get; set; }

        /// <summary>
        /// Whether e-way bill generation is applicable
        /// Required for goods movement above threshold
        /// </summary>
        public bool EWayBillApplicable { get; set; }

        // ==================== Pricing & Discounts ====================

        /// <summary>
        /// Default discount percentage for this customer
        /// </summary>
        public decimal? DefaultDiscountPercent { get; set; }

        /// <summary>
        /// Price list ID for customer-specific pricing (future)
        /// </summary>
        public Guid? PriceListId { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Property ====================

        /// <summary>
        /// Parent Party entity
        /// </summary>
        public Party? Party { get; set; }
    }
}
