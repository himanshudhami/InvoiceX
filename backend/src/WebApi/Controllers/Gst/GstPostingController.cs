using Core.Interfaces.Gst;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Gst
{
    /// <summary>
    /// GST Posting operations for ITC Blocked, Credit/Debit Notes, ITC Reversal, UTGST, and GST TDS/TCS.
    ///
    /// Endpoints follow Indian GST compliance requirements:
    /// - Section 17(5) CGST Act - Blocked ITC
    /// - Rule 42/43 CGST Rules - ITC Reversal
    /// - Section 51 - GST TDS
    /// - Section 52 - GST TCS
    /// </summary>
    [ApiController]
    [Route("api/gst/[controller]")]
    [Produces("application/json")]
    public class GstPostingController : ControllerBase
    {
        private readonly IGstPostingService _gstPostingService;

        public GstPostingController(IGstPostingService gstPostingService)
        {
            _gstPostingService = gstPostingService ?? throw new ArgumentNullException(nameof(gstPostingService));
        }

        // ==================== ITC Blocked (Section 17(5)) ====================

        /// <summary>
        /// Get all blocked ITC categories under Section 17(5).
        /// Returns motor vehicles, food/beverages, health/fitness, construction, etc.
        /// </summary>
        [HttpGet("itc-blocked/categories")]
        [ProducesResponseType(typeof(IEnumerable<ItcBlockedCategory>), 200)]
        public async Task<IActionResult> GetBlockedCategories()
        {
            var categories = await _gstPostingService.GetBlockedCategoriesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Check if ITC is blocked for a given expense/HSN code.
        /// Use this before posting an expense to determine ITC eligibility.
        /// </summary>
        [HttpPost("itc-blocked/check")]
        [ProducesResponseType(typeof(ItcBlockedCheckResult), 200)]
        public async Task<IActionResult> CheckItcBlocked([FromBody] ItcBlockedCheckRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.CheckItcBlockedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Post ITC blocked journal entry.
        /// Transfers GST from input accounts to ITC Blocked account (1149).
        /// </summary>
        [HttpPost("itc-blocked")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostItcBlocked([FromBody] ItcBlockedRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostItcBlockedAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        /// <summary>
        /// Get ITC blocked summary for a return period.
        /// Shows category-wise breakdown of blocked ITC.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="returnPeriod">Return period (MMYYYY format, e.g., "032025" for March 2025)</param>
        [HttpGet("itc-blocked/summary/{companyId}/{returnPeriod}")]
        [ProducesResponseType(typeof(ItcBlockedSummary), 200)]
        public async Task<IActionResult> GetItcBlockedSummary(Guid companyId, string returnPeriod)
        {
            var summary = await _gstPostingService.GetItcBlockedSummaryAsync(companyId, returnPeriod);
            return Ok(summary);
        }

        // ==================== Credit/Debit Notes ====================

        /// <summary>
        /// Post GST adjustment for credit note.
        /// Reduces GST output liability and reverses ITC claimed.
        /// </summary>
        [HttpPost("credit-note")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostCreditNoteGst([FromBody] CreditNoteGstRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostCreditNoteGstAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        /// <summary>
        /// Post GST adjustment for debit note.
        /// Increases GST output liability and adds ITC.
        /// </summary>
        [HttpPost("debit-note")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostDebitNoteGst([FromBody] DebitNoteGstRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostDebitNoteGstAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== ITC Reversal (Rule 42/43) ====================

        /// <summary>
        /// Calculate ITC reversal amount per Rule 42 or Rule 43.
        /// Rule 42: Common credit reversal based on exempt/taxable turnover ratio.
        /// Rule 43: Capital goods reversal based on exempt use percentage.
        /// </summary>
        [HttpPost("itc-reversal/calculate")]
        [ProducesResponseType(typeof(ItcReversalCalculation), 200)]
        public async Task<IActionResult> CalculateItcReversal([FromBody] ItcReversalCalculationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var calculation = await _gstPostingService.CalculateItcReversalAsync(request);
            return Ok(calculation);
        }

        /// <summary>
        /// Post ITC reversal journal entry per Rule 42/43.
        /// Debits ITC Reversal Expense and credits GST Input accounts.
        /// </summary>
        [HttpPost("itc-reversal")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostItcReversal([FromBody] ItcReversalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostItcReversalAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== UTGST ====================

        /// <summary>
        /// Post UTGST entry for Union Territory transactions.
        /// Union Territories: Andaman & Nicobar, Chandigarh, Dadra & Nagar Haveli,
        /// Daman & Diu, Lakshadweep, Ladakh, Jammu & Kashmir.
        /// </summary>
        [HttpPost("utgst")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostUtgst([FromBody] UtgstRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostUtgstAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== GST TDS (Section 51) ====================

        /// <summary>
        /// Post GST TDS deducted by specified deductors (Section 51).
        /// Applicable for government bodies, PSUs, and local authorities.
        /// Rate: 2% (1% CGST + 1% SGST/UTGST or 2% IGST) on taxable value > Rs.2.5 lakhs.
        /// </summary>
        [HttpPost("tds")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostGstTds([FromBody] GstTdsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostGstTdsAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== GST TCS (Section 52) ====================

        /// <summary>
        /// Post GST TCS collected by e-commerce operators (Section 52).
        /// Rate: 1% (0.5% CGST + 0.5% SGST/UTGST or 1% IGST) on net value of taxable supplies.
        /// </summary>
        [HttpPost("tcs")]
        [ProducesResponseType(typeof(GstPostingResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostGstTcs([FromBody] GstTcsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gstPostingService.PostGstTcsAsync(request);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Ok(result);
        }

        // ==================== ITC Summary & Reports ====================

        /// <summary>
        /// Get ITC availability report for a return period.
        /// Shows total ITC availed, blocked, reversed, and net available.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="returnPeriod">Return period (MMYYYY format)</param>
        [HttpGet("itc-report/{companyId}/{returnPeriod}")]
        [ProducesResponseType(typeof(ItcAvailabilityReport), 200)]
        public async Task<IActionResult> GetItcAvailabilityReport(Guid companyId, string returnPeriod)
        {
            var report = await _gstPostingService.GetItcAvailabilityReportAsync(companyId, returnPeriod);
            return Ok(report);
        }
    }
}
