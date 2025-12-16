using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ICompaniesRepository
    {
        Task<Companies?> GetByIdAsync(Guid id);
        Task<IEnumerable<Companies>> GetAllAsync();
        Task<(IEnumerable<Companies> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Companies> AddAsync(Companies entity);
        Task UpdateAsync(Companies entity);
        Task DeleteAsync(Guid id);
    }
}