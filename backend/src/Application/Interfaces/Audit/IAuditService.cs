using Core.Entities.Audit;

namespace Application.Interfaces.Audit
{
    /// <summary>
    /// Service interface for recording audit trail entries.
    /// All audit operations are non-blocking - failures are logged but don't affect main operations.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Record a create operation for an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">The created entity</param>
        /// <param name="entityId">Entity primary key</param>
        /// <param name="companyId">Company context</param>
        /// <param name="displayName">Human-readable name (e.g., "INV-2024-0001")</param>
        Task AuditCreateAsync<T>(T entity, Guid entityId, Guid companyId, string? displayName = null) where T : class;

        /// <summary>
        /// Record an update operation with before/after comparison
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="oldEntity">Entity state before update</param>
        /// <param name="newEntity">Entity state after update</param>
        /// <param name="entityId">Entity primary key</param>
        /// <param name="companyId">Company context</param>
        /// <param name="displayName">Human-readable name</param>
        Task AuditUpdateAsync<T>(T oldEntity, T newEntity, Guid entityId, Guid companyId, string? displayName = null) where T : class;

        /// <summary>
        /// Record a delete operation
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">The deleted entity</param>
        /// <param name="entityId">Entity primary key</param>
        /// <param name="companyId">Company context</param>
        /// <param name="displayName">Human-readable name</param>
        Task AuditDeleteAsync<T>(T entity, Guid entityId, Guid companyId, string? displayName = null) where T : class;

        /// <summary>
        /// Record an audit entry with explicit entity type (for cases where type cannot be inferred)
        /// </summary>
        Task AuditAsync(
            string entityType,
            Guid entityId,
            Guid companyId,
            string operation,
            object? oldValues,
            object? newValues,
            string? displayName = null);
    }
}
