using Application.DTOs.Manufacturing;
using Application.Services.Manufacturing;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Manufacturing;

[ApiController]
[Route("api/manufacturing/[controller]")]
[Produces("application/json")]
public class BomController : ControllerBase
{
    private readonly IBomService _service;

    public BomController(IBomService service)
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

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] Guid companyId)
    {
        var result = await _service.GetActiveAsync(companyId);
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
        [FromQuery] Guid? finishedGoodId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _service.GetPagedAsync(pageNumber, pageSize, companyId, searchTerm, finishedGoodId, isActive);
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

    [HttpGet("by-finished-good/{finishedGoodId}")]
    public async Task<IActionResult> GetByFinishedGood(Guid finishedGoodId)
    {
        var result = await _service.GetByFinishedGoodAsync(finishedGoodId);
        if (result.IsFailure)
            return BadRequest(result.Error!.Message);
        return Ok(result.Value);
    }

    [HttpGet("active-for-product/{finishedGoodId}")]
    public async Task<IActionResult> GetActiveBomForProduct(Guid finishedGoodId)
    {
        var result = await _service.GetActiveBomForProductAsync(finishedGoodId);
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
    public async Task<IActionResult> Create([FromBody] CreateBomDto dto)
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBomDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
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
                _ => BadRequest(result.Error.Message)
            };
        }
        return NoContent();
    }

    [HttpPost("{id}/copy")]
    public async Task<IActionResult> Copy(Guid id, [FromBody] CopyBomDto dto)
    {
        var result = await _service.CopyAsync(id, dto);
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
}
