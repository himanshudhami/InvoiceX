using Application.Interfaces.Inventory;
using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;
using WebApi.DTOs.Inventory;

namespace WebApi.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class StockTransfersController : CompanyAuthorizedController
    {
        private readonly IStockTransferService _service;

        public StockTransfersController(IStockTransferService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StockTransfer), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeItems = false)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = includeItems
                ? await _service.GetByIdWithItemsAsync(id)
                : await _service.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            return Ok(result.Value);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<StockTransfer>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] StockTransferFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
                filters["company_id"] = effectiveCompanyId.Value;

            var result = await _service.GetPagedAsync(
                request.PageNumber, request.PageSize, request.SearchTerm,
                request.SortBy, request.SortDescending, filters);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<StockTransfer>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<StockTransfer>), 200)]
        public async Task<IActionResult> GetPendingTransfers([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetPendingTransfersAsync(effectiveCompanyId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(IEnumerable<StockTransfer>), 200)]
        public async Task<IActionResult> GetByStatus(string status, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetByStatusAsync(effectiveCompanyId.Value, status);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(StockTransfer), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateStockTransferDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!dto.CompanyId.HasValue)
            {
                var effectiveCompanyId = GetEffectiveCompanyId(null);
                if (!effectiveCompanyId.HasValue)
                    return BadRequest(new { error = "Company ID is required" });
                dto.CompanyId = effectiveCompanyId.Value;
            }

            if (!HasCompanyAccess(dto.CompanyId))
                return CannotModifyDifferentCompanyResponse("create stock transfer for");

            dto.CreatedBy = CurrentUserId;

            var result = await _service.CreateAsync(dto);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockTransferDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            var result = await _service.UpdateAsync(id, dto);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            var result = await _service.DeleteAsync(id);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        [HttpPost("{id}/dispatch")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Dispatch(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            var result = await _service.DispatchAsync(id, CurrentUserId ?? Guid.Empty);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        [HttpPost("{id}/complete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Complete(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            var result = await _service.CompleteAsync(id, CurrentUserId ?? Guid.Empty);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Transfer");

            var result = await _service.CancelAsync(id, CurrentUserId ?? Guid.Empty);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }
    }
}
