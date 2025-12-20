using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities.FileStorage;

namespace Core.Interfaces.FileStorage
{
    /// <summary>
    /// Repository interface for file storage metadata operations
    /// </summary>
    public interface IFileStorageRepository
    {
        /// <summary>
        /// Get a file storage record by ID
        /// </summary>
        Task<FileStorageEntity?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get a file storage record by storage path
        /// </summary>
        Task<FileStorageEntity?> GetByPathAsync(string storagePath);

        /// <summary>
        /// Get all files for a specific entity (e.g., all attachments for an expense)
        /// </summary>
        /// <param name="entityType">Type of entity (expense, employee_document, asset)</param>
        /// <param name="entityId">ID of the entity</param>
        Task<IEnumerable<FileStorageEntity>> GetByEntityAsync(string entityType, Guid entityId);

        /// <summary>
        /// Get all files uploaded by a user
        /// </summary>
        Task<IEnumerable<FileStorageEntity>> GetByUploaderAsync(Guid uploaderId);

        /// <summary>
        /// Get paginated file storage records for a company
        /// </summary>
        Task<(IEnumerable<FileStorageEntity> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? entityType = null,
            bool includeDeleted = false);

        /// <summary>
        /// Add a new file storage record
        /// </summary>
        Task<FileStorageEntity> AddAsync(FileStorageEntity entity);

        /// <summary>
        /// Update an existing file storage record
        /// </summary>
        Task UpdateAsync(FileStorageEntity entity);

        /// <summary>
        /// Soft delete a file storage record
        /// </summary>
        /// <param name="id">File storage ID</param>
        /// <param name="deletedBy">User ID who deleted</param>
        Task SoftDeleteAsync(Guid id, Guid deletedBy);

        /// <summary>
        /// Hard delete a file storage record (use with caution)
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Restore a soft-deleted file
        /// </summary>
        Task RestoreAsync(Guid id);

        /// <summary>
        /// Get storage statistics for a company
        /// </summary>
        Task<FileStorageStats> GetStatsAsync(Guid companyId);
    }

    /// <summary>
    /// Storage usage statistics
    /// </summary>
    public class FileStorageStats
    {
        /// <summary>
        /// Total number of files
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Total storage used in bytes
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Files by entity type
        /// </summary>
        public Dictionary<string, int> FilesByEntityType { get; set; } = new();

        /// <summary>
        /// Files by MIME type
        /// </summary>
        public Dictionary<string, int> FilesByMimeType { get; set; } = new();
    }
}
