using Application.DTOs.AssetRequest;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing employee asset requests
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "AdminHrOnly")]
    public class AssetRequestsController : ControllerBase
    {
        private readonly IAssetRequestService _service;

        public AssetRequestsController(IAssetRequestService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets an asset request by ID
        /// </summary>
        /// <param name="id">Asset request ID</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Gets all asset requests for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="status">Filter by status (optional)</param>
        [HttpGet("company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<AssetRequestSummaryDto>), 200)]
        public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] string? status = null)
        {
            var result = await _service.GetByCompanyAsync(companyId, status);
            return HandleResult(result);
        }

        /// <summary>
        /// Gets all asset requests by an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="status">Filter by status (optional)</param>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(typeof(IEnumerable<AssetRequestSummaryDto>), 200)]
        public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] string? status = null)
        {
            var result = await _service.GetByEmployeeAsync(employeeId, status);
            return HandleResult(result);
        }

        /// <summary>
        /// Gets pending asset requests for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        [HttpGet("company/{companyId}/pending")]
        [ProducesResponseType(typeof(IEnumerable<AssetRequestSummaryDto>), 200)]
        public async Task<IActionResult> GetPending(Guid companyId)
        {
            var result = await _service.GetPendingForCompanyAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Gets approved but unfulfilled requests for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        [HttpGet("company/{companyId}/unfulfilled")]
        [ProducesResponseType(typeof(IEnumerable<AssetRequestSummaryDto>), 200)]
        public async Task<IActionResult> GetUnfulfilled(Guid companyId)
        {
            var result = await _service.GetApprovedUnfulfilledAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Gets asset request statistics for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        [HttpGet("company/{companyId}/stats")]
        [ProducesResponseType(typeof(AssetRequestStatsDto), 200)]
        public async Task<IActionResult> GetStats(Guid companyId)
        {
            var result = await _service.GetStatsAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Creates a new asset request
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="companyId">Company ID</param>
        /// <param name="dto">Asset request details</param>
        [HttpPost]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromQuery] Guid employeeId, [FromQuery] Guid companyId, [FromBody] CreateAssetRequestDto dto)
        {
            var result = await _service.CreateAsync(employeeId, companyId, dto);
            if (result.IsFailure)
                return HandleResult(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Updates an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="employeeId">Employee ID (for authorization)</param>
        /// <param name="dto">Updated details</param>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromQuery] Guid employeeId, [FromBody] UpdateAssetRequestDto dto)
        {
            var result = await _service.UpdateAsync(employeeId, id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Approves an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="approvedBy">Approver employee ID</param>
        /// <param name="dto">Approval comments</param>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid id, [FromQuery] Guid approvedBy, [FromBody] ApproveAssetRequestDto dto)
        {
            var result = await _service.ApproveAsync(id, approvedBy, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Rejects an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="rejectedBy">Rejector employee ID</param>
        /// <param name="dto">Rejection reason</param>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromQuery] Guid rejectedBy, [FromBody] RejectAssetRequestDto dto)
        {
            var result = await _service.RejectAsync(id, rejectedBy, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Cancels an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="cancelledBy">Employee ID</param>
        /// <param name="dto">Cancellation reason</param>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid id, [FromQuery] Guid cancelledBy, [FromBody] CancelAssetRequestDto dto)
        {
            var result = await _service.CancelAsync(id, cancelledBy, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Fulfills an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="fulfilledBy">Fulfiller employee ID (IT/Admin)</param>
        /// <param name="dto">Fulfillment details</param>
        [HttpPost("{id}/fulfill")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Fulfill(Guid id, [FromQuery] Guid fulfilledBy, [FromBody] FulfillAssetRequestDto dto)
        {
            var result = await _service.FulfillAsync(id, fulfilledBy, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Withdraws an asset request (by employee)
        /// </summary>
        /// <param name="id">Asset request ID</param>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="reason">Withdrawal reason</param>
        [HttpPost("{id}/withdraw")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Withdraw(Guid id, [FromQuery] Guid employeeId, [FromQuery] string? reason = null)
        {
            var result = await _service.WithdrawAsync(employeeId, id, reason);
            return HandleResult(result);
        }

        /// <summary>
        /// Deletes an asset request
        /// </summary>
        /// <param name="id">Asset request ID</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (result.IsFailure)
                return HandleResult(result);

            return NoContent();
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => Forbid(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        private IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok();

            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => Forbid(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }
    }
}
