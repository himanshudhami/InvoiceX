using Application.DTOs.Party;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for Unified Party Management (Customers, Vendors, Employees)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PartiesController : ControllerBase
    {
        private readonly IPartyService _partyService;
        private readonly ITdsDetectionService _tdsDetectionService;

        public PartiesController(IPartyService partyService, ITdsDetectionService tdsDetectionService)
        {
            _partyService = partyService ?? throw new ArgumentNullException(nameof(partyService));
            _tdsDetectionService = tdsDetectionService ?? throw new ArgumentNullException(nameof(tdsDetectionService));
        }

        // ==================== Basic CRUD ====================

        /// <summary>
        /// Get party by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PartyDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _partyService.GetByIdAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get party by ID with all profiles and tags
        /// </summary>
        [HttpGet("{id}/full")]
        [ProducesResponseType(typeof(PartyDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByIdWithProfiles(Guid id)
        {
            var result = await _partyService.GetByIdWithProfilesAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get parties by company with optional role filters
        /// </summary>
        [HttpGet("company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<PartyListDto>), 200)]
        public async Task<IActionResult> GetByCompanyId(
            Guid companyId,
            [FromQuery] bool? isVendor = null,
            [FromQuery] bool? isCustomer = null,
            [FromQuery] bool? isEmployee = null)
        {
            var result = await _partyService.GetByCompanyIdAsync(companyId, isVendor, isCustomer, isEmployee);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get paged list of parties with filters
        /// </summary>
        [HttpGet("company/{companyId}/paged")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] bool? isVendor = null,
            [FromQuery] bool? isCustomer = null,
            [FromQuery] bool? isEmployee = null,
            [FromQuery] bool? isActive = null)
        {
            var result = await _partyService.GetPagedAsync(
                companyId, pageNumber, pageSize, searchTerm, sortBy, sortDescending,
                isVendor, isCustomer, isEmployee, isActive);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new
            {
                items,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Create a new party
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PartyDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreatePartyDto dto)
        {
            var result = await _partyService.CreateAsync(dto);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing party
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartyDto dto)
        {
            var result = await _partyService.UpdateAsync(id, dto);
            if (result.IsFailure)
            {
                if (result.Error!.Type == ErrorType.NotFound)
                    return NotFound(result.Error.Message);
                return BadRequest(result.Error.Message);
            }
            return NoContent();
        }

        /// <summary>
        /// Delete a party
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _partyService.DeleteAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return NoContent();
        }

        // ==================== Role-specific Endpoints ====================

        /// <summary>
        /// Get all vendors for a company
        /// </summary>
        [HttpGet("company/{companyId}/vendors")]
        [ProducesResponseType(typeof(IEnumerable<PartyListDto>), 200)]
        public async Task<IActionResult> GetVendors(Guid companyId)
        {
            var result = await _partyService.GetVendorsAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get all customers for a company
        /// </summary>
        [HttpGet("company/{companyId}/customers")]
        [ProducesResponseType(typeof(IEnumerable<PartyListDto>), 200)]
        public async Task<IActionResult> GetCustomers(Guid companyId)
        {
            var result = await _partyService.GetCustomersAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get MSME registered vendors for a company
        /// </summary>
        [HttpGet("company/{companyId}/vendors/msme")]
        [ProducesResponseType(typeof(IEnumerable<PartyListDto>), 200)]
        public async Task<IActionResult> GetMsmeVendors(Guid companyId)
        {
            var result = await _partyService.GetMsmeVendorsAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS applicable vendors for a company
        /// </summary>
        [HttpGet("company/{companyId}/vendors/tds-applicable")]
        [ProducesResponseType(typeof(IEnumerable<PartyListDto>), 200)]
        public async Task<IActionResult> GetTdsApplicableVendors(Guid companyId)
        {
            var result = await _partyService.GetTdsApplicableVendorsAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        // ==================== Profile Management ====================

        /// <summary>
        /// Get vendor profile for a party
        /// </summary>
        [HttpGet("{id}/vendor-profile")]
        [ProducesResponseType(typeof(PartyVendorProfileDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetVendorProfile(Guid id)
        {
            var result = await _partyService.GetVendorProfileAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Update vendor profile for a party
        /// </summary>
        [HttpPut("{id}/vendor-profile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateVendorProfile(Guid id, [FromBody] UpdatePartyVendorProfileDto dto)
        {
            var result = await _partyService.UpdateVendorProfileAsync(id, dto);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return NoContent();
        }

        /// <summary>
        /// Get customer profile for a party
        /// </summary>
        [HttpGet("{id}/customer-profile")]
        [ProducesResponseType(typeof(PartyCustomerProfileDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCustomerProfile(Guid id)
        {
            var result = await _partyService.GetCustomerProfileAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Update customer profile for a party
        /// </summary>
        [HttpPut("{id}/customer-profile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCustomerProfile(Guid id, [FromBody] UpdatePartyCustomerProfileDto dto)
        {
            var result = await _partyService.UpdateCustomerProfileAsync(id, dto);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return NoContent();
        }

        // ==================== Tag Management ====================

        /// <summary>
        /// Get tags for a party
        /// </summary>
        [HttpGet("{id}/tags")]
        [ProducesResponseType(typeof(IEnumerable<PartyTagDto>), 200)]
        public async Task<IActionResult> GetTags(Guid id)
        {
            var result = await _partyService.GetTagsAsync(id);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Add a tag to a party
        /// </summary>
        [HttpPost("{id}/tags")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddTag(Guid id, [FromBody] AddPartyTagDto dto)
        {
            var result = await _partyService.AddTagAsync(id, dto);
            if (result.IsFailure)
            {
                if (result.Error!.Type == ErrorType.NotFound)
                    return NotFound(result.Error.Message);
                return BadRequest(result.Error.Message);
            }
            return NoContent();
        }

        /// <summary>
        /// Remove a tag from a party
        /// </summary>
        [HttpDelete("{id}/tags/{tagId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveTag(Guid id, Guid tagId)
        {
            var result = await _partyService.RemoveTagAsync(id, tagId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return NoContent();
        }

        // ==================== TDS Detection ====================

        /// <summary>
        /// Detect TDS configuration for a party based on tags, name, and Tally group
        /// </summary>
        [HttpGet("{id}/tds-configuration")]
        [ProducesResponseType(typeof(TdsConfigurationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTdsConfiguration(Guid id)
        {
            var result = await _partyService.DetectTdsConfigurationAsync(id);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return Ok(result.Value);
        }

        // ==================== Lookup Endpoints ====================

        /// <summary>
        /// Find party by GSTIN
        /// </summary>
        [HttpGet("company/{companyId}/by-gstin/{gstin}")]
        [ProducesResponseType(typeof(PartyDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByGstin(Guid companyId, string gstin)
        {
            var result = await _partyService.GetByGstinAsync(companyId, gstin);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            if (result.Value == null)
                return NotFound($"Party with GSTIN {gstin} not found");
            return Ok(result.Value);
        }

        /// <summary>
        /// Find party by PAN
        /// </summary>
        [HttpGet("company/{companyId}/by-pan/{panNumber}")]
        [ProducesResponseType(typeof(PartyDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByPan(Guid companyId, string panNumber)
        {
            var result = await _partyService.GetByPanAsync(companyId, panNumber);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            if (result.Value == null)
                return NotFound($"Party with PAN {panNumber} not found");
            return Ok(result.Value);
        }

        // ==================== TDS Tag Rules Management (Tag-driven TDS) ====================

        /// <summary>
        /// Get TDS tag rules for a company
        /// </summary>
        [HttpGet("company/{companyId}/tds-rules")]
        [ProducesResponseType(typeof(IEnumerable<TdsTagRuleDto>), 200)]
        public async Task<IActionResult> GetTdsRules(Guid companyId)
        {
            var result = await _tdsDetectionService.GetTdsTagRulesAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS tag rules with pagination
        /// </summary>
        [HttpGet("company/{companyId}/tds-rules/paged")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetTdsRulesPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? tdsSection = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            var result = await _tdsDetectionService.GetTdsTagRulesPagedAsync(
                companyId, pageNumber, pageSize, tdsSection, isActive, sortBy, sortDescending);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new
            {
                items,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Create a new TDS tag rule
        /// </summary>
        [HttpPost("company/{companyId}/tds-rules")]
        [ProducesResponseType(typeof(TdsTagRuleDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateTdsRule(Guid companyId, [FromBody] CreateTdsTagRuleDto dto)
        {
            var result = await _tdsDetectionService.CreateTdsTagRuleAsync(companyId, dto);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);
            return CreatedAtAction(nameof(GetTdsRules), new { companyId }, result.Value);
        }

        /// <summary>
        /// Update an existing TDS tag rule
        /// </summary>
        [HttpPut("tds-rules/{ruleId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateTdsRule(Guid ruleId, [FromBody] UpdateTdsTagRuleDto dto)
        {
            var result = await _tdsDetectionService.UpdateTdsTagRuleAsync(ruleId, dto);
            if (result.IsFailure)
            {
                if (result.Error!.Type == ErrorType.NotFound)
                    return NotFound(result.Error.Message);
                return BadRequest(result.Error.Message);
            }
            return NoContent();
        }

        /// <summary>
        /// Delete a TDS tag rule
        /// </summary>
        [HttpDelete("tds-rules/{ruleId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteTdsRule(Guid ruleId)
        {
            var result = await _tdsDetectionService.DeleteTdsTagRuleAsync(ruleId);
            if (result.IsFailure)
                return NotFound(result.Error!.Message);
            return NoContent();
        }

        /// <summary>
        /// Seed default TDS system (tags and rules) for a company
        /// </summary>
        [HttpPost("company/{companyId}/tds-rules/seed")]
        [ProducesResponseType(204)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> SeedDefaultTdsRules(Guid companyId)
        {
            var result = await _tdsDetectionService.SeedTdsSystemAsync(companyId);
            if (result.IsFailure)
            {
                if (result.Error!.Type == ErrorType.Conflict)
                    return Conflict(result.Error.Message);
                return BadRequest(result.Error.Message);
            }
            return NoContent();
        }
    }
}
