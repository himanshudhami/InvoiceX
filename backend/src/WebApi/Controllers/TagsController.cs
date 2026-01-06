using Application.DTOs.Tags;
using Application.Interfaces.Tags;
using Core.Common;
using Core.Entities.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Tags management - flexible labeling system for transactions
    /// </summary>
    [ApiController]
    [Route("api/tags")]
    [Produces("application/json")]
    [Authorize]
    public class TagsController : CompanyAuthorizedController
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
        }

        /// <summary>
        /// Get tag by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _tagService.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Tag");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all tags for current company
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Tag>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _tagService.GetByCompanyIdAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get paginated tags with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Tag>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] TagsFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
                filters["company_id"] = effectiveCompanyId.Value;

            var result = await _tagService.GetPagedAsync(
                request.PageNumber, request.PageSize, request.SearchTerm,
                request.SortBy, request.SortDescending, filters);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<Tag>(items, totalCount, request.PageNumber, request.PageSize));
        }

        /// <summary>
        /// Get tags by group (department, project, client, etc.)
        /// </summary>
        [HttpGet("group/{tagGroup}")]
        [ProducesResponseType(typeof(IEnumerable<Tag>), 200)]
        public async Task<IActionResult> GetByGroup(string tagGroup, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _tagService.GetByGroupAsync(effectiveCompanyId.Value, tagGroup);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get tag hierarchy (tree structure)
        /// </summary>
        [HttpGet("hierarchy")]
        [ProducesResponseType(typeof(IEnumerable<Tag>), 200)]
        public async Task<IActionResult> GetHierarchy([FromQuery] string? tagGroup = null, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _tagService.GetTagHierarchyAsync(effectiveCompanyId.Value, tagGroup);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get tag summaries (for dropdowns)
        /// </summary>
        [HttpGet("summaries")]
        [ProducesResponseType(typeof(IEnumerable<TagSummaryDto>), 200)]
        public async Task<IActionResult> GetSummaries([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _tagService.GetTagSummariesAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new tag
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Tag), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
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
                    return CannotModifyDifferentCompanyResponse("create tag for");

                dto.CompanyId = CurrentCompanyId.Value;
            }

            var result = await _tagService.CreateAsync(dto);

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
        /// Update an existing tag
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tagResult = await _tagService.GetByIdAsync(id);
            if (tagResult.IsFailure)
                return NotFound(tagResult.Error!.Message);

            if (!HasCompanyAccess(tagResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Tag");

            var result = await _tagService.UpdateAsync(id, dto);

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
        /// Delete a tag
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tagResult = await _tagService.GetByIdAsync(id);
            if (tagResult.IsFailure)
                return NotFound(tagResult.Error!.Message);

            if (!HasCompanyAccess(tagResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Tag");

            var result = await _tagService.DeleteAsync(id);

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

        /// <summary>
        /// Seed default tags for a company
        /// </summary>
        [HttpPost("seed-defaults")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> SeedDefaults([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var userId = GetCurrentUserId();
            var result = await _tagService.SeedDefaultTagsAsync(effectiveCompanyId.Value, userId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        // ==================== Transaction Tagging ====================

        /// <summary>
        /// Get tags for a transaction
        /// </summary>
        [HttpGet("transaction/{transactionType}/{transactionId}")]
        [ProducesResponseType(typeof(IEnumerable<TransactionTagDto>), 200)]
        public async Task<IActionResult> GetTransactionTags(string transactionType, Guid transactionId)
        {
            var result = await _tagService.GetTransactionTagsAsync(transactionId, transactionType);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Apply tags to a transaction
        /// </summary>
        [HttpPost("apply")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ApplyTags([FromBody] ApplyTagsToTransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _tagService.ApplyTagsAsync(dto, userId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Remove a tag from a transaction
        /// </summary>
        [HttpDelete("transaction/{transactionType}/{transactionId}/tag/{tagId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveTag(string transactionType, Guid transactionId, Guid tagId)
        {
            var result = await _tagService.RemoveTagAsync(transactionId, transactionType, tagId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Auto-attribute tags to a transaction based on rules
        /// </summary>
        [HttpPost("auto-attribute")]
        [ProducesResponseType(typeof(AutoAttributionResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AutoAttribute([FromBody] AutoAttributeRequest request)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = request.CompanyId ?? CurrentCompanyId;
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _tagService.AutoAttributeAsync(
                request.TransactionId,
                request.TransactionType,
                request.Amount,
                effectiveCompanyId.Value,
                request.VendorId,
                request.CustomerId,
                request.AccountId,
                request.Description);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;
            return Guid.Empty;
        }
    }

    public class AutoAttributeRequest
    {
        public Guid? CompanyId { get; set; }
        public Guid TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid? VendorId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? AccountId { get; set; }
        public string? Description { get; set; }
    }
}
