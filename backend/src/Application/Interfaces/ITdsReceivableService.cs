using Application.DTOs.TdsReceivable;
using Core.Entities;
using Core.Interfaces;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for TDS Receivable operations
    /// </summary>
    public interface ITdsReceivableService
    {
        /// <summary>
        /// Get TDS receivable by ID
        /// </summary>
        Task<Result<TdsReceivable>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all TDS receivables
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetAllAsync();

        /// <summary>
        /// Get paginated TDS receivables with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<TdsReceivable> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new TDS receivable entry
        /// </summary>
        Task<Result<TdsReceivable>> CreateAsync(CreateTdsReceivableDto dto);

        /// <summary>
        /// Update an existing TDS receivable entry
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateTdsReceivableDto dto);

        /// <summary>
        /// Delete a TDS receivable entry
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        // ==================== Specialized Methods ====================

        /// <summary>
        /// Get TDS receivables by company and financial year
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetByCompanyAndFYAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get TDS receivables by company, FY, and quarter
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get TDS receivables by customer
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetByCustomerAsync(Guid customerId);

        /// <summary>
        /// Get unmatched TDS entries
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetUnmatchedAsync(Guid companyId, string? financialYear = null);

        /// <summary>
        /// Get TDS entries by status
        /// </summary>
        Task<Result<IEnumerable<TdsReceivable>>> GetByStatusAsync(Guid companyId, string status);

        /// <summary>
        /// Get TDS summary for a financial year
        /// </summary>
        Task<Result<TdsSummary>> GetSummaryAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Match TDS entry with Form 26AS
        /// </summary>
        Task<Result> MatchWith26AsAsync(Guid id, Match26AsDto dto);

        /// <summary>
        /// Update TDS entry status
        /// </summary>
        Task<Result> UpdateStatusAsync(Guid id, UpdateStatusDto dto);
    }
}
