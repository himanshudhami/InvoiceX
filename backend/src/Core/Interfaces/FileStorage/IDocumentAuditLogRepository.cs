using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities.FileStorage;

namespace Core.Interfaces.FileStorage
{
    /// <summary>
    /// Repository interface for document audit log operations
    /// </summary>
    public interface IDocumentAuditLogRepository
    {
        /// <summary>
        /// Add a new audit log entry
        /// </summary>
        Task<DocumentAuditLog> AddAsync(DocumentAuditLog entry);

        /// <summary>
        /// Get audit logs for a specific document
        /// </summary>
        Task<IEnumerable<DocumentAuditLog>> GetByDocumentIdAsync(Guid documentId);

        /// <summary>
        /// Get audit logs for a specific file storage record
        /// </summary>
        Task<IEnumerable<DocumentAuditLog>> GetByFileStorageIdAsync(Guid fileStorageId);

        /// <summary>
        /// Get audit logs by actor (user)
        /// </summary>
        Task<IEnumerable<DocumentAuditLog>> GetByActorAsync(Guid actorId, int limit = 100);

        /// <summary>
        /// Get paginated audit logs for a company
        /// </summary>
        Task<(IEnumerable<DocumentAuditLog> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? action = null,
            Guid? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Get recent audit logs for a company (for dashboard)
        /// </summary>
        Task<IEnumerable<DocumentAuditLog>> GetRecentAsync(Guid companyId, int limit = 50);
    }
}
