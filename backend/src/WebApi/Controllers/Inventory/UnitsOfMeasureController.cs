using Application.Interfaces.Inventory;
using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;
using Core.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class UnitsOfMeasureController : CompanyAuthorizedController
    {
        private readonly IUnitOfMeasureRepository _repository;

        public UnitsOfMeasureController(IUnitOfMeasureRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UnitOfMeasure), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound($"Unit of Measure with ID {id} not found");

            return Ok(entity);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UnitOfMeasure>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
            {
                var entities = await _repository.GetByCompanyIdAsync(effectiveCompanyId.Value);
                return Ok(entities);
            }

            // Return system units only if no company specified
            var systemUnits = await _repository.GetSystemUnitsAsync();
            return Ok(systemUnits);
        }

        [HttpGet("system")]
        [ProducesResponseType(typeof(IEnumerable<UnitOfMeasure>), 200)]
        public async Task<IActionResult> GetSystemUnits()
        {
            var entities = await _repository.GetSystemUnitsAsync();
            return Ok(entities);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<UnitOfMeasure>), 200)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] Guid? companyId = null)
        {
            var filters = new Dictionary<string, object>();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
                filters["company_id"] = effectiveCompanyId.Value;

            var result = await _repository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);

            return Ok(new PagedResponse<UnitOfMeasure>(result.Items, result.TotalCount, pageNumber, pageSize));
        }

        [HttpPost]
        [ProducesResponseType(typeof(UnitOfMeasure), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateUnitOfMeasureDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!dto.CompanyId.HasValue)
            {
                var effectiveCompanyId = GetEffectiveCompanyId(null);
                dto.CompanyId = effectiveCompanyId;
            }

            // Check for duplicate symbol
            var exists = await _repository.ExistsAsync(dto.Symbol, dto.CompanyId);
            if (exists)
                return Conflict($"Unit with symbol '{dto.Symbol}' already exists");

            var entity = new UnitOfMeasure
            {
                CompanyId = dto.CompanyId,
                Name = dto.Name,
                Symbol = dto.Symbol,
                DecimalPlaces = dto.DecimalPlaces,
                IsSystemUnit = false,
                TallyUnitGuid = dto.TallyUnitGuid,
                TallyUnitName = dto.TallyUnitName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdEntity = await _repository.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = createdEntity.Id }, createdEntity);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitOfMeasureDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound($"Unit of Measure with ID {id} not found");

            if (existingEntity.IsSystemUnit)
                return BadRequest("Cannot update system units");

            // Check for duplicate symbol if changed
            if (dto.Symbol != existingEntity.Symbol)
            {
                var exists = await _repository.ExistsAsync(dto.Symbol, existingEntity.CompanyId, id);
                if (exists)
                    return Conflict($"Unit with symbol '{dto.Symbol}' already exists");
            }

            existingEntity.Name = dto.Name;
            existingEntity.Symbol = dto.Symbol;
            existingEntity.DecimalPlaces = dto.DecimalPlaces;
            existingEntity.TallyUnitGuid = dto.TallyUnitGuid;
            existingEntity.TallyUnitName = dto.TallyUnitName;
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return NotFound($"Unit of Measure with ID {id} not found");

            if (existingEntity.IsSystemUnit)
                return BadRequest("Cannot delete system units");

            var isInUse = await _repository.IsInUseAsync(id);
            if (isInUse)
                return Conflict("Cannot delete unit that is in use");

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
