using Core.Entities.Gst;
using Core.Entities.Ledger;

namespace Core.Interfaces.Gst
{
    /// <summary>
    /// Service interface for posting RCM (Reverse Charge Mechanism) journal entries.
    ///
    /// Implements two-stage journal model as per GST Act Section 9(3), 9(4):
    /// - Stage 1: RCM liability recognition on expense/purchase
    /// - Stage 2: RCM payment to government + ITC claim (if eligible)
    ///
    /// RCM Categories (Notification 13/2017):
    /// - Legal services, Security services, GTA, Import of services
    /// - Rent/insurance from unregistered persons, Director fees, etc.
    ///
    /// Journal Entry Structure (per CA best practices):
    ///
    /// STAGE 1 - RCM LIABILITY RECOGNITION (On Expense):
    /// Dr. Expense Account                [Base Amount]
    ///     Cr. Vendor Payable             [Base Amount]
    ///     Cr. RCM CGST Payable (2256)    [CGST Amount]
    ///     Cr. RCM SGST Payable (2257)    [SGST Amount]
    /// OR (Inter-state):
    ///     Cr. RCM IGST Payable (2258)    [IGST Amount]
    ///
    /// STAGE 2 - RCM PAYMENT + ITC CLAIM:
    /// Dr. RCM CGST Payable (2256)        [CGST Amount]
    /// Dr. RCM SGST Payable (2257)        [SGST Amount]
    /// Dr. CGST Input (1141)              [CGST - ITC]
    /// Dr. SGST Input (1142)              [SGST - ITC]
    ///     Cr. Bank Account               [Total RCM]
    ///     Cr. CGST Input - Claimed (1141)[CGST - ITC]
    ///     Cr. SGST Input - Claimed (1142)[SGST - ITC]
    ///
    /// Note: ITC is claimable only after RCM is paid. If ITC blocked per Section 17(5),
    /// it goes to ITC Blocked (1149) instead of regular ITC accounts.
    /// </summary>
    public interface IRcmPostingService
    {
        // ==================== Stage 1: Liability Recognition ====================

        /// <summary>
        /// Creates RCM liability entry when expense with RCM is recorded.
        /// This is Stage 1 of the two-stage model.
        /// </summary>
        /// <param name="request">RCM posting request details</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created RCM transaction with liability journal</returns>
        Task<RcmPostingResult> PostRcmLiabilityAsync(RcmLiabilityRequest request, Guid? postedBy = null);

        /// <summary>
        /// Creates RCM liability from an existing expense claim
        /// </summary>
        Task<RcmPostingResult> PostRcmLiabilityFromExpenseAsync(Guid expenseClaimId, Guid? postedBy = null);

        // ==================== Stage 2: RCM Payment + ITC ====================

        /// <summary>
        /// Posts RCM payment to government and claims ITC (if eligible).
        /// This is Stage 2 of the two-stage model.
        /// RCM must be paid before ITC can be claimed.
        /// </summary>
        /// <param name="rcmTransactionId">RCM transaction to pay</param>
        /// <param name="paymentDetails">Payment details (bank account, reference)</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Updated RCM transaction with payment and ITC journals</returns>
        Task<RcmPostingResult> PostRcmPaymentAsync(
            Guid rcmTransactionId,
            RcmPaymentRequest paymentDetails,
            Guid? postedBy = null);

        /// <summary>
        /// Claims ITC for RCM already paid (if done separately from payment)
        /// </summary>
        Task<RcmPostingResult> ClaimItcAsync(Guid rcmTransactionId, string claimPeriod, Guid? postedBy = null);

        /// <summary>
        /// Marks ITC as blocked per Section 17(5) CGST Act
        /// </summary>
        Task<RcmPostingResult> BlockItcAsync(Guid rcmTransactionId, string blockReason, Guid? postedBy = null);

        // ==================== Reversal ====================

        /// <summary>
        /// Reverses RCM liability entry (before payment)
        /// </summary>
        Task<JournalEntry?> ReverseLiabilityAsync(
            Guid rcmTransactionId,
            Guid reversedBy,
            string reason);

        /// <summary>
        /// Reverses RCM payment entry
        /// </summary>
        Task<JournalEntry?> ReversePaymentAsync(
            Guid rcmTransactionId,
            Guid reversedBy,
            string reason);

        // ==================== Queries ====================

        /// <summary>
        /// Gets RCM transactions pending payment
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetPendingPaymentsAsync(Guid companyId);

        /// <summary>
        /// Gets RCM transactions paid but ITC not yet claimed
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetPendingItcClaimsAsync(Guid companyId);

        /// <summary>
        /// Gets RCM summary for a return period (for GSTR-3B Table 3.1(d))
        /// </summary>
        Task<RcmPeriodSummary> GetPeriodSummaryAsync(Guid companyId, string returnPeriod);

        // ==================== Validation ====================

        /// <summary>
        /// Validates if expense is subject to RCM
        /// </summary>
        Task<RcmValidationResult> ValidateRcmApplicabilityAsync(RcmValidationRequest request);

        /// <summary>
        /// Gets applicable RCM category for a service
        /// </summary>
        Task<RcmCategoryInfo?> GetRcmCategoryAsync(string hsnSacCode, string? vendorGstin = null);
    }

    // ==================== Request DTOs ====================

    /// <summary>
    /// Request for RCM liability posting
    /// </summary>
    public class RcmLiabilityRequest
    {
        public Guid CompanyId { get; set; }

        // Source document
        public string SourceType { get; set; } = "manual";
        public Guid? SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // Vendor details
        public string VendorName { get; set; } = string.Empty;
        public string? VendorGstin { get; set; }
        public string? VendorPan { get; set; }
        public string? VendorStateCode { get; set; }
        public string? VendorInvoiceNumber { get; set; }
        public DateTime? VendorInvoiceDate { get; set; }

        // RCM category
        public string RcmCategoryCode { get; set; } = string.Empty;

        // Supply details
        public string PlaceOfSupply { get; set; } = string.Empty;
        public string SupplyType { get; set; } = "intra_state";
        public string? HsnSacCode { get; set; }
        public string? Description { get; set; }

        // Amounts
        public decimal TaxableValue { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }

        // Expense account for Dr. side
        public string ExpenseAccountCode { get; set; } = string.Empty;

        // Vendor payable account for Cr. side (base amount)
        public string? VendorPayableAccountCode { get; set; }
        public Guid? VendorId { get; set; }

        // ITC eligibility
        public bool ItcEligible { get; set; } = true;

        // Notes
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request for RCM payment posting
    /// </summary>
    public class RcmPaymentRequest
    {
        public DateTime PaymentDate { get; set; }
        public string BankAccountCode { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string? ChallanNumber { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Whether to claim ITC in the same transaction
        /// </summary>
        public bool ClaimItcNow { get; set; } = true;

        /// <summary>
        /// Return period for ITC claim (e.g., "Jan-2025")
        /// </summary>
        public string? ItcClaimPeriod { get; set; }
    }

    /// <summary>
    /// Request for RCM applicability validation
    /// </summary>
    public class RcmValidationRequest
    {
        public string? HsnSacCode { get; set; }
        public string? ServiceDescription { get; set; }
        public string? VendorGstin { get; set; }
        public decimal TaxableValue { get; set; }
        public string? PlaceOfSupply { get; set; }
    }

    // ==================== Result DTOs ====================

    /// <summary>
    /// Result of RCM posting operation
    /// </summary>
    public class RcmPostingResult
    {
        public bool Success { get; set; }
        public RcmTransaction? Transaction { get; set; }
        public JournalEntry? JournalEntry { get; set; }
        public string? ErrorMessage { get; set; }

        public static RcmPostingResult Succeeded(RcmTransaction transaction, JournalEntry? journalEntry = null) =>
            new() { Success = true, Transaction = transaction, JournalEntry = journalEntry };

        public static RcmPostingResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of RCM applicability validation
    /// </summary>
    public class RcmValidationResult
    {
        public bool IsRcmApplicable { get; set; }
        public string? RcmCategoryCode { get; set; }
        public string? CategoryName { get; set; }
        public string? NotificationReference { get; set; }
        public decimal? ApplicableRate { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// RCM category information
    /// </summary>
    public class RcmCategoryInfo
    {
        public Guid Id { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? NotificationNumber { get; set; }
        public string? HsnSacCodes { get; set; }
        public decimal DefaultGstRate { get; set; }
        public string? Description { get; set; }
    }
}
