using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Payments
{
    /// <summary>
    /// Data transfer object for creating Payments
    /// Supports both invoice-linked and direct (non-invoice) payments
    /// </summary>
    public class CreatePaymentsDto
    {
        // ==================== Linking ====================

        /// <summary>
        /// InvoiceId - optional for direct payments
        /// </summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>
        /// CompanyId - required for direct payments, auto-filled for invoice payments
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// CustomerId - required for direct payments, auto-filled for invoice payments
        /// </summary>
        public Guid? CustomerId { get; set; }

        // ==================== Payment Details ====================

        /// <summary>
        /// PaymentDate
        /// </summary>
        [Required(ErrorMessage = "Payment date is required")]
        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// Amount (net amount received after TDS, in original currency)
        /// </summary>
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Amount in INR (actual amount received in Indian Rupees)
        /// Required when currency is not INR for accurate cash flow and bank reconciliation
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount in INR must be greater than 0")]
        public decimal? AmountInInr { get; set; }

        /// <summary>
        /// Original currency of payment (defaults to INR)
        /// </summary>
        [StringLength(10, ErrorMessage = "Currency code cannot exceed 10 characters")]
        public string? Currency { get; set; }

        /// <summary>
        /// PaymentMethod - bank_transfer, cash, cheque, upi, card, etc.
        /// </summary>
        [StringLength(255, ErrorMessage = "PaymentMethod cannot exceed 255 characters")]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// ReferenceNumber - UTR, cheque number, transaction ID
        /// </summary>
        [StringLength(255, ErrorMessage = "ReferenceNumber cannot exceed 255 characters")]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Description for non-invoice payments
        /// </summary>
        public string? Description { get; set; }

        // ==================== Payment Classification ====================

        /// <summary>
        /// PaymentType - invoice_payment, advance_received, direct_income, refund_received
        /// </summary>
        [StringLength(50, ErrorMessage = "PaymentType cannot exceed 50 characters")]
        public string? PaymentType { get; set; }

        /// <summary>
        /// IncomeCategory - export_services, domestic_services, product_sale, interest, other
        /// </summary>
        [StringLength(100, ErrorMessage = "IncomeCategory cannot exceed 100 characters")]
        public string? IncomeCategory { get; set; }

        // ==================== TDS Tracking ====================

        /// <summary>
        /// Whether TDS was deducted by the payer
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// TDS section: 194J (10%), 194C (1-2%), 194H (5%), 194O (1%)
        /// </summary>
        [StringLength(20, ErrorMessage = "TdsSection cannot exceed 20 characters")]
        public string? TdsSection { get; set; }

        /// <summary>
        /// TDS rate percentage applied (e.g., 10 for 10%)
        /// </summary>
        [Range(0, 100, ErrorMessage = "TDS rate must be between 0 and 100")]
        public decimal? TdsRate { get; set; }

        /// <summary>
        /// TDS amount deducted by payer
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "TDS amount must be 0 or greater")]
        public decimal? TdsAmount { get; set; }

        /// <summary>
        /// Gross amount before TDS deduction. Net received = GrossAmount - TdsAmount
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Gross amount must be greater than 0")]
        public decimal? GrossAmount { get; set; }

        // ==================== Financial Year ====================

        /// <summary>
        /// Indian financial year (auto-calculated if not provided): 2024-25 format
        /// </summary>
        [StringLength(10, ErrorMessage = "FinancialYear cannot exceed 10 characters")]
        public string? FinancialYear { get; set; }
    }
}
