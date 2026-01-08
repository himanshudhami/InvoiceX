using Application.DTOs.Manufacturing;
using Application.Services.Manufacturing;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Manufacturing;

[ApiController]
[Route("api/manufacturing/[controller]")]
[Produces("application/json")]
public class ProductionOrdersController : ControllerBase
{
    private readonly IProductionOrderService _service;

    public ProductionOrdersController(IProductionOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId)
    {
        var result = await _service.GetAllAsync(companyId);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);
        return Ok(result.Value);
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus([FromQuery] Guid companyId, string status)
    {
        var result = await _service.GetByStatusAsync(companyId, status);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);
        return Ok(result.Value);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? bomId = null,
        [FromQuery] Guid? finishedGoodId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        var result = await _service.GetPagedAsync(pageNumber, pageSize, companyId, searchTerm,
            status, bomId, finishedGoodId, warehouseId, fromDate, toDate);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);

        var (items, totalCount) = result.Value;
        return Ok(new
        {
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdWithItemsAsync(id);
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
    public async Task<IActionResult> Create([FromBody] CreateProductionOrderDto dto)
    {
        var result = await _service.CreateAsync(dto);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionOrderDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
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

    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseProductionOrderDto dto)
    {
        var result = await _service.ReleaseAsync(id, dto);
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

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] StartProductionOrderDto dto)
    {
        var result = await _service.StartAsync(id, dto);
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

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteProductionOrderDto dto)
    {
        var result = await _service.CompleteAsync(id, dto);
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

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelProductionOrderDto dto)
    {
        var result = await _service.CancelAsync(id, dto);
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

    [HttpPost("{id}/consume")]
    public async Task<IActionResult> ConsumeItem(Guid id, [FromBody] ConsumeItemDto dto)
    {
        var result = await _service.ConsumeItemAsync(id, dto);
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
}
