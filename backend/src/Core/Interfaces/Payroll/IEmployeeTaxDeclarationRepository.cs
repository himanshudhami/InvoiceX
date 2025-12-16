using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IEmployeeTaxDeclarationRepository
    {
        Task<EmployeeTaxDeclaration?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmployeeTaxDeclaration>> GetAllAsync();
        Task<EmployeeTaxDeclaration?> GetByEmployeeAndYearAsync(Guid employeeId, string financialYear);
        Task<IEnumerable<EmployeeTaxDeclaration>> GetByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<EmployeeTaxDeclaration>> GetByFinancialYearAsync(string financialYear);
        Task<IEnumerable<EmployeeTaxDeclaration>> GetPendingVerificationAsync(string? financialYear = null);
        Task<(IEnumerable<EmployeeTaxDeclaration> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<EmployeeTaxDeclaration> AddAsync(EmployeeTaxDeclaration entity);
        Task UpdateAsync(EmployeeTaxDeclaration entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsForEmployeeAndYearAsync(Guid employeeId, string financialYear, Guid? excludeId = null);
        Task UpdateStatusAsync(Guid id, string status, string? verifiedBy = null);
        Task LockDeclarationsAsync(string financialYear);

        /// <summary>
        /// Update status with rejection details
        /// </summary>
        Task UpdateStatusWithRejectionAsync(Guid id, string status, string? rejectedBy, string? reason);

        /// <summary>
        /// Increment the revision count for a declaration
        /// </summary>
        Task IncrementRevisionCountAsync(Guid id);

        /// <summary>
        /// Clear rejection fields when declaration is revised
        /// </summary>
        Task ClearRejectionAsync(Guid id);

        /// <summary>
        /// Get rejected declarations pending revision
        /// </summary>
        Task<IEnumerable<EmployeeTaxDeclaration>> GetRejectedDeclarationsAsync(string? financialYear = null);
    }
}
