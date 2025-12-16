using Application.DTOs.InvoiceTemplates;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for InvoiceTemplates operations
    /// </summary>
    public interface IInvoiceTemplatesService
    {
        /// <summary>
        /// Get InvoiceTemplates by ID
        /// </summary>
        Task<Result<InvoiceTemplates>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all InvoiceTemplates entities
        /// </summary>
        Task<Result<IEnumerable<InvoiceTemplates>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated InvoiceTemplates entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<InvoiceTemplates> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new InvoiceTemplates
        /// </summary>
        Task<Result<InvoiceTemplates>> CreateAsync(CreateInvoiceTemplatesDto dto);
        
        /// <summary>
        /// Update an existing InvoiceTemplates
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateInvoiceTemplatesDto dto);
        
        /// <summary>
        /// Delete a InvoiceTemplates by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if InvoiceTemplates exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}