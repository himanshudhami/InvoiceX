using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.FileStorage;
using Application.Interfaces;
using Core.Common;
using Core.Entities.FileStorage;
using Core.Interfaces.FileStorage;
using Microsoft.Extensions.Logging;

namespace Application.Services.FileStorage
{
    /// <summary>
    /// Application service for file upload operations.
    /// Orchestrates storage service, repository, and audit logging.
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IFileStorageService _storageService;
        private readonly IFileStorageRepository _fileRepository;
        private readonly IDocumentAuditLogRepository _auditRepository;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IFileStorageService storageService,
            IFileStorageRepository fileRepository,
            IDocumentAuditLogRepository auditRepository,
            ILogger<FileUploadService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<FileStorageDto>> UploadAsync(
            Stream fileStream,
            string filename,
            string mimeType,
            long fileSize,
            Guid companyId,
            Guid uploadedBy,
            string? entityType = null,
            Guid? entityId = null,
            string? actorIp = null)
        {
            try
            {
                _logger.LogInformation(
                    "Uploading file {Filename} ({Size} bytes) for company {CompanyId} by user {UserId}",
                    filename, fileSize, companyId, uploadedBy);

                // Upload to storage
                var uploadResult = await _storageService.UploadAsync(
                    fileStream, filename, mimeType, companyId, uploadedBy);

                if (!uploadResult.Success)
                {
                    _logger.LogWarning("File upload failed: {Error}", uploadResult.ErrorMessage);
                    return Error.Validation(uploadResult.ErrorMessage ?? "Upload failed");
                }

                // Create metadata record
                var entity = new FileStorageEntity
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    OriginalFilename = filename,
                    StoredFilename = uploadResult.StoredFilename,
                    StoragePath = uploadResult.StoragePath,
                    StorageProvider = _storageService.ProviderName,
                    FileSize = fileSize,
                    MimeType = mimeType,
                    Checksum = uploadResult.Checksum,
                    UploadedBy = uploadedBy,
                    EntityType = entityType,
                    EntityId = entityId,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                var savedEntity = await _fileRepository.AddAsync(entity);

                // Log audit entry
                await LogAuditAsync(
                    companyId,
                    null,
                    savedEntity.Id,
                    AuditActions.Upload,
                    uploadedBy,
                    actorIp,
                    new { filename, fileSize, mimeType });

                _logger.LogInformation(
                    "File uploaded successfully: {FileId} -> {StoragePath}",
                    savedEntity.Id, savedEntity.StoragePath);

                return Result<FileStorageDto>.Success(MapToDto(savedEntity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {Filename}", filename);
                return Error.Internal("An error occurred while uploading the file");
            }
        }

        public async Task<Result<FileStorageDto>> GetByIdAsync(Guid id)
        {
            var entity = await _fileRepository.GetByIdAsync(id);

            if (entity == null)
            {
                return Error.NotFound($"File with ID {id} not found");
            }

            return Result<FileStorageDto>.Success(MapToDto(entity));
        }

        public async Task<Result<FileStorageDto>> GetByPathAsync(string storagePath)
        {
            var entity = await _fileRepository.GetByPathAsync(storagePath);

            if (entity == null)
            {
                return Error.NotFound("File not found");
            }

            return Result<FileStorageDto>.Success(MapToDto(entity));
        }

        public async Task<Result<(Stream Stream, string OriginalFilename, string MimeType)>> DownloadAsync(
            Guid id,
            Guid actorId,
            string? actorIp = null)
        {
            try
            {
                var entity = await _fileRepository.GetByIdAsync(id);

                if (entity == null)
                {
                    return Error.NotFound($"File with ID {id} not found");
                }

                var stream = await _storageService.DownloadAsync(entity.StoragePath);

                // Log audit entry
                await LogAuditAsync(
                    entity.CompanyId,
                    null,
                    entity.Id,
                    AuditActions.Download,
                    actorId,
                    actorIp,
                    new { entity.OriginalFilename, entity.FileSize });

                return Result<(Stream, string, string)>.Success((stream, entity.OriginalFilename, entity.MimeType));
            }
            catch (FileNotFoundException)
            {
                return Error.NotFound("File not found on storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", id);
                return Error.Internal("An error occurred while downloading the file");
            }
        }

        public async Task<Result<(Stream Stream, FileStorageDto Metadata)>> DownloadByPathAsync(
            string storagePath,
            Guid actorId,
            string? actorIp = null)
        {
            try
            {
                var entity = await _fileRepository.GetByPathAsync(storagePath);

                if (entity == null)
                {
                    return Error.NotFound("File not found");
                }

                var stream = await _storageService.DownloadAsync(storagePath);

                // Log audit entry
                await LogAuditAsync(
                    entity.CompanyId,
                    null,
                    entity.Id,
                    AuditActions.Download,
                    actorId,
                    actorIp,
                    new { entity.OriginalFilename, entity.FileSize });

                return Result<(Stream, FileStorageDto)>.Success((stream, MapToDto(entity)));
            }
            catch (FileNotFoundException)
            {
                return Error.NotFound("File not found on storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file by path {Path}", storagePath);
                return Error.Internal("An error occurred while downloading the file");
            }
        }

        public async Task<Result<IEnumerable<FileStorageDto>>> GetByEntityAsync(string entityType, Guid entityId)
        {
            var entities = await _fileRepository.GetByEntityAsync(entityType, entityId);
            return Result<IEnumerable<FileStorageDto>>.Success(entities.Select(MapToDto));
        }

        public async Task<Result<(IEnumerable<FileStorageDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            FileStorageFilterRequest request)
        {
            var (items, totalCount) = await _fileRepository.GetPagedAsync(
                companyId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.EntityType,
                request.IncludeDeleted);

            return Result<(IEnumerable<FileStorageDto>, int)>.Success(
                (items.Select(MapToDto), totalCount));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, Guid deletedBy, string? actorIp = null)
        {
            try
            {
                var entity = await _fileRepository.GetByIdAsync(id);

                if (entity == null)
                {
                    return Error.NotFound($"File with ID {id} not found");
                }

                // Soft delete in database
                await _fileRepository.SoftDeleteAsync(id, deletedBy);

                // Log audit entry
                await LogAuditAsync(
                    entity.CompanyId,
                    null,
                    entity.Id,
                    AuditActions.Delete,
                    deletedBy,
                    actorIp,
                    new { entity.OriginalFilename });

                _logger.LogInformation("File soft deleted: {FileId} by user {UserId}", id, deletedBy);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", id);
                return Error.Internal("An error occurred while deleting the file");
            }
        }

        public async Task<Result<FileStorageDto>> LinkToEntityAsync(Guid id, string entityType, Guid entityId)
        {
            var entity = await _fileRepository.GetByIdAsync(id);

            if (entity == null)
            {
                return Error.NotFound($"File with ID {id} not found");
            }

            entity.EntityType = entityType;
            entity.EntityId = entityId;

            await _fileRepository.UpdateAsync(entity);

            return Result<FileStorageDto>.Success(MapToDto(entity));
        }

        public async Task<Result<FileStorageStatsDto>> GetStatsAsync(Guid companyId)
        {
            var stats = await _fileRepository.GetStatsAsync(companyId);

            return Result<FileStorageStatsDto>.Success(new FileStorageStatsDto
            {
                TotalFiles = stats.TotalFiles,
                TotalSizeBytes = stats.TotalSizeBytes,
                TotalSizeFormatted = FormatFileSize(stats.TotalSizeBytes),
                FilesByEntityType = stats.FilesByEntityType,
                FilesByMimeType = stats.FilesByMimeType
            });
        }

        public async Task<Result<IEnumerable<DocumentAuditLogDto>>> GetRecentAuditLogsAsync(Guid companyId, int limit = 50)
        {
            var logs = await _auditRepository.GetRecentAsync(companyId, limit);
            return Result<IEnumerable<DocumentAuditLogDto>>.Success(logs.Select(MapAuditLogToDto));
        }

        public async Task<Result<(IEnumerable<DocumentAuditLogDto> Items, int TotalCount)>> GetAuditLogsPagedAsync(
            Guid companyId,
            AuditLogFilterRequest request)
        {
            var (items, totalCount) = await _auditRepository.GetPagedAsync(
                companyId,
                request.PageNumber,
                request.PageSize,
                request.Action,
                request.ActorId,
                request.FromDate,
                request.ToDate);

            return Result<(IEnumerable<DocumentAuditLogDto>, int)>.Success(
                (items.Select(MapAuditLogToDto), totalCount));
        }

        // Private helpers

        private async Task LogAuditAsync(
            Guid companyId,
            Guid? documentId,
            Guid? fileStorageId,
            string action,
            Guid actorId,
            string? actorIp,
            object? metadata)
        {
            try
            {
                var entry = new DocumentAuditLog
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DocumentId = documentId,
                    FileStorageId = fileStorageId,
                    Action = action,
                    ActorId = actorId,
                    ActorIp = actorIp,
                    Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                    CreatedAt = DateTime.UtcNow
                };

                await _auditRepository.AddAsync(entry);
            }
            catch (Exception ex)
            {
                // Don't fail the main operation if audit logging fails
                _logger.LogWarning(ex, "Failed to log audit entry for action {Action}", action);
            }
        }

        private FileStorageDto MapToDto(FileStorageEntity entity)
        {
            return new FileStorageDto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                OriginalFilename = entity.OriginalFilename,
                StoredFilename = entity.StoredFilename,
                StoragePath = entity.StoragePath,
                StorageProvider = entity.StorageProvider,
                FileSize = entity.FileSize,
                MimeType = entity.MimeType,
                Checksum = entity.Checksum,
                UploadedBy = entity.UploadedBy,
                UploaderName = entity.UploaderName,
                EntityType = entity.EntityType,
                EntityId = entity.EntityId,
                CreatedAt = entity.CreatedAt,
                DownloadUrl = $"/api/files/download/{Uri.EscapeDataString(entity.StoragePath)}",
                FileSizeFormatted = FormatFileSize(entity.FileSize)
            };
        }

        private DocumentAuditLogDto MapAuditLogToDto(DocumentAuditLog log)
        {
            return new DocumentAuditLogDto
            {
                Id = log.Id,
                CompanyId = log.CompanyId,
                DocumentId = log.DocumentId,
                FileStorageId = log.FileStorageId,
                Action = log.Action,
                ActorId = log.ActorId,
                ActorName = log.ActorName,
                ActorIp = log.ActorIp,
                FileName = log.FileName,
                Metadata = log.Metadata,
                CreatedAt = log.CreatedAt
            };
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
