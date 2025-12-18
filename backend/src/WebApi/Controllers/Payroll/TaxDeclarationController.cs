using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Employee tax declaration management endpoints
/// </summary>
[ApiController]
[Route("api/payroll/tax-declarations")]
[Produces("application/json")]
public class TaxDeclarationController : ControllerBase
{
    private readonly IEmployeeTaxDeclarationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IEmployeesRepository _employeesRepository;
    private readonly ITaxDeclarationService? _taxDeclarationService;

    public TaxDeclarationController(
        IEmployeeTaxDeclarationRepository repository,
        IEmployeesRepository employeesRepository,
        IMapper mapper,
        ITaxDeclarationService? taxDeclarationService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _taxDeclarationService = taxDeclarationService;
    }

    /// <summary>
    /// Get all tax declarations with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeTaxDeclarationDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeTaxDeclarationFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<EmployeeTaxDeclarationDto>>(items).ToList();
        
        // Populate employee names
        var employeeIds = dtos.Select(d => d.EmployeeId).Distinct().ToList();
        var employees = new Dictionary<Guid, string>();
        foreach (var employeeId in employeeIds)
        {
            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee != null)
            {
                employees[employeeId] = employee.EmployeeName;
            }
        }
        
        foreach (var dto in dtos)
        {
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
        }
        
        var response = new PagedResponse<EmployeeTaxDeclarationDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all tax declarations with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeTaxDeclarationDto>), 200)]
    public async Task<IActionResult> GetAllPaged([FromQuery] EmployeeTaxDeclarationFilterRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<EmployeeTaxDeclarationDto>>(items).ToList();
        
        // Populate employee names
        var employeeIds = dtos.Select(d => d.EmployeeId).Distinct().ToList();
        var employees = new Dictionary<Guid, string>();
        foreach (var employeeId in employeeIds)
        {
            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee != null)
            {
                employees[employeeId] = employee.EmployeeName;
            }
        }
        
        foreach (var dto in dtos)
        {
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
        }
        
        var response = new PagedResponse<EmployeeTaxDeclarationDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get tax declaration by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeTaxDeclarationDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var declaration = await _repository.GetByIdAsync(id);
        if (declaration == null)
            return NotFound($"Tax declaration with ID {id} not found");

        var dto = _mapper.Map<EmployeeTaxDeclarationDto>(declaration);
        
        // Populate employee name
        var employee = await _employeesRepository.GetByIdAsync(declaration.EmployeeId);
        if (employee != null)
        {
            dto.EmployeeName = employee.EmployeeName;
        }
        
        return Ok(dto);
    }

    /// <summary>
    /// Get tax declaration for an employee by financial year
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    [ProducesResponseType(typeof(EmployeeTaxDeclarationDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByEmployeeId(
        Guid employeeId,
        [FromQuery] string? financialYear = null)
    {
        // Get employee name once
        var employee = await _employeesRepository.GetByIdAsync(employeeId);
        var employeeName = employee?.EmployeeName;
        
        if (string.IsNullOrEmpty(financialYear))
        {
            // Get all declarations for employee
            var declarations = await _repository.GetByEmployeeIdAsync(employeeId);
            var dtos = _mapper.Map<IEnumerable<EmployeeTaxDeclarationDto>>(declarations).ToList();
            
            // Populate employee names
            foreach (var dto in dtos)
            {
                dto.EmployeeName = employeeName;
            }
            
            return Ok(dtos);
        }
        else
        {
            // Get specific financial year
            var declaration = await _repository.GetByEmployeeAndYearAsync(employeeId, financialYear);
            if (declaration == null)
                return NotFound($"Tax declaration not found for employee {employeeId} for financial year {financialYear}");

            var dto = _mapper.Map<EmployeeTaxDeclarationDto>(declaration);
            dto.EmployeeName = employeeName;
            return Ok(dto);
        }
    }

    /// <summary>
    /// Get pending verifications
    /// </summary>
    [HttpGet("pending-verification")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeTaxDeclarationDto>), 200)]
    public async Task<IActionResult> GetPendingVerification([FromQuery] string? financialYear = null)
    {
        var declarations = await _repository.GetPendingVerificationAsync(financialYear);
        var dtos = _mapper.Map<IEnumerable<EmployeeTaxDeclarationDto>>(declarations).ToList();
        
        // Populate employee names
        var employeeIds = dtos.Select(d => d.EmployeeId).Distinct().ToList();
        var employees = new Dictionary<Guid, string>();
        foreach (var employeeId in employeeIds)
        {
            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee != null)
            {
                employees[employeeId] = employee.EmployeeName;
            }
        }
        
        foreach (var dto in dtos)
        {
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
        }
        
        return Ok(dtos);
    }

    /// <summary>
    /// Create a new tax declaration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeTaxDeclarationDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeTaxDeclarationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if declaration already exists for this employee and financial year
        var exists = await _repository.ExistsForEmployeeAndYearAsync(dto.EmployeeId, dto.FinancialYear);
        if (exists)
            return Conflict($"Tax declaration already exists for employee {dto.EmployeeId} for financial year {dto.FinancialYear}");

        var entity = _mapper.Map<EmployeeTaxDeclaration>(dto);
        entity.Status = "draft";
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<EmployeeTaxDeclarationDto>(created));
    }

    /// <summary>
    /// Update a tax declaration
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeTaxDeclarationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Tax declaration with ID {id} not found");

        // Cannot update if locked or verified
        if (existing.Status == "locked" || existing.Status == "verified")
            return BadRequest($"Cannot update a {existing.Status} tax declaration");

        // Update fields
        if (!string.IsNullOrEmpty(dto.TaxRegime))
            existing.TaxRegime = dto.TaxRegime;
        if (dto.Sec80cPpf.HasValue)
            existing.Sec80cPpf = dto.Sec80cPpf.Value;
        if (dto.Sec80cElss.HasValue)
            existing.Sec80cElss = dto.Sec80cElss.Value;
        if (dto.Sec80cLifeInsurance.HasValue)
            existing.Sec80cLifeInsurance = dto.Sec80cLifeInsurance.Value;
        if (dto.Sec80cHomeLoanPrincipal.HasValue)
            existing.Sec80cHomeLoanPrincipal = dto.Sec80cHomeLoanPrincipal.Value;
        if (dto.Sec80cChildrenTuition.HasValue)
            existing.Sec80cChildrenTuition = dto.Sec80cChildrenTuition.Value;
        if (dto.Sec80cNsc.HasValue)
            existing.Sec80cNsc = dto.Sec80cNsc.Value;
        if (dto.Sec80cSukanyaSamriddhi.HasValue)
            existing.Sec80cSukanyaSamriddhi = dto.Sec80cSukanyaSamriddhi.Value;
        if (dto.Sec80cFixedDeposit.HasValue)
            existing.Sec80cFixedDeposit = dto.Sec80cFixedDeposit.Value;
        if (dto.Sec80cOthers.HasValue)
            existing.Sec80cOthers = dto.Sec80cOthers.Value;
        if (dto.Sec80ccdNps.HasValue)
            existing.Sec80ccdNps = dto.Sec80ccdNps.Value;
        if (dto.Sec80dSelfFamily.HasValue)
            existing.Sec80dSelfFamily = dto.Sec80dSelfFamily.Value;
        if (dto.Sec80dParents.HasValue)
            existing.Sec80dParents = dto.Sec80dParents.Value;
        if (dto.Sec80dPreventiveCheckup.HasValue)
            existing.Sec80dPreventiveCheckup = dto.Sec80dPreventiveCheckup.Value;
        if (dto.Sec80dSelfSeniorCitizen.HasValue)
            existing.Sec80dSelfSeniorCitizen = dto.Sec80dSelfSeniorCitizen.Value;
        if (dto.Sec80dParentsSeniorCitizen.HasValue)
            existing.Sec80dParentsSeniorCitizen = dto.Sec80dParentsSeniorCitizen.Value;
        if (dto.Sec80eEducationLoan.HasValue)
            existing.Sec80eEducationLoan = dto.Sec80eEducationLoan.Value;
        if (dto.Sec24HomeLoanInterest.HasValue)
            existing.Sec24HomeLoanInterest = dto.Sec24HomeLoanInterest.Value;
        if (dto.Sec80gDonations.HasValue)
            existing.Sec80gDonations = dto.Sec80gDonations.Value;
        if (dto.Sec80ttaSavingsInterest.HasValue)
            existing.Sec80ttaSavingsInterest = dto.Sec80ttaSavingsInterest.Value;
        if (dto.HraRentPaidAnnual.HasValue)
            existing.HraRentPaidAnnual = dto.HraRentPaidAnnual.Value;
        if (dto.HraMetroCity.HasValue)
            existing.HraMetroCity = dto.HraMetroCity.Value;
        if (!string.IsNullOrEmpty(dto.HraLandlordPan))
            existing.HraLandlordPan = dto.HraLandlordPan;
        if (!string.IsNullOrEmpty(dto.HraLandlordName))
            existing.HraLandlordName = dto.HraLandlordName;
        if (dto.OtherIncomeAnnual.HasValue)
            existing.OtherIncomeAnnual = dto.OtherIncomeAnnual.Value;
        if (dto.PrevEmployerIncome.HasValue)
            existing.PrevEmployerIncome = dto.PrevEmployerIncome.Value;
        if (dto.PrevEmployerTds.HasValue)
            existing.PrevEmployerTds = dto.PrevEmployerTds.Value;
        if (dto.PrevEmployerPf.HasValue)
            existing.PrevEmployerPf = dto.PrevEmployerPf.Value;
        if (dto.PrevEmployerPt.HasValue)
            existing.PrevEmployerPt = dto.PrevEmployerPt.Value;
        if (!string.IsNullOrEmpty(dto.ProofDocuments))
            existing.ProofDocuments = dto.ProofDocuments;

        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Submit a tax declaration (draft/rejected → submitted)
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Submit(Guid id, [FromQuery] string? submittedBy = null)
    {
        var declaration = await _repository.GetByIdAsync(id);
        if (declaration == null)
            return NotFound($"Tax declaration with ID {id} not found");

        // Allow both draft and rejected declarations to be submitted
        if (declaration.Status != "draft" && declaration.Status != "rejected")
            return BadRequest($"Only draft or rejected declarations can be submitted. Current status: {declaration.Status}");

        // Update status and submitted date together
        declaration.Status = "submitted";
        declaration.SubmittedAt = DateTime.UtcNow;
        declaration.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(declaration);

        return NoContent();
    }

    /// <summary>
    /// Verify a tax declaration (submitted → verified)
    /// </summary>
    [HttpPost("{id}/verify")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Verify(Guid id, [FromQuery] string? verifiedBy = null)
    {
        var declaration = await _repository.GetByIdAsync(id);
        if (declaration == null)
            return NotFound($"Tax declaration with ID {id} not found");

        if (declaration.Status != "submitted")
            return BadRequest($"Only submitted declarations can be verified. Current status: {declaration.Status}");

        // Update status and verified date together
        declaration.Status = "verified";
        declaration.VerifiedBy = verifiedBy;
        declaration.VerifiedAt = DateTime.UtcNow;
        declaration.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(declaration);

        return NoContent();
    }

    /// <summary>
    /// Lock tax declarations for a financial year (prevents further edits)
    /// </summary>
    [HttpPost("lock")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Lock([FromQuery] string financialYear)
    {
        await _repository.LockDeclarationsAsync(financialYear);
        return NoContent();
    }

    /// <summary>
    /// Delete a tax declaration
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var declaration = await _repository.GetByIdAsync(id);
        if (declaration == null)
            return NotFound($"Tax declaration with ID {id} not found");

        if (declaration.Status == "locked" || declaration.Status == "verified")
            return BadRequest($"Cannot delete a {declaration.Status} tax declaration");

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Reject a submitted tax declaration (submitted → rejected)
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectDeclarationDto dto, [FromQuery] string? rejectedBy = null)
    {
        if (_taxDeclarationService == null)
        {
            // Fallback if service not registered
            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
                return NotFound($"Tax declaration with ID {id} not found");

            if (declaration.Status != "submitted")
                return BadRequest($"Only submitted declarations can be rejected. Current status: {declaration.Status}");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest("Rejection reason is required");

            await _repository.UpdateStatusWithRejectionAsync(id, "rejected", rejectedBy, dto.Reason);
            return NoContent();
        }

        var result = await _taxDeclarationService.RejectAsync(id, dto, rejectedBy ?? "system");
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                Core.Common.ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Revise a rejected declaration and resubmit (rejected → submitted)
    /// </summary>
    [HttpPost("{id}/revise")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Revise(Guid id, [FromBody] UpdateEmployeeTaxDeclarationDto dto, [FromQuery] string? submittedBy = null)
    {
        if (_taxDeclarationService == null)
        {
            // Fallback if service not registered
            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
                return NotFound($"Tax declaration with ID {id} not found");

            if (declaration.Status != "rejected")
                return BadRequest($"Only rejected declarations can be revised. Current status: {declaration.Status}");

            // Clear rejection and resubmit
            await _repository.ClearRejectionAsync(id);
            await _repository.IncrementRevisionCountAsync(id);
            await _repository.UpdateStatusAsync(id, "submitted");
            return NoContent();
        }

        var result = await _taxDeclarationService.ReviseAndResubmitAsync(id, dto, submittedBy ?? "system");
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                Core.Common.ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Get audit history for a tax declaration
    /// </summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(IEnumerable<DeclarationHistoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        if (_taxDeclarationService == null)
            return NotFound("Tax declaration service not available");

        var result = await _taxDeclarationService.GetHistoryAsync(id);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Validate a tax declaration without saving
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(TaxDeclarationSummaryDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Validate([FromBody] CreateEmployeeTaxDeclarationDto dto)
    {
        if (_taxDeclarationService == null)
            return BadRequest("Tax declaration service not available");

        var result = await _taxDeclarationService.ValidateAsync(dto, dto.EmployeeId);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Core.Common.ErrorType.Validation => BadRequest(new { error = result.Error.Message, summary = result.Value }),
                Core.Common.ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get tax declaration summary with capped deduction values
    /// </summary>
    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(TaxDeclarationSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSummary(Guid id)
    {
        if (_taxDeclarationService == null)
            return NotFound("Tax declaration service not available");

        var result = await _taxDeclarationService.GetSummaryAsync(id);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Core.Common.ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get rejected declarations pending revision
    /// </summary>
    [HttpGet("rejected")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeTaxDeclarationDto>), 200)]
    public async Task<IActionResult> GetRejectedDeclarations([FromQuery] string? financialYear = null)
    {
        var declarations = await _repository.GetRejectedDeclarationsAsync(financialYear);
        var dtos = _mapper.Map<IEnumerable<EmployeeTaxDeclarationDto>>(declarations).ToList();

        // Populate employee names
        var employeeIds = dtos.Select(d => d.EmployeeId).Distinct().ToList();
        var employees = new Dictionary<Guid, string>();
        foreach (var employeeId in employeeIds)
        {
            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee != null)
            {
                employees[employeeId] = employee.EmployeeName;
            }
        }

        foreach (var dto in dtos)
        {
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Unlock a locked declaration (admin action)
    /// </summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Unlock(Guid id, [FromQuery] string? unlockedBy = null)
    {
        var declaration = await _repository.GetByIdAsync(id);
        if (declaration == null)
            return NotFound($"Tax declaration with ID {id} not found");

        if (declaration.Status != "locked")
            return BadRequest($"Only locked declarations can be unlocked. Current status: {declaration.Status}");

        // Unlock back to verified status
        await _repository.UpdateStatusAsync(id, "verified");
        return NoContent();
    }
}





