using Core.Entities.Gst;

namespace Core.Interfaces.Gst
{
    /// <summary>
    /// Repository interface for RCM (Reverse Charge Mechanism) transactions
    /// </summary>
    public interface IRcmTransactionRepository
    {
        Task<RcmTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<RcmTransaction>> GetAllAsync();
        Task<(IEnumerable<RcmTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null);
        Task<RcmTransaction> AddAsync(RcmTransaction entity);
        Task UpdateAsync(RcmTransaction entity);
        Task DeleteAsync(Guid id);

        // ==================== Company & Period Queries ====================

        /// <summary>
        /// Get RCM transactions by company
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetByCompanyAsync(Guid companyId, string? returnPeriod = null);

        /// <summary>
        /// Get RCM transactions by company and financial year
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetByCompanyAndFYAsync(Guid companyId, string financialYear);

        // ==================== Status-based Queries ====================

        /// <summary>
        /// Get RCM transactions pending liability recognition
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetPendingLiabilityAsync(Guid companyId);

        /// <summary>
        /// Get RCM transactions pending payment
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetPendingPaymentAsync(Guid companyId);

        /// <summary>
        /// Get RCM transactions paid but awaiting ITC claim
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetPaidAwaitingItcClaimAsync(Guid companyId);

        /// <summary>
        /// Get RCM transactions by status
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetByStatusAsync(Guid companyId, string status);

        // ==================== Source Document Queries ====================

        /// <summary>
        /// Get RCM transaction by source document
        /// </summary>
        Task<RcmTransaction?> GetBySourceAsync(string sourceType, Guid sourceId);

        /// <summary>
        /// Check if RCM exists for a source document
        /// </summary>
        Task<bool> ExistsBySourceAsync(string sourceType, Guid sourceId);

        // ==================== Category Queries ====================

        /// <summary>
        /// Get RCM transactions by category
        /// </summary>
        Task<IEnumerable<RcmTransaction>> GetByCategoryAsync(Guid companyId, string categoryCode);

        // ==================== Aggregations ====================

        /// <summary>
        /// Get total RCM liability for a period
        /// </summary>
        Task<decimal> GetTotalRcmLiabilityAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get total pending RCM payment
        /// </summary>
        Task<decimal> GetTotalPendingPaymentAsync(Guid companyId);

        /// <summary>
        /// Get RCM summary for a return period
        /// </summary>
        Task<RcmPeriodSummary> GetPeriodSummaryAsync(Guid companyId, string returnPeriod);

        // ==================== Status Updates ====================

        /// <summary>
        /// Mark liability as recognized
        /// </summary>
        Task MarkLiabilityRecognizedAsync(Guid id, Guid journalEntryId);

        /// <summary>
        /// Mark RCM as paid
        /// </summary>
        Task MarkRcmPaidAsync(Guid id, DateTime paymentDate, Guid journalEntryId, string? paymentReference = null);

        /// <summary>
        /// Mark ITC as claimed
        /// </summary>
        Task MarkItcClaimedAsync(Guid id, Guid journalEntryId, string claimPeriod);

        /// <summary>
        /// Mark ITC as blocked (Section 17(5))
        /// </summary>
        Task MarkItcBlockedAsync(Guid id, string reason);
    }

    /// <summary>
    /// RCM summary for a return period
    /// </summary>
    public class RcmPeriodSummary
    {
        public string ReturnPeriod { get; set; } = string.Empty;
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalRcmTax { get; set; }
        public decimal RcmPaidAmount { get; set; }
        public decimal RcmPendingAmount { get; set; }
        public decimal ItcClaimedAmount { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public List<RcmCategorySummary> CategoryBreakdown { get; set; } = new();
    }

    /// <summary>
    /// RCM summary by category
    /// </summary>
    public class RcmCategorySummary
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal RcmTax { get; set; }
        public int TransactionCount { get; set; }
    }
}
