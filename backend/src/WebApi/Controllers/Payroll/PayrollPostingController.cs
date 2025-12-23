using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Core.Interfaces.Payroll;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// API endpoints for payroll journal entry posting and management
/// </summary>
[ApiController]
[Route("api/payroll-posting")]
[Produces("application/json")]
public class PayrollPostingController : ControllerBase
{
    private readonly IPayrollPostingService _postingService;
    private readonly IPayrollRunRepository _payrollRunRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IStatutoryPaymentRepository _statutoryPaymentRepository;
    private readonly ILogger<PayrollPostingController> _logger;

    public PayrollPostingController(
        IPayrollPostingService postingService,
        IPayrollRunRepository payrollRunRepository,
        IJournalEntryRepository journalEntryRepository,
        IStatutoryPaymentRepository statutoryPaymentRepository,
        ILogger<PayrollPostingController> logger)
    {
        _postingService = postingService ?? throw new ArgumentNullException(nameof(postingService));
        _payrollRunRepository = payrollRunRepository ?? throw new ArgumentNullException(nameof(payrollRunRepository));
        _journalEntryRepository = journalEntryRepository ?? throw new ArgumentNullException(nameof(journalEntryRepository));
        _statutoryPaymentRepository = statutoryPaymentRepository ?? throw new ArgumentNullException(nameof(statutoryPaymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== Accrual Posting ====================

    /// <summary>
    /// Post accrual journal entry for a payroll run (expense recognition on approval)
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <param name="postedBy">User ID posting the entry (optional)</param>
    /// <returns>Created journal entry or existing if already posted</returns>
    [HttpPost("{id}/accrual")]
    [ProducesResponseType(typeof(JournalEntryDto), 200)]
    [ProducesResponseType(typeof(JournalEntryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostAccrual(Guid id, [FromQuery] Guid? postedBy = null)
    {
        _logger.LogInformation("Posting accrual journal entry for payroll run {PayrollRunId}", id);

        // Verify payroll run exists
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
        {
            _logger.LogWarning("Payroll run {PayrollRunId} not found", id);
            return NotFound($"Payroll run with ID {id} not found");
        }

        try
        {
            var journalEntry = await _postingService.PostAccrualAsync(id, postedBy);

            if (journalEntry == null)
            {
                return BadRequest("Failed to create accrual journal entry. Check that the payroll run is approved and has transactions.");
            }

            var dto = MapToDto(journalEntry);

            // Check if it was a new entry or already existed (idempotency)
            if (payrollRun.AccrualJournalEntryId == journalEntry.Id)
            {
                _logger.LogInformation("Accrual journal entry {JournalEntryId} already existed for payroll run {PayrollRunId}",
                    journalEntry.Id, id);
                return Ok(dto);
            }

            _logger.LogInformation("Created accrual journal entry {JournalEntryId} for payroll run {PayrollRunId}",
                journalEntry.Id, id);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = journalEntry.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation posting accrual for payroll run {PayrollRunId}", id);
            return BadRequest(ex.Message);
        }
    }

    // ==================== Disbursement Posting ====================

    /// <summary>
    /// Post disbursement journal entry for a payroll run (salary payment)
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <param name="request">Disbursement request with bank account ID</param>
    /// <returns>Created journal entry or existing if already posted</returns>
    [HttpPost("{id}/disbursement")]
    [ProducesResponseType(typeof(JournalEntryDto), 200)]
    [ProducesResponseType(typeof(JournalEntryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostDisbursement(Guid id, [FromBody] DisbursementRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Posting disbursement journal entry for payroll run {PayrollRunId}", id);

        // Verify payroll run exists
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
        {
            _logger.LogWarning("Payroll run {PayrollRunId} not found", id);
            return NotFound($"Payroll run with ID {id} not found");
        }

        try
        {
            var journalEntry = await _postingService.PostDisbursementAsync(id, request.BankAccountId, request.PostedBy);

            if (journalEntry == null)
            {
                return BadRequest("Failed to create disbursement journal entry. Check that the payroll run is paid.");
            }

            var dto = MapToDto(journalEntry);

            // Check if it was a new entry or already existed (idempotency)
            if (payrollRun.DisbursementJournalEntryId == journalEntry.Id)
            {
                _logger.LogInformation("Disbursement journal entry {JournalEntryId} already existed for payroll run {PayrollRunId}",
                    journalEntry.Id, id);
                return Ok(dto);
            }

            _logger.LogInformation("Created disbursement journal entry {JournalEntryId} for payroll run {PayrollRunId}",
                journalEntry.Id, id);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = journalEntry.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation posting disbursement for payroll run {PayrollRunId}", id);
            return BadRequest(ex.Message);
        }
    }

    // ==================== Statutory Payment Posting ====================

    /// <summary>
    /// Post journal entry for a statutory payment (TDS/PF/ESI/PT remittance)
    /// </summary>
    /// <param name="id">Statutory payment ID</param>
    /// <param name="request">Posting request with bank account ID</param>
    /// <returns>Created journal entry or existing if already posted</returns>
    [HttpPost("statutory/{id}")]
    [ProducesResponseType(typeof(JournalEntryDto), 200)]
    [ProducesResponseType(typeof(JournalEntryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostStatutoryPayment(Guid id, [FromBody] StatutoryPaymentPostingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Posting journal entry for statutory payment {StatutoryPaymentId}", id);

        // Verify statutory payment exists
        var statutoryPayment = await _statutoryPaymentRepository.GetByIdAsync(id);
        if (statutoryPayment == null)
        {
            _logger.LogWarning("Statutory payment {StatutoryPaymentId} not found", id);
            return NotFound($"Statutory payment with ID {id} not found");
        }

        try
        {
            var journalEntry = await _postingService.PostStatutoryPaymentAsync(id, request.BankAccountId, request.PostedBy);

            if (journalEntry == null)
            {
                return BadRequest("Failed to create statutory payment journal entry. Check that the payment is marked as paid.");
            }

            var dto = MapToDto(journalEntry);

            // Check if it was a new entry or already existed (idempotency)
            if (statutoryPayment.JournalEntryId == journalEntry.Id)
            {
                _logger.LogInformation("Statutory payment journal entry {JournalEntryId} already existed for payment {StatutoryPaymentId}",
                    journalEntry.Id, id);
                return Ok(dto);
            }

            _logger.LogInformation("Created statutory payment journal entry {JournalEntryId} for payment {StatutoryPaymentId}",
                journalEntry.Id, id);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = journalEntry.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation posting statutory payment {StatutoryPaymentId}", id);
            return BadRequest(ex.Message);
        }
    }

    // ==================== Journal Entry Queries ====================

    /// <summary>
    /// Get all journal entries for a payroll run
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>List of journal entries (accrual, disbursement, statutory)</returns>
    [HttpGet("{id}/entries")]
    [ProducesResponseType(typeof(PayrollJournalEntriesDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayrollEntries(Guid id)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        var result = new PayrollJournalEntriesDto
        {
            PayrollRunId = id,
            MonthYear = $"{payrollRun.PayrollMonth:D2}/{payrollRun.PayrollYear}",
            Status = payrollRun.Status
        };

        // Get accrual entry
        if (payrollRun.AccrualJournalEntryId.HasValue)
        {
            var accrualEntry = await _journalEntryRepository.GetByIdAsync(payrollRun.AccrualJournalEntryId.Value);
            if (accrualEntry != null)
            {
                result.AccrualEntry = MapToDto(accrualEntry);
            }
        }

        // Get disbursement entry
        if (payrollRun.DisbursementJournalEntryId.HasValue)
        {
            var disbursementEntry = await _journalEntryRepository.GetByIdAsync(payrollRun.DisbursementJournalEntryId.Value);
            if (disbursementEntry != null)
            {
                result.DisbursementEntry = MapToDto(disbursementEntry);
            }
        }

        // Get statutory payment entries
        var statutoryPaymentEntries = await _journalEntryRepository.GetBySourceAsync("statutory_payment", id);
        result.StatutoryEntries = statutoryPaymentEntries.Select(MapToDto).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get a single journal entry by ID
    /// </summary>
    /// <param name="id">Journal entry ID</param>
    /// <returns>Journal entry with lines</returns>
    [HttpGet("entry/{id}")]
    [ProducesResponseType(typeof(JournalEntryDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetJournalEntry(Guid id)
    {
        var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
        if (journalEntry == null)
            return NotFound($"Journal entry with ID {id} not found");

        var dto = MapToDetailDto(journalEntry);
        return Ok(dto);
    }

    // ==================== Reversal ====================

    /// <summary>
    /// Reverse a journal entry
    /// </summary>
    /// <param name="id">Journal entry ID to reverse</param>
    /// <param name="request">Reversal request with reason</param>
    /// <returns>Reversing journal entry</returns>
    [HttpPost("reverse/{id}")]
    [ProducesResponseType(typeof(JournalEntryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReverseEntry(Guid id, [FromBody] ReversalRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Reversing journal entry {JournalEntryId}", id);

        var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
        if (journalEntry == null)
        {
            _logger.LogWarning("Journal entry {JournalEntryId} not found", id);
            return NotFound($"Journal entry with ID {id} not found");
        }

        try
        {
            var reversedBy = request.ReversedBy ?? Guid.Empty;
            var reversalEntry = await _postingService.ReversePayrollEntryAsync(id, reversedBy, request.Reason);

            if (reversalEntry == null)
            {
                return BadRequest("Failed to reverse journal entry");
            }

            var dto = MapToDto(reversalEntry);
            _logger.LogInformation("Created reversal journal entry {ReversalId} for original entry {OriginalId}",
                reversalEntry.Id, id);

            return CreatedAtAction(nameof(GetJournalEntry), new { id = reversalEntry.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation reversing journal entry {JournalEntryId}", id);
            return BadRequest(ex.Message);
        }
    }

    // ==================== Posting Summary ====================

    /// <summary>
    /// Get posting summary for a payroll run
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>Summary of posting status and amounts</returns>
    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(PayrollPostingSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPostingSummary(Guid id)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        var summary = await _postingService.GetPostingSummaryAsync(
            payrollRun.CompanyId,
            payrollRun.PayrollMonth,
            payrollRun.PayrollYear);

        if (summary == null)
        {
            // No summary found - return basic info from payroll run
            return Ok(new PayrollPostingSummaryDto
            {
                PayrollRunId = id,
                MonthYear = $"{payrollRun.PayrollMonth:D2}/{payrollRun.PayrollYear}",
                PayrollStatus = payrollRun.Status,
                AccrualPosted = payrollRun.AccrualJournalEntryId.HasValue,
                AccrualJournalEntryId = payrollRun.AccrualJournalEntryId,
                AccrualAmount = payrollRun.TotalGrossSalary + payrollRun.TotalEmployerPf + payrollRun.TotalEmployerEsi,
                DisbursementPosted = payrollRun.DisbursementJournalEntryId.HasValue,
                DisbursementJournalEntryId = payrollRun.DisbursementJournalEntryId,
                DisbursementAmount = payrollRun.TotalNetSalary,
                StatutoryPaymentsCount = 0,
                StatutoryPaymentsPending = 0
            });
        }

        return Ok(new PayrollPostingSummaryDto
        {
            PayrollRunId = summary.PayrollRunId,
            MonthYear = $"{summary.PayrollMonth:D2}/{summary.PayrollYear}",
            PayrollStatus = summary.PayrollStatus,
            AccrualPosted = summary.HasAccrualEntry,
            AccrualJournalEntryId = summary.AccrualJournalEntryId,
            AccrualAmount = summary.TotalGrossSalary + summary.TotalEmployerCost - summary.TotalGrossSalary, // Employer cost portion
            DisbursementPosted = summary.HasDisbursementEntry,
            DisbursementJournalEntryId = summary.DisbursementJournalEntryId,
            DisbursementAmount = summary.TotalNetSalary,
            StatutoryPaymentsCount = 0,
            StatutoryPaymentsPending = 0
        });
    }

    // ==================== Statutory Payments ====================

    /// <summary>
    /// Get pending statutory payments for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of pending statutory payments</returns>
    [HttpGet("statutory/pending/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<PendingStatutoryPaymentDto>), 200)]
    public async Task<IActionResult> GetPendingStatutoryPayments(Guid companyId, [FromQuery] string? status = null)
    {
        var payments = await _statutoryPaymentRepository.GetPendingPaymentsViewAsync(companyId, status);

        var dtos = payments.Select(p => new PendingStatutoryPaymentDto
        {
            CompanyId = p.CompanyId,
            FinancialYear = p.FinancialYear,
            PeriodMonth = p.PeriodMonth,
            PeriodYear = p.PeriodYear,
            PaymentType = p.PaymentType,
            PaymentTypeName = p.PaymentTypeName,
            PaymentCategory = p.PaymentCategory,
            AmountDue = p.AmountDue,
            AmountPaid = p.AmountPaid,
            BalanceDue = p.BalanceDue,
            DueDate = p.DueDate,
            PaymentStatus = p.PaymentStatus,
            DaysOverdue = p.DaysOverdue,
            StatutoryPaymentId = p.StatutoryPaymentId,
            ReferenceNumber = p.ReferenceNumber,
            PaymentDate = p.PaymentDate,
            ChallanStatus = p.ChallanStatus
        });

        return Ok(dtos);
    }

    // ==================== Private Helpers ====================

    private static JournalEntryDto MapToDto(JournalEntry entry)
    {
        return new JournalEntryDto
        {
            Id = entry.Id,
            JournalNumber = entry.JournalNumber,
            JournalDate = entry.JournalDate,
            FinancialYear = entry.FinancialYear,
            PeriodMonth = entry.PeriodMonth,
            EntryType = entry.EntryType,
            SourceType = entry.SourceType,
            SourceId = entry.SourceId,
            SourceNumber = entry.SourceNumber,
            Description = entry.Description,
            TotalDebit = entry.TotalDebit,
            TotalCredit = entry.TotalCredit,
            Status = entry.Status,
            PostedAt = entry.PostedAt,
            IsReversed = entry.IsReversed
        };
    }

    private static JournalEntryDetailDto MapToDetailDto(JournalEntry entry)
    {
        var dto = new JournalEntryDetailDto
        {
            Id = entry.Id,
            JournalNumber = entry.JournalNumber,
            JournalDate = entry.JournalDate,
            FinancialYear = entry.FinancialYear,
            PeriodMonth = entry.PeriodMonth,
            EntryType = entry.EntryType,
            SourceType = entry.SourceType,
            SourceId = entry.SourceId,
            SourceNumber = entry.SourceNumber,
            Description = entry.Description,
            Narration = entry.Narration,
            TotalDebit = entry.TotalDebit,
            TotalCredit = entry.TotalCredit,
            Status = entry.Status,
            PostedAt = entry.PostedAt,
            IsReversed = entry.IsReversed,
            IdempotencyKey = entry.IdempotencyKey,
            RuleCode = entry.RuleCode,
            Lines = entry.Lines?.Select(l => new JournalEntryLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                AccountCode = l.Account?.AccountCode ?? string.Empty,
                AccountName = l.Account?.AccountName,
                Description = l.Description,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount
            }).ToList() ?? new List<JournalEntryLineDto>()
        };

        return dto;
    }
}

// ==================== Request/Response DTOs ====================

/// <summary>
/// Request for disbursement journal posting
/// </summary>
public class DisbursementRequest
{
    /// <summary>
    /// Bank account ID for the payment
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// User posting the entry (optional)
    /// </summary>
    public Guid? PostedBy { get; set; }
}

/// <summary>
/// Request for statutory payment journal posting
/// </summary>
public class StatutoryPaymentPostingRequest
{
    /// <summary>
    /// Bank account ID for the payment
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// User posting the entry (optional)
    /// </summary>
    public Guid? PostedBy { get; set; }
}

/// <summary>
/// Request for journal entry reversal
/// </summary>
public class ReversalRequest
{
    /// <summary>
    /// Reason for reversal
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User reversing the entry (optional)
    /// </summary>
    public Guid? ReversedBy { get; set; }
}

/// <summary>
/// Summary journal entry DTO
/// </summary>
public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public DateOnly JournalDate { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int PeriodMonth { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PostedAt { get; set; }
    public bool IsReversed { get; set; }
}

/// <summary>
/// Detailed journal entry DTO with lines
/// </summary>
public class JournalEntryDetailDto : JournalEntryDto
{
    public string? Narration { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? RuleCode { get; set; }
    public List<JournalEntryLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Journal entry line DTO
/// </summary>
public class JournalEntryLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

/// <summary>
/// DTO for all journal entries related to a payroll run
/// </summary>
public class PayrollJournalEntriesDto
{
    public Guid PayrollRunId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public JournalEntryDto? AccrualEntry { get; set; }
    public JournalEntryDto? DisbursementEntry { get; set; }
    public List<JournalEntryDto> StatutoryEntries { get; set; } = new();
}

/// <summary>
/// DTO for posting summary
/// </summary>
public class PayrollPostingSummaryDto
{
    public Guid PayrollRunId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public string PayrollStatus { get; set; } = string.Empty;
    public bool AccrualPosted { get; set; }
    public Guid? AccrualJournalEntryId { get; set; }
    public decimal AccrualAmount { get; set; }
    public bool DisbursementPosted { get; set; }
    public Guid? DisbursementJournalEntryId { get; set; }
    public decimal DisbursementAmount { get; set; }
    public int StatutoryPaymentsCount { get; set; }
    public int StatutoryPaymentsPending { get; set; }
}

/// <summary>
/// DTO for pending statutory payment view
/// </summary>
public class PendingStatutoryPaymentDto
{
    public Guid CompanyId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentTypeName { get; set; } = string.Empty;
    public string PaymentCategory { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public DateOnly DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int DaysOverdue { get; set; }
    public Guid? StatutoryPaymentId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? ChallanStatus { get; set; }
}
