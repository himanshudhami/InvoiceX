using Application.DTOs.TaxRates;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for TaxRates operations
    /// </summary>
    public interface ITaxRatesService
    {
        /// <summary>
        /// Get TaxRates by ID
        /// </summary>
        Task<Result<TaxRates>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all TaxRates entities
        /// </summary>
        Task<Result<IEnumerable<TaxRates>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated TaxRates entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<TaxRates> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new TaxRates
        /// </summary>
        Task<Result<TaxRates>> CreateAsync(CreateTaxRatesDto dto);
        
        /// <summary>
        /// Update an existing TaxRates
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateTaxRatesDto dto);
        
        /// <summary>
        /// Delete a TaxRates by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if TaxRates exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}