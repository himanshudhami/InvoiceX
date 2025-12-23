using Core.Entities.Ledger;

namespace Core.Interfaces.Gst
{
    /// <summary>
    /// Service interface for GST posting operations beyond standard invoice/expense posting.
    ///
    /// Handles:
    /// - ITC Blocked tracking (Section 17(5) CGST Act)
    /// - Credit Note GST adjustment
    /// - Debit Note GST adjustment
    /// - ITC Reversal (Rule 42/43 CGST Rules)
    /// - UTGST posting for Union Territories
    /// - GST TDS/TCS (Section 51/52)
    ///
    /// Section 17(5) - Blocked ITC Categories:
    /// - Motor vehicles (except certain cases)
    /// - Food and beverages, outdoor catering
    /// - Beauty treatment, health services
    /// - Membership of club, health and fitness
    /// - Travel benefits for employees
    /// - Works contract for immovable property
    /// - Goods for personal consumption
    /// - Goods lost, stolen, destroyed, written off
    ///
    /// Journal Entry Structures:
    ///
    /// ITC BLOCKED:
    /// Dr. ITC Blocked - Sec 17(5) (1149)     [Blocked GST]
    ///     Cr. CGST Input (1141)               [Blocked CGST]
    ///     Cr. SGST Input (1142)               [Blocked SGST]
    ///
    /// CREDIT NOTE GST ADJUSTMENT:
    /// Dr. CGST Output Payable (2251)          [CGST Reduction]
    /// Dr. SGST Output Payable (2252)          [SGST Reduction]
    ///     Cr. CGST Input (1141)               [ITC Reversal]
    ///     Cr. SGST Input (1142)               [ITC Reversal]
    ///
    /// ITC REVERSAL (Rule 42/43):
    /// Dr. ITC Reversal Expense (5xxx)         [Reversed Amount]
    ///     Cr. CGST Input (1141)               [CGST Reversal]
    ///     Cr. SGST Input (1142)               [SGST Reversal]
    /// </summary>
    public interface IGstPostingService
    {
        // ==================== ITC Blocked (Section 17(5)) ====================

        /// <summary>
        /// Checks if ITC is blocked for a given expense category
        /// </summary>
        Task<ItcBlockedCheckResult> CheckItcBlockedAsync(ItcBlockedCheckRequest request);

        /// <summary>
        /// Posts ITC blocked entry when GST credit cannot be claimed
        /// </summary>
        Task<GstPostingResult> PostItcBlockedAsync(ItcBlockedRequest request, Guid? postedBy = null);

        /// <summary>
        /// Gets blocked ITC categories
        /// </summary>
        Task<IEnumerable<ItcBlockedCategory>> GetBlockedCategoriesAsync();

        // ==================== Credit/Debit Notes ====================

        /// <summary>
        /// Posts GST adjustment for credit note
        /// </summary>
        Task<GstPostingResult> PostCreditNoteGstAsync(CreditNoteGstRequest request, Guid? postedBy = null);

        /// <summary>
        /// Posts GST adjustment for debit note
        /// </summary>
        Task<GstPostingResult> PostDebitNoteGstAsync(DebitNoteGstRequest request, Guid? postedBy = null);

        // ==================== ITC Reversal (Rule 42/43) ====================

        /// <summary>
        /// Posts ITC reversal entry per Rule 42/43
        /// </summary>
        Task<GstPostingResult> PostItcReversalAsync(ItcReversalRequest request, Guid? postedBy = null);

        /// <summary>
        /// Calculates ITC reversal amount per Rule 42/43
        /// </summary>
        Task<ItcReversalCalculation> CalculateItcReversalAsync(ItcReversalCalculationRequest request);

        // ==================== UTGST Posting ====================

        /// <summary>
        /// Posts UTGST entry for Union Territory transactions
        /// </summary>
        Task<GstPostingResult> PostUtgstAsync(UtgstRequest request, Guid? postedBy = null);

        // ==================== GST TDS/TCS (Section 51/52) ====================

        /// <summary>
        /// Posts GST TDS deducted by deductor (Section 51)
        /// </summary>
        Task<GstPostingResult> PostGstTdsAsync(GstTdsRequest request, Guid? postedBy = null);

        /// <summary>
        /// Posts GST TCS collected by e-commerce operator (Section 52)
        /// </summary>
        Task<GstPostingResult> PostGstTcsAsync(GstTcsRequest request, Guid? postedBy = null);

        // ==================== Summary & Reports ====================

        /// <summary>
        /// Gets ITC blocked summary for a period
        /// </summary>
        Task<ItcBlockedSummary> GetItcBlockedSummaryAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Gets ITC available vs blocked breakdown
        /// </summary>
        Task<ItcAvailabilityReport> GetItcAvailabilityReportAsync(Guid companyId, string returnPeriod);
    }

    // ==================== ITC Blocked DTOs ====================

    /// <summary>
    /// Request to check if ITC is blocked
    /// </summary>
    public class ItcBlockedCheckRequest
    {
        public string? HsnSacCode { get; set; }
        public string? ExpenseCategory { get; set; }
        public string? Description { get; set; }
        public decimal GstAmount { get; set; }
    }

    /// <summary>
    /// Result of ITC blocked check
    /// </summary>
    public class ItcBlockedCheckResult
    {
        public bool IsBlocked { get; set; }
        public string? BlockedCategoryCode { get; set; }
        public string? BlockedCategoryName { get; set; }
        public string? SectionReference { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request to post ITC blocked entry
    /// </summary>
    public class ItcBlockedRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }

        // Source document
        public string? SourceType { get; set; }
        public Guid? SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // Blocked category
        public string BlockedCategoryCode { get; set; } = string.Empty;
        public string? HsnSacCode { get; set; }
        public string? Description { get; set; }

        // Amounts
        public decimal TaxableValue { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Blocked ITC category
    /// </summary>
    public class ItcBlockedCategory
    {
        public Guid Id { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SectionReference { get; set; } = string.Empty;
        public string? HsnSacCodes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    // ==================== Credit/Debit Note DTOs ====================

    /// <summary>
    /// Request to post credit note GST adjustment
    /// </summary>
    public class CreditNoteGstRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly CreditNoteDate { get; set; }

        // Credit note details
        public string CreditNoteNumber { get; set; } = string.Empty;
        public Guid? OriginalInvoiceId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }

        // GST amounts to adjust (reduce)
        public decimal TaxableValue { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }

        // Supply type
        public string SupplyType { get; set; } = "intra_state";

        // Vendor/Customer details
        public string? PartyName { get; set; }
        public string? PartyGstin { get; set; }

        // Notes
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to post debit note GST adjustment
    /// </summary>
    public class DebitNoteGstRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly DebitNoteDate { get; set; }

        // Debit note details
        public string DebitNoteNumber { get; set; } = string.Empty;
        public Guid? OriginalInvoiceId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }

        // GST amounts to adjust (increase)
        public decimal TaxableValue { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }

        // Supply type
        public string SupplyType { get; set; } = "intra_state";

        // Vendor/Customer details
        public string? PartyName { get; set; }
        public string? PartyGstin { get; set; }

        // Notes
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    // ==================== ITC Reversal DTOs ====================

    /// <summary>
    /// Request for ITC reversal calculation
    /// </summary>
    public class ItcReversalCalculationRequest
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string ReturnPeriod { get; set; } = string.Empty;

        /// <summary>
        /// Reversal rule: Rule42 (common credit), Rule43 (capital goods)
        /// </summary>
        public string ReversalRule { get; set; } = "Rule42";

        // For Rule 42 - Common Credit
        public decimal? TotalTurnover { get; set; }
        public decimal? ExemptTurnover { get; set; }
        public decimal? TotalCommonCredit { get; set; }

        // For Rule 43 - Capital Goods
        public decimal? CapitalGoodsValue { get; set; }
        public decimal? UsefulLife { get; set; } // In quarters
        public decimal? ExemptUsePercentage { get; set; }
    }

    /// <summary>
    /// Result of ITC reversal calculation
    /// </summary>
    public class ItcReversalCalculation
    {
        public string ReversalRule { get; set; } = string.Empty;
        public decimal TotalItcAvailable { get; set; }
        public decimal ItcToReverse { get; set; }
        public decimal CgstReversal { get; set; }
        public decimal SgstReversal { get; set; }
        public decimal IgstReversal { get; set; }
        public decimal CessReversal { get; set; }
        public string? CalculationDetails { get; set; }
    }

    /// <summary>
    /// Request to post ITC reversal
    /// </summary>
    public class ItcReversalRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly ReversalDate { get; set; }
        public string ReversalRule { get; set; } = "Rule42";
        public string ReturnPeriod { get; set; } = string.Empty;

        // Amounts to reverse
        public decimal CgstReversal { get; set; }
        public decimal SgstReversal { get; set; }
        public decimal IgstReversal { get; set; }
        public decimal CessReversal { get; set; }

        // Reversal reason
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    // ==================== UTGST DTOs ====================

    /// <summary>
    /// Request for UTGST posting
    /// </summary>
    public class UtgstRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }

        // Transaction type
        public string TransactionType { get; set; } = "input"; // input, output

        // Source document
        public string? SourceType { get; set; }
        public Guid? SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // Amounts
        public decimal TaxableValue { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal UtgstRate { get; set; }
        public decimal UtgstAmount { get; set; }

        // Party details
        public string? PartyName { get; set; }
        public string? PartyGstin { get; set; }
        public string? UnionTerritory { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    // ==================== GST TDS/TCS DTOs ====================

    /// <summary>
    /// Request for GST TDS posting (Section 51)
    /// </summary>
    public class GstTdsRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }

        // Source document
        public Guid? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }

        // TDS details
        public decimal TaxableValue { get; set; }
        public decimal TdsRate { get; set; } // Usually 2%
        public decimal CgstTdsAmount { get; set; }
        public decimal SgstTdsAmount { get; set; }
        public decimal IgstTdsAmount { get; set; }

        // Supply type
        public string SupplyType { get; set; } = "intra_state";

        // Deductor details
        public string DeductorName { get; set; } = string.Empty;
        public string? DeductorGstin { get; set; }
        public string? DeductorTan { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request for GST TCS posting (Section 52)
    /// </summary>
    public class GstTcsRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }

        // TCS details
        public decimal NetValue { get; set; }
        public decimal TcsRate { get; set; } // Usually 1%
        public decimal CgstTcsAmount { get; set; }
        public decimal SgstTcsAmount { get; set; }
        public decimal IgstTcsAmount { get; set; }

        // Supply type
        public string SupplyType { get; set; } = "intra_state";

        // E-commerce operator details
        public string OperatorName { get; set; } = string.Empty;
        public string? OperatorGstin { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    // ==================== Result & Summary DTOs ====================

    /// <summary>
    /// Result of GST posting operation
    /// </summary>
    public class GstPostingResult
    {
        public bool Success { get; set; }
        public JournalEntry? JournalEntry { get; set; }
        public string? ErrorMessage { get; set; }

        public static GstPostingResult Succeeded(JournalEntry? journalEntry = null) =>
            new() { Success = true, JournalEntry = journalEntry };

        public static GstPostingResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Summary of blocked ITC for a period
    /// </summary>
    public class ItcBlockedSummary
    {
        public string ReturnPeriod { get; set; } = string.Empty;
        public decimal TotalBlockedCgst { get; set; }
        public decimal TotalBlockedSgst { get; set; }
        public decimal TotalBlockedIgst { get; set; }
        public decimal TotalBlockedCess { get; set; }
        public decimal TotalBlockedAmount { get; set; }
        public int TransactionCount { get; set; }
        public List<ItcBlockedCategorySummary> CategoryBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Blocked ITC by category
    /// </summary>
    public class ItcBlockedCategorySummary
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal BlockedAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>
    /// ITC availability report
    /// </summary>
    public class ItcAvailabilityReport
    {
        public string ReturnPeriod { get; set; } = string.Empty;
        public decimal TotalItcAvailed { get; set; }
        public decimal ItcBlocked { get; set; }
        public decimal ItcReversed { get; set; }
        public decimal NetItcAvailable { get; set; }
        public decimal ItcUtilized { get; set; }
        public decimal ItcBalance { get; set; }
    }
}
