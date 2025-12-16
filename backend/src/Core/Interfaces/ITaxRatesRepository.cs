using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ITaxRatesRepository
    {
        Task<TaxRates?> GetByIdAsync(Guid id);
        Task<IEnumerable<TaxRates>> GetAllAsync();
        Task<(IEnumerable<TaxRates> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<TaxRates> AddAsync(TaxRates entity);
        Task UpdateAsync(TaxRates entity);
        Task DeleteAsync(Guid id);
    }
}