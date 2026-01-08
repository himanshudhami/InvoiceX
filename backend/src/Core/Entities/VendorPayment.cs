namespace Core.Entities
{
    /// <summary>
    /// Vendor Payment record - tracks all outgoing payments to vendors
    /// Enhanced for Indian tax compliance with TDS deduction tracking
    /// Equivalent to Payment voucher in Tally
    /// </summary>
    public class VendorPayment
    {
        public Guid Id { get; set; }

        // ==================== Linking ====================

        /// <summary>
        /// Company making this payment
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Party (vendor) receiving this payment.
        /// References parties table where is_vendor = true
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Bank account from which payment is made
        /// </summary>
        public Guid? BankAccountId { get; set; }

        // ==================== Payment Details ====================

        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// Net amount paid (after TDS deduction if applicable)
        /// NetAmount = GrossAmount - TdsAmount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gross amount before TDS deduction
        /// </summary>
        public decimal? GrossAmount { get; set; }

        /// <summary>
        /// Amount in INR (if foreign currency payment)
        /// </summary>
        public decimal? AmountInInr { get; set; }

        /// <summary>
        /// Currency of payment
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Payment method: bank_transfer, cheque, cash, neft, rtgs, upi
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// UTR number, cheque number, or other reference
        /// </summary>
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Cheque number if payment by cheque
        /// </summary>
        public string? ChequeNumber { get; set; }

        /// <summary>
        /// Cheque date (may differ from payment date for post-dated cheques)
        /// </summary>
        public DateOnly? ChequeDate { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Description/narration for the payment
        /// </summary>
        public string? Description { get; set; }

        // ==================== Payment Classification ====================

        /// <summary>
        /// Type: bill_payment, advance_paid, expense_reimbursement, refund_paid
        /// </summary>
        public string? PaymentType { get; set; }

        /// <summary>
        /// Status: draft, pending_approval, approved, processed, cancelled
        /// </summary>
        public string? Status { get; set; }

        // ==================== TDS Deduction ====================

        /// <summary>
        /// Whether TDS was deducted on this payment
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// TDS section: 194C, 194J, 194H, 194I, 194A, 194Q
        /// </summary>
        public string? TdsSection { get; set; }

        /// <summary>
        /// TDS rate percentage applied
        /// </summary>
        public decimal? TdsRate { get; set; }

        /// <summary>
        /// TDS amount deducted
        /// </summary>
        public decimal? TdsAmount { get; set; }

        /// <summary>
        /// TDS deposited to government flag
        /// </summary>
        public bool TdsDeposited { get; set; }

        /// <summary>
        /// TDS payment reference/challan number
        /// </summary>
        public string? TdsChallanNumber { get; set; }

        /// <summary>
        /// Date TDS was deposited
        /// </summary>
        public DateOnly? TdsDepositDate { get; set; }

        // ==================== Financial Year ====================

        /// <summary>
        /// Indian financial year: 2024-25 format (auto-calculated from PaymentDate)
        /// </summary>
        public string? FinancialYear { get; set; }

        // ==================== Ledger Posting ====================

        /// <summary>
        /// Whether posted to general ledger
        /// </summary>
        public bool IsPosted { get; set; }

        /// <summary>
        /// Journal entry ID after posting
        /// </summary>
        public Guid? PostedJournalId { get; set; }

        /// <summary>
        /// When posted to ledger
        /// </summary>
        public DateTime? PostedAt { get; set; }

        // ==================== Bank Reconciliation ====================

        /// <summary>
        /// Linked bank transaction after reconciliation
        /// </summary>
        public Guid? BankTransactionId { get; set; }

        /// <summary>
        /// Whether reconciled with bank statement
        /// </summary>
        public bool IsReconciled { get; set; }

        /// <summary>
        /// When reconciled
        /// </summary>
        public DateTime? ReconciledAt { get; set; }

        // ==================== Approval Workflow ====================

        /// <summary>
        /// Who approved this payment
        /// </summary>
        public Guid? ApprovedBy { get; set; }

        /// <summary>
        /// When approved
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        // ==================== Tally Migration Fields ====================

        /// <summary>
        /// Original Tally Voucher GUID for migration tracking
        /// </summary>
        public string? TallyVoucherGuid { get; set; }

        /// <summary>
        /// Original Tally Voucher Number
        /// </summary>
        public string? TallyVoucherNumber { get; set; }

        /// <summary>
        /// Migration batch ID
        /// </summary>
        public Guid? TallyMigrationBatchId { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Vendor party (is_vendor = true)
        /// </summary>
        public Party? Party { get; set; }
        public Companies? Company { get; set; }
        public BankAccount? BankAccount { get; set; }
        public ICollection<VendorPaymentAllocation>? Allocations { get; set; }
    }
}
