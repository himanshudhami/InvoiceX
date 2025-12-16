using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    /// <summary>
    /// Generic repository interface for common CRUD operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get an entity by ID
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Get paginated entities with filtering, sorting, and searching
        /// </summary>
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Add a new entity
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Update an existing entity
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Delete an entity by ID
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}




