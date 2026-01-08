using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Migration
{
    /// <summary>
    /// Controller for Tally ERP data migration
    /// </summary>
    [ApiController]
    [Route("api/migration/tally")]
    [Authorize]
    [Produces("application/json")]
    public class TallyMigrationController : ControllerBase
    {
        private readonly ITallyImportService _importService;
        private readonly ITallyValidationService _validationService;
        private readonly ITallyRollbackService _rollbackService;
        private readonly ILogger<TallyMigrationController> _logger;

        public TallyMigrationController(
            ITallyImportService importService,
            ITallyValidationService validationService,
            ITallyRollbackService rollbackService,
            ILogger<TallyMigrationController> logger)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _rollbackService = rollbackService ?? throw new ArgumentNullException(nameof(rollbackService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Upload and parse a Tally export file (XML or JSON)
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="file">Tally export file</param>
        /// <param name="importType">Import type: 'full' for initial migration, 'incremental' for sync</param>
        /// <returns>Parsed data preview with batch ID</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(TallyUploadResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [RequestSizeLimit(104857600)] // 100MB limit
        public async Task<IActionResult> Upload(
            [FromQuery] Guid companyId,
            IFormFile file,
            [FromQuery] string importType = "full")
        {
            if (companyId == Guid.Empty)
            {
                return BadRequest("Company ID is required");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xml" && extension != ".json")
            {
                return BadRequest("Only XML and JSON files are supported");
            }

            var userId = GetCurrentUserId();

            var request = new TallyUploadRequestDto
            {
                CompanyId = companyId,
                ImportType = importType,
                FileName = file.FileName
            };

            using var stream = file.OpenReadStream();
            var result = await _importService.UploadAndParseAsync(request, stream, file.FileName, userId);

            return HandleResult(result);
        }

        /// <summary>
        /// Upload Tally data as base64 encoded content (alternative to file upload)
        /// </summary>
        [HttpPost("upload/base64")]
        [ProducesResponseType(typeof(TallyUploadResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadBase64([FromBody] TallyUploadRequestDto request)
        {
            if (request.CompanyId == Guid.Empty)
            {
                return BadRequest("Company ID is required");
            }

            if (string.IsNullOrEmpty(request.FileContent))
            {
                return BadRequest("File content is required");
            }

            if (string.IsNullOrEmpty(request.FileName))
            {
                return BadRequest("File name is required");
            }

            var userId = GetCurrentUserId();

            try
            {
                var bytes = Convert.FromBase64String(request.FileContent);
                using var stream = new MemoryStream(bytes);
                var result = await _importService.UploadAndParseAsync(request, stream, request.FileName, userId);
                return HandleResult(result);
            }
            catch (FormatException)
            {
                return BadRequest("Invalid base64 content");
            }
        }

        /// <summary>
        /// Get preview of parsed data for a batch
        /// </summary>
        [HttpGet("{batchId}/preview")]
        [ProducesResponseType(typeof(TallyParsedDataDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPreview(Guid batchId)
        {
            var result = await _importService.GetParsedDataAsync(batchId);
            return HandleResult(result);
        }

        /// <summary>
        /// Configure field mappings for a batch
        /// </summary>
        [HttpPut("{batchId}/mappings")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConfigureMappings(Guid batchId, [FromBody] TallyMappingConfigDto config)
        {
            config.BatchId = batchId;
            var result = await _importService.ConfigureMappingsAsync(config);
            return HandleResult(result);
        }

        /// <summary>
        /// Start the import process
        /// </summary>
        [HttpPost("{batchId}/import")]
        [ProducesResponseType(typeof(TallyImportResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> StartImport(Guid batchId, [FromBody] TallyImportRequestDto? request = null)
        {
            request ??= new TallyImportRequestDto();
            request.BatchId = batchId;

            var userId = GetCurrentUserId();
            var result = await _importService.StartImportAsync(request, userId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get current import progress
        /// </summary>
        [HttpGet("{batchId}/progress")]
        [ProducesResponseType(typeof(TallyImportProgressDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetProgress(Guid batchId)
        {
            var result = await _importService.GetProgressAsync(batchId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get import result/summary
        /// </summary>
        [HttpGet("{batchId}/result")]
        [ProducesResponseType(typeof(TallyImportResultDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetResult(Guid batchId)
        {
            var result = await _importService.GetResultAsync(batchId);
            return HandleResult(result);
        }

        /// <summary>
        /// Preview what will be affected by a rollback
        /// </summary>
        [HttpGet("{batchId}/rollback/preview")]
        [ProducesResponseType(typeof(TallyRollbackPreviewDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PreviewRollback(Guid batchId)
        {
            var result = await _rollbackService.PreviewRollbackAsync(batchId);
            return HandleResult(result);
        }

        /// <summary>
        /// Rollback an import batch
        /// </summary>
        [HttpPost("{batchId}/rollback")]
        [ProducesResponseType(typeof(TallyRollbackResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Rollback(Guid batchId, [FromBody] TallyRollbackRequestDto? request = null)
        {
            request ??= new TallyRollbackRequestDto();
            request.BatchId = batchId;

            var userId = GetCurrentUserId();
            var serviceResult = await _rollbackService.RollbackBatchAsync(batchId, request);
            return HandleResult(serviceResult);
        }

        /// <summary>
        /// Get list of all import batches for a company
        /// </summary>
        [HttpGet("batches")]
        [ProducesResponseType(typeof(PagedResult<TallyBatchListItemDto>), 200)]
        public async Task<IActionResult> GetBatches(
            [FromQuery] Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            if (companyId == Guid.Empty)
            {
                return BadRequest("Company ID is required");
            }

            var result = await _importService.GetBatchesAsync(companyId, pageNumber, pageSize, status);

            if (result.IsFailure)
            {
                return HandleError(result.Error!);
            }

            var (items, totalCount) = result.Value;
            return Ok(new
            {
                items,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Get detailed batch information
        /// </summary>
        [HttpGet("{batchId}")]
        [ProducesResponseType(typeof(TallyBatchDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBatchDetail(Guid batchId)
        {
            var result = await _importService.GetBatchDetailAsync(batchId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get import logs for a batch
        /// </summary>
        [HttpGet("{batchId}/logs")]
        [ProducesResponseType(typeof(PagedResult<TallyImportErrorDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLogs(
            Guid batchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? recordType = null,
            [FromQuery] string? status = null)
        {
            var result = await _importService.GetLogsAsync(batchId, pageNumber, pageSize, recordType, status);

            if (result.IsFailure)
            {
                return HandleError(result.Error!);
            }

            var (items, totalCount) = result.Value;
            return Ok(new
            {
                items,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Validate parsed data without importing
        /// </summary>
        [HttpPost("{batchId}/validate")]
        [ProducesResponseType(typeof(TallyValidationResultDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Validate(Guid batchId, [FromQuery] Guid companyId)
        {
            var parsedDataResult = await _importService.GetParsedDataAsync(batchId);
            if (parsedDataResult.IsFailure)
            {
                return HandleError(parsedDataResult.Error!);
            }

            var result = await _validationService.ValidateAsync(companyId, parsedDataResult.Value!);
            return HandleResult(result);
        }

        /// <summary>
        /// Check for duplicate records
        /// </summary>
        [HttpPost("{batchId}/check-duplicates")]
        [ProducesResponseType(typeof(TallyDuplicateCheckResultDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CheckDuplicates(Guid batchId, [FromQuery] Guid companyId)
        {
            var parsedDataResult = await _importService.GetParsedDataAsync(batchId);
            if (parsedDataResult.IsFailure)
            {
                return HandleError(parsedDataResult.Error!);
            }

            var result = await _validationService.CheckDuplicatesAsync(companyId, parsedDataResult.Value!);
            return HandleResult(result);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return HandleError(result.Error!);
        }

        private IActionResult HandleError(Error error)
        {
            return error.Type switch
            {
                ErrorType.NotFound => NotFound(new { error = error.Message }),
                ErrorType.Validation => BadRequest(new { error = error.Message }),
                ErrorType.Conflict => Conflict(new { error = error.Message }),
                _ => StatusCode(500, new { error = error.Message })
            };
        }
    }

    /// <summary>
    /// Generic paged result for API responses
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
