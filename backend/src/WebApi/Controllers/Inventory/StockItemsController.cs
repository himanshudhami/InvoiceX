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
    public class StockItemsController : CompanyAuthorizedController
    {
        private readonly IStockItemService _service;

        public StockItemsController(IStockItemService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StockItem), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeDetails = false)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = includeDetails
                ? await _service.GetByIdWithDetailsAsync(id)
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
                return AccessDeniedDifferentCompanyResponse("Stock Item");

            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StockItem>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetByCompanyIdAsync(effectiveCompanyId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<StockItem>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] StockItemFilterRequest request, [FromQuery] Guid? companyId = null)
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
            return Ok(new PagedResponse<StockItem>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpGet("active")]
        [ProducesResponseType(typeof(IEnumerable<StockItem>), 200)]
        public async Task<IActionResult> GetActive([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetActiveAsync(effectiveCompanyId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(IEnumerable<StockItem>), 200)]
        public async Task<IActionResult> GetLowStock([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetLowStockItemsAsync(effectiveCompanyId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpGet("by-group/{stockGroupId}")]
        [ProducesResponseType(typeof(IEnumerable<StockItem>), 200)]
        public async Task<IActionResult> GetByStockGroup(Guid stockGroupId)
        {
            var result = await _service.GetByStockGroupIdAsync(stockGroupId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        [HttpGet("{id}/position")]
        [ProducesResponseType(typeof(StockPositionDto), 200)]
        public async Task<IActionResult> GetStockPosition(Guid id, [FromQuery] Guid? warehouseId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetStockPositionAsync(id, warehouseId);
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
        [ProducesResponseType(typeof(StockItem), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateStockItemDto dto)
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
                return CannotModifyDifferentCompanyResponse("create stock item for");

            var result = await _service.CreateAsync(dto);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockItemDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingResult = await _service.GetByIdAsync(id);
            if (existingResult.IsFailure)
                return NotFound(existingResult.Error!.Message);

            if (!HasCompanyAccess(existingResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Stock Item");

            var result = await _service.UpdateAsync(id, dto);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
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
                return AccessDeniedDifferentCompanyResponse("Stock Item");

            var result = await _service.DeleteAsync(id);
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        [HttpPost("{id}/recalculate")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RecalculateStock(Guid id)
        {
            var result = await _service.RecalculateStockAsync(id);
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
    }
}
