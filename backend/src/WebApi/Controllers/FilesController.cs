using Application.DTOs.FileStorage;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// File upload and download endpoints.
/// Provides secure file handling with company-level access control.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class FilesController : CompanyAuthorizedController
{
    private readonly IFileUploadService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileUploadService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="entityType">Optional entity type to associate with (expense, employee_document, asset)</param>
    /// <param name="entityId">Optional entity ID to associate with</param>
    /// <returns>File storage metadata</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)] // 25 MB
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileStorageDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var companyId = GetEffectiveCompanyId();
        if (!companyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        if (!CurrentUserId.HasValue)
        {
            return Unauthorized("User not authenticated");
        }

        var actorIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        using var stream = file.OpenReadStream();
        var result = await _fileService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            companyId.Value,
            CurrentUserId.Value,
            entityType,
            entityId,
            actorIp);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(104_857_600)] // 100 MB total for multiple files
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IEnumerable<FileStorageDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadMultiple(
        IFormFileCollection files,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded");
        }

        var companyId = GetEffectiveCompanyId();
        if (!companyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        if (!CurrentUserId.HasValue)
        {
            return Unauthorized("User not authenticated");
        }

        var actorIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var results = new List<FileStorageDto>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            using var stream = file.OpenReadStream();
            var result = await _fileService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                companyId.Value,
                CurrentUserId.Value,
                entityType,
                entityId,
                actorIp);

            if (result.IsSuccess)
            {
                results.Add(result.Value!);
            }
            else
            {
                errors.Add($"{file.FileName}: {result.Error?.Message}");
            }
        }

        if (errors.Any() && !results.Any())
        {
            return BadRequest(new { errors });
        }

        return Ok(new { files = results, errors });
    }

    /// <summary>
    /// Download a file by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Download(Guid id)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized("User not authenticated");
        }

        var actorIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _fileService.DownloadAsync(id, CurrentUserId.Value, actorIp);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        var (stream, filename, mimeType) = result.Value;
        return File(stream, mimeType, filename);
    }

    /// <summary>
    /// Download a file by storage path
    /// </summary>
    [HttpGet("download/{*path}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DownloadByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path is required");
        }

        if (!CurrentUserId.HasValue)
        {
            return Unauthorized("User not authenticated");
        }

        // Decode the path
        var decodedPath = Uri.UnescapeDataString(path);
        var actorIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _fileService.DownloadByPathAsync(decodedPath, CurrentUserId.Value, actorIp);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        var (stream, metadata) = result.Value;

        // Verify company access
        if (!HasCompanyAccess(metadata.CompanyId))
        {
            stream.Dispose();
            return AccessDeniedDifferentCompanyResponse("File");
        }

        return File(stream, metadata.MimeType, metadata.OriginalFilename);
    }

    /// <summary>
    /// Get file metadata by ID
    /// </summary>
    [HttpGet("{id}/metadata")]
    [ProducesResponseType(typeof(FileStorageDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMetadata(Guid id)
    {
        var result = await _fileService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        // Verify company access
        if (!HasCompanyAccess(result.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("File");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all files for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(IEnumerable<FileStorageDto>), 200)]
    public async Task<IActionResult> GetByEntity(string entityType, Guid entityId)
    {
        var result = await _fileService.GetByEntityAsync(entityType, entityId);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        // Filter to only files the user has access to
        var accessibleFiles = result.Value!.Where(f => HasCompanyAccess(f.CompanyId));

        return Ok(accessibleFiles);
    }

    /// <summary>
    /// Get paginated files for a company
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(PagedResponse<FileStorageDto>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] FileStorageFilterRequest request, [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _fileService.GetPagedAsync(effectiveCompanyId.Value, request);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        var (items, totalCount) = result.Value;
        var pagedResponse = new PagedResponse<FileStorageDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Delete a file (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized("User not authenticated");
        }

        // Check file exists and user has access
        var fileResult = await _fileService.GetByIdAsync(id);
        if (fileResult.IsFailure)
        {
            return NotFound(fileResult.Error!.Message);
        }

        if (!HasCompanyAccess(fileResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("File");
        }

        var actorIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _fileService.DeleteAsync(id, CurrentUserId.Value, actorIp);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(new { message = "File deleted successfully" });
    }

    /// <summary>
    /// Link a file to an entity
    /// </summary>
    [HttpPut("{id}/link")]
    [ProducesResponseType(typeof(FileStorageDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> LinkToEntity(Guid id, [FromBody] LinkFileDto dto)
    {
        // Check file exists and user has access
        var fileResult = await _fileService.GetByIdAsync(id);
        if (fileResult.IsFailure)
        {
            return NotFound(fileResult.Error!.Message);
        }

        if (!HasCompanyAccess(fileResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("File");
        }

        var result = await _fileService.LinkToEntityAsync(id, dto.EntityType, dto.EntityId);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get file storage statistics for a company
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(FileStorageStatsDto), 200)]
    public async Task<IActionResult> GetStats([FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _fileService.GetStatsAsync(effectiveCompanyId.Value);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get recent audit logs
    /// </summary>
    [HttpGet("audit/recent")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAuditLogDto>), 200)]
    public async Task<IActionResult> GetRecentAuditLogs([FromQuery] Guid? companyId = null, [FromQuery] int limit = 50)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _fileService.GetRecentAuditLogsAsync(effectiveCompanyId.Value, limit);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get paginated audit logs
    /// </summary>
    [HttpGet("audit/paged")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(PagedResponse<DocumentAuditLogDto>), 200)]
    public async Task<IActionResult> GetAuditLogsPaged(
        [FromQuery] AuditLogFilterRequest request,
        [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _fileService.GetAuditLogsPagedAsync(effectiveCompanyId.Value, request);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        var (items, totalCount) = result.Value;
        var pagedResponse = new PagedResponse<DocumentAuditLogDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }
}
