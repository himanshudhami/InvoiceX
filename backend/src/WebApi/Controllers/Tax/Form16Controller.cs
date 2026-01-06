using Core.Common;
using Core.Interfaces.Tax;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// Form 16 (TDS Certificate) management endpoints.
    /// Form 16 is the annual TDS certificate issued by employers to employees under Section 192.
    ///
    /// Part A: Summary of TDS deducted and deposited (quarterly basis)
    /// Part B: Detailed salary computation and tax calculation
    ///
    /// Legal Requirement: Must be issued by June 15 following the financial year end.
    /// </summary>
    [ApiController]
    [Route("api/tax/[controller]")]
    [Produces("application/json")]
    public class Form16Controller : ControllerBase
    {
        private readonly IForm16Service _form16Service;

        public Form16Controller(IForm16Service form16Service)
        {
            _form16Service = form16Service ?? throw new ArgumentNullException(nameof(form16Service));
        }

        // ==================== Generation Operations ====================

        /// <summary>
        /// Generate Form 16 for a single employee.
        /// Aggregates salary and TDS data from payroll transactions.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="generatedBy">User ID generating the form (optional)</param>
        [HttpPost("{companyId}/generate/{employeeId}/{financialYear}")]
        [ProducesResponseType(typeof(Form16GenerationResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateForEmployee(
            Guid companyId,
            Guid employeeId,
            string financialYear,
            [FromQuery] Guid? generatedBy = null)
        {
            var result = await _form16Service.GenerateForEmployeeAsync(
                companyId, employeeId, financialYear, generatedBy);

            return HandleResult(result);
        }

        /// <summary>
        /// Bulk generate Form 16 for all eligible employees in a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="generatedBy">User ID generating the forms (optional)</param>
        /// <param name="regenerateExisting">If true, regenerates existing Form 16s</param>
        [HttpPost("{companyId}/generate-bulk/{financialYear}")]
        [ProducesResponseType(typeof(BulkForm16GenerationResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GenerateBulk(
            Guid companyId,
            string financialYear,
            [FromQuery] Guid? generatedBy = null,
            [FromQuery] bool regenerateExisting = false)
        {
            var result = await _form16Service.GenerateBulkAsync(
                companyId, financialYear, generatedBy, regenerateExisting);

            return HandleResult(result);
        }

        /// <summary>
        /// Preview Form 16 data without saving.
        /// Useful for verification before generation.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/preview/{employeeId}/{financialYear}")]
        [ProducesResponseType(typeof(Form16PreviewData), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Preview(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var result = await _form16Service.PreviewAsync(companyId, employeeId, financialYear);
            return HandleResult(result);
        }

        // ==================== Retrieval Operations ====================

        /// <summary>
        /// Get Form 16 by ID with full details.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Form16Dto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _form16Service.GetByIdAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get Form 16 for a specific employee and financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/employee/{employeeId}/{financialYear}")]
        [ProducesResponseType(typeof(Form16Dto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByEmployeeAndFy(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var result = await _form16Service.GetByEmployeeAndFyAsync(
                companyId, employeeId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get paged list of Form 16s for a company.
        /// Supports filtering by financial year, status, and search term.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="financialYear">Filter by financial year (optional)</param>
        /// <param name="status">Filter by status: draft, generated, verified, issued, cancelled (optional)</param>
        /// <param name="searchTerm">Search by employee name, PAN, or certificate number (optional)</param>
        /// <param name="sortBy">Sort by field (optional)</param>
        /// <param name="sortDescending">Sort descending (default: false)</param>
        [HttpGet("{companyId}/list")]
        [ProducesResponseType(typeof(PagedResult<Form16SummaryDto>), 200)]
        public async Task<IActionResult> GetPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            var result = await _form16Service.GetPagedAsync(
                companyId, pageNumber, pageSize,
                financialYear, status, searchTerm,
                sortBy, sortDescending);

            return HandleResult(result);
        }

        /// <summary>
        /// Get Form 16 statistics for a financial year.
        /// Shows counts by status, total TDS, and breakdown by tax regime.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/statistics/{financialYear}")]
        [ProducesResponseType(typeof(Form16StatisticsDto), 200)]
        public async Task<IActionResult> GetStatistics(
            Guid companyId,
            string financialYear)
        {
            var result = await _form16Service.GetStatisticsAsync(companyId, financialYear);
            return HandleResult(result);
        }

        // ==================== PDF Operations ====================

        /// <summary>
        /// Generate PDF for a Form 16.
        /// Creates the official Form 16 document with Part A and Part B.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        /// <param name="generatedBy">User ID generating the PDF (optional)</param>
        [HttpPost("{id}/generate-pdf")]
        [ProducesResponseType(typeof(Form16PdfResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GeneratePdf(
            Guid id,
            [FromQuery] Guid? generatedBy = null)
        {
            var result = await _form16Service.GeneratePdfAsync(id, generatedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Bulk generate PDFs for all Form 16s in a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="generatedBy">User ID generating the PDFs (optional)</param>
        [HttpPost("{companyId}/generate-pdf-bulk/{financialYear}")]
        [ProducesResponseType(typeof(BulkPdfGenerationResult), 200)]
        public async Task<IActionResult> GenerateBulkPdf(
            Guid companyId,
            string financialYear,
            [FromQuery] Guid? generatedBy = null)
        {
            var result = await _form16Service.GenerateBulkPdfAsync(
                companyId, financialYear, generatedBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Download PDF for a Form 16.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var result = await _form16Service.DownloadPdfAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return File(result.Value!, "application/pdf", $"Form16_{id}.pdf");
        }

        // ==================== Workflow Operations ====================

        /// <summary>
        /// Verify Form 16 (HR/Finance approval).
        /// Moves status from 'generated' to 'verified'.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        /// <param name="request">Verification details</param>
        [HttpPost("{id}/verify")]
        [ProducesResponseType(typeof(Form16Dto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyForm16Request request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _form16Service.VerifyAsync(id, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Mark Form 16 as issued (sent to employee).
        /// Moves status from 'verified' to 'issued'.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        /// <param name="request">Issue details</param>
        [HttpPost("{id}/issue")]
        [ProducesResponseType(typeof(Form16Dto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Issue(Guid id, [FromBody] IssueForm16Request request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _form16Service.IssueAsync(id, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Cancel Form 16.
        /// Only allowed for forms that haven't been issued.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        /// <param name="request">Cancellation reason</param>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelForm16Request request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _form16Service.CancelAsync(id, request.Reason, request.CancelledBy);

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

        /// <summary>
        /// Regenerate Form 16 (recalculate from payroll data).
        /// Updates the existing Form 16 with fresh calculations.
        /// </summary>
        /// <param name="id">Form 16 ID</param>
        /// <param name="regeneratedBy">User ID regenerating the form (optional)</param>
        [HttpPost("{id}/regenerate")]
        [ProducesResponseType(typeof(Form16GenerationResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Regenerate(
            Guid id,
            [FromQuery] Guid? regeneratedBy = null)
        {
            var result = await _form16Service.RegenerateAsync(id, regeneratedBy);
            return HandleResult(result);
        }

        // ==================== Validation ====================

        /// <summary>
        /// Validate Form 16 data before generation.
        /// Checks for required data availability.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/validate/{employeeId}/{financialYear}")]
        [ProducesResponseType(typeof(Form16ValidationResult), 200)]
        public async Task<IActionResult> Validate(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var result = await _form16Service.ValidateAsync(
                companyId, employeeId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Check if Form 16 can be generated for an employee.
        /// Quick check without full validation.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/can-generate/{employeeId}/{financialYear}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CanGenerate(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var result = await _form16Service.CanGenerateAsync(
                companyId, employeeId, financialYear);
            return HandleResult(result);
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
    /// Request to cancel a Form 16
    /// </summary>
    public class CancelForm16Request
    {
        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// User ID of the person cancelling
        /// </summary>
        public Guid CancelledBy { get; set; }
    }
}
