using Core.Entities.Ledger;
using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Service interface for TCS (Tax Collected at Source) operations.
    ///
    /// TCS is collected by seller from buyer at the time of sale per Section 206C.
    /// Common scenarios: Sale of goods > 50L, scrap, motor vehicles, forest produce.
    ///
    /// TCS Rates (FY 2024-25):
    /// - 206C(1H): Sale of goods > 50L - 0.1% (0.075% till Mar 2025)
    /// - 206C(1): Scrap - 1%
    /// - 206C(1F): Motor vehicles > 10L - 1%
    /// - 206C(1G): Foreign remittance/tour - 5%/20%
    ///
    /// Journal Entry Structure:
    ///
    /// ON SALE (TCS COLLECTION):
    /// Dr. Customer Receivable            [Invoice Amount + TCS]
    ///     Cr. Sales Revenue              [Invoice Amount]
    ///     Cr. TCS Payable (2282)         [TCS Amount]
    ///
    /// ON REMITTANCE TO GOVERNMENT:
    /// Dr. TCS Payable (2282)             [TCS Amount]
    ///     Cr. Bank Account               [TCS Amount]
    /// </summary>
    public interface ITcsService
    {
        // ==================== TCS Calculation ====================

        /// <summary>
        /// Calculates TCS amount based on section and transaction details.
        /// Considers cumulative threshold for 206C(1H).
        /// </summary>
        Task<TcsCalculationResult> CalculateTcsAsync(TcsCalculationRequest request);

        /// <summary>
        /// Check if TCS is applicable for a customer based on cumulative value
        /// </summary>
        Task<bool> IsTcsApplicableAsync(Guid companyId, string customerPan, string financialYear);

        /// <summary>
        /// Get cumulative transaction value for threshold check
        /// </summary>
        Task<decimal> GetCumulativeValueAsync(Guid companyId, string customerPan, string financialYear);

        // ==================== TCS Collection (On Invoice) ====================

        /// <summary>
        /// Posts TCS collection entry when invoice is raised.
        /// </summary>
        Task<TcsPostingResult> PostTcsCollectionAsync(TcsCollectionRequest request, Guid? postedBy = null);

        /// <summary>
        /// Posts TCS collection from an existing invoice
        /// </summary>
        Task<TcsPostingResult> PostTcsFromInvoiceAsync(Guid invoiceId, Guid? postedBy = null);

        // ==================== TCS Remittance ====================

        /// <summary>
        /// Posts TCS remittance to government.
        /// </summary>
        Task<TcsPostingResult> PostTcsRemittanceAsync(
            Guid tcsTransactionId,
            TcsRemittanceRequest remittanceDetails,
            Guid? postedBy = null);

        /// <summary>
        /// Bulk remittance for multiple TCS transactions
        /// </summary>
        Task<TcsBulkRemittanceResult> PostBulkRemittanceAsync(
            IEnumerable<Guid> tcsTransactionIds,
            TcsRemittanceRequest remittanceDetails,
            Guid? postedBy = null);

        // ==================== TCS Received (When we buy) ====================

        /// <summary>
        /// Records TCS paid when we are the buyer
        /// </summary>
        Task<TcsPostingResult> RecordTcsPaidAsync(TcsPaidRequest request, Guid? postedBy = null);

        // ==================== Form 27EQ ====================

        /// <summary>
        /// Marks transactions as filed in Form 27EQ
        /// </summary>
        Task MarkForm27EqFiledAsync(IEnumerable<Guid> transactionIds, string acknowledgement);

        /// <summary>
        /// Get data for Form 27EQ preparation
        /// </summary>
        Task<TcsQuarterlySummary> GetForm27EqDataAsync(Guid companyId, string financialYear, string quarter);

        // ==================== Queries ====================

        /// <summary>
        /// Gets TCS transactions pending remittance
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetPendingRemittanceAsync(Guid companyId);

        /// <summary>
        /// Gets quarterly summary
        /// </summary>
        Task<TcsQuarterlySummary> GetQuarterlySummaryAsync(Guid companyId, string financialYear, string quarter);

        // ==================== Reversal ====================

        /// <summary>
        /// Reverses a TCS collection entry
        /// </summary>
        Task<JournalEntry?> ReverseCollectionAsync(Guid tcsTransactionId, Guid reversedBy, string reason);

        // ==================== Section Configuration ====================

        /// <summary>
        /// Get TCS section details by code
        /// </summary>
        Task<TcsSectionInfo?> GetSectionInfoAsync(string sectionCode);

        /// <summary>
        /// Get all active TCS sections
        /// </summary>
        Task<IEnumerable<TcsSectionInfo>> GetAllSectionsAsync();
    }

    // ==================== Request DTOs ====================

    /// <summary>
    /// Request for TCS calculation
    /// </summary>
    public class TcsCalculationRequest
    {
        public Guid CompanyId { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public decimal TransactionValue { get; set; }
        public string? CustomerPan { get; set; }
        public string? CustomerGstin { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Whether to include cumulative value check for threshold
        /// </summary>
        public bool CheckCumulativeThreshold { get; set; } = true;
    }

    /// <summary>
    /// Request for TCS collection posting
    /// </summary>
    public class TcsCollectionRequest
    {
        public Guid CompanyId { get; set; }
        public string SectionCode { get; set; } = string.Empty;

        // Transaction details
        public DateOnly TransactionDate { get; set; }
        public decimal TransactionValue { get; set; }
        public decimal TcsRate { get; set; }
        public decimal TcsAmount { get; set; }

        // Customer details
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPan { get; set; }
        public string? CustomerGstin { get; set; }

        // Linked documents
        public Guid? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }

        // Accounts
        public string? CustomerReceivableAccountCode { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request for TCS remittance
    /// </summary>
    public class TcsRemittanceRequest
    {
        public DateTime RemittanceDate { get; set; }
        public string BankAccountCode { get; set; } = string.Empty;
        public string ChallanNumber { get; set; } = string.Empty;
        public string? BsrCode { get; set; }
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request for recording TCS paid (when we buy)
    /// </summary>
    public class TcsPaidRequest
    {
        public Guid CompanyId { get; set; }
        public string SectionCode { get; set; } = string.Empty;

        // Transaction details
        public DateOnly TransactionDate { get; set; }
        public decimal TransactionValue { get; set; }
        public decimal TcsRate { get; set; }
        public decimal TcsAmount { get; set; }

        // Vendor details
        public Guid? VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? VendorPan { get; set; }
        public string? VendorGstin { get; set; }
        public string? VendorTan { get; set; }

        // Linked documents
        public Guid? PaymentId { get; set; }
        public string? InvoiceNumber { get; set; }

        // Notes
        public string? Notes { get; set; }
    }

    // ==================== Result DTOs ====================

    /// <summary>
    /// Result of TCS calculation
    /// </summary>
    public class TcsCalculationResult
    {
        public bool IsApplicable { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string SectionDescription { get; set; } = string.Empty;
        public decimal TransactionValue { get; set; }
        public decimal TcsRate { get; set; }
        public decimal TcsAmount { get; set; }
        public decimal? CumulativeValue { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Result of TCS posting operation
    /// </summary>
    public class TcsPostingResult
    {
        public bool Success { get; set; }
        public TcsTransaction? Transaction { get; set; }
        public JournalEntry? JournalEntry { get; set; }
        public string? ErrorMessage { get; set; }

        public static TcsPostingResult Succeeded(TcsTransaction transaction, JournalEntry? journalEntry = null) =>
            new() { Success = true, Transaction = transaction, JournalEntry = journalEntry };

        public static TcsPostingResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of bulk TCS remittance
    /// </summary>
    public class TcsBulkRemittanceResult
    {
        public bool Success { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public JournalEntry? JournalEntry { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// TCS section information
    /// </summary>
    public class TcsSectionInfo
    {
        public Guid Id { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal? HigherRate { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public string? ApplicableGoodsServices { get; set; }
        public string? AccountCode { get; set; }
        public bool IsActive { get; set; }
    }
}
