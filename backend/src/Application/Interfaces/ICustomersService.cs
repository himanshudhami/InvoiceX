using Application.DTOs.Customers;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Customers operations
    /// </summary>
    public interface ICustomersService
    {
        /// <summary>
        /// Get Customers by ID
        /// </summary>
        Task<Result<Customers>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all Customers entities
        /// </summary>
        Task<Result<IEnumerable<Customers>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated Customers entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Customers> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Customers
        /// </summary>
        Task<Result<Customers>> CreateAsync(CreateCustomersDto dto);
        
        /// <summary>
        /// Update an existing Customers
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateCustomersDto dto);
        
        /// <summary>
        /// Delete a Customers by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Customers exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}