using Core.Entities.Gst;
using Core.Interfaces.Gst;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Gst
{
    /// <summary>
    /// RCM (Reverse Charge Mechanism) endpoints for GST compliance.
    /// Handles two-stage RCM posting: liability recognition and payment + ITC claim.
    /// As per Section 9(3), 9(4) of CGST Act and Notification 13/2017.
    /// </summary>
    [ApiController]
    [Route("api/gst/[controller]")]
    [Produces("application/json")]
    public class RcmController : ControllerBase
    {
        private readonly IRcmPostingService _rcmService;
        private readonly IRcmTransactionRepository _repository;

        public RcmController(
            IRcmPostingService rcmService,
            IRcmTransactionRepository repository)
        {
            _rcmService = rcmService ?? throw new ArgumentNullException(nameof(rcmService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ==================== CRUD Operations ====================

        /// <summary>
        /// Get RCM transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RcmTransaction), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
                return NotFound($"RCM transaction with ID {id} not found");

            return Ok(transaction);
        }

        /// <summary>
        /// Get paginated RCM transactions with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<RcmTransaction>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] RcmFilterRequest request)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());

            var response = new PagedResponse<RcmTransaction>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        // ==================== Stage 1: Liability Recognition ====================

        /// <summary>
        /// Post RCM liability entry (Stage 1).
        /// Creates liability for RCM taxes on expense/purchase.
        /// </summary>
        /// <param name="request">RCM liability details</param>
        /// <returns>Created RCM transaction with journal entry</returns>
        [HttpPost("liability")]
        [ProducesResponseType(typeof(RcmPostingResult), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostLiability([FromBody] RcmLiabilityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rcmService.PostRcmLiabilityAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result);
        }

        /// <summary>
        /// Post RCM liability from existing expense claim
        /// </summary>
        /// <param name="expenseClaimId">The expense claim ID</param>
        [HttpPost("liability/from-expense/{expenseClaimId}")]
        [ProducesResponseType(typeof(RcmPostingResult), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostLiabilityFromExpense(Guid expenseClaimId)
        {
            var result = await _rcmService.PostRcmLiabilityFromExpenseAsync(expenseClaimId);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result);
        }

        // ==================== Stage 2: RCM Payment + ITC ====================

        /// <summary>
        /// Post RCM payment to government and claim ITC (Stage 2).
        /// RCM must be paid before ITC can be claimed.
        /// </summary>
        /// <param name="id">RCM transaction ID</param>
        /// <param name="request">Payment details</param>
        [HttpPost("{id}/payment")]
        [ProducesResponseType(typeof(RcmPostingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostPayment(Guid id, [FromBody] RcmPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rcmService.PostRcmPaymentAsync(id, request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        /// <summary>
        /// Claim ITC for RCM already paid (if done separately from payment)
        /// </summary>
        /// <param name="id">RCM transaction ID</param>
        /// <param name="request">ITC claim details</param>
        [HttpPost("{id}/claim-itc")]
        [ProducesResponseType(typeof(RcmPostingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ClaimItc(Guid id, [FromBody] ItcClaimRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rcmService.ClaimItcAsync(id, request.ClaimPeriod);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        /// <summary>
        /// Block ITC per Section 17(5) CGST Act
        /// </summary>
        /// <param name="id">RCM transaction ID</param>
        /// <param name="request">Block reason</param>
        [HttpPost("{id}/block-itc")]
        [ProducesResponseType(typeof(RcmPostingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> BlockItc(Guid id, [FromBody] ItcBlockRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rcmService.BlockItcAsync(id, request.BlockReason);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== Query Endpoints ====================

        /// <summary>
        /// Get RCM transactions by company
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<RcmTransaction>), 200)]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            var transactions = await _repository.GetByCompanyAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get RCM transactions pending payment
        /// </summary>
        [HttpGet("pending-payments/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<RcmTransaction>), 200)]
        public async Task<IActionResult> GetPendingPayments(Guid companyId)
        {
            var transactions = await _rcmService.GetPendingPaymentsAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get RCM transactions paid but ITC not yet claimed
        /// </summary>
        [HttpGet("pending-itc/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<RcmTransaction>), 200)]
        public async Task<IActionResult> GetPendingItcClaims(Guid companyId)
        {
            var transactions = await _rcmService.GetPendingItcClaimsAsync(companyId);
            return Ok(transactions);
        }

        /// <summary>
        /// Get RCM transactions by status
        /// </summary>
        [HttpGet("by-status/{companyId}/{status}")]
        [ProducesResponseType(typeof(IEnumerable<RcmTransaction>), 200)]
        public async Task<IActionResult> GetByStatus(Guid companyId, string status)
        {
            var transactions = await _repository.GetByStatusAsync(companyId, status);
            return Ok(transactions);
        }

        /// <summary>
        /// Get RCM transactions by return period
        /// </summary>
        [HttpGet("by-period/{companyId}/{returnPeriod}")]
        [ProducesResponseType(typeof(IEnumerable<RcmTransaction>), 200)]
        public async Task<IActionResult> GetByPeriod(Guid companyId, string returnPeriod)
        {
            var transactions = await _repository.GetByCompanyAsync(companyId, returnPeriod);
            return Ok(transactions);
        }

        /// <summary>
        /// Get RCM summary for GSTR-3B Table 3.1(d)
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="returnPeriod">Return period (e.g., "Jan-2025")</param>
        [HttpGet("summary/{companyId}/{returnPeriod}")]
        [ProducesResponseType(typeof(RcmPeriodSummary), 200)]
        public async Task<IActionResult> GetPeriodSummary(Guid companyId, string returnPeriod)
        {
            var summary = await _rcmService.GetPeriodSummaryAsync(companyId, returnPeriod);
            return Ok(summary);
        }

        // ==================== Validation ====================

        /// <summary>
        /// Validate if expense is subject to RCM
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(RcmValidationResult), 200)]
        public async Task<IActionResult> ValidateRcmApplicability([FromBody] RcmValidationRequest request)
        {
            var result = await _rcmService.ValidateRcmApplicabilityAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get RCM category information
        /// </summary>
        /// <param name="hsnSacCode">HSN/SAC code</param>
        /// <param name="vendorGstin">Optional vendor GSTIN</param>
        [HttpGet("category/{hsnSacCode}")]
        [ProducesResponseType(typeof(RcmCategoryInfo), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRcmCategory(string hsnSacCode, [FromQuery] string? vendorGstin = null)
        {
            var category = await _rcmService.GetRcmCategoryAsync(hsnSacCode, vendorGstin);
            if (category == null)
                return NotFound($"RCM category not found for HSN/SAC code: {hsnSacCode}");

            return Ok(category);
        }

        // ==================== Reversal ====================

        /// <summary>
        /// Reverse RCM liability entry (before payment)
        /// </summary>
        [HttpPost("{id}/reverse-liability")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReverseLiability(Guid id, [FromBody] ReversalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var journal = await _rcmService.ReverseLiabilityAsync(id, request.ReversedBy, request.Reason);
            if (journal == null)
                return BadRequest("Failed to reverse liability");

            return Ok(new { JournalEntryId = journal.Id });
        }

        /// <summary>
        /// Reverse RCM payment entry
        /// </summary>
        [HttpPost("{id}/reverse-payment")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReversePayment(Guid id, [FromBody] ReversalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var journal = await _rcmService.ReversePaymentAsync(id, request.ReversedBy, request.Reason);
            if (journal == null)
                return BadRequest("Failed to reverse payment");

            return Ok(new { JournalEntryId = journal.Id });
        }
    }

    // ==================== Request DTOs ====================

    /// <summary>
    /// Filter request for RCM transactions
    /// </summary>
    public class RcmFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public Guid? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? RcmCategory { get; set; }
        public string? ReturnPeriod { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public bool? ItcClaimed { get; set; }

        public Dictionary<string, object?> GetFilters()
        {
            var filters = new Dictionary<string, object?>();
            if (CompanyId.HasValue)
                filters["company_id"] = CompanyId.Value;
            if (!string.IsNullOrWhiteSpace(Status))
                filters["status"] = Status;
            if (!string.IsNullOrWhiteSpace(RcmCategory))
                filters["rcm_category"] = RcmCategory;
            if (!string.IsNullOrWhiteSpace(ReturnPeriod))
                filters["return_period"] = ReturnPeriod;
            if (ItcClaimed.HasValue)
                filters["itc_claimed"] = ItcClaimed.Value;
            return filters;
        }
    }

    /// <summary>
    /// Request for ITC claim
    /// </summary>
    public class ItcClaimRequest
    {
        public string ClaimPeriod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for ITC block
    /// </summary>
    public class ItcBlockRequest
    {
        public string BlockReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for reversal
    /// </summary>
    public class ReversalRequest
    {
        public Guid ReversedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
