using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ICustomersRepository
    {
        Task<Customers?> GetByIdAsync(Guid id);
        Task<IEnumerable<Customers>> GetAllAsync();
        Task<(IEnumerable<Customers> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Customers> AddAsync(Customers entity);
        Task UpdateAsync(Customers entity);
        Task DeleteAsync(Guid id);
    }
}