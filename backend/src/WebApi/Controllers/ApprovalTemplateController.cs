using Application.DTOs.Approval;
using Application.Interfaces.Approval;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Approval workflow template management endpoints (Admin)
    /// </summary>
    [ApiController]
    [Route("api/approval-templates")]
    [Produces("application/json")]
    [Authorize]
    public class ApprovalTemplateController : ControllerBase
    {
        private readonly IApprovalTemplateService _templateService;

        public ApprovalTemplateController(IApprovalTemplateService templateService)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        }

        #region Template Endpoints

        /// <summary>
        /// Get all templates for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="activityType">Optional activity type filter</param>
        /// <returns>List of templates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ApprovalWorkflowTemplateDto>), 200)]
        public async Task<IActionResult> GetTemplates([FromQuery] Guid companyId, [FromQuery] string? activityType = null)
        {
            Result<IEnumerable<ApprovalWorkflowTemplateDto>> result;

            if (!string.IsNullOrEmpty(activityType))
            {
                result = await _templateService.GetByCompanyAndActivityTypeAsync(companyId, activityType);
            }
            else
            {
                result = await _templateService.GetByCompanyAsync(companyId);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Get template by ID with all steps
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Template with steps</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApprovalWorkflowTemplateDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _templateService.GetByIdAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new approval template
        /// </summary>
        /// <param name="dto">Template data</param>
        /// <returns>Created template</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApprovalWorkflowTemplateDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateApprovalTemplateDto dto)
        {
            var result = await _templateService.CreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="dto">Updated template data</param>
        /// <returns>Updated template</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApprovalWorkflowTemplateDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApprovalTemplateDto dto)
        {
            var result = await _templateService.UpdateAsync(id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete a template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Success</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _templateService.DeleteAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Set a template as the default for its activity type
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Success</returns>
        [HttpPost("{id}/set-default")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetAsDefault(Guid id)
        {
            var result = await _templateService.SetAsDefaultAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok();
        }

        #endregion

        #region Step Endpoints

        /// <summary>
        /// Add a step to a template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="dto">Step data</param>
        /// <returns>Created step</returns>
        [HttpPost("{templateId}/steps")]
        [ProducesResponseType(typeof(ApprovalWorkflowStepDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddStep(Guid templateId, [FromBody] CreateApprovalStepDto dto)
        {
            var result = await _templateService.AddStepAsync(templateId, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Created($"/api/approval-templates/{templateId}/steps/{result.Value!.Id}", result.Value);
        }

        /// <summary>
        /// Update a step
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="stepId">Step ID</param>
        /// <param name="dto">Updated step data</param>
        /// <returns>Updated step</returns>
        [HttpPut("{templateId}/steps/{stepId}")]
        [ProducesResponseType(typeof(ApprovalWorkflowStepDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStep(Guid templateId, Guid stepId, [FromBody] UpdateApprovalStepDto dto)
        {
            var result = await _templateService.UpdateStepAsync(stepId, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete a step
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="stepId">Step ID</param>
        /// <returns>Success</returns>
        [HttpDelete("{templateId}/steps/{stepId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteStep(Guid templateId, Guid stepId)
        {
            var result = await _templateService.DeleteStepAsync(stepId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Reorder steps in a template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="dto">Ordered step IDs</param>
        /// <returns>Success</returns>
        [HttpPost("{templateId}/reorder-steps")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReorderSteps(Guid templateId, [FromBody] ReorderStepsDto dto)
        {
            var result = await _templateService.ReorderStepsAsync(templateId, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok();
        }

        #endregion

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }
    }
}
