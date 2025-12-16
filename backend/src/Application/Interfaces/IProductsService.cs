using Application.DTOs.Products;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Products operations
    /// </summary>
    public interface IProductsService
    {
        /// <summary>
        /// Get Products by ID
        /// </summary>
        Task<Result<Products>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all Products entities
        /// </summary>
        Task<Result<IEnumerable<Products>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated Products entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Products> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Products
        /// </summary>
        Task<Result<Products>> CreateAsync(CreateProductsDto dto);
        
        /// <summary>
        /// Update an existing Products
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateProductsDto dto);
        
        /// <summary>
        /// Delete a Products by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Products exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}