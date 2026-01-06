using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IPayrollTransactionRepository
    {
        Task<PayrollTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<PayrollTransaction>> GetAllAsync();
        Task<IEnumerable<PayrollTransaction>> GetByPayrollRunIdAsync(Guid payrollRunId);
        Task<PayrollTransaction?> GetByEmployeeAndMonthAsync(Guid employeeId, int payrollMonth, int payrollYear);
        Task<IEnumerable<PayrollTransaction>> GetByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<PayrollTransaction>> GetByFinancialYearAsync(Guid employeeId, string financialYear);
        Task<IEnumerable<PayrollTransaction>> GetByStatusAsync(string status);
        Task<(IEnumerable<PayrollTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<PayrollTransaction> AddAsync(PayrollTransaction entity);
        Task UpdateAsync(PayrollTransaction entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<PayrollTransaction>> BulkAddAsync(IEnumerable<PayrollTransaction> entities);
        Task<bool> ExistsForEmployeeAndMonthAsync(Guid employeeId, int payrollMonth, int payrollYear, Guid? excludeId = null);
        Task UpdateStatusAsync(Guid id, string status);
        Task UpdateTdsOverrideAsync(Guid id, decimal tdsOverride, string reason);

        // Summary methods
        Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(Guid payrollRunId);
        Task<Dictionary<string, decimal>> GetYtdSummaryAsync(Guid employeeId, string financialYear);

        /// <summary>
        /// Gets transactions for a company and period (for statutory filing)
        /// </summary>
        Task<IEnumerable<PayrollTransaction>> GetByCompanyAndPeriodAsync(
            Guid companyId,
            int payrollMonth,
            int payrollYear);
    }
}
