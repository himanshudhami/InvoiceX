using Core.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Base service interface for common CRUD operations
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TCreateDto">The DTO type for creation</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type for updates</typeparam>
    public interface IBaseService<TEntity, TCreateDto, TUpdateDto>
    {
        /// <summary>
        /// Get an entity by ID
        /// </summary>
        Task<Result<TEntity>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all entities
        /// </summary>
        Task<Result<IEnumerable<TEntity>>> GetAllAsync();

        /// <summary>
        /// Get paginated entities with filtering, sorting, and searching
        /// </summary>
        Task<Result<(IEnumerable<TEntity> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new entity
        /// </summary>
        Task<Result<TEntity>> CreateAsync(TCreateDto dto);

        /// <summary>
        /// Update an existing entity
        /// </summary>
        Task<Result> UpdateAsync(Guid id, TUpdateDto dto);

        /// <summary>
        /// Delete an entity by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
    }
}






