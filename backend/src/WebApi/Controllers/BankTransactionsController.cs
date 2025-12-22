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
        private readonly IReconciliationService _reconciliationService;
        private readonly IBankStatementImportService _importService;
        private readonly IBrsService _brsService;
        private readonly IReversalDetectionService _reversalService;
        private readonly IOutgoingPaymentsService _outgoingPaymentsService;

        public BankTransactionsController(
            IBankTransactionService service,
            IReconciliationService reconciliationService,
            IBankStatementImportService importService,
            IBrsService brsService,
            IReversalDetectionService reversalService,
            IOutgoingPaymentsService outgoingPaymentsService)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _reconciliationService = reconciliationService ?? throw new ArgumentNullException(nameof(reconciliationService));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _brsService = brsService ?? throw new ArgumentNullException(nameof(brsService));
            _reversalService = reversalService ?? throw new ArgumentNullException(nameof(reversalService));
            _outgoingPaymentsService = outgoingPaymentsService ?? throw new ArgumentNullException(nameof(outgoingPaymentsService));
        }

        // ==================== CRUD Endpoints ====================

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankTransaction), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<BankTransaction>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] BankTransactionFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber, request.PageSize, request.SearchTerm,
                request.SortBy, request.SortDescending, request.GetFilters());

            if (result.IsFailure) return HandleError(result.Error!);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<BankTransaction>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpPost]
        [ProducesResponseType(typeof(BankTransaction), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateBankTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.CreateAsync(dto);
            if (result.IsFailure) return HandleError(result.Error!);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.UpdateAsync(id, dto);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        // ==================== Bank Account Specific Endpoints ====================

        [HttpGet("by-account/{bankAccountId}")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByBankAccountId(Guid bankAccountId)
        {
            if (bankAccountId == Guid.Empty) return BadRequest("Bank account ID cannot be empty");
            var result = await _service.GetByBankAccountIdAsync(bankAccountId);
            return HandleResult(result);
        }

        [HttpGet("by-account/{bankAccountId}/date-range")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByDateRange(Guid bankAccountId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            if (bankAccountId == Guid.Empty) return BadRequest("Bank account ID cannot be empty");
            var result = await _service.GetByDateRangeAsync(bankAccountId, fromDate, toDate);
            return HandleResult(result);
        }

        [HttpGet("unreconciled")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        public async Task<IActionResult> GetUnreconciled([FromQuery] Guid? bankAccountId = null)
        {
            var result = await _service.GetUnreconciledAsync(bankAccountId);
            return HandleResult(result);
        }

        [HttpGet("summary/{bankAccountId}")]
        [ProducesResponseType(typeof(BankTransactionSummaryDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSummary(Guid bankAccountId, [FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
        {
            if (bankAccountId == Guid.Empty) return BadRequest("Bank account ID cannot be empty");
            var result = await _service.GetSummaryAsync(bankAccountId, fromDate, toDate);
            return HandleResult(result);
        }

        // ==================== Reconciliation Endpoints (via IReconciliationService) ====================

        [HttpPost("{id}/reconcile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reconcile(Guid id, [FromBody] ReconcileTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _reconciliationService.ReconcileTransactionAsync(id, dto);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        [HttpPost("{id}/unreconcile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Unreconcile(Guid id)
        {
            var result = await _reconciliationService.UnreconcileTransactionAsync(id);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        [HttpGet("{id}/reconciliation-suggestions")]
        [ProducesResponseType(typeof(IEnumerable<ReconciliationSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReconciliationSuggestions(Guid id, [FromQuery] decimal tolerance = 1000m, [FromQuery] int maxResults = 10)
        {
            var result = await _reconciliationService.GetReconciliationSuggestionsAsync(id, tolerance, maxResults);
            return HandleResult(result);
        }

        [HttpGet("search-payments")]
        [ProducesResponseType(typeof(IEnumerable<ReconciliationSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SearchPayments([FromQuery] Guid companyId, [FromQuery] string? searchTerm = null, [FromQuery] decimal? amountMin = null, [FromQuery] decimal? amountMax = null, [FromQuery] int maxResults = 20)
        {
            var result = await _reconciliationService.SearchPaymentsAsync(companyId, searchTerm, amountMin, amountMax, maxResults);
            return HandleResult(result);
        }

        [HttpGet("{id}/debit-reconciliation-suggestions")]
        [ProducesResponseType(typeof(IEnumerable<DebitReconciliationSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDebitReconciliationSuggestions(Guid id, [FromQuery] decimal tolerance = 1000m, [FromQuery] int maxResults = 10)
        {
            var result = await _reconciliationService.GetDebitReconciliationSuggestionsAsync(id, tolerance, maxResults);
            return HandleResult(result);
        }

        [HttpPost("search-reconciliation-candidates")]
        [ProducesResponseType(typeof(PagedResponse<DebitReconciliationSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SearchReconciliationCandidates([FromBody] ReconciliationSearchRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _reconciliationService.SearchReconciliationCandidatesAsync(request);
            if (result.IsFailure) return HandleError(result.Error!);
            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<DebitReconciliationSuggestionDto>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpPost("by-account/{bankAccountId}/auto-reconcile")]
        [ProducesResponseType(typeof(AutoReconcileResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AutoReconcile(Guid bankAccountId, [FromQuery] int minMatchScore = 80, [FromQuery] decimal amountTolerance = 100m, [FromQuery] int dateTolerance = 3)
        {
            var result = await _reconciliationService.AutoReconcileAsync(bankAccountId, minMatchScore, amountTolerance, dateTolerance);
            return HandleResult(result);
        }

        /// <summary>
        /// Reconcile a bank transaction directly to a journal entry.
        /// Used for manual JE reconciliation (opening entries, adjustments) without source documents.
        /// </summary>
        [HttpPost("{id}/reconcile-to-journal")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReconcileToJournal(Guid id, [FromBody] ReconcileToJournalDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _reconciliationService.ReconcileToJournalAsync(id, dto);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        // ==================== Import Endpoints (via IBankStatementImportService) ====================

        [HttpPost("import")]
        [ProducesResponseType(typeof(ImportBankTransactionsResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Import([FromBody] ImportBankTransactionsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _importService.ImportTransactionsAsync(request);
            return HandleResult(result);
        }

        [HttpGet("by-batch/{batchId}")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByImportBatch(Guid batchId)
        {
            if (batchId == Guid.Empty) return BadRequest("Batch ID cannot be empty");
            var result = await _importService.GetByImportBatchIdAsync(batchId);
            return HandleResult(result);
        }

        [HttpDelete("batch/{batchId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteImportBatch(Guid batchId)
        {
            var result = await _importService.DeleteImportBatchAsync(batchId);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        // ==================== BRS Endpoints (via IBrsService) ====================

        [HttpGet("brs/{bankAccountId}")]
        [ProducesResponseType(typeof(BankReconciliationStatementDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GenerateBrs(Guid bankAccountId, [FromQuery] DateOnly? asOfDate = null)
        {
            var result = await _brsService.GenerateBrsAsync(bankAccountId, asOfDate ?? DateOnly.FromDateTime(DateTime.Today));
            return HandleResult(result);
        }

        /// <summary>
        /// Generate enhanced BRS with journal entry perspective for CA compliance.
        /// Includes ledger balance, TDS summary, and audit metrics.
        /// </summary>
        [HttpGet("brs/{bankAccountId}/enhanced")]
        [ProducesResponseType(typeof(EnhancedBrsReportDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GenerateEnhancedBrs(
            Guid bankAccountId,
            [FromQuery] DateOnly? asOfDate = null,
            [FromQuery] DateOnly? periodStart = null)
        {
            var result = await _brsService.GenerateEnhancedBrsAsync(
                bankAccountId,
                asOfDate ?? DateOnly.FromDateTime(DateTime.Today),
                periodStart);
            return HandleResult(result);
        }

        // ==================== Reversal Pairing Endpoints (via IReversalDetectionService) ====================

        [HttpGet("{id}/detect-reversal")]
        [ProducesResponseType(typeof(ReversalDetectionResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DetectReversal(Guid id)
        {
            var result = await _reversalService.DetectReversalAsync(id);
            return HandleResult(result);
        }

        [HttpGet("{id}/potential-originals")]
        [ProducesResponseType(typeof(IEnumerable<ReversalMatchSuggestionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FindPotentialOriginals(Guid id, [FromQuery] int maxDaysBack = 90, [FromQuery] int maxResults = 10)
        {
            var result = await _reversalService.FindPotentialOriginalsAsync(id, maxDaysBack, maxResults);
            return HandleResult(result);
        }

        [HttpPost("pair-reversal")]
        [ProducesResponseType(typeof(PairReversalResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> PairReversal([FromBody] PairReversalRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _reversalService.PairReversalAsync(request);
            return HandleResult(result);
        }

        [HttpPost("{id}/unpair-reversal")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UnpairReversal(Guid id)
        {
            var result = await _reversalService.UnpairReversalAsync(id);
            return result.IsFailure ? HandleError(result.Error!) : NoContent();
        }

        [HttpGet("unpaired-reversals")]
        [ProducesResponseType(typeof(IEnumerable<BankTransaction>), 200)]
        public async Task<IActionResult> GetUnpairedReversals([FromQuery] Guid? bankAccountId = null)
        {
            var result = await _reversalService.GetUnpairedReversalsAsync(bankAccountId);
            return HandleResult(result);
        }

        // ==================== Outgoing Payments Endpoints (via IOutgoingPaymentsService) ====================

        [HttpGet("outgoing-payments")]
        [ProducesResponseType(typeof(PagedResponse<OutgoingPaymentDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOutgoingPayments(
            [FromQuery] Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? reconciled = null,
            [FromQuery] string? types = null,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null)
        {
            var typeList = string.IsNullOrEmpty(types) ? null : types.Split(',').ToList();
            var result = await _outgoingPaymentsService.GetOutgoingPaymentsAsync(companyId, pageNumber, pageSize, reconciled, typeList, fromDate, toDate);
            if (result.IsFailure) return HandleError(result.Error!);
            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<OutgoingPaymentDto>(items, totalCount, pageNumber, pageSize));
        }

        [HttpGet("outgoing-payments/summary")]
        [ProducesResponseType(typeof(OutgoingPaymentsSummaryDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOutgoingPaymentsSummary([FromQuery] Guid companyId, [FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
        {
            var result = await _outgoingPaymentsService.GetOutgoingPaymentsSummaryAsync(companyId, fromDate, toDate);
            return HandleResult(result);
        }

        // ==================== Helper Methods ====================

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsFailure) return HandleError(result.Error!);
            return Ok(result.Value);
        }

        private IActionResult HandleError(Error error)
        {
            return error.Type switch
            {
                ErrorType.Validation => BadRequest(error.Message),
                ErrorType.NotFound => NotFound(error.Message),
                ErrorType.Conflict => Conflict(error.Message),
                ErrorType.Internal => StatusCode(500, error.Message),
                _ => BadRequest(error.Message)
            };
        }
    }
}
