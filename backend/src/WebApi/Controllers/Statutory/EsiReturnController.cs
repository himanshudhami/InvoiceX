using Core.Common;
using Core.Interfaces.Statutory;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Statutory
{
    /// <summary>
    /// ESI (Employee State Insurance) return management endpoints.
    /// Handles return generation, filing, and payment tracking for ESIC.
    ///
    /// Due date: 15th of following month
    /// Employee rate: 0.75%, Employer rate: 3.25%
    /// Ceiling: ₹21,000 gross/month
    /// Interest: 12% per annum on late payment
    /// </summary>
    [ApiController]
    [Route("api/statutory/esi-return")]
    [Produces("application/json")]
    public class EsiReturnController : ControllerBase
    {
        private readonly IEsiReturnService _returnService;

        public EsiReturnController(IEsiReturnService returnService)
        {
            _returnService = returnService ?? throw new ArgumentNullException(nameof(returnService));
        }

        // ==================== Return Generation ====================

        /// <summary>
        /// Generate ESI return data for a month.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month (1-12)</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/generate/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(EsiReturnData), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateReturn(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _returnService.GenerateReturnAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Generate ESI return from a specific payroll run.
        /// </summary>
        /// <param name="payrollRunId">Payroll run ID</param>
        [HttpGet("payroll-run/{payrollRunId}")]
        [ProducesResponseType(typeof(EsiReturnData), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateFromPayrollRun(Guid payrollRunId)
        {
            var result = await _returnService.GenerateReturnFromPayrollRunAsync(payrollRunId);
            return HandleResult(result);
        }

        /// <summary>
        /// Preview ESI return with interest calculation.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/preview/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(EsiReturnPreview), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PreviewReturn(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _returnService.PreviewReturnAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Generate ESI return file for ESIC portal upload.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/file/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(EsiReturnFileResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GenerateReturnFile(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _returnService.GenerateReturnFileAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Download ESI return file directly.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/download/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DownloadReturnFile(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _returnService.GenerateReturnFileAsync(companyId, periodMonth, periodYear);

            if (result.IsFailure)
                return HandleResult(result);

            var file = result.Value;
            var bytes = System.Text.Encoding.UTF8.GetBytes(file.FileContent);

            return File(bytes, "text/plain", file.FileName);
        }

        // ==================== Return Operations ====================

        /// <summary>
        /// Create statutory payment record for ESI return.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateReturnPayment([FromBody] CreateEsiReturnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _returnService.CreateReturnPaymentAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Record ESI payment after deposit.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="request">Payment details</param>
        [HttpPost("{paymentId}/payment")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> RecordPayment(
            Guid paymentId,
            [FromBody] RecordEsiPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _returnService.RecordPaymentAsync(paymentId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Update challan number after ESIC filing.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="challanNumber">ESIC challan number</param>
        [HttpPost("{paymentId}/challan")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateChallanNumber(
            Guid paymentId,
            [FromQuery] string challanNumber)
        {
            if (string.IsNullOrWhiteSpace(challanNumber))
                return BadRequest("Challan number is required");

            var result = await _returnService.UpdateChallanNumberAsync(paymentId, challanNumber);
            return HandleResult(result);
        }

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of ESI returns.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        /// <param name="status">Optional: Filter by status</param>
        /// <param name="searchTerm">Optional: Search by challan number</param>
        [HttpGet("{companyId}/paged")]
        [ProducesResponseType(typeof(PagedEsiReturnResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _returnService.GetPagedAsync(
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

            return Ok(new PagedEsiReturnResponse
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Get pending ESI returns.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        [HttpGet("{companyId}/pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingEsiReturnDto>), 200)]
        public async Task<IActionResult> GetPendingReturns(
            Guid companyId,
            [FromQuery] string? financialYear = null)
        {
            var result = await _returnService.GetPendingReturnsAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get filed ESI returns for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        [HttpGet("{companyId}/filed/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<FiledEsiReturnDto>), 200)]
        public async Task<IActionResult> GetFiledReturns(
            Guid companyId,
            string financialYear)
        {
            var result = await _returnService.GetFiledReturnsAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get monthly ESI summary for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/summary/{financialYear}")]
        [ProducesResponseType(typeof(EsiReturnSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetMonthlySummary(
            Guid companyId,
            string financialYear)
        {
            var result = await _returnService.GetMonthlySummaryAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get ESI return details by ID.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        [HttpGet("detail/{paymentId}")]
        [ProducesResponseType(typeof(EsiReturnDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReturnById(Guid paymentId)
        {
            var result = await _returnService.GetReturnByIdAsync(paymentId);
            return HandleResult(result);
        }

        // ==================== Utility ====================

        /// <summary>
        /// Calculate interest on late ESI payment.
        /// Rate: 12% per annum (simple interest)
        /// </summary>
        /// <param name="esiAmount">ESI amount</param>
        /// <param name="dueDate">Due date</param>
        /// <param name="paymentDate">Actual/proposed payment date</param>
        [HttpGet("calculate-interest")]
        [ProducesResponseType(typeof(EsiInterestCalculationResult), 200)]
        public IActionResult CalculateInterest(
            [FromQuery] decimal esiAmount,
            [FromQuery] DateOnly dueDate,
            [FromQuery] DateOnly paymentDate)
        {
            var interest = _returnService.CalculateInterest(esiAmount, dueDate, paymentDate);

            return Ok(new EsiInterestCalculationResult
            {
                EsiAmount = esiAmount,
                DueDate = dueDate,
                PaymentDate = paymentDate,
                InterestAmount = interest,
                TotalPayable = esiAmount + interest,
                DaysLate = Math.Max(0, paymentDate.DayNumber - dueDate.DayNumber)
            });
        }

        /// <summary>
        /// Get due date for ESI payment.
        /// </summary>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("due-date/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(EsiDueDateResult), 200)]
        public IActionResult GetDueDate(int periodMonth, int periodYear)
        {
            var dueDate = _returnService.GetDueDate(periodMonth, periodYear);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var contributionPeriod = _returnService.GetContributionPeriod(periodMonth, periodYear);

            return Ok(new EsiDueDateResult
            {
                PeriodMonth = periodMonth,
                PeriodYear = periodYear,
                PeriodName = new DateTime(periodYear, periodMonth, 1).ToString("MMM yyyy"),
                ContributionPeriod = contributionPeriod,
                DueDate = dueDate,
                IsOverdue = today > dueDate,
                DaysUntilDue = Math.Max(0, dueDate.DayNumber - today.DayNumber),
                DaysOverdue = Math.Max(0, today.DayNumber - dueDate.DayNumber)
            });
        }

        // ==================== Reconciliation ====================

        /// <summary>
        /// Reconcile ESI deducted vs deposited for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        [HttpGet("{companyId}/reconcile/{financialYear}")]
        [ProducesResponseType(typeof(EsiReconciliation), 200)]
        public async Task<IActionResult> Reconcile(
            Guid companyId,
            string financialYear)
        {
            var result = await _returnService.ReconcileAsync(companyId, financialYear);
            return HandleResult(result);
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
    /// Paginated ESI return response
    /// </summary>
    public class PagedEsiReturnResponse
    {
        public List<EsiReturnListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// ESI interest calculation result
    /// </summary>
    public class EsiInterestCalculationResult
    {
        public decimal EsiAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int DaysLate { get; set; }
    }

    /// <summary>
    /// ESI due date result
    /// </summary>
    public class EsiDueDateResult
    {
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public string ContributionPeriod { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDue { get; set; }
        public int DaysOverdue { get; set; }
    }
}
