using Application.DTOs.EmployeeSalaryTransactions;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Employee Salary Transactions operations
    /// </summary>
    public interface IEmployeeSalaryTransactionsService
    {
        /// <summary>
        /// Get Employee Salary Transaction by ID
        /// </summary>
        Task<Result<EmployeeSalaryTransactions>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get Employee Salary Transaction by Employee and Month
        /// </summary>
        Task<Result<EmployeeSalaryTransactions>> GetByEmployeeAndMonthAsync(Guid employeeId, int salaryMonth, int salaryYear);
        
        /// <summary>
        /// Get all Employee Salary Transactions
        /// </summary>
        Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetAllAsync();

        /// <summary>
        /// Get Employee Salary Transactions by Employee ID
        /// </summary>
        Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetByEmployeeIdAsync(Guid employeeId);

        /// <summary>
        /// Get Employee Salary Transactions by Month and Year
        /// </summary>
        Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetByMonthYearAsync(int salaryMonth, int salaryYear);
        
        /// <summary>
        /// Get paginated Employee Salary Transactions with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<EmployeeSalaryTransactions> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Employee Salary Transaction
        /// </summary>
        Task<Result<EmployeeSalaryTransactions>> CreateAsync(CreateEmployeeSalaryTransactionsDto dto);
        
        /// <summary>
        /// Update an existing Employee Salary Transaction
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateEmployeeSalaryTransactionsDto dto);
        
        /// <summary>
        /// Delete an Employee Salary Transaction by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Employee Salary Transaction exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);

        /// <summary>
        /// Check if salary record exists for employee and month
        /// </summary>
        Task<Result<bool>> SalaryRecordExistsAsync(Guid employeeId, int salaryMonth, int salaryYear, Guid? excludeId = null);

        /// <summary>
        /// Bulk create Employee Salary Transactions
        /// </summary>
        Task<Result<BulkUploadResultDto>> BulkCreateAsync(BulkEmployeeSalaryTransactionsDto dto);

        /// <summary>
        /// Get monthly salary summary
        /// </summary>
        Task<Result<Dictionary<string, decimal>>> GetMonthlySummaryAsync(int salaryMonth, int salaryYear, Guid? companyId = null);

        /// <summary>
        /// Get yearly salary summary
        /// </summary>
        Task<Result<Dictionary<string, decimal>>> GetYearlySummaryAsync(int salaryYear, Guid? companyId = null);

        /// <summary>
        /// Copy salary transactions from one period to another
        /// </summary>
        Task<Result<BulkUploadResultDto>> CopyTransactionsAsync(CopySalaryTransactionsDto dto);
    }
}