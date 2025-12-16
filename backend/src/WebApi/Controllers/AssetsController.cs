using Application.DTOs.Assets;
using Application.Interfaces;
using Core.Common;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;
using System.Collections.Generic;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AssetsController : ControllerBase
{
    private readonly IAssetsService _service;

    public AssetsController(IAssetsService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Assets), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<Assets>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] AssetsFilterRequest request)
    {
        var result = await _service.GetPagedAsync(request.PageNumber, request.PageSize, request.SearchTerm, request.SortBy, request.SortDescending, request.GetFilters());
        if (result.IsFailure) return FromError(result.Error!);
        var (items, total) = result.Value;
        return Ok(new PagedResponse<Assets>(items, total, request.PageNumber, request.PageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Assets), 201)]
    public async Task<IActionResult> Create([FromBody] CreateAssetDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpGet("assignments")]
    [ProducesResponseType(typeof(IEnumerable<AssetAssignments>), 200)]
    public async Task<IActionResult> GetAllAssignments()
    {
        var result = await _service.GetAllAssignmentsAsync();
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("{id}/assignments")]
    [ProducesResponseType(typeof(IEnumerable<AssetAssignments>), 200)]
    public async Task<IActionResult> GetAssignments(Guid id)
    {
        var result = await _service.GetAssignmentsAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("assignments/employee/{employeeId}")]
    [ProducesResponseType(typeof(IEnumerable<AssetAssignments>), 200)]
    public async Task<IActionResult> GetAssignmentsByEmployee(Guid employeeId)
    {
        var result = await _service.GetAssignmentsByEmployeeAsync(employeeId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/assign")]
    [ProducesResponseType(typeof(AssetAssignments), 201)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] CreateAssetAssignmentDto dto)
    {
        var result = await _service.AddAssignmentAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetAssignments), new { id }, result.Value);
    }

    [HttpPost("assignments/{assignmentId}/return")]
    public async Task<IActionResult> Return(Guid assignmentId, [FromBody] ReturnAssetAssignmentDto dto)
    {
        var result = await _service.ReturnAssignmentAsync(assignmentId, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(IEnumerable<AssetDocuments>), 200)]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var result = await _service.GetDocumentsAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/documents")]
    [ProducesResponseType(typeof(AssetDocuments), 201)]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] CreateAssetDocumentDto dto)
    {
        var result = await _service.AddDocumentAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetDocuments), new { id }, result.Value);
    }

    [HttpDelete("documents/{documentId}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
    {
        var result = await _service.DeleteDocumentAsync(documentId);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpGet("maintenance")]
    [ProducesResponseType(typeof(PagedResponse<AssetMaintenance>), 200)]
    public async Task<IActionResult> GetMaintenancePaged([FromQuery] AssetMaintenanceFilterRequest request)
    {
        var result = await _service.GetMaintenancePagedAsync(request.PageNumber, request.PageSize, request.GetFilters());
        if (result.IsFailure) return FromError(result.Error!);
        var (items, total) = result.Value;
        return Ok(new PagedResponse<AssetMaintenance>(items, total, request.PageNumber, request.PageSize));
    }

    [HttpGet("{id}/maintenance")]
    [ProducesResponseType(typeof(IEnumerable<AssetMaintenance>), 200)]
    public async Task<IActionResult> GetMaintenanceForAsset(Guid id)
    {
        var result = await _service.GetMaintenanceByAssetAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/maintenance")]
    [ProducesResponseType(typeof(AssetMaintenance), 201)]
    public async Task<IActionResult> CreateMaintenance(Guid id, [FromBody] CreateAssetMaintenanceDto dto)
    {
        var result = await _service.CreateMaintenanceAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetMaintenanceForAsset), new { id }, result.Value);
    }

    [HttpPut("maintenance/{maintenanceId}")]
    public async Task<IActionResult> UpdateMaintenance(Guid maintenanceId, [FromBody] UpdateAssetMaintenanceDto dto)
    {
        var result = await _service.UpdateMaintenanceAsync(maintenanceId, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpPost("{id}/dispose")]
    [ProducesResponseType(typeof(AssetDisposals), 201)]
    public async Task<IActionResult> Dispose(Guid id, [FromBody] CreateAssetDisposalDto dto)
    {
        var result = await _service.DisposeAssetAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetById), new { id }, result.Value);
    }

    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(AssetCostSummaryDto), 200)]
    public async Task<IActionResult> GetCostSummary(Guid id)
    {
        var result = await _service.GetCostSummaryAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("cost-report")]
    [ProducesResponseType(typeof(AssetCostReportDto), 200)]
    public async Task<IActionResult> GetCostReport([FromQuery] Guid? companyId = null)
    {
        var result = await _service.GetCostReportAsync(companyId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    /// <summary>
    /// Bulk create assets
    /// </summary>
    /// <param name="dto">Bulk assets payload</param>
    /// <returns>Bulk upload summary</returns>
    /// <response code="200">Bulk upload processed</response>
    /// <response code="400">Validation errors</response>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkAssetsResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkAssetsDto dto)
    {
        var result = await _service.BulkCreateAsync(dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Internal => StatusCode(500, result.Error.Message),
                _ => BadRequest(result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("{id}/link-loan")]
    [ProducesResponseType(typeof(Assets), 200)]
    public async Task<IActionResult> LinkAssetToLoan(Guid id, [FromBody] Guid loanId)
    {
        var result = await _service.LinkAssetToLoanAsync(id, loanId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("by-loan/{loanId}")]
    [ProducesResponseType(typeof(IEnumerable<Assets>), 200)]
    public async Task<IActionResult> GetAssetsByLoan(Guid loanId)
    {
        var result = await _service.GetAssetsByLoanAsync(loanId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    private IActionResult FromError(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => BadRequest(error.Message),
            ErrorType.NotFound => NotFound(error.Message),
            _ => StatusCode(500, error.Message)
        };
}




