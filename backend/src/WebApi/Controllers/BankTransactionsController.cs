using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

// DTOs from service interface
using BankTransactionSummaryDto = Application.Interfaces.BankTransactionSummaryDto;

namespace WebApi.Controllers
{
    /// <summary>
    /// Bank transaction management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BankTransactionsController : ControllerBase
    {
        private readonly IBankTransactionService _service;

        /// <summary>
        /// Initializes a new instance of the BankTransactionsController
        /// </summary>
        public BankTransactionsController(IBankTransactionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get bank transaction by ID
        /// </summary>
        /// <param name="id">The bank transaction ID</param>
        /// <returns>The bank transaction</returns>
        /// <response code="200">Returns the bank transaction</response>
        /// <response code="404">Bank transaction not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankTransaction), 200)]
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
        /// Get all bank transactions
        /// </summary>
        /// <returns>List of bank transactions</returns>
        /// <response code="200">Returns the list of bank transactions</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
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
        /// Get paginated bank transactions with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of bank transactions</returns>
        /// <response code="200">Returns the paginated list of bank transactions</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<BankTransaction>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] BankTransactionFilterRequest request)
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
            var response = new PagedResponse<BankTransaction>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new bank transaction (manual entry)
        /// </summary>
        /// <param name="dto">The bank transaction to create</param>
        /// <returns>The created bank transaction</returns>
        /// <response code="201">Bank transaction created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(BankTransaction), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateBankTransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

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

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing bank transaction
        /// </summary>
        /// <param name="id">The bank transaction ID</param>
        /// <param name="dto">The updated bank transaction data</param>
        /// <returns>No content</returns>
        /// <response code="204">Bank transaction updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Bank transaction not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankTransactionDto dto)
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
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a bank transaction
        /// </summary>
        /// <param name="id">The bank transaction ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Bank transaction deleted successfully</response>
        /// <response code="404">Bank transaction not found</response>
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
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        // ==================== Bank Account Specific Endpoints ====================

        /// <summary>
        /// Get transactions for a specific bank account
        /// </summary>
        /// <param name="bankAccountId">The bank account ID</param>
        /// <returns>List of transactions for the bank account</returns>
        /// <response code="200">Returns the list of transactions</response>
        [HttpGet("by-account/{bankAccountId}")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByBankAccountId(Guid bankAccountId)
        {
            if (bankAccountId == Guid.Empty)
                return BadRequest("Bank account ID cannot be empty");

            var result = await _service.GetByBankAccountIdAsync(bankAccountId);

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
        /// Get transactions within a date range for a specific bank account
        /// </summary>
        /// <param name="bankAccountId">The bank account ID</param>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <returns>List of transactions in the date range</returns>
        /// <response code="200">Returns the list of transactions</response>
        [HttpGet("by-account/{bankAccountId}/date-range")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByDateRange(
            Guid bankAccountId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            if (bankAccountId == Guid.Empty)
                return BadRequest("Bank account ID cannot be empty");

            var result = await _service.GetByDateRangeAsync(bankAccountId, fromDate, toDate);

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

        // ==================== Reconciliation Endpoints ====================

        /// <summary>
        /// Get unreconciled transactions
        /// </summary>
        /// <param name="bankAccountId">Optional bank account ID filter</param>
        /// <returns>List of unreconciled transactions</returns>
        /// <response code="200">Returns the list of unreconciled transactions</response>
        [HttpGet("unreconciled")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        public async Task<IActionResult> GetUnreconciled([FromQuery] Guid? bankAccountId = null)
        {
            var result = await _service.GetUnreconciledAsync(bankAccountId);

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
        /// Reconcile a transaction with a payment or other record
        /// </summary>
        /// <param name="id">The bank transaction ID</param>
        /// <param name="dto">The reconciliation data</param>
        /// <returns>No content</returns>
        /// <response code="204">Transaction reconciled successfully</response>
        /// <response code="400">Invalid input or transaction already reconciled</response>
        /// <response code="404">Transaction not found</response>
        [HttpPost("{id}/reconcile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reconcile(Guid id, [FromBody] ReconcileTransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.ReconcileTransactionAsync(id, dto);

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
        /// Remove reconciliation from a transaction
        /// </summary>
        /// <param name="id">The bank transaction ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Reconciliation removed successfully</response>
        /// <response code="400">Transaction is not reconciled</response>
        /// <response code="404">Transaction not found</response>
        [HttpPost("{id}/unreconcile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Unreconcile(Guid id)
        {
            var result = await _service.UnreconcileTransactionAsync(id);

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
        /// Get reconciliation suggestions for a transaction
        /// </summary>
        /// <param name="id">The bank transaction ID</param>
        /// <param name="tolerance">Amount tolerance for matching (default 0.01)</param>
        /// <param name="maxResults">Maximum number of suggestions (default 10)</param>
        /// <returns>List of potential payment matches</returns>
        /// <response code="200">Returns the list of suggestions</response>
        /// <response code="404">Transaction not found</response>
        [HttpGet("{id}/reconciliation-suggestions")]
        [ProducesResponseType(typeof(IEnumerable<ReconciliationSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReconciliationSuggestions(
            Guid id,
            [FromQuery] decimal tolerance = 0.01m,
            [FromQuery] int maxResults = 10)
        {
            var result = await _service.GetReconciliationSuggestionsAsync(id, tolerance, maxResults);

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

            return Ok(result.Value);
        }

        // ==================== Import Endpoints ====================

        /// <summary>
        /// Import bank transactions from parsed CSV data
        /// </summary>
        /// <param name="request">The import request with transactions</param>
        /// <returns>Import result with counts</returns>
        /// <response code="200">Import completed</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Bank account not found</response>
        [HttpPost("import")]
        [ProducesResponseType(typeof(ImportBankTransactionsResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Import([FromBody] ImportBankTransactionsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.ImportTransactionsAsync(request);

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

            return Ok(result.Value);
        }

        /// <summary>
        /// Get transactions from a specific import batch
        /// </summary>
        /// <param name="batchId">The import batch ID</param>
        /// <returns>List of transactions from the batch</returns>
        /// <response code="200">Returns the list of transactions</response>
        [HttpGet("by-batch/{batchId}")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByImportBatch(Guid batchId)
        {
            if (batchId == Guid.Empty)
                return BadRequest("Batch ID cannot be empty");

            var result = await _service.GetByImportBatchIdAsync(batchId);

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
        /// Delete all transactions from a specific import batch (rollback import)
        /// </summary>
        /// <param name="batchId">The import batch ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Batch deleted successfully</response>
        /// <response code="400">Cannot delete batch with reconciled transactions</response>
        /// <response code="404">Batch not found</response>
        [HttpDelete("batch/{batchId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteImportBatch(Guid batchId)
        {
            var result = await _service.DeleteImportBatchAsync(batchId);

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

        // ==================== Summary Endpoints ====================

        /// <summary>
        /// Get summary statistics for a bank account
        /// </summary>
        /// <param name="bankAccountId">The bank account ID</param>
        /// <param name="fromDate">Optional start date</param>
        /// <param name="toDate">Optional end date</param>
        /// <returns>Summary statistics</returns>
        /// <response code="200">Returns the summary</response>
        [HttpGet("summary/{bankAccountId}")]
        [ProducesResponseType(typeof(BankTransactionSummaryDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSummary(
            Guid bankAccountId,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null)
        {
            if (bankAccountId == Guid.Empty)
                return BadRequest("Bank account ID cannot be empty");

            var result = await _service.GetSummaryAsync(bankAccountId, fromDate, toDate);

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
    }
}
