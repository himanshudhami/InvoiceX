using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Core.Entities.FileStorage;
using Core.Interfaces.FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileStorage
{
    /// <summary>
    /// Local disk implementation of IFileStorageService.
    /// Stores files on the local filesystem with company-based directory structure.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly long _maxFileSizeBytes;
        private readonly ILogger<LocalFileStorageService> _logger;

        public string ProviderName => StorageProviders.Local;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get configuration with defaults
            var fileStorageSection = configuration.GetSection("FileStorage");
            _basePath = fileStorageSection["LocalPath"] ?? "./uploads";

            var maxFileSizeStr = fileStorageSection["MaxFileSizeMB"];
            var maxFileSizeMB = int.TryParse(maxFileSizeStr, out var parsed) ? parsed : 25;
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;

            // Ensure base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created file storage base directory: {BasePath}", _basePath);
            }
        }

        /// <summary>
        /// Upload a file to local disk storage
        /// </summary>
        public async Task<FileUploadResult> UploadAsync(
            Stream fileStream,
            string filename,
            string mimeType,
            Guid companyId,
            Guid uploadedBy)
        {
            try
            {
                // Validate MIME type
                if (!AllowedMimeTypes.IsAllowed(mimeType))
                {
                    _logger.LogWarning("File upload rejected: Invalid MIME type {MimeType}", mimeType);
                    return FileUploadResult.Fail($"File type '{mimeType}' is not allowed. Allowed types: PDF, PNG, JPG, DOC, DOCX");
                }

                // Validate file size
                if (fileStream.Length > _maxFileSizeBytes)
                {
                    var maxMB = _maxFileSizeBytes / (1024 * 1024);
                    _logger.LogWarning("File upload rejected: Size {Size} exceeds limit {MaxSize}", fileStream.Length, _maxFileSizeBytes);
                    return FileUploadResult.Fail($"File size exceeds the maximum allowed size of {maxMB} MB");
                }

                // Generate secure filename (UUID-based, no user input)
                var extension = Path.GetExtension(filename)?.ToLowerInvariant() ?? string.Empty;
                var storedFilename = $"{Guid.NewGuid()}{extension}";

                // Create directory structure: companyId/yyyy/MM/
                var yearMonth = DateTime.UtcNow.ToString("yyyy/MM");
                var relativePath = Path.Combine(companyId.ToString(), yearMonth, storedFilename);
                var fullPath = Path.Combine(_basePath, relativePath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Calculate checksum while writing file
                string checksum;
                using (var sha256 = SHA256.Create())
                using (var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Read in chunks for memory efficiency
                    var buffer = new byte[81920]; // 80 KB buffer
                    int bytesRead;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStreamOut.WriteAsync(buffer, 0, bytesRead);
                        sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }

                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    checksum = BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
                }

                // Set file to read-only for security (optional, can be disabled)
                // File.SetAttributes(fullPath, FileAttributes.ReadOnly);

                _logger.LogInformation(
                    "File uploaded successfully: {StoredFilename} for company {CompanyId} by user {UserId}",
                    storedFilename, companyId, uploadedBy);

                return FileUploadResult.Succeed(relativePath, storedFilename, checksum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {Filename} for company {CompanyId}", filename, companyId);
                return FileUploadResult.Fail("An error occurred while uploading the file. Please try again.");
            }
        }

        /// <summary>
        /// Download a file from local disk storage
        /// </summary>
        public Task<Stream> DownloadAsync(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));
            }

            var fullPath = Path.Combine(_basePath, storagePath);

            // Security check: ensure path doesn't escape base directory
            var normalizedFullPath = Path.GetFullPath(fullPath);
            var normalizedBasePath = Path.GetFullPath(_basePath);

            if (!normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {StoragePath}", storagePath);
                throw new UnauthorizedAccessException("Invalid file path");
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {StoragePath}", storagePath);
                throw new FileNotFoundException("File not found", storagePath);
            }

            _logger.LogInformation("File download: {StoragePath}", storagePath);

            // Return FileStream - caller is responsible for disposing
            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        /// <summary>
        /// Delete a file from local disk storage
        /// </summary>
        public Task<bool> DeleteAsync(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                return Task.FromResult(false);
            }

            var fullPath = Path.Combine(_basePath, storagePath);

            // Security check
            var normalizedFullPath = Path.GetFullPath(fullPath);
            var normalizedBasePath = Path.GetFullPath(_basePath);

            if (!normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt in delete: {StoragePath}", storagePath);
                return Task.FromResult(false);
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {StoragePath}", storagePath);
                return Task.FromResult(false);
            }

            try
            {
                // Remove read-only attribute if set
                var attributes = File.GetAttributes(fullPath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(fullPath, attributes & ~FileAttributes.ReadOnly);
                }

                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {StoragePath}", storagePath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {StoragePath}", storagePath);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Check if a file exists in local disk storage
        /// </summary>
        public Task<bool> ExistsAsync(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                return Task.FromResult(false);
            }

            var fullPath = Path.Combine(_basePath, storagePath);

            // Security check
            var normalizedFullPath = Path.GetFullPath(fullPath);
            var normalizedBasePath = Path.GetFullPath(_basePath);

            if (!normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(File.Exists(fullPath));
        }
    }
}
