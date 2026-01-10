using System.Security.Claims;
using Application.DTOs.Tax;
using Application.Interfaces.Tax;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// Advance Tax (Section 207) management for corporates
    /// </summary>
    [ApiController]
    [Route("api/tax/advance-tax")]
    [Produces("application/json")]
    [Authorize]
    public class AdvanceTaxController : ControllerBase
    {
        private readonly IAdvanceTaxService _service;

        public AdvanceTaxController(IAdvanceTaxService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
            }
        }

        // ==================== Assessment Endpoints ====================

        /// <summary>
        /// Compute and create advance tax assessment
        /// </summary>
        [HttpPost("compute")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> ComputeAssessment([FromBody] CreateAdvanceTaxAssessmentDto request)
        {
            var result = await _service.ComputeAssessmentAsync(request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get assessment by ID
        /// </summary>
        [HttpGet("assessment/{id:guid}")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAssessmentById(Guid id)
        {
            var result = await _service.GetAssessmentByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get assessment for company and financial year
        /// </summary>
        [HttpGet("{companyId:guid}/{financialYear}")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAssessment(Guid companyId, string financialYear)
        {
            var result = await _service.GetAssessmentAsync(companyId, financialYear);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all assessments for a company
        /// </summary>
        [HttpGet("company/{companyId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxAssessmentDto>), 200)]
        public async Task<IActionResult> GetAssessmentsByCompany(Guid companyId)
        {
            var result = await _service.GetAssessmentsByCompanyAsync(companyId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Update assessment projections
        /// </summary>
        [HttpPut("assessment/{id:guid}")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAdvanceTaxAssessmentDto request)
        {
            var result = await _service.UpdateAssessmentAsync(id, request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Activate assessment (move from draft to active)
        /// </summary>
        [HttpPost("assessment/{id:guid}/activate")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ActivateAssessment(Guid id)
        {
            var result = await _service.ActivateAssessmentAsync(id, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Finalize assessment (after FY end)
        /// </summary>
        [HttpPost("assessment/{id:guid}/finalize")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FinalizeAssessment(Guid id)
        {
            var result = await _service.FinalizeAssessmentAsync(id, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Delete assessment (draft only)
        /// </summary>
        [HttpDelete("assessment/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var result = await _service.DeleteAssessmentAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Refresh YTD actuals from ledger
        /// </summary>
        [HttpPost("assessment/refresh-ytd")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RefreshYtd([FromBody] RefreshYtdRequest request)
        {
            var result = await _service.RefreshYtdAsync(request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Preview YTD financials with trend-based projections
        /// </summary>
        [HttpGet("ytd-preview/{companyId:guid}/{financialYear}")]
        [ProducesResponseType(typeof(YtdFinancialsDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetYtdFinancialsPreview(Guid companyId, string financialYear)
        {
            var result = await _service.GetYtdFinancialsPreviewAsync(companyId, financialYear);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Refresh TDS receivable and TCS credit from modules
        /// </summary>
        [HttpPost("assessment/{id:guid}/refresh-tds-tcs")]
        [ProducesResponseType(typeof(AdvanceTaxAssessmentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RefreshTdsTcs(Guid id)
        {
            var result = await _service.RefreshTdsTcsAsync(id, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Preview TDS/TCS values from modules (without saving)
        /// </summary>
        [HttpGet("tds-tcs-preview/{companyId:guid}/{financialYear}")]
        [ProducesResponseType(typeof(TdsTcsPreviewDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTdsTcsPreview(Guid companyId, string financialYear)
        {
            var result = await _service.GetTdsTcsPreviewAsync(companyId, financialYear);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Schedule Endpoints ====================

        /// <summary>
        /// Get payment schedule for an assessment
        /// </summary>
        [HttpGet("schedule/{assessmentId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxScheduleDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaymentSchedule(Guid assessmentId)
        {
            var result = await _service.GetPaymentScheduleAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Recalculate schedules after changes
        /// </summary>
        [HttpPost("schedule/{assessmentId:guid}/recalculate")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxScheduleDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecalculateSchedules(Guid assessmentId)
        {
            var result = await _service.RecalculateSchedulesAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Payment Endpoints ====================

        /// <summary>
        /// Record advance tax payment
        /// </summary>
        [HttpPost("payment")]
        [ProducesResponseType(typeof(AdvanceTaxPaymentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecordPayment([FromBody] RecordAdvanceTaxPaymentDto request)
        {
            var result = await _service.RecordPaymentAsync(request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get payments for an assessment
        /// </summary>
        [HttpGet("payments/{assessmentId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxPaymentDto>), 200)]
        public async Task<IActionResult> GetPayments(Guid assessmentId)
        {
            var result = await _service.GetPaymentsAsync(assessmentId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Delete a payment
        /// </summary>
        [HttpDelete("payment/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var result = await _service.DeletePaymentAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return NoContent();
        }

        // ==================== Interest Endpoints ====================

        /// <summary>
        /// Get interest liability breakdown
        /// </summary>
        [HttpGet("interest/{assessmentId:guid}")]
        [ProducesResponseType(typeof(InterestCalculationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetInterestBreakdown(Guid assessmentId)
        {
            var result = await _service.GetInterestBreakdownAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Scenario Endpoints ====================

        /// <summary>
        /// Run a what-if scenario
        /// </summary>
        [HttpPost("scenario")]
        [ProducesResponseType(typeof(AdvanceTaxScenarioDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RunScenario([FromBody] RunScenarioDto request)
        {
            var result = await _service.RunScenarioAsync(request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get scenarios for an assessment
        /// </summary>
        [HttpGet("scenarios/{assessmentId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxScenarioDto>), 200)]
        public async Task<IActionResult> GetScenarios(Guid assessmentId)
        {
            var result = await _service.GetScenariosAsync(assessmentId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Delete a scenario
        /// </summary>
        [HttpDelete("scenario/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteScenario(Guid id)
        {
            var result = await _service.DeleteScenarioAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return NoContent();
        }

        // ==================== Dashboard Endpoints ====================

        /// <summary>
        /// Get advance tax tracker/dashboard
        /// </summary>
        [HttpGet("tracker/{companyId:guid}/{financialYear}")]
        [ProducesResponseType(typeof(AdvanceTaxTrackerDto), 200)]
        public async Task<IActionResult> GetTracker(Guid companyId, string financialYear)
        {
            var result = await _service.GetTrackerAsync(companyId, financialYear);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get tax computation breakdown
        /// </summary>
        [HttpGet("computation/{assessmentId:guid}")]
        [ProducesResponseType(typeof(TaxComputationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTaxComputation(Guid assessmentId)
        {
            var result = await _service.GetTaxComputationAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get assessments with pending payments
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxAssessmentDto>), 200)]
        public async Task<IActionResult> GetPendingPayments([FromQuery] Guid? companyId = null)
        {
            var result = await _service.GetPendingPaymentsAsync(companyId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Revision Endpoints ====================

        /// <summary>
        /// Create a revision with updated projections
        /// </summary>
        [HttpPost("revision")]
        [ProducesResponseType(typeof(AdvanceTaxRevisionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreateRevision([FromBody] CreateRevisionDto request)
        {
            var result = await _service.CreateRevisionAsync(request, CurrentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all revisions for an assessment
        /// </summary>
        [HttpGet("revisions/{assessmentId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AdvanceTaxRevisionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRevisions(Guid assessmentId)
        {
            var result = await _service.GetRevisionsAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get revision status (for dashboard prompt)
        /// </summary>
        [HttpGet("revision-status/{assessmentId:guid}")]
        [ProducesResponseType(typeof(RevisionStatusDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRevisionStatus(Guid assessmentId)
        {
            var result = await _service.GetRevisionStatusAsync(assessmentId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }
    }
}
