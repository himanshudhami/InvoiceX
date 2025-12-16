using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IInvoicesRepository
    {
        Task<Invoices?> GetByIdAsync(Guid id);
        Task<IEnumerable<Invoices>> GetAllAsync();
        Task<(IEnumerable<Invoices> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Invoices> AddAsync(Invoices entity);
        Task UpdateAsync(Invoices entity);
        Task DeleteAsync(Guid id);
    }
}