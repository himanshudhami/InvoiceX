using Application.DTOs.Tags;
using Application.Interfaces.Tags;
using Core.Common;
using Core.Entities.Tags;
using Core.Interfaces.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Attribution Rules - automatic tag assignment based on conditions
    /// </summary>
    [ApiController]
    [Route("api/attribution-rules")]
    [Produces("application/json")]
    [Authorize]
    public class AttributionRulesController : CompanyAuthorizedController
    {
        private readonly IAttributionRuleService _ruleService;

        public AttributionRulesController(IAttributionRuleService ruleService)
        {
            _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        }

        /// <summary>
        /// Get attribution rule by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AttributionRule), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _ruleService.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Attribution Rule");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all attribution rules for current company
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AttributionRule>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _ruleService.GetByCompanyIdAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get paginated attribution rules with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<AttributionRule>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] AttributionRulesFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
                filters["company_id"] = effectiveCompanyId.Value;

            var result = await _ruleService.GetPagedAsync(
                request.PageNumber, request.PageSize, request.SearchTerm,
                request.SortBy, request.SortDescending, filters);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<AttributionRule>(items, totalCount, request.PageNumber, request.PageSize));
        }

        /// <summary>
        /// Get active rules only
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(typeof(IEnumerable<AttributionRule>), 200)]
        public async Task<IActionResult> GetActiveRules([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _ruleService.GetActiveRulesAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get rules applicable to a specific transaction type
        /// </summary>
        [HttpGet("for-type/{transactionType}")]
        [ProducesResponseType(typeof(IEnumerable<AttributionRule>), 200)]
        public async Task<IActionResult> GetRulesForType(string transactionType, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _ruleService.GetRulesForTransactionTypeAsync(effectiveCompanyId.Value, transactionType);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get rule performance statistics
        /// </summary>
        [HttpGet("performance")]
        [ProducesResponseType(typeof(IEnumerable<RulePerformanceSummary>), 200)]
        public async Task<IActionResult> GetPerformance([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _ruleService.GetRulePerformanceAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new attribution rule
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AttributionRule), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateAttributionRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsAdminOrHR)
            {
                if (!dto.CompanyId.HasValue)
                    return BadRequest(new { error = "Company ID is required" });
            }
            else
            {
                if (CurrentCompanyId == null)
                    return CompanyIdNotFoundResponse();

                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create attribution rule for");

                dto.CompanyId = CurrentCompanyId.Value;
            }

            var result = await _ruleService.CreateAsync(dto);

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

        /// <summary>
        /// Update an existing attribution rule
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttributionRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ruleResult = await _ruleService.GetByIdAsync(id);
            if (ruleResult.IsFailure)
                return NotFound(ruleResult.Error!.Message);

            if (!HasCompanyAccess(ruleResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Attribution Rule");

            var result = await _ruleService.UpdateAsync(id, dto);

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

            return NoContent();
        }

        /// <summary>
        /// Delete an attribution rule
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ruleResult = await _ruleService.GetByIdAsync(id);
            if (ruleResult.IsFailure)
                return NotFound(ruleResult.Error!.Message);

            if (!HasCompanyAccess(ruleResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Attribution Rule");

            var result = await _ruleService.DeleteAsync(id);

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

        /// <summary>
        /// Reorder rule priorities
        /// </summary>
        [HttpPost("reorder")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ReorderPriorities([FromBody] ReorderPrioritiesRequest request)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = request.CompanyId ?? CurrentCompanyId;
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var priorities = request.Priorities.Select(p => (p.RuleId, p.NewPriority));
            var result = await _ruleService.ReorderPrioritiesAsync(effectiveCompanyId.Value, priorities);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Test a rule against a transaction (dry run)
        /// </summary>
        [HttpPost("{id}/test")]
        [ProducesResponseType(typeof(AutoAttributionResult), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TestRule(Guid id, [FromBody] TestRuleRequest request)
        {
            var ruleResult = await _ruleService.GetByIdAsync(id);
            if (ruleResult.IsFailure)
                return NotFound(ruleResult.Error!.Message);

            if (!HasCompanyAccess(ruleResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Attribution Rule");

            var result = await _ruleService.TestRuleAsync(id, request.TransactionId, request.TransactionType);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }
    }

    public class ReorderPrioritiesRequest
    {
        public Guid? CompanyId { get; set; }
        public List<PriorityItem> Priorities { get; set; } = new();
    }

    public class PriorityItem
    {
        public Guid RuleId { get; set; }
        public int NewPriority { get; set; }
    }

    public class TestRuleRequest
    {
        public Guid TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
    }
}
