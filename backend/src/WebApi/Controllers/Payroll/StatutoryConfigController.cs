using Application.DTOs.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Company statutory configuration management endpoints
/// </summary>
[ApiController]
[Route("api/payroll/statutory-configs")]
[Produces("application/json")]
public class StatutoryConfigController : ControllerBase
{
    private readonly ICompanyStatutoryConfigRepository _repository;
    private readonly IMapper _mapper;

    public StatutoryConfigController(
        ICompanyStatutoryConfigRepository repository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Get all statutory configs with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CompanyStatutoryConfigDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] CompanyStatutoryConfigFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<CompanyStatutoryConfigDto>>(items);
        var response = new PagedResponse<CompanyStatutoryConfigDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all statutory configs with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<CompanyStatutoryConfigDto>), 200)]
    public async Task<IActionResult> GetAllPaged([FromQuery] CompanyStatutoryConfigFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<CompanyStatutoryConfigDto>>(items);
        var response = new PagedResponse<CompanyStatutoryConfigDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get statutory config by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CompanyStatutoryConfigDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null)
            return NotFound($"Statutory config with ID {id} not found");

        return Ok(_mapper.Map<CompanyStatutoryConfigDto>(config));
    }

    /// <summary>
    /// Get statutory config for a company
    /// </summary>
    [HttpGet("company/{companyId}")]
    [ProducesResponseType(typeof(CompanyStatutoryConfigDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCompanyId(Guid companyId)
    {
        var config = await _repository.GetByCompanyIdAsync(companyId);
        if (config == null)
            return NotFound($"Statutory config not found for company {companyId}");

        return Ok(_mapper.Map<CompanyStatutoryConfigDto>(config));
    }

    /// <summary>
    /// Get all active statutory configs
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<CompanyStatutoryConfigDto>), 200)]
    public async Task<IActionResult> GetActiveConfigs()
    {
        var configs = await _repository.GetActiveConfigsAsync();
        var dtos = _mapper.Map<IEnumerable<CompanyStatutoryConfigDto>>(configs);
        return Ok(dtos);
    }

    /// <summary>
    /// Create a new statutory config
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CompanyStatutoryConfigDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyStatutoryConfigDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if config already exists for this company
            var exists = await _repository.ExistsForCompanyAsync(dto.CompanyId);
            if (exists)
                return Conflict($"Statutory config already exists for company {dto.CompanyId}");

            var entity = _mapper.Map<CompanyStatutoryConfig>(dto);
            
            // Handle field name differences between DTO and Entity
            entity.PfRegistrationNumber = dto.PfEstablishmentCode;
            entity.EsiRegistrationNumber = dto.EsiCode;
            entity.EsiGrossCeiling = dto.EsiWageCeiling;
            
            // Set required fields
            entity.EffectiveFrom = DateTime.UtcNow;
            entity.PtState = dto.PtState ?? "Karnataka";
            
            entity.IsActive = true;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<CompanyStatutoryConfigDto>(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, details = ex.ToString() });
        }
    }

    /// <summary>
    /// Update a statutory config
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyStatutoryConfigDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Statutory config with ID {id} not found");

        // Preserve IsActive if not explicitly set in the update
        var preserveIsActive = existing.IsActive;

        // Use AutoMapper to update, but handle field name differences manually
        _mapper.Map(dto, existing);

        // Handle field name differences between DTO and Entity
        if (!string.IsNullOrEmpty(dto.PfEstablishmentCode))
            existing.PfRegistrationNumber = dto.PfEstablishmentCode;
        if (!string.IsNullOrEmpty(dto.EsiCode))
            existing.EsiRegistrationNumber = dto.EsiCode;
        if (dto.EsiWageCeiling.HasValue)
            existing.EsiGrossCeiling = dto.EsiWageCeiling.Value;

        // Restore IsActive if not explicitly provided in the DTO
        if (!dto.IsActive.HasValue)
            existing.IsActive = preserveIsActive;

        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Delete a statutory config
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null)
            return NotFound($"Statutory config with ID {id} not found");

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}





