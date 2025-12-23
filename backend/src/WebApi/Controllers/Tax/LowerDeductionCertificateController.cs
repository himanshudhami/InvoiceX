using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Tax
{
    /// <summary>
    /// Lower Deduction Certificate (Form 13) management endpoints per Section 197.
    /// Tracks certificates for lower/nil TDS deduction rates.
    /// Must be validated before applying reduced rates on payments.
    /// </summary>
    [ApiController]
    [Route("api/tax/ldc")]
    [Produces("application/json")]
    public class LowerDeductionCertificateController : ControllerBase
    {
        private readonly ILowerDeductionCertificateRepository _repository;

        public LowerDeductionCertificateController(ILowerDeductionCertificateRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ==================== CRUD Operations ====================

        /// <summary>
        /// Get certificate by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LowerDeductionCertificate), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var certificate = await _repository.GetByIdAsync(id);
            if (certificate == null)
                return NotFound($"Certificate with ID {id} not found");

            return Ok(certificate);
        }

        /// <summary>
        /// Get all certificates
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var certificates = await _repository.GetAllAsync();
            return Ok(certificates);
        }

        /// <summary>
        /// Get paginated certificates with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] LdcFilterRequest request)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());

            var response = new PagedResponse<LowerDeductionCertificate>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new certificate
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(LowerDeductionCertificate), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateLdcDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var certificate = new LowerDeductionCertificate
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                CertificateNumber = dto.CertificateNumber,
                CertificateDate = dto.CertificateDate,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                FinancialYear = dto.FinancialYear,
                CertificateType = dto.CertificateType,
                DeducteeType = dto.DeducteeType,
                DeducteeId = dto.DeducteeId,
                DeducteeName = dto.DeducteeName,
                DeducteePan = dto.DeducteePan,
                DeducteeAddress = dto.DeducteeAddress,
                TdsSection = dto.TdsSection,
                NormalRate = dto.NormalRate,
                CertificateRate = dto.CertificateRate,
                ThresholdAmount = dto.ThresholdAmount,
                UtilizedAmount = 0,
                AssessingOfficer = dto.AssessingOfficer,
                AoDesignation = dto.AoDesignation,
                AoOfficeAddress = dto.AoOfficeAddress,
                CertificateDocumentId = dto.CertificateDocumentId,
                Status = LdcStatus.Active,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            };

            var created = await _repository.AddAsync(certificate);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Update an existing certificate
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLdcDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            // Update allowed fields
            existing.CertificateNumber = dto.CertificateNumber ?? existing.CertificateNumber;
            existing.CertificateDate = dto.CertificateDate ?? existing.CertificateDate;
            existing.ValidFrom = dto.ValidFrom ?? existing.ValidFrom;
            existing.ValidTo = dto.ValidTo ?? existing.ValidTo;
            existing.FinancialYear = dto.FinancialYear ?? existing.FinancialYear;
            existing.CertificateType = dto.CertificateType ?? existing.CertificateType;
            existing.DeducteeType = dto.DeducteeType ?? existing.DeducteeType;
            existing.DeducteeId = dto.DeducteeId ?? existing.DeducteeId;
            existing.DeducteeName = dto.DeducteeName ?? existing.DeducteeName;
            existing.DeducteePan = dto.DeducteePan ?? existing.DeducteePan;
            existing.DeducteeAddress = dto.DeducteeAddress ?? existing.DeducteeAddress;
            existing.TdsSection = dto.TdsSection ?? existing.TdsSection;
            existing.NormalRate = dto.NormalRate ?? existing.NormalRate;
            existing.CertificateRate = dto.CertificateRate ?? existing.CertificateRate;
            existing.ThresholdAmount = dto.ThresholdAmount ?? existing.ThresholdAmount;
            existing.AssessingOfficer = dto.AssessingOfficer ?? existing.AssessingOfficer;
            existing.AoDesignation = dto.AoDesignation ?? existing.AoDesignation;
            existing.AoOfficeAddress = dto.AoOfficeAddress ?? existing.AoOfficeAddress;
            existing.CertificateDocumentId = dto.CertificateDocumentId ?? existing.CertificateDocumentId;
            existing.Notes = dto.Notes ?? existing.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return NoContent();
        }

        /// <summary>
        /// Delete a certificate
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        // ==================== Company Queries ====================

        /// <summary>
        /// Get all certificates for a company
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            var certificates = await _repository.GetByCompanyAsync(companyId);
            return Ok(certificates);
        }

        /// <summary>
        /// Get active certificates for a company
        /// </summary>
        [HttpGet("by-company/{companyId}/active")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetActiveByCompany(Guid companyId)
        {
            var certificates = await _repository.GetActiveByCompanyAsync(companyId);
            return Ok(certificates);
        }

        // ==================== Certificate Validation ====================

        /// <summary>
        /// Validate certificate for TDS calculation.
        /// This is the primary endpoint for TDS calculation to use.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="deducteePan">PAN of the deductee</param>
        /// <param name="tdsSection">TDS section (e.g., "194C", "194J")</param>
        /// <param name="transactionDate">Date of transaction</param>
        /// <param name="amount">Transaction amount</param>
        [HttpGet("validate/{companyId}")]
        [ProducesResponseType(typeof(LdcValidationResult), 200)]
        public async Task<IActionResult> ValidateCertificate(
            Guid companyId,
            [FromQuery] string deducteePan,
            [FromQuery] string tdsSection,
            [FromQuery] DateOnly transactionDate,
            [FromQuery] decimal amount)
        {
            var result = await _repository.ValidateCertificateAsync(
                companyId,
                deducteePan,
                tdsSection,
                transactionDate,
                amount);

            return Ok(result);
        }

        /// <summary>
        /// Get valid certificate for a deductee
        /// </summary>
        [HttpGet("valid/{companyId}")]
        [ProducesResponseType(typeof(LowerDeductionCertificate), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetValidCertificate(
            Guid companyId,
            [FromQuery] string deducteePan,
            [FromQuery] string tdsSection,
            [FromQuery] DateOnly transactionDate)
        {
            var certificate = await _repository.GetValidCertificateAsync(
                companyId,
                deducteePan,
                tdsSection,
                transactionDate);

            if (certificate == null)
                return NotFound("No valid certificate found");

            return Ok(certificate);
        }

        /// <summary>
        /// Check if valid certificate exists
        /// </summary>
        [HttpGet("exists/{companyId}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> HasValidCertificate(
            Guid companyId,
            [FromQuery] string deducteePan,
            [FromQuery] string tdsSection,
            [FromQuery] DateOnly transactionDate)
        {
            var exists = await _repository.HasValidCertificateAsync(
                companyId,
                deducteePan,
                tdsSection,
                transactionDate);

            return Ok(new { HasValidCertificate = exists });
        }

        // ==================== Deductee Queries ====================

        /// <summary>
        /// Get certificates by deductee PAN
        /// </summary>
        [HttpGet("by-pan/{companyId}/{deducteePan}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetByDeducteePan(Guid companyId, string deducteePan)
        {
            var certificates = await _repository.GetByDeducteePanAsync(companyId, deducteePan);
            return Ok(certificates);
        }

        /// <summary>
        /// Get certificates by deductee ID
        /// </summary>
        [HttpGet("by-deductee/{deducteeId}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetByDeducteeId(Guid deducteeId)
        {
            var certificates = await _repository.GetByDeducteeIdAsync(deducteeId);
            return Ok(certificates);
        }

        // ==================== Section Queries ====================

        /// <summary>
        /// Get certificates by TDS section
        /// </summary>
        [HttpGet("by-section/{companyId}/{tdsSection}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetBySection(Guid companyId, string tdsSection)
        {
            var certificates = await _repository.GetBySectionAsync(companyId, tdsSection);
            return Ok(certificates);
        }

        // ==================== Status Queries ====================

        /// <summary>
        /// Get certificates by status
        /// </summary>
        [HttpGet("by-status/{companyId}/{status}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetByStatus(Guid companyId, string status)
        {
            var certificates = await _repository.GetByStatusAsync(companyId, status);
            return Ok(certificates);
        }

        /// <summary>
        /// Get expiring certificates (within n days)
        /// </summary>
        [HttpGet("expiring/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetExpiring(Guid companyId, [FromQuery] int daysAhead = 30)
        {
            var certificates = await _repository.GetExpiringAsync(companyId, daysAhead);
            return Ok(certificates);
        }

        /// <summary>
        /// Get exhausted certificates (threshold reached)
        /// </summary>
        [HttpGet("exhausted/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<LowerDeductionCertificate>), 200)]
        public async Task<IActionResult> GetExhausted(Guid companyId)
        {
            var certificates = await _repository.GetExhaustedAsync(companyId);
            return Ok(certificates);
        }

        // ==================== Utilization ====================

        /// <summary>
        /// Update utilized amount manually
        /// </summary>
        [HttpPost("{id}/update-utilization")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUtilization(Guid id, [FromBody] UpdateUtilizationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            await _repository.UpdateUtilizedAmountAsync(id, dto.AdditionalAmount);
            return NoContent();
        }

        /// <summary>
        /// Record certificate usage (with audit trail)
        /// </summary>
        [HttpPost("{id}/record-usage")]
        [ProducesResponseType(typeof(Guid), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecordUsage(Guid id, [FromBody] RecordUsageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            var usage = new LdcUsageRecord
            {
                Id = Guid.NewGuid(),
                CertificateId = id,
                CompanyId = existing.CompanyId,
                TransactionDate = dto.TransactionDate,
                TransactionType = dto.TransactionType,
                TransactionId = dto.TransactionId,
                TransactionNumber = dto.TransactionNumber,
                GrossAmount = dto.GrossAmount,
                NormalTdsAmount = dto.NormalTdsAmount,
                ActualTdsAmount = dto.ActualTdsAmount,
                TdsSavings = dto.NormalTdsAmount - dto.ActualTdsAmount,
                CumulativeUtilized = existing.UtilizedAmount + dto.GrossAmount,
                RemainingThreshold = existing.ThresholdAmount.HasValue
                    ? existing.ThresholdAmount.Value - (existing.UtilizedAmount + dto.GrossAmount)
                    : null,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            };

            var usageId = await _repository.RecordUsageAsync(usage);

            // Update utilized amount
            await _repository.UpdateUtilizedAmountAsync(id, dto.GrossAmount);

            return CreatedAtAction(nameof(GetUsageHistory), new { id }, new { UsageId = usageId });
        }

        /// <summary>
        /// Get usage history for a certificate
        /// </summary>
        [HttpGet("{id}/usage-history")]
        [ProducesResponseType(typeof(IEnumerable<LdcUsageRecord>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUsageHistory(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            var history = await _repository.GetUsageHistoryAsync(id);
            return Ok(history);
        }

        // ==================== Status Updates ====================

        /// <summary>
        /// Update certificate status
        /// </summary>
        [HttpPost("{id}/update-status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            await _repository.UpdateStatusAsync(id, dto.Status, dto.Reason);
            return NoContent();
        }

        /// <summary>
        /// Revoke a certificate
        /// </summary>
        [HttpPost("{id}/revoke")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Certificate with ID {id} not found");

            await _repository.RevokeCertificateAsync(id, dto.Reason);
            return NoContent();
        }
    }

    // ==================== Request DTOs ====================

    /// <summary>
    /// Filter request for certificates
    /// </summary>
    public class LdcFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public Guid? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? TdsSection { get; set; }
        public string? CertificateType { get; set; }
        public string? DeducteeType { get; set; }
        public string? FinancialYear { get; set; }

        public Dictionary<string, object?> GetFilters()
        {
            var filters = new Dictionary<string, object?>();
            if (CompanyId.HasValue)
                filters["company_id"] = CompanyId.Value;
            if (!string.IsNullOrWhiteSpace(Status))
                filters["status"] = Status;
            if (!string.IsNullOrWhiteSpace(TdsSection))
                filters["tds_section"] = TdsSection;
            if (!string.IsNullOrWhiteSpace(CertificateType))
                filters["certificate_type"] = CertificateType;
            if (!string.IsNullOrWhiteSpace(DeducteeType))
                filters["deductee_type"] = DeducteeType;
            if (!string.IsNullOrWhiteSpace(FinancialYear))
                filters["financial_year"] = FinancialYear;
            return filters;
        }
    }

    /// <summary>
    /// DTO for creating a certificate
    /// </summary>
    public class CreateLdcDto
    {
        public Guid CompanyId { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public DateOnly CertificateDate { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string CertificateType { get; set; } = string.Empty;
        public string DeducteeType { get; set; } = string.Empty;
        public Guid? DeducteeId { get; set; }
        public string DeducteeName { get; set; } = string.Empty;
        public string DeducteePan { get; set; } = string.Empty;
        public string? DeducteeAddress { get; set; }
        public string TdsSection { get; set; } = string.Empty;
        public decimal NormalRate { get; set; }
        public decimal CertificateRate { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public string? AssessingOfficer { get; set; }
        public string? AoDesignation { get; set; }
        public string? AoOfficeAddress { get; set; }
        public Guid? CertificateDocumentId { get; set; }
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating a certificate
    /// </summary>
    public class UpdateLdcDto
    {
        public string? CertificateNumber { get; set; }
        public DateOnly? CertificateDate { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string? FinancialYear { get; set; }
        public string? CertificateType { get; set; }
        public string? DeducteeType { get; set; }
        public Guid? DeducteeId { get; set; }
        public string? DeducteeName { get; set; }
        public string? DeducteePan { get; set; }
        public string? DeducteeAddress { get; set; }
        public string? TdsSection { get; set; }
        public decimal? NormalRate { get; set; }
        public decimal? CertificateRate { get; set; }
        public decimal? ThresholdAmount { get; set; }
        public string? AssessingOfficer { get; set; }
        public string? AoDesignation { get; set; }
        public string? AoOfficeAddress { get; set; }
        public Guid? CertificateDocumentId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating utilization
    /// </summary>
    public class UpdateUtilizationDto
    {
        public decimal AdditionalAmount { get; set; }
    }

    /// <summary>
    /// DTO for recording usage
    /// </summary>
    public class RecordUsageDto
    {
        public DateOnly TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public Guid? TransactionId { get; set; }
        public string? TransactionNumber { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NormalTdsAmount { get; set; }
        public decimal ActualTdsAmount { get; set; }
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating status
    /// </summary>
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for revoking certificate
    /// </summary>
    public class RevokeDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
