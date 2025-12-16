using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IEmployeeSalaryTransactionsRepository
    {
        Task<EmployeeSalaryTransactions?> GetByIdAsync(Guid id);
        Task<EmployeeSalaryTransactions?> GetByEmployeeAndMonthAsync(Guid employeeId, int salaryMonth, int salaryYear);
        Task<IEnumerable<EmployeeSalaryTransactions>> GetAllAsync();
        Task<IEnumerable<EmployeeSalaryTransactions>> GetByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<EmployeeSalaryTransactions>> GetByMonthYearAsync(int salaryMonth, int salaryYear);
        Task<(IEnumerable<EmployeeSalaryTransactions> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<EmployeeSalaryTransactions> AddAsync(EmployeeSalaryTransactions entity);
        Task UpdateAsync(EmployeeSalaryTransactions entity);
        Task DeleteAsync(Guid id);
        Task<bool> SalaryRecordExistsAsync(Guid employeeId, int salaryMonth, int salaryYear, Guid? excludeId = null);
        Task<IEnumerable<EmployeeSalaryTransactions>> BulkAddAsync(IEnumerable<EmployeeSalaryTransactions> entities);
        Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(int salaryMonth, int salaryYear, Guid? companyId = null);
        Task<Dictionary<string, decimal>> GetYearlySummaryAsync(int salaryYear, Guid? companyId = null);
    }
}