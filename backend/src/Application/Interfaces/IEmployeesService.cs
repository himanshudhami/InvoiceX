using Application.DTOs.Employees;
using Application.DTOs.EmployeesBulk;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Employees operations
    /// </summary>
    public interface IEmployeesService
    {
        /// <summary>
        /// Get Employee by ID
        /// </summary>
        Task<Result<Employees>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get Employee by Employee ID
        /// </summary>
        Task<Result<Employees>> GetByEmployeeIdAsync(string employeeId);
        
        /// <summary>
        /// Get all Employees entities
        /// </summary>
        Task<Result<IEnumerable<Employees>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated Employees entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Employees> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Employee
        /// </summary>
        Task<Result<Employees>> CreateAsync(CreateEmployeesDto dto);
        
        /// <summary>
        /// Update an existing Employee
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateEmployeesDto dto);
        
        /// <summary>
        /// Delete an Employee by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Employee exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);

        /// <summary>
        /// Check if Employee ID is unique
        /// </summary>
        Task<Result<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null, Guid? companyId = null);

        /// <summary>
        /// Check if Email is unique
        /// </summary>
        Task<Result<bool>> IsEmailUniqueAsync(string email, Guid? excludeId = null, Guid? companyId = null);

        /// <summary>
        /// Bulk create employees
        /// </summary>
        Task<Result<BulkEmployeesResultDto>> BulkCreateAsync(BulkEmployeesDto dto);
    }
}
