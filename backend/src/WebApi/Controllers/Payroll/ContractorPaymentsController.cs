using Application.DTOs.Payroll;
using Application.DTOs.Contractors;
using Application.Services.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Contractor payments management endpoints.
/// Links to parties table (unified party model) for contractor information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContractorPaymentsController : ControllerBase
{
    private readonly IContractorPaymentRepository _repository;
    private readonly PayrollCalculationService _calculationService;
    private readonly ICompaniesRepository _companiesRepository;
    private readonly IContractorPostingService _postingService;
    private readonly IPartyRepository _partyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ContractorPaymentsController> _logger;

    public ContractorPaymentsController(
        IContractorPaymentRepository repository,
        PayrollCalculationService calculationService,
        ICompaniesRepository companiesRepository,
        IContractorPostingService postingService,
        IPartyRepository partyRepository,
        IMapper mapper,
        ILogger<ContractorPaymentsController> logger)
    {
        _repository = repository;
        _calculationService = calculationService;
        _companiesRepository = companiesRepository;
        _postingService = postingService;
        _partyRepository = partyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all contractor payments with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ContractorPaymentDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ContractorPaymentFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<ContractorPaymentDto>>(items).ToList();

        // PartyName is populated from repository join, just need company names
        var companyIds = dtos.Select(d => d.CompanyId).Distinct().ToList();
        var companies = new Dictionary<Guid, string>();
        foreach (var companyId in companyIds)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company != null)
            {
                companies[companyId] = company.Name;
            }
        }

        foreach (var dto in dtos)
        {
            dto.CompanyName = companies.GetValueOrDefault(dto.CompanyId);
        }

        var response = new PagedResponse<ContractorPaymentDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all contractor payments with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<ContractorPaymentDto>), 200)]
    public async Task<IActionResult> GetAllPaged([FromQuery] ContractorPaymentFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<ContractorPaymentDto>>(items).ToList();

        // PartyName is populated from repository join, just need company names
        var companyIds = dtos.Select(d => d.CompanyId).Distinct().ToList();
        var companies = new Dictionary<Guid, string>();
        foreach (var companyId in companyIds)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company != null)
            {
                companies[companyId] = company.Name;
            }
        }

        foreach (var dto in dtos)
        {
            dto.CompanyName = companies.GetValueOrDefault(dto.CompanyId);
        }

        var response = new PagedResponse<ContractorPaymentDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get contractor payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ContractorPaymentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            return NotFound($"Contractor payment with ID {id} not found");

        return Ok(_mapper.Map<ContractorPaymentDto>(payment));
    }

    /// <summary>
    /// Create a new contractor payment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContractorPaymentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateContractorPaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check for duplicate
        var exists = await _repository.ExistsForPartyAndMonthAsync(
            dto.PartyId, dto.PaymentMonth, dto.PaymentYear);
        if (exists)
            return Conflict($"Payment already exists for this contractor for {dto.PaymentMonth}/{dto.PaymentYear}");

        // Use calculation service to create payment with calculated values
        var payment = _calculationService.CalculateContractorPayment(
            dto.PartyId,
            dto.CompanyId,
            dto.PaymentMonth,
            dto.PaymentYear,
            dto.GrossAmount,
            dto.TdsRate,
            dto.GstApplicable,
            dto.GstRate,
            dto.OtherDeductions,
            dto.InvoiceNumber,
            dto.ContractReference,
            dto.Description);

        payment.Remarks = dto.Remarks;
        payment.CreatedBy = dto.CreatedBy;

        var created = await _repository.AddAsync(payment);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<ContractorPaymentDto>(created));
    }

    /// <summary>
    /// Update a contractor payment
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractorPaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Contractor payment with ID {id} not found");

        if (existing.Status == "paid")
            return BadRequest("Cannot update a paid payment");

        // Update fields
        if (dto.GrossAmount.HasValue)
            existing.GrossAmount = dto.GrossAmount.Value;
        if (dto.TdsRate.HasValue)
            existing.TdsRate = dto.TdsRate.Value;
        if (dto.OtherDeductions.HasValue)
            existing.OtherDeductions = dto.OtherDeductions.Value;
        if (dto.GstApplicable.HasValue)
            existing.GstApplicable = dto.GstApplicable.Value;
        if (dto.GstRate.HasValue)
            existing.GstRate = dto.GstRate.Value;

        // Recalculate amounts
        existing.TdsAmount = Math.Round(existing.GrossAmount * existing.TdsRate / 100, 0, MidpointRounding.AwayFromZero);
        existing.GstAmount = existing.GstApplicable
            ? Math.Round(existing.GrossAmount * existing.GstRate / 100, 2, MidpointRounding.AwayFromZero)
            : 0;
        existing.TotalInvoiceAmount = existing.GrossAmount + existing.GstAmount;
        // Net payable uses TotalInvoiceAmount to include GST - contractor receives invoice total minus TDS
        existing.NetPayable = (existing.TotalInvoiceAmount ?? existing.GrossAmount) - existing.TdsAmount - existing.OtherDeductions;

        // Update other fields
        if (!string.IsNullOrEmpty(dto.InvoiceNumber))
            existing.InvoiceNumber = dto.InvoiceNumber;
        if (!string.IsNullOrEmpty(dto.ContractReference))
            existing.ContractReference = dto.ContractReference;
        if (!string.IsNullOrEmpty(dto.Description))
            existing.Description = dto.Description;
        if (!string.IsNullOrEmpty(dto.Remarks))
            existing.Remarks = dto.Remarks;
        if (!string.IsNullOrEmpty(dto.Status))
            existing.Status = dto.Status;
        if (dto.PaymentDate.HasValue)
            existing.PaymentDate = dto.PaymentDate;
        if (!string.IsNullOrEmpty(dto.PaymentMethod))
            existing.PaymentMethod = dto.PaymentMethod;
        if (!string.IsNullOrEmpty(dto.PaymentReference))
            existing.PaymentReference = dto.PaymentReference;

        existing.UpdatedBy = dto.UpdatedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Delete a contractor payment
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            return NotFound($"Contractor payment with ID {id} not found");

        if (payment.Status == "paid")
            return BadRequest("Cannot delete a paid payment");

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Approve a contractor payment and post accrual journal entry
    /// </summary>
    /// <remarks>
    /// Posts accrual journal entry following Indian accounting standards:
    /// Dr. Professional Fees (Gross), Dr. Input GST (if applicable)
    /// Cr. TDS Payable (194J/194C), Cr. Contractor Payable (Net)
    /// </remarks>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            return NotFound($"Contractor payment with ID {id} not found");

        if (payment.Status != "pending")
            return BadRequest("Only pending payments can be approved");

        // Update status to approved
        await _repository.UpdateStatusAsync(id, "approved");

        // Post accrual journal entry (expense recognition with TDS liability)
        try
        {
            var journalEntry = await _postingService.PostAccrualAsync(id);
            if (journalEntry != null)
            {
                _logger.LogInformation(
                    "Posted accrual journal entry {JournalEntryId} for contractor payment {PaymentId}",
                    journalEntry.Id, id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to post accrual journal entry for contractor payment {PaymentId}. Payment approved but journal entry not created.",
                id);
            // Payment is approved but journal entry failed - don't fail the whole request
            // The posting can be retried manually or through a batch process
        }

        return NoContent();
    }

    /// <summary>
    /// Mark a contractor payment as paid and post disbursement journal entry
    /// </summary>
    /// <remarks>
    /// Posts disbursement journal entry following Indian accounting standards:
    /// Dr. Contractor Payable (Net), Cr. Bank Account (Net)
    /// Requires bank account ID for journal entry creation.
    /// </remarks>
    [HttpPost("{id}/pay")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkAsPaid(Guid id, [FromBody] UpdateContractorPaymentDto dto)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            return NotFound($"Contractor payment with ID {id} not found");

        if (payment.Status != "approved")
            return BadRequest("Only approved payments can be marked as paid");

        // Bank account is required for journal entry posting
        if (!dto.BankAccountId.HasValue)
            return BadRequest("Bank account ID is required for marking payment as paid");

        payment.PaymentDate = dto.PaymentDate ?? DateTime.UtcNow;
        payment.PaymentMethod = dto.PaymentMethod;
        payment.PaymentReference = dto.PaymentReference;
        payment.BankAccountId = dto.BankAccountId;
        payment.Status = "paid";
        payment.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(payment);

        // Post disbursement journal entry (contractor payable settlement)
        try
        {
            var journalEntry = await _postingService.PostDisbursementAsync(id, dto.BankAccountId.Value);
            if (journalEntry != null)
            {
                _logger.LogInformation(
                    "Posted disbursement journal entry {JournalEntryId} for contractor payment {PaymentId}",
                    journalEntry.Id, id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to post disbursement journal entry for contractor payment {PaymentId}. Payment marked as paid but journal entry not created.",
                id);
            // Payment is marked as paid but journal entry failed - don't fail the whole request
            // The posting can be retried manually
        }

        return NoContent();
    }

    /// <summary>
    /// Get monthly summary for contractor payments
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
    public async Task<IActionResult> GetMonthlySummary(
        [FromQuery] int paymentMonth,
        [FromQuery] int paymentYear,
        [FromQuery] Guid? companyId = null)
    {
        var summary = await _repository.GetMonthlySummaryAsync(paymentMonth, paymentYear, companyId);
        return Ok(summary);
    }

    /// <summary>
    /// Get YTD summary for a contractor (party)
    /// </summary>
    [HttpGet("ytd/{partyId}")]
    [ProducesResponseType(typeof(ContractorPaymentSummaryDto), 200)]
    public async Task<IActionResult> GetYtdSummary(Guid partyId, [FromQuery] string financialYear)
    {
        var summary = await _repository.GetYtdSummaryAsync(partyId, financialYear);

        return Ok(new ContractorPaymentSummaryDto
        {
            PartyId = partyId,
            FinancialYear = financialYear,
            TotalGross = summary.TryGetValue("YtdGross", out var g) ? g : 0,
            TotalTds = summary.TryGetValue("YtdTds", out var t) ? t : 0,
            TotalGst = summary.TryGetValue("YtdGst", out var gst) ? gst : 0,
            TotalNet = summary.TryGetValue("YtdNet", out var n) ? n : 0,
            PaymentCount = (int)(summary.TryGetValue("PaymentCount", out var c) ? c : 0)
        });
    }

    /// <summary>
    /// Get payment breakdown for contractors in a company (similar to vendor payment summary)
    /// </summary>
    /// <param name="companyId">Company ID to filter payments</param>
    /// <returns>Payment breakdown with per-contractor details</returns>
    [HttpGet("payment-breakdown")]
    [ProducesResponseType(typeof(ContractorPaymentBreakdownDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetPaymentBreakdown([FromQuery] Guid companyId)
    {
        if (companyId == Guid.Empty)
            return BadRequest(new { error = "Company ID is required" });

        // Get all contractor payments for the company
        var payments = await _repository.GetByCompanyIdAsync(companyId);
        var paymentsList = payments.ToList();

        if (!paymentsList.Any())
        {
            return Ok(new ContractorPaymentBreakdownDto
            {
                TotalPaid = 0,
                TotalGross = 0,
                TotalTds = 0,
                ContractorCount = 0,
                PaymentCount = 0,
                Contractors = new List<ContractorPaymentDetailDto>()
            });
        }

        // Get unique party IDs and fetch their names
        var partyIds = paymentsList
            .Select(p => p.PartyId)
            .Distinct()
            .ToList();
        var parties = await _partyRepository.GetByIdsAsync(partyIds);
        var partyNameMap = parties.ToDictionary(p => p.Id, p => p.Name);

        // Aggregate by contractor
        var contractorSummaries = paymentsList
            .GroupBy(p => p.PartyId)
            .Select(g => new ContractorPaymentDetailDto
            {
                ContractorId = g.Key,
                ContractorName = partyNameMap.TryGetValue(g.Key, out var name) ? name : "Unknown",
                TotalPaid = g.Sum(p => p.NetPayable),
                TotalGross = g.Sum(p => p.GrossAmount),
                TotalTds = g.Sum(p => p.TdsAmount),
                PaymentCount = g.Count(),
                LastPaymentDate = g.Max(p => p.PaymentDate)
            })
            .OrderByDescending(c => c.TotalPaid)
            .ToList();

        var breakdown = new ContractorPaymentBreakdownDto
        {
            TotalPaid = paymentsList.Sum(p => p.NetPayable),
            TotalGross = paymentsList.Sum(p => p.GrossAmount),
            TotalTds = paymentsList.Sum(p => p.TdsAmount),
            ContractorCount = contractorSummaries.Count(),
            PaymentCount = paymentsList.Count(),
            Contractors = contractorSummaries
        };

        return Ok(breakdown);
    }
}
