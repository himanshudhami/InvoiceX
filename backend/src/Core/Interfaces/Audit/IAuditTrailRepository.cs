using Core.Entities.Audit;

namespace Core.Interfaces.Audit
{
    /// <summary>
    /// Repository interface for MCA-compliant audit trail operations
    /// </summary>
    public interface IAuditTrailRepository
    {
        /// <summary>
        /// Add a new audit trail entry
        /// </summary>
        Task<AuditTrail> AddAsync(AuditTrail entry);

        /// <summary>
        /// Add multiple audit entries in batch
        /// </summary>
        Task AddBatchAsync(IEnumerable<AuditTrail> entries);

        /// <summary>
        /// Get audit entry by ID
        /// </summary>
        Task<AuditTrail?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get audit history for a specific entity
        /// </summary>
        Task<IEnumerable<AuditTrail>> GetByEntityAsync(string entityType, Guid entityId);

        /// <summary>
        /// Get audit logs by actor (user)
        /// </summary>
        Task<IEnumerable<AuditTrail>> GetByActorAsync(Guid actorId, int limit = 100);

        /// <summary>
        /// Get paginated audit logs for a company with filtering
        /// </summary>
        Task<(IEnumerable<AuditTrail> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? entityType = null,
            Guid? entityId = null,
            string? operation = null,
            Guid? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null);

        /// <summary>
        /// Get recent audit logs for a company (for dashboard)
        /// </summary>
        Task<IEnumerable<AuditTrail>> GetRecentAsync(Guid companyId, int limit = 50);

        /// <summary>
        /// Get audit logs by date range (for compliance reports/export)
        /// </summary>
        Task<IEnumerable<AuditTrail>> GetByDateRangeAsync(
            Guid companyId,
            DateTime fromDate,
            DateTime toDate,
            string? entityType = null);

        /// <summary>
        /// Get count of audit entries by operation type (for analytics)
        /// </summary>
        Task<Dictionary<string, int>> GetOperationCountsAsync(
            Guid companyId,
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }
}
