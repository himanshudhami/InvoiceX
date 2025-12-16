using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Repository interface for tax declaration audit history
    /// </summary>
    public interface IEmployeeTaxDeclarationHistoryRepository
    {
        /// <summary>
        /// Get all history entries for a declaration
        /// </summary>
        Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByDeclarationIdAsync(Guid declarationId);

        /// <summary>
        /// Get history entries by action type
        /// </summary>
        Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByActionAsync(string action, string? financialYear = null);

        /// <summary>
        /// Get recent history entries for an employee
        /// </summary>
        Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByEmployeeIdAsync(Guid employeeId, int limit = 50);

        /// <summary>
        /// Add a new history entry
        /// </summary>
        Task<EmployeeTaxDeclarationHistory> AddAsync(EmployeeTaxDeclarationHistory history);

        /// <summary>
        /// Get the revision count for a declaration
        /// </summary>
        Task<int> GetRevisionCountAsync(Guid declarationId);

        /// <summary>
        /// Get history entries within a date range
        /// </summary>
        Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? action = null);
    }
}
