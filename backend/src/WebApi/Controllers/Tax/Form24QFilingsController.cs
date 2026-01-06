using Core.Common;
using Core.Interfaces.Tax;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// Form 24Q quarterly TDS filing management endpoints.
    /// Form 24Q is the quarterly TDS return for salary payments (Section 192).
    ///
    /// Annexure I: Challan/Transfer voucher details (all quarters)
    /// Annexure II: Employee-wise annual salary details (Q4 only)
    ///
    /// Filing Due Dates:
    /// - Q1 (Apr-Jun): July 31
    /// - Q2 (Jul-Sep): October 31
    /// - Q3 (Oct-Dec): January 31
    /// - Q4 (Jan-Mar): May 31
    /// </summary>
    [ApiController]
    [Route("api/tax/form-24q-filings")]
    [Produces("application/json")]
    public class Form24QFilingsController : ControllerBase
    {
        private readonly IForm24QFilingService _filingService;

        public Form24QFilingsController(IForm24QFilingService filingService)
        {
            _filingService = filingService ?? throw new ArgumentNullException(nameof(filingService));
        }

        // ==================== Retrieval Operations ====================

        /// <summary>
        /// Get Form 24Q filing by ID.
        /// </summary>
        /// <param name="id">Filing ID</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _filingService.GetByIdAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get Form 24Q filing for a specific company/FY/quarter.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        [HttpGet("company/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByCompanyQuarter(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            var result = await _filingService.GetByCompanyQuarterAsync(companyId, financialYear, quarter);
            return HandleResult(result);
        }

        /// <summary>
        /// Get paginated list of Form 24Q filings for a company.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="financialYear">Filter by financial year (optional)</param>
        /// <param name="quarter">Filter by quarter (optional)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="sortBy">Sort column (optional)</param>
        /// <param name="sortDescending">Sort direction (default: false)</param>
        [HttpGet("company/{companyId}")]
        [ProducesResponseType(typeof(PagedResult<Form24QFilingSummaryDto>), 200)]
        public async Task<IActionResult> GetByCompany(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? quarter = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            var result = await _filingService.GetPagedAsync(
                companyId, pageNumber, pageSize, financialYear, quarter, status, sortBy, sortDescending);
            return HandleResult(result);
        }

        /// <summary>
        /// Get Form 24Q filing statistics for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("company/{companyId}/statistics/{financialYear}")]
        [ProducesResponseType(typeof(Form24QFilingStatisticsDto), 200)]
        public async Task<IActionResult> GetStatistics(Guid companyId, string financialYear)
        {
            var result = await _filingService.GetStatisticsAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all filings for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("company/{companyId}/year/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<Form24QFilingSummaryDto>), 200)]
        public async Task<IActionResult> GetByFinancialYear(Guid companyId, string financialYear)
        {
            var result = await _filingService.GetByFinancialYearAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get pending (not acknowledged) filings for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("company/{companyId}/pending/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<Form24QFilingSummaryDto>), 200)]
        public async Task<IActionResult> GetPendingFilings(Guid companyId, string financialYear)
        {
            var result = await _filingService.GetPendingFilingsAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get overdue filings (past due date, not acknowledged).
        /// </summary>
        /// <param name="companyId">Company ID</param>
        [HttpGet("company/{companyId}/overdue")]
        [ProducesResponseType(typeof(IEnumerable<Form24QFilingSummaryDto>), 200)]
        public async Task<IActionResult> GetOverdueFilings(Guid companyId)
        {
            var result = await _filingService.GetOverdueFilingsAsync(companyId);
            return HandleResult(result);
        }

        // ==================== Draft Operations ====================

        /// <summary>
        /// Create a draft Form 24Q filing for a quarter.
        /// Generates data from payroll transactions.
        /// </summary>
        /// <param name="request">Create filing request</param>
        [HttpPost]
        [ProducesResponseType(typeof(Form24QFilingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateDraft([FromBody] CreateForm24QFilingRequest request)
        {
            var result = await _filingService.CreateDraftAsync(
                request.CompanyId, request.FinancialYear, request.Quarter, request.CreatedBy);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Refresh filing data from current payroll transactions.
        /// Only allowed for draft status.
        /// </summary>
        /// <param name="id">Filing ID</param>
        /// <param name="updatedBy">User ID (optional)</param>
        [HttpPost("{id}/refresh")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RefreshData(Guid id, [FromQuery] Guid? updatedBy = null)
        {
            var result = await _filingService.RefreshDataAsync(id, updatedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Preview Form 24Q data without saving.
        /// Useful for verification before creating a filing.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        [HttpGet("company/{companyId}/preview/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(Form24QPreviewData), 200)]
        public async Task<IActionResult> Preview(Guid companyId, string financialYear, string quarter)
        {
            var result = await _filingService.PreviewAsync(companyId, financialYear, quarter);
            return HandleResult(result);
        }

        // ==================== Validation Operations ====================

        /// <summary>
        /// Validate Form 24Q filing data.
        /// Checks for compliance with NSDL requirements.
        /// </summary>
        /// <param name="id">Filing ID</param>
        [HttpPost("{id}/validate")]
        [ProducesResponseType(typeof(Form24QValidationResult), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Validate(Guid id)
        {
            var result = await _filingService.ValidateFilingAsync(id);
            return HandleResult(result);
        }

        // ==================== FVU Operations ====================

        /// <summary>
        /// Generate FVU file for NSDL upload.
        /// Filing must be validated first.
        /// </summary>
        /// <param name="id">Filing ID</param>
        /// <param name="generatedBy">User ID (optional)</param>
        [HttpPost("{id}/generate-fvu")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateFvu(Guid id, [FromQuery] Guid? generatedBy = null)
        {
            var result = await _filingService.GenerateFvuAsync(id, generatedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Download the generated FVU file.
        /// </summary>
        /// <param name="id">Filing ID</param>
        [HttpGet("{id}/download-fvu")]
        [Produces("text/plain")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadFvu(Guid id)
        {
            var result = await _filingService.DownloadFvuAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return File(result.Value!.FileStream, result.Value.ContentType, result.Value.FileName);
        }

        // ==================== Workflow Operations ====================

        /// <summary>
        /// Mark filing as submitted to NSDL.
        /// </summary>
        /// <param name="id">Filing ID</param>
        /// <param name="request">Submit request</param>
        [HttpPost("{id}/submit")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsSubmitted(Guid id, [FromBody] SubmitFilingRequest request)
        {
            var result = await _filingService.MarkAsSubmittedAsync(id, request.FilingDate, request.SubmittedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Record acknowledgement from NSDL.
        /// </summary>
        /// <param name="id">Filing ID</param>
        /// <param name="request">Acknowledgement request</param>
        [HttpPost("{id}/acknowledge")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecordAcknowledgement(
            Guid id,
            [FromBody] RecordAcknowledgementRequest request)
        {
            var result = await _filingService.RecordAcknowledgementAsync(
                id, request.AcknowledgementNumber, request.TokenNumber, request.FilingDate, request.UpdatedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Mark filing as rejected by NSDL.
        /// </summary>
        /// <param name="id">Filing ID</param>
        /// <param name="request">Rejection request</param>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(Form24QFilingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsRejected(Guid id, [FromBody] RejectFilingRequest request)
        {
            var result = await _filingService.MarkAsRejectedAsync(id, request.RejectionReason, request.UpdatedBy);
            return HandleResult(result);
        }

        // ==================== Correction Returns ====================

        /// <summary>
        /// Create a correction return based on an original filing.
        /// Original filing will be marked as 'revised'.
        /// </summary>
        /// <param name="id">Original filing ID</param>
        /// <param name="createdBy">User ID (optional)</param>
        [HttpPost("{id}/create-correction")]
        [ProducesResponseType(typeof(Form24QFilingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreateCorrection(Guid id, [FromQuery] Guid? createdBy = null)
        {
            var result = await _filingService.CreateCorrectionReturnAsync(id, createdBy);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Get correction returns for an original filing.
        /// </summary>
        /// <param name="id">Original filing ID</param>
        [HttpGet("{id}/corrections")]
        [ProducesResponseType(typeof(IEnumerable<Form24QFilingSummaryDto>), 200)]
        public async Task<IActionResult> GetCorrections(Guid id)
        {
            var result = await _filingService.GetCorrectionsAsync(id);
            return HandleResult(result);
        }

        // ==================== Delete Operations ====================

        /// <summary>
        /// Delete a draft filing. Only draft status filings can be deleted.
        /// </summary>
        /// <param name="id">Filing ID</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDraft(Guid id)
        {
            var result = await _filingService.DeleteDraftAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return NoContent();
        }

        // ==================== Helper Methods ====================

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }
    }

    // ==================== Request DTOs ====================

    /// <summary>
    /// Request to create a Form 24Q filing
    /// </summary>
    public class CreateForm24QFilingRequest
    {
        /// <summary>
        /// Company ID
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Financial year (e.g., "2024-25")
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter (Q1, Q2, Q3, Q4)
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// User ID creating the filing (optional)
        /// </summary>
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request to submit a filing
    /// </summary>
    public class SubmitFilingRequest
    {
        /// <summary>
        /// Filing date (optional, defaults to today)
        /// </summary>
        public DateOnly? FilingDate { get; set; }

        /// <summary>
        /// User ID submitting the filing (optional)
        /// </summary>
        public Guid? SubmittedBy { get; set; }
    }

    /// <summary>
    /// Request to record acknowledgement
    /// </summary>
    public class RecordAcknowledgementRequest
    {
        /// <summary>
        /// Acknowledgement number from NSDL
        /// </summary>
        public string AcknowledgementNumber { get; set; } = string.Empty;

        /// <summary>
        /// Token number from NSDL (optional)
        /// </summary>
        public string? TokenNumber { get; set; }

        /// <summary>
        /// Filing date (optional)
        /// </summary>
        public DateOnly? FilingDate { get; set; }

        /// <summary>
        /// User ID recording the acknowledgement (optional)
        /// </summary>
        public Guid? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Request to reject a filing
    /// </summary>
    public class RejectFilingRequest
    {
        /// <summary>
        /// Rejection reason from NSDL
        /// </summary>
        public string RejectionReason { get; set; } = string.Empty;

        /// <summary>
        /// User ID recording the rejection (optional)
        /// </summary>
        public Guid? UpdatedBy { get; set; }
    }
}
