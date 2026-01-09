using Application.DTOs.Quotes;
using Core.Entities;
using Core.Common;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Quotes operations
    /// </summary>
    public interface IQuotesService
    {
        /// <summary>
        /// Get Quotes by ID
        /// </summary>
        Task<Result<Quotes>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all Quotes entities
        /// </summary>
        Task<Result<IEnumerable<Quotes>>> GetAllAsync();

        /// <summary>
        /// Get paginated Quotes entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Quotes> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new Quotes
        /// </summary>
        Task<Result<Quotes>> CreateAsync(CreateQuotesDto dto);

        /// <summary>
        /// Update an existing Quotes
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateQuotesDto dto);

        /// <summary>
        /// Delete a Quotes by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Check if Quotes exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);

        /// <summary>
        /// Duplicate an existing quote
        /// </summary>
        Task<Result<Quotes>> DuplicateAsync(Guid id);

        /// <summary>
        /// Send quote to customer
        /// </summary>
        Task<Result> SendAsync(Guid id);

        /// <summary>
        /// Accept quote
        /// </summary>
        Task<Result> AcceptAsync(Guid id);

        /// <summary>
        /// Reject quote
        /// </summary>
        Task<Result> RejectAsync(Guid id);

        /// <summary>
        /// Convert quote to invoice
        /// </summary>
        Task<Result<Invoices>> ConvertToInvoiceAsync(Guid id);
    }
}
