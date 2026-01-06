using Core.Common;
using Core.Interfaces.Statutory;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Statutory
{
    /// <summary>
    /// TDS Challan 281 management endpoints.
    /// Handles challan generation, deposit recording, and reconciliation.
    ///
    /// Challan 281 is used for depositing TDS to the government.
    /// Due dates: 7th of following month (30th April for March)
    /// Late payment interest: 1.5% per month
    /// </summary>
    [ApiController]
    [Route("api/statutory/tds-challan")]
    [Produces("application/json")]
    public class TdsChallanController : ControllerBase
    {
        private readonly ITdsChallanService _challanService;

        public TdsChallanController(ITdsChallanService challanService)
        {
            _challanService = challanService ?? throw new ArgumentNullException(nameof(challanService));
        }

        // ==================== Challan Generation ====================

        /// <summary>
        /// Generate TDS challan data for a month.
        /// Aggregates TDS from payroll and contractor payments.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month (1-12)</param>
        /// <param name="periodYear">Period year</param>
        /// <param name="challanType">Challan type: "salary" (Section 192) or "non-salary" (Form 26Q)</param>
        [HttpGet("{companyId}/generate/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(TdsChallanData), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateChallan(
            Guid companyId,
            int periodMonth,
            int periodYear,
            [FromQuery] string challanType = "salary")
        {
            var result = await _challanService.GenerateChallanAsync(
                companyId, periodMonth, periodYear, challanType);

            return HandleResult(result);
        }

        /// <summary>
        /// Get monthly TDS challan summary for a financial year.
        /// Shows status of all 12 months.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/summary/{financialYear}")]
        [ProducesResponseType(typeof(TdsChalllanSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetMonthlySummary(
            Guid companyId,
            string financialYear)
        {
            var result = await _challanService.GetMonthlySummaryAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Preview challan before creating payment record.
        /// Includes interest calculation if depositing late.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        /// <param name="challanType">Challan type</param>
        /// <param name="paymentDate">Proposed payment date (optional, defaults to today)</param>
        [HttpGet("{companyId}/preview/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(TdsChallanPreview), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PreviewChallan(
            Guid companyId,
            int periodMonth,
            int periodYear,
            [FromQuery] string challanType = "salary",
            [FromQuery] DateOnly? paymentDate = null)
        {
            var result = await _challanService.PreviewChallanAsync(
                companyId, periodMonth, periodYear, challanType, paymentDate);

            return HandleResult(result);
        }

        // ==================== Challan Operations ====================

        /// <summary>
        /// Create statutory payment record for TDS challan.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateChallanPayment([FromBody] CreateTdsChallanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _challanService.CreateChallanPaymentAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Record challan deposit with bank/BSR details.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="request">Deposit details</param>
        [HttpPost("{paymentId}/deposit")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> RecordDeposit(
            Guid paymentId,
            [FromBody] RecordChallanDepositRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _challanService.RecordChallanDepositAsync(paymentId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Update CIN (Challan Identification Number) after bank confirmation.
        /// CIN format: BSR Code (7 digits) + Date (DDMMYYYY) + Serial Number (5 digits)
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="request">CIN update details</param>
        [HttpPost("{paymentId}/update-cin")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCin(
            Guid paymentId,
            [FromBody] UpdateCinRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _challanService.UpdateCinAsync(paymentId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Verify challan against OLTAS/TIN-NSDL.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="request">Verification details</param>
        [HttpPost("{paymentId}/verify")]
        [ProducesResponseType(typeof(ChallanVerificationResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> VerifyChallan(
            Guid paymentId,
            [FromBody] ChallanVerificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _challanService.VerifyChallanAsync(paymentId, request);
            return HandleResult(result);
        }

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of TDS challans.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        /// <param name="status">Optional: Filter by status (pending, paid, overdue)</param>
        /// <param name="searchTerm">Optional: Search by challan/CIN number</param>
        [HttpGet("{companyId}/paged")]
        [ProducesResponseType(typeof(PagedTdsChallanResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _challanService.GetPagedAsync(
                companyId, pageNumber, pageSize, financialYear, status, searchTerm);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            var (items, totalCount) = result.Value;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new PagedTdsChallanResponse
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Get pending TDS challans (unpaid).
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        [HttpGet("{companyId}/pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingChallanDto>), 200)]
        public async Task<IActionResult> GetPendingChallans(
            Guid companyId,
            [FromQuery] string? financialYear = null)
        {
            var result = await _challanService.GetPendingChallansAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get overdue TDS challans with interest calculation.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        [HttpGet("{companyId}/overdue")]
        [ProducesResponseType(typeof(IEnumerable<OverdueChallanDto>), 200)]
        public async Task<IActionResult> GetOverdueChallans(Guid companyId)
        {
            var result = await _challanService.GetOverdueChallansAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get paid TDS challans for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        /// <param name="quarter">Optional: Filter by quarter (Q1-Q4)</param>
        [HttpGet("{companyId}/paid/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<PaidChallanDto>), 200)]
        public async Task<IActionResult> GetPaidChallans(
            Guid companyId,
            string financialYear,
            [FromQuery] string? quarter = null)
        {
            var result = await _challanService.GetPaidChallansAsync(companyId, financialYear, quarter);
            return HandleResult(result);
        }

        /// <summary>
        /// Get challan details by ID.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        [HttpGet("detail/{paymentId}")]
        [ProducesResponseType(typeof(TdsChallanDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetChallanById(Guid paymentId)
        {
            var result = await _challanService.GetChallanByIdAsync(paymentId);
            return HandleResult(result);
        }

        // ==================== Utility ====================

        /// <summary>
        /// Calculate interest on late TDS payment.
        /// Rate: 1.5% per month (simple interest)
        /// </summary>
        /// <param name="tdsAmount">TDS amount</param>
        /// <param name="dueDate">Due date</param>
        /// <param name="paymentDate">Actual/proposed payment date</param>
        [HttpGet("calculate-interest")]
        [ProducesResponseType(typeof(InterestCalculationResult), 200)]
        public IActionResult CalculateInterest(
            [FromQuery] decimal tdsAmount,
            [FromQuery] DateOnly dueDate,
            [FromQuery] DateOnly paymentDate)
        {
            var interest = _challanService.CalculateInterest(tdsAmount, dueDate, paymentDate);

            return Ok(new InterestCalculationResult
            {
                TdsAmount = tdsAmount,
                DueDate = dueDate,
                PaymentDate = paymentDate,
                InterestAmount = interest,
                TotalPayable = tdsAmount + interest,
                DaysLate = Math.Max(0, paymentDate.DayNumber - dueDate.DayNumber),
                MonthsLate = Math.Max(0, (int)Math.Ceiling((paymentDate.DayNumber - dueDate.DayNumber) / 30.0))
            });
        }

        /// <summary>
        /// Get due date for TDS payment.
        /// </summary>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("due-date/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(DueDateResult), 200)]
        public IActionResult GetDueDate(int periodMonth, int periodYear)
        {
            var dueDate = _challanService.GetDueDate(periodMonth, periodYear);
            var today = DateOnly.FromDateTime(DateTime.Today);

            return Ok(new DueDateResult
            {
                PeriodMonth = periodMonth,
                PeriodYear = periodYear,
                PeriodName = new DateTime(periodYear, periodMonth, 1).ToString("MMM yyyy"),
                DueDate = dueDate,
                IsOverdue = today > dueDate,
                DaysUntilDue = Math.Max(0, dueDate.DayNumber - today.DayNumber),
                DaysOverdue = Math.Max(0, today.DayNumber - dueDate.DayNumber)
            });
        }

        // ==================== Reconciliation ====================

        /// <summary>
        /// Reconcile TDS deducted vs deposited for a period.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        /// <param name="quarter">Optional: Quarter (Q1-Q4)</param>
        [HttpGet("{companyId}/reconcile/{financialYear}")]
        [ProducesResponseType(typeof(TdsChallanReconciliation), 200)]
        public async Task<IActionResult> Reconcile(
            Guid companyId,
            string financialYear,
            [FromQuery] string? quarter = null)
        {
            var result = await _challanService.ReconcileAsync(companyId, financialYear, quarter);
            return HandleResult(result);
        }

        /// <summary>
        /// Link challan to payroll transactions.
        /// </summary>
        /// <param name="paymentId">Challan payment ID</param>
        /// <param name="transactionIds">Payroll transaction IDs</param>
        [HttpPost("{paymentId}/link-payroll")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> LinkToPayroll(
            Guid paymentId,
            [FromBody] IEnumerable<Guid> transactionIds)
        {
            var result = await _challanService.LinkChallanToPayrollAsync(paymentId, transactionIds);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        // ==================== Helper Methods ====================

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }
    }

    // ==================== Response DTOs ====================

    /// <summary>
    /// Paginated TDS challan response
    /// </summary>
    public class PagedTdsChallanResponse
    {
        public List<TdsChallanListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Interest calculation result
    /// </summary>
    public class InterestCalculationResult
    {
        public decimal TdsAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int DaysLate { get; set; }
        public int MonthsLate { get; set; }
    }

    /// <summary>
    /// Due date result
    /// </summary>
    public class DueDateResult
    {
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDue { get; set; }
        public int DaysOverdue { get; set; }
    }
}
