using System;

namespace Application.DTOs.FileStorage
{
    /// <summary>
    /// DTO for file storage metadata returned to clients
    /// </summary>
    public class FileStorageDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string OriginalFilename { get; set; } = string.Empty;
        public string StoredFilename { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string StorageProvider { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public string? Checksum { get; set; }
        public Guid? UploadedBy { get; set; }
        public string? UploaderName { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// API endpoint URL for downloading this file
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable file size (e.g., "2.5 MB")
        /// </summary>
        public string FileSizeFormatted { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for uploading a file
    /// </summary>
    public class UploadFileDto
    {
        /// <summary>
        /// Optional entity type to associate the file with
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// Optional entity ID to associate the file with
        /// </summary>
        public Guid? EntityId { get; set; }
    }

    /// <summary>
    /// DTO for linking a file to an entity after upload
    /// </summary>
    public class LinkFileDto
    {
        /// <summary>
        /// Entity type to link to
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Entity ID to link to
        /// </summary>
        public Guid EntityId { get; set; }
    }

    /// <summary>
    /// DTO for file storage statistics
    /// </summary>
    public class FileStorageStatsDto
    {
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public string TotalSizeFormatted { get; set; } = string.Empty;
        public Dictionary<string, int> FilesByEntityType { get; set; } = new();
        public Dictionary<string, int> FilesByMimeType { get; set; } = new();
    }

    /// <summary>
    /// DTO for audit log entries
    /// </summary>
    public class DocumentAuditLogDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? FileStorageId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid ActorId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorIp { get; set; }
        public string? FileName { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request for paginated file storage list
    /// </summary>
    public class FileStorageFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? EntityType { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }

    /// <summary>
    /// Request for paginated audit log list
    /// </summary>
    public class AuditLogFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? Action { get; set; }
        public Guid? ActorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
