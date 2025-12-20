using Application.Services.EInvoice;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// E-Invoice API endpoints for IRN generation, cancellation, and management
    /// </summary>
    [ApiController]
    [Route("api/einvoice")]
    [Produces("application/json")]
    public class EInvoiceController : ControllerBase
    {
        private readonly IEInvoiceService _eInvoiceService;
        private readonly IEInvoiceCredentialsRepository _credentialsRepository;
        private readonly IEInvoiceAuditLogRepository _auditLogRepository;
        private readonly IEInvoiceQueueRepository _queueRepository;
        private readonly ILogger<EInvoiceController> _logger;

        public EInvoiceController(
            IEInvoiceService eInvoiceService,
            IEInvoiceCredentialsRepository credentialsRepository,
            IEInvoiceAuditLogRepository auditLogRepository,
            IEInvoiceQueueRepository queueRepository,
            ILogger<EInvoiceController> logger)
        {
            _eInvoiceService = eInvoiceService;
            _credentialsRepository = credentialsRepository;
            _auditLogRepository = auditLogRepository;
            _queueRepository = queueRepository;
            _logger = logger;
        }

        #region IRN Operations

        /// <summary>
        /// Generate IRN for an invoice
        /// </summary>
        [HttpPost("generate/{invoiceId}")]
        [ProducesResponseType(typeof(EInvoiceGenerationResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateIrn(Guid invoiceId)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _eInvoiceService.GenerateIrnAsync(invoiceId, null, ipAddress);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                    Core.Common.ErrorType.Validation => BadRequest(result.Error.Message),
                    Core.Common.ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Cancel IRN for an invoice
        /// </summary>
        [HttpPost("cancel/{invoiceId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelIrn(
            Guid invoiceId,
            [FromBody] CancelIrnRequest request)
        {
            if (string.IsNullOrEmpty(request.Reason))
                return BadRequest("Cancel reason is required");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _eInvoiceService.CancelIrnAsync(
                invoiceId,
                request.ReasonCode ?? IrnCancelReasonCodes.Other,
                request.Reason,
                null,
                ipAddress);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                    Core.Common.ErrorType.Validation => BadRequest(result.Error.Message),
                    Core.Common.ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { success = true, message = "IRN cancelled successfully" });
        }

        /// <summary>
        /// Check if e-invoice is applicable for an invoice
        /// </summary>
        [HttpGet("applicable/{invoiceId}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CheckApplicability(Guid invoiceId)
        {
            var isApplicable = await _eInvoiceService.IsEInvoiceApplicableAsync(invoiceId);
            return Ok(new { invoiceId, isApplicable });
        }

        /// <summary>
        /// Queue invoice for async IRN generation
        /// </summary>
        [HttpPost("queue/{invoiceId}")]
        [ProducesResponseType(typeof(Guid), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> QueueForGeneration(Guid invoiceId, [FromQuery] int priority = 5)
        {
            var result = await _eInvoiceService.QueueForIrnGenerationAsync(invoiceId, priority);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                    Core.Common.ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(new { queueId = result.Value, message = "Invoice queued for IRN generation" });
        }

        #endregion

        #region Credentials Management

        /// <summary>
        /// Get e-invoice credentials for a company
        /// </summary>
        [HttpGet("credentials/company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<EInvoiceCredentialsDto>), 200)]
        public async Task<IActionResult> GetCredentials(Guid companyId)
        {
            var credentials = await _credentialsRepository.GetAllByCompanyIdAsync(companyId);
            var dtos = credentials.Select(c => new EInvoiceCredentialsDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                GspProvider = c.GspProvider,
                Environment = c.Environment,
                ClientId = c.ClientId,
                Username = c.Username,
                AutoGenerateIrn = c.AutoGenerateIrn,
                AutoCancelOnVoid = c.AutoCancelOnVoid,
                GenerateEwayBill = c.GenerateEwayBill,
                EinvoiceThreshold = c.EinvoiceThreshold,
                IsActive = c.IsActive,
                TokenExpiry = c.TokenExpiry,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Create or update e-invoice credentials
        /// </summary>
        [HttpPost("credentials")]
        [ProducesResponseType(typeof(EInvoiceCredentialsDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SaveCredentials([FromBody] SaveCredentialsRequest request)
        {
            if (request.CompanyId == Guid.Empty)
                return BadRequest("CompanyId is required");

            if (string.IsNullOrEmpty(request.GspProvider))
                return BadRequest("GspProvider is required");

            // Check for existing
            var existing = await _credentialsRepository.GetByCompanyIdAsync(
                request.CompanyId, request.Environment ?? "sandbox");

            if (existing != null)
            {
                // Update
                existing.GspProvider = request.GspProvider;
                existing.ClientId = request.ClientId;
                existing.ClientSecret = request.ClientSecret;
                existing.Username = request.Username;
                existing.Password = request.Password;
                existing.AutoGenerateIrn = request.AutoGenerateIrn;
                existing.AutoCancelOnVoid = request.AutoCancelOnVoid;
                existing.GenerateEwayBill = request.GenerateEwayBill;
                existing.EinvoiceThreshold = request.EinvoiceThreshold ?? 50000000m;
                existing.IsActive = request.IsActive;

                await _credentialsRepository.UpdateAsync(existing);
                return Ok(MapToDto(existing));
            }

            // Create new
            var credentials = new EInvoiceCredentials
            {
                CompanyId = request.CompanyId,
                GspProvider = request.GspProvider,
                Environment = request.Environment ?? "sandbox",
                ClientId = request.ClientId,
                ClientSecret = request.ClientSecret,
                Username = request.Username,
                Password = request.Password,
                AutoGenerateIrn = request.AutoGenerateIrn,
                AutoCancelOnVoid = request.AutoCancelOnVoid,
                GenerateEwayBill = request.GenerateEwayBill,
                EinvoiceThreshold = request.EinvoiceThreshold ?? 50000000m,
                IsActive = request.IsActive
            };

            await _credentialsRepository.AddAsync(credentials);
            return Ok(MapToDto(credentials));
        }

        /// <summary>
        /// Delete e-invoice credentials
        /// </summary>
        [HttpDelete("credentials/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCredentials(Guid id)
        {
            var existing = await _credentialsRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound("Credentials not found");

            await _credentialsRepository.DeleteAsync(id);
            return Ok(new { message = "Credentials deleted" });
        }

        #endregion

        #region Audit Log

        /// <summary>
        /// Get audit log for a company
        /// </summary>
        [HttpGet("audit/company/{companyId}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetAuditLog(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
                companyId, pageNumber, pageSize, actionType, fromDate, toDate);

            return Ok(new
            {
                items = items.Select(MapAuditLogToDto),
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        /// <summary>
        /// Get audit log for an invoice
        /// </summary>
        [HttpGet("audit/invoice/{invoiceId}")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetInvoiceAuditLog(Guid invoiceId)
        {
            var logs = await _auditLogRepository.GetByInvoiceIdAsync(invoiceId);
            return Ok(logs.Select(MapAuditLogToDto));
        }

        /// <summary>
        /// Get recent errors for a company
        /// </summary>
        [HttpGet("audit/errors/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetErrors(Guid companyId, [FromQuery] int limit = 50)
        {
            var errors = await _auditLogRepository.GetErrorsAsync(companyId, limit);
            return Ok(errors.Select(MapAuditLogToDto));
        }

        #endregion

        #region Queue Management

        /// <summary>
        /// Get queue status for a company
        /// </summary>
        [HttpGet("queue/company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetQueueStatus(Guid companyId, [FromQuery] string? status = null)
        {
            var items = await _queueRepository.GetByCompanyIdAsync(companyId, status);
            return Ok(items.Select(MapQueueToDto));
        }

        /// <summary>
        /// Get queue item by invoice
        /// </summary>
        [HttpGet("queue/invoice/{invoiceId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetQueueByInvoice(Guid invoiceId)
        {
            var item = await _queueRepository.GetByInvoiceIdAsync(invoiceId);
            if (item == null)
                return NotFound("No queue item found for this invoice");

            return Ok(MapQueueToDto(item));
        }

        /// <summary>
        /// Cancel queued item
        /// </summary>
        [HttpPost("queue/{id}/cancel")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CancelQueueItem(Guid id)
        {
            await _queueRepository.UpdateStatusAsync(id, EInvoiceQueueStatus.Cancelled);
            return Ok(new { message = "Queue item cancelled" });
        }

        #endregion

        #region Helpers

        private static EInvoiceCredentialsDto MapToDto(EInvoiceCredentials c) => new()
        {
            Id = c.Id,
            CompanyId = c.CompanyId,
            GspProvider = c.GspProvider,
            Environment = c.Environment,
            ClientId = c.ClientId,
            Username = c.Username,
            AutoGenerateIrn = c.AutoGenerateIrn,
            AutoCancelOnVoid = c.AutoCancelOnVoid,
            GenerateEwayBill = c.GenerateEwayBill,
            EinvoiceThreshold = c.EinvoiceThreshold,
            IsActive = c.IsActive,
            TokenExpiry = c.TokenExpiry,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        private static object MapAuditLogToDto(EInvoiceAuditLog log) => new
        {
            log.Id,
            log.CompanyId,
            log.InvoiceId,
            log.ActionType,
            log.RequestTimestamp,
            log.ResponseStatus,
            log.Irn,
            log.AckNumber,
            log.AckDate,
            log.ErrorCode,
            log.ErrorMessage,
            log.GspProvider,
            log.Environment,
            log.ResponseTimeMs,
            log.CreatedAt
        };

        private static object MapQueueToDto(EInvoiceQueue item) => new
        {
            item.Id,
            item.CompanyId,
            item.InvoiceId,
            item.ActionType,
            item.Priority,
            item.Status,
            item.RetryCount,
            item.MaxRetries,
            item.NextRetryAt,
            item.StartedAt,
            item.CompletedAt,
            item.ErrorCode,
            item.ErrorMessage,
            item.CreatedAt,
            item.UpdatedAt
        };

        #endregion
    }

    #region DTOs

    public class CancelIrnRequest
    {
        public string? ReasonCode { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class SaveCredentialsRequest
    {
        public Guid CompanyId { get; set; }
        public string GspProvider { get; set; } = "cleartax";
        public string? Environment { get; set; } = "sandbox";
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool AutoGenerateIrn { get; set; }
        public bool AutoCancelOnVoid { get; set; }
        public bool GenerateEwayBill { get; set; }
        public decimal? EinvoiceThreshold { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class EInvoiceCredentialsDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string GspProvider { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string? ClientId { get; set; }
        public string? Username { get; set; }
        public bool AutoGenerateIrn { get; set; }
        public bool AutoCancelOnVoid { get; set; }
        public bool GenerateEwayBill { get; set; }
        public decimal EinvoiceThreshold { get; set; }
        public bool IsActive { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    #endregion
}
