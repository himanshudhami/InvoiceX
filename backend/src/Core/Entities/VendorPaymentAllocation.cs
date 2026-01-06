namespace Core.Entities
{
    /// <summary>
    /// Vendor Payment Allocation - tracks how payments are allocated to vendor invoices
    /// Supports partial payments, advance payments, and bill-wise tracking (like Tally)
    /// </summary>
    public class VendorPaymentAllocation
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
        public Guid VendorPaymentId { get; set; }

        /// <summary>
        /// Vendor invoice this payment is allocated to (null for advances/on-account)
        /// </summary>
        public Guid? VendorInvoiceId { get; set; }

        // ==================== Allocation Amount ====================

        /// <summary>
        /// Amount allocated to this invoice
        /// </summary>
        public decimal AllocatedAmount { get; set; }

        /// <summary>
        /// TDS portion allocated to this invoice
        /// </summary>
        public decimal TdsAllocated { get; set; }

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
        /// Type: bill_settlement, advance_adjustment, debit_note, on_account
        /// Mirrors Tally's bill allocation types:
        /// - bill_settlement = "Agst Ref" (against existing bill)
        /// - advance_adjustment = "Advance" (marking as advance)
        /// - debit_note = Against debit note
        /// - on_account = "On Account" (unallocated)
        /// </summary>
        public string AllocationType { get; set; } = "bill_settlement";

        /// <summary>
        /// Tally bill reference name (for migration)
        /// </summary>
        public string? TallyBillRef { get; set; }

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

        public VendorPayment? VendorPayment { get; set; }
        public VendorInvoice? VendorInvoice { get; set; }
        public Companies? Company { get; set; }
    }
}
