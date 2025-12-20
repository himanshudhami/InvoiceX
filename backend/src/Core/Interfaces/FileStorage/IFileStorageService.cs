using System;
using System.IO;
using System.Threading.Tasks;

namespace Core.Interfaces.FileStorage
{
    /// <summary>
    /// Abstraction for file storage operations.
    /// Implementations can target local disk, S3, Azure Blob, etc.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Upload a file to storage
        /// </summary>
        /// <param name="fileStream">The file content stream</param>
        /// <param name="filename">Original filename (for extension extraction)</param>
        /// <param name="mimeType">MIME type of the file</param>
        /// <param name="companyId">Company ID for path organization</param>
        /// <param name="uploadedBy">User ID of uploader</param>
        /// <returns>Upload result with storage path or error</returns>
        Task<FileUploadResult> UploadAsync(
            Stream fileStream,
            string filename,
            string mimeType,
            Guid companyId,
            Guid uploadedBy);

        /// <summary>
        /// Download a file from storage
        /// </summary>
        /// <param name="storagePath">Relative path within storage</param>
        /// <returns>File content stream</returns>
        Task<Stream> DownloadAsync(string storagePath);

        /// <summary>
        /// Delete a file from storage
        /// </summary>
        /// <param name="storagePath">Relative path within storage</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(string storagePath);

        /// <summary>
        /// Check if a file exists in storage
        /// </summary>
        /// <param name="storagePath">Relative path within storage</param>
        /// <returns>True if exists</returns>
        Task<bool> ExistsAsync(string storagePath);

        /// <summary>
        /// Get the storage provider name (local, s3, azure)
        /// </summary>
        string ProviderName { get; }
    }

    /// <summary>
    /// Result of a file upload operation
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Whether the upload succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Relative path where the file is stored
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Generated filename (UUID-based)
        /// </summary>
        public string StoredFilename { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 checksum of the file
        /// </summary>
        public string? Checksum { get; set; }

        /// <summary>
        /// Error message if upload failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Create a success result
        /// </summary>
        public static FileUploadResult Succeed(string storagePath, string storedFilename, string? checksum = null)
        {
            return new FileUploadResult
            {
                Success = true,
                StoragePath = storagePath,
                StoredFilename = storedFilename,
                Checksum = checksum
            };
        }

        /// <summary>
        /// Create a failure result
        /// </summary>
        public static FileUploadResult Fail(string errorMessage)
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
