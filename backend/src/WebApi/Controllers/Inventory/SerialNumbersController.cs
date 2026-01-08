using Application.DTOs.Inventory;
using Application.Services.Inventory;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Inventory;

[ApiController]
[Route("api/inventory/[controller]")]
[Produces("application/json")]
public class SerialNumbersController : ControllerBase
{
    private readonly ISerialNumberService _service;

    public SerialNumbersController(ISerialNumberService service)
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

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] SerialNumberFilterParams filter)
    {
        var result = await _service.GetPagedAsync(filter);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);

        var (items, totalCount) = result.Value;
        return Ok(new
        {
            items,
            totalCount,
            pageNumber = filter.PageNumber,
            pageSize = filter.PageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
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

    [HttpGet("by-item/{stockItemId}")]
    public async Task<IActionResult> GetByStockItem(Guid stockItemId)
    {
        var result = await _service.GetByStockItemAsync(stockItemId);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);
        return Ok(result.Value);
    }

    [HttpGet("available/{stockItemId}")]
    public async Task<IActionResult> GetAvailable(Guid stockItemId, [FromQuery] Guid? warehouseId = null)
    {
        var result = await _service.GetAvailableAsync(stockItemId, warehouseId);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSerialNumberDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateSerialNumberDto dto)
    {
        var result = await _service.BulkCreateAsync(dto);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSerialNumberDto dto)
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

    [HttpPost("{id}/mark-sold")]
    public async Task<IActionResult> MarkAsSold(Guid id, [FromBody] MarkSerialAsSoldDto dto)
    {
        var result = await _service.MarkAsSoldAsync(id, dto);
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

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        var result = await _service.UpdateStatusAsync(id, status);
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
