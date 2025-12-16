using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IInvoiceItemsRepository
    {
        Task<InvoiceItems?> GetByIdAsync(Guid id);
        Task<IEnumerable<InvoiceItems>> GetAllAsync();
        Task<(IEnumerable<InvoiceItems> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<InvoiceItems> AddAsync(InvoiceItems entity);
        Task UpdateAsync(InvoiceItems entity);
        Task DeleteAsync(Guid id);
    }
}