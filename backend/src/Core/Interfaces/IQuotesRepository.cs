using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IQuotesRepository
    {
        Task<Quotes?> GetByIdAsync(Guid id);
        Task<IEnumerable<Quotes>> GetAllAsync();
        Task<(IEnumerable<Quotes> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Quotes> AddAsync(Quotes entity);
        Task UpdateAsync(Quotes entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
