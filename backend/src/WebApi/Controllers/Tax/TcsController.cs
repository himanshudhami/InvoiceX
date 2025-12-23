using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// TCS (Tax Collected at Source) endpoints per Section 206C.
    /// Handles TCS calculation, collection on invoices, and remittance to government.
    /// Supports 206C(1H) for sale of goods > 50L and other TCS sections.
    /// </summary>
    [ApiController]
    [Route("api/tax/[controller]")]
    [Produces("application/json")]
    public class TcsController : ControllerBase
    {
        private readonly ITcsService _tcsService;
        private readonly ITcsTransactionRepository _repository;

        public TcsController(
            ITcsService tcsService,
            ITcsTransactionRepository repository)
        {
            _tcsService = tcsService ?? throw new ArgumentNullException(nameof(tcsService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ==================== CRUD Operations ====================

        /// <summary>
        /// Get TCS transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TcsTransaction), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
                return NotFound($"TCS transaction with ID {id} not found");

            return Ok(transaction);
        }

        /// <summary>
        /// Get paginated TCS transactions with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<TcsTransaction>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] TcsFilterRequest request)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());

            var response = new PagedResponse<TcsTransaction>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        // ==================== TCS Calculation ====================

        /// <summary>
        /// Calculate TCS amount for a transaction.
        /// Considers cumulative threshold for 206C(1H).
        /// </summary>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(TcsCalculationResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Calculate([FromBody] TcsCalculationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _tcsService.CalculateTcsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Check if TCS is applicable for a customer
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="customerPan">Customer PAN</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("is-applicable/{companyId}")]
        [ProducesResponseType(typeof(TcsApplicabilityResponse), 200)]
        public async Task<IActionResult> CheckApplicability(
            Guid companyId,
            [FromQuery] string customerPan,
            [FromQuery] string financialYear)
        {
            var isApplicable = await _tcsService.IsTcsApplicableAsync(companyId, customerPan, financialYear);
            var cumulativeValue = await _tcsService.GetCumulativeValueAsync(companyId, customerPan, financialYear);

            return Ok(new TcsApplicabilityResponse
            {
                IsApplicable = isApplicable,
                CustomerPan = customerPan,
                FinancialYear = financialYear,
                CumulativeValue = cumulativeValue,
                ThresholdAmount = 5000000 // 50L for 206C(1H)
            });
        }

        /// <summary>
        /// Get cumulative transaction value for threshold check
        /// </summary>
        [HttpGet("cumulative-value/{companyId}")]
        [ProducesResponseType(typeof(decimal), 200)]
        public async Task<IActionResult> GetCumulativeValue(
            Guid companyId,
            [FromQuery] string customerPan,
            [FromQuery] string financialYear)
        {
            var value = await _tcsService.GetCumulativeValueAsync(companyId, customerPan, financialYear);
            return Ok(new { CumulativeValue = value });
        }

        // ==================== TCS Collection (On Invoice) ====================

        /// <summary>
        /// Post TCS collection entry when invoice is raised
        /// </summary>
        [HttpPost("collect")]
        [ProducesResponseType(typeof(TcsPostingResult), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostCollection([FromBody] TcsCollectionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _tcsService.PostTcsCollectionAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result);
        }

        /// <summary>
        /// Post TCS collection from existing invoice
        /// </summary>
        [HttpPost("collect/from-invoice/{invoiceId}")]
        [ProducesResponseType(typeof(TcsPostingResult), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostCollectionFromInvoice(Guid invoiceId)
        {
            var result = await _tcsService.PostTcsFromInvoiceAsync(invoiceId);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result);
        }

        // ==================== TCS Remittance ====================

        /// <summary>
        /// Post TCS remittance to government for a single transaction
        /// </summary>
        [HttpPost("{id}/remit")]
        [ProducesResponseType(typeof(TcsPostingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostRemittance(Guid id, [FromBody] TcsRemittanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _tcsService.PostTcsRemittanceAsync(id, request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        /// <summary>
        /// Bulk TCS remittance for multiple transactions
        /// </summary>
        [HttpPost("remit/bulk")]
        [ProducesResponseType(typeof(TcsBulkRemittanceResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostBulkRemittance([FromBody] BulkRemittanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _tcsService.PostBulkRemittanceAsync(
                request.TransactionIds,
                request.RemittanceDetails);

            return Ok(result);
        }

        // ==================== TCS Received (When we buy) ====================

        /// <summary>
        /// Record TCS paid when we are the buyer
        /// </summary>
        [HttpPost("received")]
        [ProducesResponseType(typeof(TcsPostingResult), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RecordTcsPaid([FromBody] TcsPaidRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _tcsService.RecordTcsPaidAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result);
        }

        // ==================== Query Endpoints ====================

        /// <summary>
        /// Get TCS transactions by company
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            var transactions = await _repository.GetByCompanyAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS transactions pending remittance
        /// </summary>
        [HttpGet("pending-remittance/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetPendingRemittance(Guid companyId)
        {
            var transactions = await _tcsService.GetPendingRemittanceAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS transactions pending remittance (alias for frontend compatibility)
        /// </summary>
        [HttpGet("pending/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetPending(Guid companyId)
        {
            var transactions = await _tcsService.GetPendingRemittanceAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS transactions by party PAN
        /// </summary>
        [HttpGet("by-party/{companyId}/{partyPan}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetByParty(Guid companyId, string partyPan)
        {
            var transactions = await _repository.GetByPartyAsync(companyId, partyPan);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS collected (when we are seller)
        /// </summary>
        [HttpGet("collected/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetCollected(Guid companyId, [FromQuery] string? quarter = null)
        {
            var transactions = await _repository.GetCollectedAsync(companyId, quarter);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS paid (when we are buyer)
        /// </summary>
        [HttpGet("paid/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetPaid(Guid companyId, [FromQuery] string? quarter = null)
        {
            var transactions = await _repository.GetPaidAsync(companyId, quarter);
            return Ok(transactions);
        }

        /// <summary>
        /// Get TCS transactions by status
        /// </summary>
        [HttpGet("by-status/{companyId}/{status}")]
        [ProducesResponseType(typeof(IEnumerable<TcsTransaction>), 200)]
        public async Task<IActionResult> GetByStatus(Guid companyId, string status)
        {
            var transactions = await _repository.GetByStatusAsync(companyId, status);
            return Ok(transactions);
        }

        // ==================== Summary & Reports ====================

        /// <summary>
        /// Get TCS summary for a financial year (with optional quarter)
        /// </summary>
        [HttpGet("summary/{companyId}/{financialYear}")]
        [ProducesResponseType(typeof(TcsLiabilityReport), 200)]
        public async Task<IActionResult> GetSummary(
            Guid companyId,
            string financialYear,
            [FromQuery] string? quarter = null)
        {
            if (!string.IsNullOrEmpty(quarter))
            {
                var summary = await _tcsService.GetQuarterlySummaryAsync(companyId, financialYear, quarter);
                return Ok(summary);
            }

            // Return aggregated summary for full year
            var liabilityReport = await GetLiabilityReportInternal(companyId, financialYear);
            return Ok(liabilityReport);
        }

        /// <summary>
        /// Get quarterly TCS summary
        /// </summary>
        [HttpGet("summary/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(TcsQuarterlySummary), 200)]
        public async Task<IActionResult> GetQuarterlySummary(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            var summary = await _tcsService.GetQuarterlySummaryAsync(companyId, financialYear, quarter);
            return Ok(summary);
        }

        /// <summary>
        /// Get TCS liability report for a financial year
        /// </summary>
        [HttpGet("liability/{companyId}/{financialYear}")]
        [ProducesResponseType(typeof(TcsLiabilityReport), 200)]
        public async Task<IActionResult> GetLiabilityReport(
            Guid companyId,
            string financialYear)
        {
            var report = await GetLiabilityReportInternal(companyId, financialYear);
            return Ok(report);
        }

        private async Task<TcsLiabilityReport> GetLiabilityReportInternal(Guid companyId, string financialYear)
        {
            var transactions = await _repository.GetByCompanyAsync(companyId);
            var fyTransactions = transactions.Where(t => t.FinancialYear == financialYear).ToList();

            var collected = fyTransactions.Sum(t => t.TcsAmount);
            var remitted = fyTransactions.Where(t => t.Status == TcsTransactionStatus.Remitted || t.Status == TcsTransactionStatus.Filed).Sum(t => t.TcsAmount);
            var pending = collected - remitted;

            // Determine current quarter due date
            var now = DateTime.UtcNow;
            var currentQuarter = GetCurrentQuarter(now);
            var dueDate = GetQuarterDueDate(financialYear, currentQuarter);

            return new TcsLiabilityReport
            {
                Collected = collected,
                Remitted = remitted,
                Pending = pending,
                DueDate = dueDate.ToString("yyyy-MM-dd"),
                IsOverdue = pending > 0 && now > dueDate
            };
        }

        private static string GetCurrentQuarter(DateTime date)
        {
            return date.Month switch
            {
                >= 4 and <= 6 => "Q1",
                >= 7 and <= 9 => "Q2",
                >= 10 and <= 12 => "Q3",
                _ => "Q4"
            };
        }

        private static DateTime GetQuarterDueDate(string financialYear, string quarter)
        {
            // Parse financial year (e.g., "2024-25" -> 2024)
            var startYear = int.Parse(financialYear.Split('-')[0]);

            return quarter switch
            {
                "Q1" => new DateTime(startYear, 7, 7),      // Due 7th July
                "Q2" => new DateTime(startYear, 10, 7),    // Due 7th Oct
                "Q3" => new DateTime(startYear + 1, 1, 7), // Due 7th Jan
                "Q4" => new DateTime(startYear + 1, 5, 30), // Due 30th May (annual)
                _ => new DateTime(startYear + 1, 5, 30)
            };
        }

        /// <summary>
        /// Get Form 27EQ data for quarterly filing
        /// </summary>
        [HttpGet("form-27eq/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(TcsQuarterlySummary), 200)]
        public async Task<IActionResult> GetForm27EqData(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            var data = await _tcsService.GetForm27EqDataAsync(companyId, financialYear, quarter);
            return Ok(data);
        }

        /// <summary>
        /// Mark transactions as filed in Form 27EQ
        /// </summary>
        [HttpPost("mark-filed")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> MarkForm27EqFiled([FromBody] MarkFiledRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _tcsService.MarkForm27EqFiledAsync(request.TransactionIds, request.Acknowledgement);
            return NoContent();
        }

        // ==================== Section Configuration ====================

        /// <summary>
        /// Get all TCS sections
        /// </summary>
        [HttpGet("sections")]
        [ProducesResponseType(typeof(IEnumerable<TcsSectionInfo>), 200)]
        public async Task<IActionResult> GetAllSections()
        {
            var sections = await _tcsService.GetAllSectionsAsync();
            return Ok(sections);
        }

        /// <summary>
        /// Get TCS section by code
        /// </summary>
        [HttpGet("sections/{sectionCode}")]
        [ProducesResponseType(typeof(TcsSectionInfo), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSectionInfo(string sectionCode)
        {
            var section = await _tcsService.GetSectionInfoAsync(sectionCode);
            if (section == null)
                return NotFound($"TCS section '{sectionCode}' not found");

            return Ok(section);
        }

        // ==================== Reversal ====================

        /// <summary>
        /// Reverse a TCS collection entry
        /// </summary>
        [HttpPost("{id}/reverse")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReverseCollection(Guid id, [FromBody] TcsReversalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var journal = await _tcsService.ReverseCollectionAsync(id, request.ReversedBy, request.Reason);
            if (journal == null)
                return BadRequest("Failed to reverse TCS collection");

            return Ok(new { JournalEntryId = journal.Id });
        }
    }

    // ==================== Request/Response DTOs ====================

    /// <summary>
    /// Filter request for TCS transactions
    /// </summary>
    public class TcsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public Guid? CompanyId { get; set; }
        public string? SectionCode { get; set; }
        public string? Status { get; set; }
        public string? FinancialYear { get; set; }
        public string? Quarter { get; set; }
        public Guid? CustomerId { get; set; }
        public bool? IsCollected { get; set; }

        public Dictionary<string, object?> GetFilters()
        {
            var filters = new Dictionary<string, object?>();
            if (CompanyId.HasValue)
                filters["company_id"] = CompanyId.Value;
            if (!string.IsNullOrWhiteSpace(SectionCode))
                filters["section_code"] = SectionCode;
            if (!string.IsNullOrWhiteSpace(Status))
                filters["status"] = Status;
            if (!string.IsNullOrWhiteSpace(FinancialYear))
                filters["financial_year"] = FinancialYear;
            if (!string.IsNullOrWhiteSpace(Quarter))
                filters["quarter"] = Quarter;
            if (CustomerId.HasValue)
                filters["customer_id"] = CustomerId.Value;
            if (IsCollected.HasValue)
                filters["is_collected"] = IsCollected.Value;
            return filters;
        }
    }

    /// <summary>
    /// Response for TCS applicability check
    /// </summary>
    public class TcsApplicabilityResponse
    {
        public bool IsApplicable { get; set; }
        public string CustomerPan { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public decimal CumulativeValue { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal RemainingThreshold => Math.Max(0, ThresholdAmount - CumulativeValue);
    }

    /// <summary>
    /// Request for bulk remittance
    /// </summary>
    public class BulkRemittanceRequest
    {
        public IEnumerable<Guid> TransactionIds { get; set; } = new List<Guid>();
        public TcsRemittanceRequest RemittanceDetails { get; set; } = new();
    }

    /// <summary>
    /// Request for marking transactions as filed
    /// </summary>
    public class MarkFiledRequest
    {
        public IEnumerable<Guid> TransactionIds { get; set; } = new List<Guid>();
        public string Acknowledgement { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for TCS reversal
    /// </summary>
    public class TcsReversalRequest
    {
        public Guid ReversedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// TCS liability report for a financial year
    /// </summary>
    public class TcsLiabilityReport
    {
        public decimal Collected { get; set; }
        public decimal Remitted { get; set; }
        public decimal Pending { get; set; }
        public string DueDate { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
    }
}
