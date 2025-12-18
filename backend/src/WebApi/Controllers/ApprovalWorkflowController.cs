using Application.DTOs.Approval;
using Application.Interfaces.Approval;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Approval workflow operations endpoints
    /// </summary>
    [ApiController]
    [Route("api/approvals")]
    [Produces("application/json")]
    [Authorize(Policy = "ManagerOrAbove")]
    public class ApprovalWorkflowController : ControllerBase
    {
        private readonly IApprovalWorkflowService _workflowService;

        public ApprovalWorkflowController(IApprovalWorkflowService workflowService)
        {
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        }

        /// <summary>
        /// Get pending approvals for the current user
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>List of pending approvals</returns>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingApprovalDto>), 200)]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] Guid employeeId)
        {
            var result = await _workflowService.GetPendingApprovalsForUserAsync(employeeId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get pending approvals count for a user
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>Count of pending approvals</returns>
        [HttpGet("pending/count")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> GetPendingApprovalsCount([FromQuery] Guid employeeId)
        {
            var result = await _workflowService.GetPendingApprovalsCountAsync(employeeId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get approval request details
        /// </summary>
        /// <param name="requestId">Request ID</param>
        /// <returns>Request details with steps</returns>
        [HttpGet("{requestId}")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRequestDetails(Guid requestId)
        {
            var result = await _workflowService.GetRequestStatusAsync(requestId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get approval status for a specific activity
        /// </summary>
        /// <param name="activityType">Activity type (leave, asset_request, etc.)</param>
        /// <param name="activityId">Activity ID</param>
        /// <returns>Approval status if exists</returns>
        [HttpGet("activity/{activityType}/{activityId}")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        public async Task<IActionResult> GetActivityApprovalStatus(string activityType, Guid activityId)
        {
            var result = await _workflowService.GetActivityApprovalStatusAsync(activityType, activityId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all requests by a requestor
        /// </summary>
        /// <param name="requestorId">Requestor's employee ID</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of approval requests</returns>
        [HttpGet("by-requestor/{requestorId}")]
        [ProducesResponseType(typeof(IEnumerable<ApprovalRequestDto>), 200)]
        public async Task<IActionResult> GetRequestsByRequestor(Guid requestorId, [FromQuery] string? status = null)
        {
            var result = await _workflowService.GetRequestsByRequestorAsync(requestorId, status);
            return HandleResult(result);
        }

        /// <summary>
        /// Approve the current step
        /// </summary>
        /// <param name="requestId">Request ID</param>
        /// <param name="approverId">Approver's employee ID</param>
        /// <param name="dto">Approval details</param>
        /// <returns>Updated request details</returns>
        [HttpPost("{requestId}/approve")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid requestId, [FromQuery] Guid approverId, [FromBody] ApproveRequestDto dto)
        {
            var result = await _workflowService.ApproveAsync(requestId, approverId, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Reject the request
        /// </summary>
        /// <param name="requestId">Request ID</param>
        /// <param name="approverId">Approver's employee ID</param>
        /// <param name="dto">Rejection details</param>
        /// <returns>Updated request details</returns>
        [HttpPost("{requestId}/reject")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid requestId, [FromQuery] Guid approverId, [FromBody] RejectRequestDto dto)
        {
            var result = await _workflowService.RejectAsync(requestId, approverId, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Cancel the request (by requestor)
        /// </summary>
        /// <param name="requestId">Request ID</param>
        /// <param name="requestorId">Requestor's employee ID</param>
        /// <returns>Success</returns>
        [HttpPost("{requestId}/cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid requestId, [FromQuery] Guid requestorId)
        {
            var result = await _workflowService.CancelAsync(requestId, requestorId);
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
