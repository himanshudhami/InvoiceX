using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IInvoiceTemplatesRepository
    {
        Task<InvoiceTemplates?> GetByIdAsync(Guid id);
        Task<IEnumerable<InvoiceTemplates>> GetAllAsync();
        Task<(IEnumerable<InvoiceTemplates> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<InvoiceTemplates> AddAsync(InvoiceTemplates entity);
        Task UpdateAsync(InvoiceTemplates entity);
        Task DeleteAsync(Guid id);
    }
}