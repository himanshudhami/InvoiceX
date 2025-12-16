using Application.Interfaces;
using Application.DTOs.BankAccounts;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Bank account management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BankAccountsController : ControllerBase
    {
        private readonly IBankAccountService _service;

        /// <summary>
        /// Initializes a new instance of the BankAccountsController
        /// </summary>
        public BankAccountsController(IBankAccountService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get bank account by ID
        /// </summary>
        /// <param name="id">The bank account ID</param>
        /// <returns>The bank account</returns>
        /// <response code="200">Returns the bank account</response>
        /// <response code="404">Bank account not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankAccount), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all bank accounts
        /// </summary>
        /// <returns>List of bank accounts</returns>
        /// <response code="200">Returns the list of bank accounts</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BankAccount>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get paginated bank accounts with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of bank accounts</returns>
        /// <response code="200">Returns the paginated list of bank accounts</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<BankAccount>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] BankAccountFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            var (items, totalCount) = result.Value;
            var response = new PagedResponse<BankAccount>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new bank account
        /// </summary>
        /// <param name="dto">The bank account to create</param>
        /// <returns>The created bank account</returns>
        /// <response code="201">Bank account created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(BankAccount), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateBankAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing bank account
        /// </summary>
        /// <param name="id">The bank account ID</param>
        /// <param name="dto">The updated bank account data</param>
        /// <returns>No content</returns>
        /// <response code="204">Bank account updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Bank account not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a bank account
        /// </summary>
        /// <param name="id">The bank account ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Bank account deleted successfully</response>
        /// <response code="404">Bank account not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        // ==================== Specialized Endpoints ====================

        /// <summary>
        /// Get bank accounts by company ID
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <returns>List of bank accounts for the company</returns>
        /// <response code="200">Returns the list of bank accounts</response>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<BankAccount>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetByCompanyIdAsync(companyId);

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

        /// <summary>
        /// Get the primary bank account for a company
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <returns>The primary bank account</returns>
        /// <response code="200">Returns the primary bank account</response>
        /// <response code="204">No primary bank account set</response>
        [HttpGet("primary/{companyId}")]
        [ProducesResponseType(typeof(BankAccount), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPrimaryAccount(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetPrimaryAccountAsync(companyId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (result.Value == null)
                return NoContent();

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all active bank accounts
        /// </summary>
        /// <param name="companyId">Optional company ID filter</param>
        /// <returns>List of active bank accounts</returns>
        /// <response code="200">Returns the list of active bank accounts</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(IEnumerable<BankAccount>), 200)]
        public async Task<IActionResult> GetActiveAccounts([FromQuery] Guid? companyId = null)
        {
            var result = await _service.GetActiveAccountsAsync(companyId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Update the balance of a bank account
        /// </summary>
        /// <param name="id">The bank account ID</param>
        /// <param name="dto">The balance update data</param>
        /// <returns>No content</returns>
        /// <response code="204">Balance updated successfully</response>
        /// <response code="404">Bank account not found</response>
        [HttpPost("{id}/update-balance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateBalance(Guid id, [FromBody] UpdateBalanceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateBalanceAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Set a bank account as the primary account for a company
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="accountId">The bank account ID to set as primary</param>
        /// <returns>No content</returns>
        /// <response code="204">Primary account set successfully</response>
        /// <response code="404">Bank account not found</response>
        [HttpPost("{companyId}/set-primary/{accountId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetPrimaryAccount(Guid companyId, Guid accountId)
        {
            var result = await _service.SetPrimaryAccountAsync(companyId, accountId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }
    }
}
