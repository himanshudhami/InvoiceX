using Application.DTOs.Invoices;
using Core.Entities;
using Core.Common;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Invoices operations
    /// </summary>
    public interface IInvoicesService
    {
        /// <summary>
        /// Get Invoices by ID
        /// </summary>
        Task<Result<Invoices>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all Invoices entities
        /// </summary>
        Task<Result<IEnumerable<Invoices>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated Invoices entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Invoices> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Invoices
        /// </summary>
        Task<Result<Invoices>> CreateAsync(CreateInvoicesDto dto);
        
        /// <summary>
        /// Update an existing Invoices
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateInvoicesDto dto);
        
        /// <summary>
        /// Delete a Invoices by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Invoices exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
        
        /// <summary>
        /// Duplicate an existing invoice
        /// </summary>
        Task<Result<Invoices>> DuplicateAsync(Guid id);
        
        /// <summary>
        /// Record a payment for an invoice
        /// </summary>
        Task<Result<Payments>> RecordPaymentAsync(Guid invoiceId, Application.DTOs.Payments.CreatePaymentsDto paymentDto);
    }
}