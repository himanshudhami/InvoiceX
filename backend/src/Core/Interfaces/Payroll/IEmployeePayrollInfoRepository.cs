using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IEmployeePayrollInfoRepository
    {
        Task<EmployeePayrollInfo?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmployeePayrollInfo>> GetAllAsync();
        Task<EmployeePayrollInfo?> GetByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<EmployeePayrollInfo>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<EmployeePayrollInfo>> GetByPayrollTypeAsync(string payrollType);
        Task<IEnumerable<EmployeePayrollInfo>> GetActiveEmployeesForPayrollAsync(Guid companyId, int payrollMonth, int payrollYear, string? payrollType = null);
        Task<(IEnumerable<EmployeePayrollInfo> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<EmployeePayrollInfo> AddAsync(EmployeePayrollInfo entity);
        Task UpdateAsync(EmployeePayrollInfo entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsForEmployeeAsync(Guid employeeId, Guid? excludeId = null);
        Task<bool> UanExistsAsync(string uan, Guid? excludeEmployeeId = null);
        Task<bool> EsiNumberExistsAsync(string esiNumber, Guid? excludeEmployeeId = null);
        Task ResignEmployeeAsync(Guid employeeId, DateTime lastWorkingDay);
        Task RejoinEmployeeAsync(Guid employeeId, DateTime? rejoiningDate = null);
    }
}
