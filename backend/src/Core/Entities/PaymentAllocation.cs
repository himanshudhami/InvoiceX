namespace Core.Entities
{
    /// <summary>
    /// Payment allocation record - tracks how payments are allocated to invoices
    /// Supports partial payments, advance payments, and over-payments
    /// </summary>
    public class PaymentAllocation
    {
        public Guid Id { get; set; }

        // ==================== Linking ====================

        /// <summary>
        /// Company this allocation belongs to
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Payment being allocated
        /// </summary>
        public Guid PaymentId { get; set; }

        /// <summary>
        /// Invoice this payment is allocated to (null for unallocated/advance)
        /// </summary>
        public Guid? InvoiceId { get; set; }

        // ==================== Allocation Amount ====================

        /// <summary>
        /// Amount allocated to this invoice
        /// </summary>
        public decimal AllocatedAmount { get; set; }

        /// <summary>
        /// Currency of the allocation
        /// </summary>
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Amount in INR (for foreign currency payments)
        /// </summary>
        public decimal? AmountInInr { get; set; }

        /// <summary>
        /// Exchange rate used for conversion
        /// </summary>
        public decimal ExchangeRate { get; set; } = 1;

        // ==================== Allocation Details ====================

        /// <summary>
        /// Date of allocation
        /// </summary>
        public DateOnly AllocationDate { get; set; }

        /// <summary>
        /// Type: invoice_settlement, advance_adjustment, credit_note, refund, write_off
        /// </summary>
        public string AllocationType { get; set; } = "invoice_settlement";

        /// <summary>
        /// TDS portion of this allocation (from payment.tds_amount)
        /// </summary>
        public decimal TdsAllocated { get; set; }

        /// <summary>
        /// Additional notes about this allocation
        /// </summary>
        public string? Notes { get; set; }

        // ==================== Audit ====================

        /// <summary>
        /// User who created this allocation
        /// </summary>
        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Payments? Payment { get; set; }
        public Invoices? Invoice { get; set; }
        public Companies? Company { get; set; }
    }
}
