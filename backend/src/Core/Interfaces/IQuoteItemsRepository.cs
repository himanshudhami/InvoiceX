using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IQuoteItemsRepository
    {
        Task<QuoteItems?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuoteItems>> GetAllAsync();
        Task<(IEnumerable<QuoteItems> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<QuoteItems> AddAsync(QuoteItems entity);
        Task UpdateAsync(QuoteItems entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<QuoteItems>> GetByQuoteIdAsync(Guid quoteId);
    }
}
