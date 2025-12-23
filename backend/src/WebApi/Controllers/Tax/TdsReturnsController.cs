using Core.Interfaces.Tax;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// TDS Returns preparation endpoints for Form 26Q (non-salary) and Form 24Q (salary).
    /// Provides data generation, validation, and filing support for quarterly TDS returns.
    /// </summary>
    [ApiController]
    [Route("api/tax/[controller]")]
    [Produces("application/json")]
    public class TdsReturnsController : ControllerBase
    {
        private readonly ITdsReturnService _tdsReturnService;

        public TdsReturnsController(ITdsReturnService tdsReturnService)
        {
            _tdsReturnService = tdsReturnService ?? throw new ArgumentNullException(nameof(tdsReturnService));
        }

        // ==================== Form 26Q (Non-Salary TDS) ====================

        /// <summary>
        /// Generate Form 26Q data for non-salary TDS return.
        /// Covers 194A (Interest), 194C (Contractors), 194H (Commission),
        /// 194I (Rent), 194J (Professional), etc.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        [HttpGet("26q/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(Form26QData), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetForm26Q(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var data = await _tdsReturnService.GenerateForm26QAsync(companyId, financialYear, quarter);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Validate Form 26Q data before filing.
        /// Checks for mandatory fields, PAN validation, challan reconciliation.
        /// </summary>
        [HttpGet("26q/{companyId}/{financialYear}/{quarter}/validate")]
        [ProducesResponseType(typeof(TdsReturnValidationResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ValidateForm26Q(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var result = await _tdsReturnService.ValidateForm26QAsync(companyId, financialYear, quarter);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Form 26Q summary with section-wise and month-wise breakdown.
        /// </summary>
        [HttpGet("26q/{companyId}/{financialYear}/{quarter}/summary")]
        [ProducesResponseType(typeof(Form26QSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetForm26QSummary(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var summary = await _tdsReturnService.GetForm26QSummaryAsync(companyId, financialYear, quarter);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== Form 24Q (Salary TDS) ====================

        /// <summary>
        /// Generate Form 24Q data for salary TDS return.
        /// Contains employee-wise salary and TDS details for the quarter.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        [HttpGet("24q/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(Form24QData), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetForm24Q(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var data = await _tdsReturnService.GenerateForm24QAsync(companyId, financialYear, quarter);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Validate Form 24Q data before filing.
        /// Checks employee PAN, date of joining, salary details.
        /// </summary>
        [HttpGet("24q/{companyId}/{financialYear}/{quarter}/validate")]
        [ProducesResponseType(typeof(TdsReturnValidationResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ValidateForm24Q(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var result = await _tdsReturnService.ValidateForm24QAsync(companyId, financialYear, quarter);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Form 24Q summary with employee count and month-wise breakdown.
        /// </summary>
        [HttpGet("24q/{companyId}/{financialYear}/{quarter}/summary")]
        [ProducesResponseType(typeof(Form24QSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetForm24QSummary(Guid companyId, string financialYear, string quarter)
        {
            try
            {
                var summary = await _tdsReturnService.GetForm24QSummaryAsync(companyId, financialYear, quarter);
                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Generate Form 24Q Annexure II (Q4 only).
        /// Contains full annual salary details for each employee.
        /// Required only for Q4 return.
        /// </summary>
        [HttpGet("24q/{companyId}/{financialYear}/annexure-ii")]
        [ProducesResponseType(typeof(Form24QAnnexureII), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetForm24QAnnexureII(Guid companyId, string financialYear)
        {
            try
            {
                var data = await _tdsReturnService.GenerateForm24QAnnexureIIAsync(companyId, financialYear);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== Challan Reconciliation ====================

        /// <summary>
        /// Get challan details for TDS deposits in a quarter.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        /// <param name="quarter">Quarter</param>
        /// <param name="formType">Optional: Filter by form type (26Q or 24Q)</param>
        [HttpGet("challans/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(IEnumerable<TdsChallanDetail>), 200)]
        public async Task<IActionResult> GetChallans(
            Guid companyId,
            string financialYear,
            string quarter,
            [FromQuery] string? formType = null)
        {
            var challans = await _tdsReturnService.GetChallanDetailsAsync(companyId, financialYear, quarter, formType);
            return Ok(challans);
        }

        /// <summary>
        /// Reconcile TDS deducted with challans deposited.
        /// Returns variance and mismatches if any.
        /// </summary>
        [HttpGet("challans/{companyId}/{financialYear}/{quarter}/reconcile")]
        [ProducesResponseType(typeof(ChallanReconciliationResult), 200)]
        public async Task<IActionResult> ReconcileChallans(Guid companyId, string financialYear, string quarter)
        {
            var result = await _tdsReturnService.ReconcileChallansAsync(companyId, financialYear, quarter);
            return Ok(result);
        }

        // ==================== Due Dates & Pending Returns ====================

        /// <summary>
        /// Get TDS return due dates for a financial year.
        /// Shows Q1-Q4 due dates for both Form 26Q and 24Q.
        /// </summary>
        [HttpGet("due-dates/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReturnDueDate>), 200)]
        public async Task<IActionResult> GetDueDates(string financialYear)
        {
            var dueDates = await _tdsReturnService.GetDueDatesAsync(financialYear);
            return Ok(dueDates);
        }

        /// <summary>
        /// Get pending/overdue TDS returns for a company.
        /// Shows returns due within next 30 days and overdue returns.
        /// </summary>
        [HttpGet("pending/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<PendingTdsReturn>), 200)]
        public async Task<IActionResult> GetPendingReturns(Guid companyId)
        {
            var pending = await _tdsReturnService.GetPendingReturnsAsync(companyId);
            return Ok(pending);
        }

        // ==================== Filing Status ====================

        /// <summary>
        /// Mark a TDS return as filed.
        /// Records acknowledgement number, token, and filing date.
        /// </summary>
        [HttpPost("mark-filed")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> MarkReturnFiled([FromBody] MarkReturnFiledRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _tdsReturnService.MarkReturnFiledAsync(request);
            return NoContent();
        }

        /// <summary>
        /// Get filing history for a company.
        /// Shows all filed returns with acknowledgement details.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        [HttpGet("filing-history/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReturnFilingHistory>), 200)]
        public async Task<IActionResult> GetFilingHistory(Guid companyId, [FromQuery] string? financialYear = null)
        {
            var history = await _tdsReturnService.GetFilingHistoryAsync(companyId, financialYear);
            return Ok(history);
        }

        // ==================== Combined Reports ====================

        /// <summary>
        /// Get combined TDS summary for both salary and non-salary.
        /// Useful for quarterly overview.
        /// </summary>
        [HttpGet("combined-summary/{companyId}/{financialYear}/{quarter}")]
        [ProducesResponseType(typeof(CombinedTdsSummary), 200)]
        public async Task<IActionResult> GetCombinedSummary(Guid companyId, string financialYear, string quarter)
        {
            var form26Q = await _tdsReturnService.GetForm26QSummaryAsync(companyId, financialYear, quarter);
            var form24Q = await _tdsReturnService.GetForm24QSummaryAsync(companyId, financialYear, quarter);

            var combined = new CombinedTdsSummary
            {
                FinancialYear = financialYear,
                Quarter = quarter,
                Form26Q = form26Q,
                Form24Q = form24Q,
                TotalTdsDeducted = form26Q.TotalTdsDeducted + form24Q.TotalTdsDeducted,
                TotalTdsDeposited = form26Q.TotalTdsDeposited + form24Q.TotalTdsDeposited,
                TotalVariance = form26Q.Variance + form24Q.Variance
            };

            return Ok(combined);
        }
    }

    // ==================== Response DTOs ====================

    /// <summary>
    /// Combined TDS summary for quarterly overview
    /// </summary>
    public class CombinedTdsSummary
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public Form26QSummary Form26Q { get; set; } = new();
        public Form24QSummary Form24Q { get; set; } = new();
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalVariance { get; set; }
    }
}
