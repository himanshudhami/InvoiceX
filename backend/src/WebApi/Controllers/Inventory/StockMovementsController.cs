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
    public class StockMovementsController : CompanyAuthorizedController
    {
        private readonly IStockMovementService _service;

        public StockMovementsController(IStockMovementService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StockMovement), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Movement");

            return Ok(result.Value);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<StockMovement>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] StockMovementFilterRequest request, [FromQuery] Guid? companyId = null)
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
            return Ok(new PagedResponse<StockMovement>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpGet("ledger/{stockItemId}")]
        [ProducesResponseType(typeof(IEnumerable<StockLedgerDto>), 200)]
        public async Task<IActionResult> GetStockLedger(
            Guid stockItemId,
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetStockLedgerAsync(stockItemId, warehouseId, fromDate, toDate);
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

        [HttpGet("position/{stockItemId}")]
        [ProducesResponseType(typeof(StockPositionDto), 200)]
        public async Task<IActionResult> GetStockPosition(
            Guid stockItemId,
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] DateOnly? asOfDate = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetStockPositionAsync(stockItemId, warehouseId, asOfDate);
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

        [HttpPost]
        [ProducesResponseType(typeof(StockMovement), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RecordMovement([FromBody] CreateStockMovementDto dto)
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
                return CannotModifyDifferentCompanyResponse("record stock movement for");

            var result = await _service.RecordMovementAsync(dto);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockMovementDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Movement");

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
        public async Task<IActionResult> Delete(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Movement");

            var result = await _service.DeleteAsync(id);
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

        [HttpPost("{stockItemId}/recalculate-totals")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RecalculateRunningTotals(Guid stockItemId, [FromQuery] Guid? warehouseId = null)
        {
            var result = await _service.RecalculateRunningTotalsAsync(stockItemId, warehouseId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }
    }
}
