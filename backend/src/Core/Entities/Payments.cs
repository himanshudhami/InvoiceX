namespace Core.Entities
{
    /// <summary>
    /// Payment record - tracks all incoming payments (invoice-linked and direct)
    /// Enhanced for Indian tax compliance with TDS tracking
    /// </summary>
    public class Payments
    {
        public Guid Id { get; set; }

        // ==================== Linking ====================

        /// <summary>
        /// Optional invoice reference. Null for direct/non-invoice payments
        /// </summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>
        /// Company that received this payment
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Customer who made this payment (especially for non-invoice payments)
        /// </summary>
        public Guid? CustomerId { get; set; }

        // ==================== Payment Details ====================

        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// Net amount received (after TDS deduction if applicable)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Amount in INR (actual amount received)
        /// </summary>
        public decimal? AmountInInr { get; set; }

        /// <summary>
        /// Original currency of payment
        /// </summary>
        public string? Currency { get; set; }

        public string? PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Description for non-invoice payments
        /// </summary>
        public string? Description { get; set; }

        // ==================== Payment Classification ====================

        /// <summary>
        /// Type: invoice_payment, advance_received, direct_income, refund_received
        /// </summary>
        public string? PaymentType { get; set; }

        /// <summary>
        /// Category: export_services, domestic_services, product_sale, interest, other
        /// </summary>
        public string? IncomeCategory { get; set; }

        // ==================== TDS Tracking ====================

        /// <summary>
        /// Whether TDS was deducted by the payer
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// TDS section: 194J (10%), 194C (1-2%), 194H (5%), 194O (1%)
        /// </summary>
        public string? TdsSection { get; set; }

        /// <summary>
        /// TDS rate percentage applied
        /// </summary>
        public decimal? TdsRate { get; set; }

        /// <summary>
        /// TDS amount deducted by payer
        /// </summary>
        public decimal? TdsAmount { get; set; }

        /// <summary>
        /// Gross amount before TDS deduction. Net received = GrossAmount - TdsAmount
        /// </summary>
        public decimal? GrossAmount { get; set; }

        // ==================== Financial Year ====================

        /// <summary>
        /// Indian financial year: 2024-25 format (auto-calculated from PaymentDate)
        /// </summary>
        public string? FinancialYear { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Computed/Joined Properties ====================

        /// <summary>
        /// Invoice number (populated from JOIN query, not stored in payments table)
        /// </summary>
        public string? InvoiceNumber { get; set; }

        // ==================== Navigation Properties ====================

        public Invoices? Invoice { get; set; }
        public Companies? Company { get; set; }
        public Customers? Customer { get; set; }
    }
}
