using Application.DTOs.AssetRequest;
using Application.Interfaces;
using Core.Common;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal
{
    /// <summary>
    /// Employee Portal Asset Request API endpoints.
    /// All endpoints are scoped to the authenticated employee.
    /// </summary>
    [ApiController]
    [Route("api/portal/asset-requests")]
    [Produces("application/json")]
    [Authorize]
    public class AssetRequestPortalController : ControllerBase
    {
        private readonly IAssetRequestService _assetRequestService;

        public AssetRequestPortalController(IAssetRequestService assetRequestService)
        {
            _assetRequestService = assetRequestService ?? throw new ArgumentNullException(nameof(assetRequestService));
        }

        private Guid? CurrentEmployeeId
        {
            get
            {
                var claim = User.FindFirst("employee_id");
                if (claim != null && Guid.TryParse(claim.Value, out var employeeId))
                    return employeeId;
                return null;
            }
        }

        private Guid? CurrentCompanyId
        {
            get
            {
                var claim = User.FindFirst("company_id");
                if (claim != null && Guid.TryParse(claim.Value, out var companyId))
                    return companyId;
                return null;
            }
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                _ => StatusCode(500, new { error = result.Error.Message })
            };
        }

        private IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(new { success = true });

            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                _ => StatusCode(500, new { error = result.Error.Message })
            };
        }

        // ==================== Asset Requests ====================

        /// <summary>
        /// Get employee's asset requests
        /// </summary>
        /// <param name="status">Optional filter by status (pending, approved, rejected, fulfilled, cancelled)</param>
        /// <returns>List of asset requests</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AssetRequestSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyRequests([FromQuery] string? status = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _assetRequestService.GetByEmployeeAsync(CurrentEmployeeId.Value, status);
            return HandleResult(result);
        }

        /// <summary>
        /// Get asset request detail by ID
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <returns>Asset request detail</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRequest(Guid id)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _assetRequestService.GetByIdAsync(id);
            if (result.IsSuccess && result.Value!.EmployeeId != CurrentEmployeeId)
                return StatusCode(403, new { error = "Access denied to this asset request" });

            return HandleResult(result);
        }

        /// <summary>
        /// Submit a new asset request
        /// </summary>
        /// <param name="dto">Asset request data</param>
        /// <returns>Created asset request</returns>
        [HttpPost]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SubmitRequest([FromBody] CreateAssetRequestDto dto)
        {
            if (CurrentEmployeeId == null || CurrentCompanyId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _assetRequestService.CreateAsync(CurrentEmployeeId.Value, CurrentCompanyId.Value, dto);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetRequest), new { id = result.Value!.Id }, result.Value);

            return HandleResult(result);
        }

        /// <summary>
        /// Update pending asset request
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <param name="dto">Updated request data</param>
        /// <returns>Updated asset request</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AssetRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateRequest(Guid id, [FromBody] UpdateAssetRequestDto dto)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _assetRequestService.UpdateAsync(CurrentEmployeeId.Value, id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Withdraw pending asset request
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <param name="dto">Optional cancellation reason</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/withdraw")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> WithdrawRequest(Guid id, [FromBody] CancelAssetRequestDto? dto = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _assetRequestService.WithdrawAsync(CurrentEmployeeId.Value, id, dto?.Reason);
            return HandleResult(result);
        }

        // ==================== Categories ====================

        /// <summary>
        /// Get available asset categories
        /// </summary>
        /// <returns>List of asset categories</returns>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        public IActionResult GetCategories()
        {
            // Return predefined categories from AssetCategory constants
            return Ok(AssetCategory.All);
        }

        /// <summary>
        /// Get available asset priorities
        /// </summary>
        /// <returns>List of priority levels</returns>
        [HttpGet("priorities")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        [ProducesResponseType(401)]
        public IActionResult GetPriorities()
        {
            var priorities = new[]
            {
                new { value = AssetRequestPriority.Low, label = "Low" },
                new { value = AssetRequestPriority.Normal, label = "Normal" },
                new { value = AssetRequestPriority.High, label = "High" },
                new { value = AssetRequestPriority.Urgent, label = "Urgent" }
            };
            return Ok(priorities);
        }
    }
}
