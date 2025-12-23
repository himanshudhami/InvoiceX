using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Repository interface for TCS (Tax Collected at Source) transactions
    /// </summary>
    public interface ITcsTransactionRepository
    {
        Task<TcsTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<TcsTransaction>> GetAllAsync();
        Task<(IEnumerable<TcsTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null);
        Task<TcsTransaction> AddAsync(TcsTransaction entity);
        Task UpdateAsync(TcsTransaction entity);
        Task DeleteAsync(Guid id);

        // ==================== Company & Period Queries ====================

        /// <summary>
        /// Get TCS transactions by company
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetByCompanyAsync(Guid companyId, string? quarter = null);

        /// <summary>
        /// Get TCS transactions by company and financial year
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetByCompanyAndFYAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get TCS transactions by company, FY, and quarter
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter);

        // ==================== Transaction Type Queries ====================

        /// <summary>
        /// Get TCS collected (when we sell)
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetCollectedAsync(Guid companyId, string? quarter = null);

        /// <summary>
        /// Get TCS paid (when we buy)
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetPaidAsync(Guid companyId, string? quarter = null);

        // ==================== Status-based Queries ====================

        /// <summary>
        /// Get TCS transactions pending remittance
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetPendingRemittanceAsync(Guid companyId);

        /// <summary>
        /// Get TCS transactions by status
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetByStatusAsync(Guid companyId, string status);

        // ==================== Party Queries ====================

        /// <summary>
        /// Get TCS transactions by party PAN
        /// </summary>
        Task<IEnumerable<TcsTransaction>> GetByPartyAsync(Guid companyId, string partyPan);

        /// <summary>
        /// Get cumulative transaction value for a party in FY
        /// </summary>
        Task<decimal> GetCumulativePartyValueAsync(Guid companyId, string partyPan, string financialYear);

        // ==================== Invoice/Payment Queries ====================

        /// <summary>
        /// Get TCS transaction by invoice
        /// </summary>
        Task<TcsTransaction?> GetByInvoiceAsync(Guid invoiceId);

        /// <summary>
        /// Get TCS transaction by payment
        /// </summary>
        Task<TcsTransaction?> GetByPaymentAsync(Guid paymentId);

        // ==================== Aggregations ====================

        /// <summary>
        /// Get total TCS collected for a quarter
        /// </summary>
        Task<decimal> GetTotalTcsCollectedAsync(Guid companyId, string quarter);

        /// <summary>
        /// Get total TCS pending remittance
        /// </summary>
        Task<decimal> GetTotalPendingRemittanceAsync(Guid companyId);

        /// <summary>
        /// Get TCS quarterly summary
        /// </summary>
        Task<TcsQuarterlySummary> GetQuarterlySummaryAsync(Guid companyId, string financialYear, string quarter);

        // ==================== Status Updates ====================

        /// <summary>
        /// Mark TCS as collected
        /// </summary>
        Task MarkCollectedAsync(Guid id, Guid? journalEntryId = null);

        /// <summary>
        /// Mark TCS as remitted to government
        /// </summary>
        Task MarkRemittedAsync(Guid id, string challanNumber, string? bsrCode = null);

        /// <summary>
        /// Mark Form 27EQ as filed
        /// </summary>
        Task MarkForm27EqFiledAsync(Guid id, string acknowledgement);
    }

    /// <summary>
    /// TCS quarterly summary for Form 27EQ
    /// </summary>
    public class TcsQuarterlySummary
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public decimal TotalTransactionValue { get; set; }
        public decimal TotalTcsAmount { get; set; }
        public decimal TcsCollected { get; set; }
        public decimal TcsRemitted { get; set; }
        public decimal TcsPending { get; set; }
        public int TotalTransactions { get; set; }
        public List<TcsSectionSummary> SectionBreakdown { get; set; } = new();
    }

    /// <summary>
    /// TCS summary by section
    /// </summary>
    public class TcsSectionSummary
    {
        public string SectionCode { get; set; } = string.Empty;
        public decimal TransactionValue { get; set; }
        public decimal TcsAmount { get; set; }
        public int TransactionCount { get; set; }
    }
}
