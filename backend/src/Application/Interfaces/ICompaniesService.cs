using Application.DTOs.Companies;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
// AutoMapper is used in the implementation; interface doesn't require it directly

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Companies operations
    /// </summary>
    public interface ICompaniesService
    {
        /// <summary>
        /// Get Companies by ID
        /// </summary>
        Task<Result<Companies>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Get all Companies entities
        /// </summary>
        Task<Result<IEnumerable<Companies>>> GetAllAsync();
        
        /// <summary>
        /// Get paginated Companies entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Companies> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        
        /// <summary>
        /// Create a new Companies
        /// </summary>
        Task<Result<Companies>> CreateAsync(CreateCompaniesDto dto);
        
        /// <summary>
        /// Update an existing Companies
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateCompaniesDto dto);
        
        /// <summary>
        /// Delete a Companies by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
        
        /// <summary>
        /// Check if Companies exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}