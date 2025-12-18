using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IEmployeesRepository
    {
        Task<Employees?> GetByIdAsync(Guid id);
        Task<Employees?> GetByEmployeeIdAsync(string employeeId);
        Task<IEnumerable<Employees>> GetAllAsync();
        Task<(IEnumerable<Employees> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Employees> AddAsync(Employees entity);
        Task UpdateAsync(Employees entity);
        Task DeleteAsync(Guid id);
        Task<bool> EmployeeIdExistsAsync(string employeeId, Guid? excludeId = null);
        Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
        Task<IEnumerable<Employees>> GetByManagerIdAsync(Guid managerId);
    }
}