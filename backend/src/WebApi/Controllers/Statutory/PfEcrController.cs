using Core.Common;
using Core.Interfaces.Statutory;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Statutory
{
    /// <summary>
    /// PF ECR (Electronic Challan-cum-Return) management endpoints.
    /// Handles ECR generation, filing, and TRRN tracking for EPFO.
    ///
    /// ECR is the format required by EPFO for PF contribution filing.
    /// Due date: 15th of following month
    /// Interest: 1% per month (Section 7Q)
    /// Damages: 5% to 25% based on delay (Section 14B)
    /// </summary>
    [ApiController]
    [Route("api/statutory/pf-ecr")]
    [Produces("application/json")]
    public class PfEcrController : ControllerBase
    {
        private readonly IPfEcrService _ecrService;

        public PfEcrController(IPfEcrService ecrService)
        {
            _ecrService = ecrService ?? throw new ArgumentNullException(nameof(ecrService));
        }

        // ==================== ECR Generation ====================

        /// <summary>
        /// Generate ECR data for a month.
        /// Aggregates PF contributions from payroll transactions.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month (1-12)</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/generate/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(PfEcrData), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateEcr(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _ecrService.GenerateEcrAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Generate ECR data from a specific payroll run.
        /// </summary>
        /// <param name="payrollRunId">Payroll run ID</param>
        [HttpGet("payroll-run/{payrollRunId}")]
        [ProducesResponseType(typeof(PfEcrData), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateFromPayrollRun(Guid payrollRunId)
        {
            var result = await _ecrService.GenerateEcrFromPayrollRunAsync(payrollRunId);
            return HandleResult(result);
        }

        /// <summary>
        /// Preview ECR with interest/damages calculation.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/preview/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(PfEcrPreview), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PreviewEcr(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _ecrService.PreviewEcrAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Generate ECR text file in EPFO format.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/file/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(PfEcrFileResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GenerateEcrFile(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _ecrService.GenerateEcrFileAsync(companyId, periodMonth, periodYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Download ECR file directly.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("{companyId}/download/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DownloadEcrFile(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var result = await _ecrService.GenerateEcrFileAsync(companyId, periodMonth, periodYear);

            if (result.IsFailure)
                return HandleResult(result);

            var file = result.Value;
            var bytes = System.Text.Encoding.UTF8.GetBytes(file.FileContent);

            return File(bytes, "text/plain", file.FileName);
        }

        // ==================== ECR Operations ====================

        /// <summary>
        /// Create statutory payment record for ECR.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateEcrPayment([FromBody] CreatePfEcrRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _ecrService.CreateEcrPaymentAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Record ECR payment after deposit.
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
            [FromBody] RecordPfEcrPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _ecrService.RecordEcrPaymentAsync(paymentId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Update TRRN after EPFO filing.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        /// <param name="trrn">Transaction Reference Return Number</param>
        [HttpPost("{paymentId}/trrn")]
        [ProducesResponseType(typeof(Core.Entities.Payroll.StatutoryPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateTrrn(
            Guid paymentId,
            [FromQuery] string trrn)
        {
            if (string.IsNullOrWhiteSpace(trrn))
                return BadRequest("TRRN is required");

            var result = await _ecrService.UpdateTrrnAsync(paymentId, trrn);
            return HandleResult(result);
        }

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of PF ECR filings.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        /// <param name="status">Optional: Filter by status</param>
        /// <param name="searchTerm">Optional: Search by TRRN</param>
        [HttpGet("{companyId}/paged")]
        [ProducesResponseType(typeof(PagedPfEcrResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaged(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _ecrService.GetPagedAsync(
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

            return Ok(new PagedPfEcrResponse
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Get pending ECR filings.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Optional: Filter by financial year</param>
        [HttpGet("{companyId}/pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingEcrDto>), 200)]
        public async Task<IActionResult> GetPendingEcr(
            Guid companyId,
            [FromQuery] string? financialYear = null)
        {
            var result = await _ecrService.GetPendingEcrAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get filed ECRs for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        [HttpGet("{companyId}/filed/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<FiledEcrDto>), 200)]
        public async Task<IActionResult> GetFiledEcr(
            Guid companyId,
            string financialYear)
        {
            var result = await _ecrService.GetFiledEcrAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get monthly ECR summary for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        [HttpGet("{companyId}/summary/{financialYear}")]
        [ProducesResponseType(typeof(PfEcrSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetMonthlySummary(
            Guid companyId,
            string financialYear)
        {
            var result = await _ecrService.GetMonthlySummaryAsync(companyId, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Get ECR details by ID.
        /// </summary>
        /// <param name="paymentId">Statutory payment ID</param>
        [HttpGet("detail/{paymentId}")]
        [ProducesResponseType(typeof(PfEcrDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEcrById(Guid paymentId)
        {
            var result = await _ecrService.GetEcrByIdAsync(paymentId);
            return HandleResult(result);
        }

        // ==================== Utility ====================

        /// <summary>
        /// Calculate interest on late PF payment.
        /// Rate: 1% per month (simple interest under Section 7Q)
        /// </summary>
        /// <param name="pfAmount">PF amount</param>
        /// <param name="dueDate">Due date</param>
        /// <param name="paymentDate">Actual/proposed payment date</param>
        [HttpGet("calculate-interest")]
        [ProducesResponseType(typeof(PfInterestCalculationResult), 200)]
        public IActionResult CalculateInterest(
            [FromQuery] decimal pfAmount,
            [FromQuery] DateOnly dueDate,
            [FromQuery] DateOnly paymentDate)
        {
            var interest = _ecrService.CalculateInterest(pfAmount, dueDate, paymentDate);
            var damages = _ecrService.CalculateDamages(pfAmount, dueDate, paymentDate);

            return Ok(new PfInterestCalculationResult
            {
                PfAmount = pfAmount,
                DueDate = dueDate,
                PaymentDate = paymentDate,
                InterestAmount = interest,
                DamagesAmount = damages,
                TotalPayable = pfAmount + interest + damages,
                DaysLate = Math.Max(0, paymentDate.DayNumber - dueDate.DayNumber)
            });
        }

        /// <summary>
        /// Get due date for PF payment.
        /// </summary>
        /// <param name="periodMonth">Period month</param>
        /// <param name="periodYear">Period year</param>
        [HttpGet("due-date/{periodYear}/{periodMonth}")]
        [ProducesResponseType(typeof(PfDueDateResult), 200)]
        public IActionResult GetDueDate(int periodMonth, int periodYear)
        {
            var dueDate = _ecrService.GetDueDate(periodMonth, periodYear);
            var today = DateOnly.FromDateTime(DateTime.Today);

            return Ok(new PfDueDateResult
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
        /// Reconcile PF deducted vs deposited for a financial year.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        [HttpGet("{companyId}/reconcile/{financialYear}")]
        [ProducesResponseType(typeof(PfReconciliation), 200)]
        public async Task<IActionResult> Reconcile(
            Guid companyId,
            string financialYear)
        {
            var result = await _ecrService.ReconcileAsync(companyId, financialYear);
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
    /// Paginated PF ECR response
    /// </summary>
    public class PagedPfEcrResponse
    {
        public List<PfEcrListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// PF interest calculation result
    /// </summary>
    public class PfInterestCalculationResult
    {
        public decimal PfAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal DamagesAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int DaysLate { get; set; }
    }

    /// <summary>
    /// PF due date result
    /// </summary>
    public class PfDueDateResult
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
