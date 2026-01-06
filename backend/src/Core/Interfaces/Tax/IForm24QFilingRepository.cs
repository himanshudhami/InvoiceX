using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Repository interface for Form 24Q Filing operations.
    /// Provides CRUD operations and specialized queries for quarterly TDS return management.
    /// </summary>
    public interface IForm24QFilingRepository
    {
        // ==================== Basic CRUD Operations ====================

        /// <summary>
        /// Get Form 24Q filing by ID
        /// </summary>
        Task<Form24QFiling?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all Form 24Q filings for a company
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetByCompanyAsync(Guid companyId, string? financialYear = null);

        /// <summary>
        /// Get paged Form 24Q filings with filtering and sorting
        /// </summary>
        Task<(IEnumerable<Form24QFiling> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? quarter = null,
            string? status = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Add new Form 24Q filing record
        /// </summary>
        Task<Form24QFiling> AddAsync(Form24QFiling filing);

        /// <summary>
        /// Update existing Form 24Q filing record
        /// </summary>
        Task UpdateAsync(Form24QFiling filing);

        /// <summary>
        /// Delete Form 24Q filing record
        /// </summary>
        Task DeleteAsync(Guid id);

        // ==================== Specialized Queries ====================

        /// <summary>
        /// Get active Form 24Q filing for a specific company, FY, and quarter
        /// Returns the most recent non-revised, non-rejected regular filing
        /// </summary>
        Task<Form24QFiling?> GetByCompanyQuarterAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get all filings for a financial year
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetByFinancialYearAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Check if an active filing exists for company/FY/quarter
        /// </summary>
        Task<bool> ExistsAsync(Guid companyId, string financialYear, string quarter, string formType = "regular");

        /// <summary>
        /// Get all correction returns for an original filing
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetCorrectionsAsync(Guid originalFilingId);

        /// <summary>
        /// Get next revision number for a correction return
        /// </summary>
        Task<int> GetNextRevisionNumberAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get filings by status
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetByStatusAsync(Guid companyId, string status);

        /// <summary>
        /// Get pending filings (not acknowledged) for a financial year
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetPendingFilingsAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get overdue filings (past due date, not acknowledged)
        /// </summary>
        Task<IEnumerable<Form24QFiling>> GetOverdueFilingsAsync(Guid companyId);

        /// <summary>
        /// Get Form 24Q filing statistics for a financial year
        /// </summary>
        Task<Form24QFilingStatistics> GetStatisticsAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Bulk update status for multiple filings
        /// </summary>
        Task UpdateStatusBulkAsync(IEnumerable<Guid> ids, string status, Guid? updatedBy);
    }

    /// <summary>
    /// Statistics for Form 24Q filings
    /// </summary>
    public class Form24QFilingStatistics
    {
        public string FinancialYear { get; set; } = string.Empty;
        public int TotalFilings { get; set; }
        public int DraftCount { get; set; }
        public int ValidatedCount { get; set; }
        public int FvuGeneratedCount { get; set; }
        public int SubmittedCount { get; set; }
        public int AcknowledgedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalVariance { get; set; }

        // Quarterly breakdown
        public QuarterStatus? Q1Status { get; set; }
        public QuarterStatus? Q2Status { get; set; }
        public QuarterStatus? Q3Status { get; set; }
        public QuarterStatus? Q4Status { get; set; }
    }

    /// <summary>
    /// Status of a single quarter filing
    /// </summary>
    public class QuarterStatus
    {
        public string Quarter { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HasFiling { get; set; }
        public bool IsOverdue { get; set; }
        public DateOnly DueDate { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }
        public string? AcknowledgementNumber { get; set; }
    }
}
