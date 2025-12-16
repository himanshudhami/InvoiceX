using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for TDS Receivable operations
    /// </summary>
    public interface ITdsReceivableRepository
    {
        Task<TdsReceivable?> GetByIdAsync(Guid id);
        Task<IEnumerable<TdsReceivable>> GetAllAsync();
        Task<(IEnumerable<TdsReceivable> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<TdsReceivable> AddAsync(TdsReceivable entity);
        Task UpdateAsync(TdsReceivable entity);
        Task DeleteAsync(Guid id);

        // ==================== Specialized Queries ====================

        /// <summary>
        /// Get TDS receivables by company and financial year
        /// </summary>
        Task<IEnumerable<TdsReceivable>> GetByCompanyAndFYAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get TDS receivables by company, FY, and quarter
        /// </summary>
        Task<IEnumerable<TdsReceivable>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get TDS receivables by customer
        /// </summary>
        Task<IEnumerable<TdsReceivable>> GetByCustomerAsync(Guid customerId);

        /// <summary>
        /// Get unmatched TDS entries (not yet matched with 26AS)
        /// </summary>
        Task<IEnumerable<TdsReceivable>> GetUnmatchedAsync(Guid companyId, string? financialYear = null);

        /// <summary>
        /// Get TDS entries by status
        /// </summary>
        Task<IEnumerable<TdsReceivable>> GetByStatusAsync(Guid companyId, string status);

        /// <summary>
        /// Get TDS summary by financial year (for reporting)
        /// </summary>
        Task<TdsSummary> GetSummaryAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Mark TDS entry as matched with 26AS
        /// </summary>
        Task MatchWith26AsAsync(Guid id, decimal form26AsAmount, decimal? difference);

        /// <summary>
        /// Update claiming status
        /// </summary>
        Task UpdateStatusAsync(Guid id, string status, string? claimedInReturn = null);
    }

    /// <summary>
    /// TDS Summary for reporting
    /// </summary>
    public class TdsSummary
    {
        public string FinancialYear { get; set; } = string.Empty;
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalTdsAmount { get; set; }
        public decimal TotalNetReceived { get; set; }
        public int TotalEntries { get; set; }
        public int MatchedEntries { get; set; }
        public int UnmatchedEntries { get; set; }
        public decimal MatchedAmount { get; set; }
        public decimal UnmatchedAmount { get; set; }
        public List<TdsQuarterlySummary> QuarterlySummary { get; set; } = new();
    }

    /// <summary>
    /// Quarterly TDS summary
    /// </summary>
    public class TdsQuarterlySummary
    {
        public string Quarter { get; set; } = string.Empty;
        public decimal TdsAmount { get; set; }
        public int EntryCount { get; set; }
    }
}
