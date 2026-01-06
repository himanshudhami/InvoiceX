using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Repository interface for Form 16 (TDS Certificate) operations.
    /// Provides CRUD operations and specialized queries for Form 16 management.
    /// </summary>
    public interface IForm16Repository
    {
        // ==================== Basic CRUD Operations ====================

        /// <summary>
        /// Get Form 16 by ID
        /// </summary>
        Task<Form16?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all Form 16s for a company
        /// </summary>
        Task<IEnumerable<Form16>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get paged Form 16s with filtering and sorting
        /// </summary>
        Task<(IEnumerable<Form16> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Add new Form 16 record
        /// </summary>
        Task<Form16> AddAsync(Form16 form16);

        /// <summary>
        /// Update existing Form 16 record
        /// </summary>
        Task UpdateAsync(Form16 form16);

        /// <summary>
        /// Delete Form 16 record
        /// </summary>
        Task DeleteAsync(Guid id);

        // ==================== Specialized Queries ====================

        /// <summary>
        /// Get Form 16 for a specific employee and financial year
        /// </summary>
        Task<Form16?> GetByEmployeeAndFyAsync(Guid companyId, Guid employeeId, string financialYear);

        /// <summary>
        /// Get all Form 16s for a financial year
        /// </summary>
        Task<IEnumerable<Form16>> GetByFinancialYearAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get Form 16s pending generation for a financial year
        /// </summary>
        Task<IEnumerable<Guid>> GetEmployeesPendingForm16Async(Guid companyId, string financialYear);

        /// <summary>
        /// Check if Form 16 exists for employee and FY
        /// </summary>
        Task<bool> ExistsAsync(Guid companyId, Guid employeeId, string financialYear);

        /// <summary>
        /// Get next certificate number for a company and FY
        /// </summary>
        Task<int> GetNextCertificateSerialAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Bulk insert Form 16 records
        /// </summary>
        Task BulkInsertAsync(IEnumerable<Form16> form16s);

        /// <summary>
        /// Update status for multiple Form 16s
        /// </summary>
        Task UpdateStatusBulkAsync(IEnumerable<Guid> ids, string status, Guid? updatedBy);

        /// <summary>
        /// Get Form 16 statistics for a financial year
        /// </summary>
        Task<Form16Statistics> GetStatisticsAsync(Guid companyId, string financialYear);
    }

    /// <summary>
    /// Statistics for Form 16 generation
    /// </summary>
    public class Form16Statistics
    {
        public string FinancialYear { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public int Form16Generated { get; set; }
        public int Form16Verified { get; set; }
        public int Form16Issued { get; set; }
        public int Form16Pending { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
    }
}
