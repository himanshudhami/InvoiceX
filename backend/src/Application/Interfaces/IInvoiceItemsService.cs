using Application.DTOs.InvoiceItems;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for InvoiceItems operations
    /// </summary>
    public interface IInvoiceItemsService
    {
        /// <summary>
        /// Get InvoiceItems by ID
        /// </summary>
        Task<Result<InvoiceItems>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all InvoiceItems entities
        /// </summary>
        Task<Result<IEnumerable<InvoiceItems>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated InvoiceItems entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<InvoiceItems> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new InvoiceItems
        /// </summary>
        Task<Result<InvoiceItems>> CreateAsync(CreateInvoiceItemsDto dto);
        
        /// <summary>
        /// Update an existing InvoiceItems
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateInvoiceItemsDto dto);
        
        /// <summary>
        /// Delete a InvoiceItems by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if InvoiceItems exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}