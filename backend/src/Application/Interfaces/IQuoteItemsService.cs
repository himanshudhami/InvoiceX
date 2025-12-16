using Application.DTOs.QuoteItems;
using Core.Entities;
using Core.Common;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for QuoteItems operations
    /// </summary>
    public interface IQuoteItemsService
    {
        /// <summary>
        /// Get QuoteItem by ID
        /// </summary>
        Task<Result<QuoteItems>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all QuoteItems entities
        /// </summary>
        Task<Result<IEnumerable<QuoteItems>>> GetAllAsync();

        /// <summary>
        /// Get paginated QuoteItems entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<QuoteItems> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new QuoteItem
        /// </summary>
        Task<Result<QuoteItems>> CreateAsync(CreateQuoteItemsDto dto);

        /// <summary>
        /// Update an existing QuoteItem
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateQuoteItemsDto dto);

        /// <summary>
        /// Delete a QuoteItem by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
    }
}
