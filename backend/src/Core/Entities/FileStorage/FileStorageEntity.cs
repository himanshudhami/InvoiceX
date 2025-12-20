namespace Core.Entities.FileStorage
{
    /// <summary>
    /// Represents metadata for a stored file with abstraction for multiple storage providers.
    /// Actual file content is stored externally (local disk, S3, Azure Blob, etc.)
    /// </summary>
    public class FileStorageEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Company this file belongs to (required for tenant isolation)
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Original filename as uploaded by the user (for display purposes)
        /// </summary>
        public string OriginalFilename { get; set; } = string.Empty;

        /// <summary>
        /// Generated filename used for storage (UUID-based for security)
        /// </summary>
        public string StoredFilename { get; set; } = string.Empty;

        /// <summary>
        /// Relative path within the storage provider (e.g., companyId/2024/01/uuid.pdf)
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Storage backend: local, s3, azure
        /// </summary>
        public string StorageProvider { get; set; } = StorageProviders.Local;

        /// <summary>
        /// File size in bytes (max 25 MB = 26214400 bytes)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file (e.g., application/pdf, image/png)
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 checksum for file integrity verification
        /// </summary>
        public string? Checksum { get; set; }

        /// <summary>
        /// User who uploaded the file
        /// </summary>
        public Guid? UploadedBy { get; set; }

        /// <summary>
        /// Type of entity this file is attached to (employee_document, expense, asset)
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// ID of the entity this file is attached to
        /// </summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when file was soft deleted
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User who deleted the file
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when file was uploaded
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // Navigation properties (populated by service layer)
        public string? UploaderName { get; set; }
    }

    /// <summary>
    /// Constants for storage provider types
    /// </summary>
    public static class StorageProviders
    {
        public const string Local = "local";
        public const string S3 = "s3";
        public const string Azure = "azure";
    }

    /// <summary>
    /// Constants for entity types that files can be attached to
    /// </summary>
    public static class FileEntityTypes
    {
        public const string EmployeeDocument = "employee_document";
        public const string Expense = "expense";
        public const string Asset = "asset";
        public const string AssetRequest = "asset_request";
    }

    /// <summary>
    /// Allowed MIME types for file uploads
    /// </summary>
    public static class AllowedMimeTypes
    {
        public const string Pdf = "application/pdf";
        public const string Png = "image/png";
        public const string Jpeg = "image/jpeg";
        public const string Jpg = "image/jpg";
        public const string Doc = "application/msword";
        public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        public static readonly string[] All = new[]
        {
            Pdf, Png, Jpeg, Jpg, Doc, Docx
        };

        public static bool IsAllowed(string mimeType)
        {
            return All.Contains(mimeType.ToLowerInvariant());
        }
    }
}
