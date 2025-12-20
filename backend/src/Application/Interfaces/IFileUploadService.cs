using System;
using System.IO;
using System.Threading.Tasks;
using Application.DTOs.FileStorage;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Application service interface for file upload operations.
    /// Orchestrates storage service and repository operations.
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload a file and store its metadata
        /// </summary>
        /// <param name="fileStream">File content stream</param>
        /// <param name="filename">Original filename</param>
        /// <param name="mimeType">MIME type</param>
        /// <param name="fileSize">File size in bytes</param>
        /// <param name="companyId">Company ID for tenant isolation</param>
        /// <param name="uploadedBy">User ID of uploader</param>
        /// <param name="entityType">Optional entity type to associate with</param>
        /// <param name="entityId">Optional entity ID to associate with</param>
        /// <param name="actorIp">IP address of the uploader</param>
        /// <returns>File storage DTO with metadata</returns>
        Task<Result<FileStorageDto>> UploadAsync(
            Stream fileStream,
            string filename,
            string mimeType,
            long fileSize,
            Guid companyId,
            Guid uploadedBy,
            string? entityType = null,
            Guid? entityId = null,
            string? actorIp = null);

        /// <summary>
        /// Get file metadata by ID
        /// </summary>
        Task<Result<FileStorageDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get file metadata by storage path
        /// </summary>
        Task<Result<FileStorageDto>> GetByPathAsync(string storagePath);

        /// <summary>
        /// Download a file's content stream
        /// </summary>
        /// <param name="id">File storage ID</param>
        /// <param name="actorId">User ID of downloader (for audit)</param>
        /// <param name="actorIp">IP address of downloader</param>
        Task<Result<(Stream Stream, string OriginalFilename, string MimeType)>> DownloadAsync(
            Guid id,
            Guid actorId,
            string? actorIp = null);

        /// <summary>
        /// Download a file by path (used for direct path access)
        /// </summary>
        Task<Result<(Stream Stream, FileStorageDto Metadata)>> DownloadByPathAsync(
            string storagePath,
            Guid actorId,
            string? actorIp = null);

        /// <summary>
        /// Get all files for a specific entity
        /// </summary>
        Task<Result<IEnumerable<FileStorageDto>>> GetByEntityAsync(string entityType, Guid entityId);

        /// <summary>
        /// Get paginated list of files for a company
        /// </summary>
        Task<Result<(IEnumerable<FileStorageDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            FileStorageFilterRequest request);

        /// <summary>
        /// Soft delete a file
        /// </summary>
        /// <param name="id">File storage ID</param>
        /// <param name="deletedBy">User ID performing deletion</param>
        /// <param name="actorIp">IP address</param>
        Task<Result<bool>> DeleteAsync(Guid id, Guid deletedBy, string? actorIp = null);

        /// <summary>
        /// Link a file to an entity (update entity_type and entity_id)
        /// </summary>
        Task<Result<FileStorageDto>> LinkToEntityAsync(Guid id, string entityType, Guid entityId);

        /// <summary>
        /// Get file storage statistics for a company
        /// </summary>
        Task<Result<FileStorageStatsDto>> GetStatsAsync(Guid companyId);

        /// <summary>
        /// Get recent audit logs for a company
        /// </summary>
        Task<Result<IEnumerable<DocumentAuditLogDto>>> GetRecentAuditLogsAsync(Guid companyId, int limit = 50);

        /// <summary>
        /// Get paginated audit logs for a company
        /// </summary>
        Task<Result<(IEnumerable<DocumentAuditLogDto> Items, int TotalCount)>> GetAuditLogsPagedAsync(
            Guid companyId,
            AuditLogFilterRequest request);
    }
}
