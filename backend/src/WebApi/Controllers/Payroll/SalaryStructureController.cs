using Application.DTOs.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Employee salary structure management endpoints
/// </summary>
[ApiController]
[Route("api/payroll/salary-structures")]
[Produces("application/json")]
public class SalaryStructureController : ControllerBase
{
    private readonly IEmployeeSalaryStructureRepository _repository;
    private readonly IMapper _mapper;
    private readonly IEmployeePayrollInfoRepository _payrollInfoRepository;
    private readonly IEmployeesRepository _employeesRepository;

    public SalaryStructureController(
        IEmployeeSalaryStructureRepository repository,
        IEmployeePayrollInfoRepository payrollInfoRepository,
        IEmployeesRepository employeesRepository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _payrollInfoRepository = payrollInfoRepository ?? throw new ArgumentNullException(nameof(payrollInfoRepository));
        _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Get all salary structures with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeSalaryStructureDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeSalaryStructureFilterRequest request)
    {
        var filters = request.GetFilters();

        // When searching, support searching by employee name by resolving matching employee IDs
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var employeeFilters = new Dictionary<string, object>();
            if (request.CompanyId.HasValue)
            {
                employeeFilters["company_id"] = request.CompanyId.Value;
            }

            // Use a reasonably high page size to capture matching employees; this is only for ID lookup
            var (matchingEmployees, _) = await _employeesRepository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 500,
                searchTerm: request.SearchTerm,
                sortBy: "employee_name",
                sortDescending: false,
                filters: employeeFilters);

            var matchingEmployeeIds = matchingEmployees.Select(e => e.Id).Distinct().ToList();

            // If no employees match the search term, return an empty page early
            if (matchingEmployeeIds.Count == 0)
            {
                var empty = new PagedResponse<EmployeeSalaryStructureDto>(
                    Enumerable.Empty<EmployeeSalaryStructureDto>(),
                    0,
                    request.PageNumber,
                    request.PageSize);
                return Ok(empty);
            }

            // Use an IN filter on employee_id for matching employees
            filters["employee_id_in"] = matchingEmployeeIds;
        }

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            null, // search term handled via employee name above
            request.SortBy,
            request.SortDescending,
            filters);

        var dtos = _mapper.Map<IEnumerable<EmployeeSalaryStructureDto>>(items).ToList();
        
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
        
        var response = new PagedResponse<EmployeeSalaryStructureDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all salary structures with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeSalaryStructureDto>), 200)]
    public async Task<IActionResult> GetAllPaged([FromQuery] EmployeeSalaryStructureFilterRequest request)
    {
        var filters = request.GetFilters();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var employeeFilters = new Dictionary<string, object>();
            if (request.CompanyId.HasValue)
            {
                employeeFilters["company_id"] = request.CompanyId.Value;
            }

            var (matchingEmployees, _) = await _employeesRepository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 500,
                searchTerm: request.SearchTerm,
                sortBy: "employee_name",
                sortDescending: false,
                filters: employeeFilters);

            var matchingEmployeeIds = matchingEmployees.Select(e => e.Id).Distinct().ToList();

            if (matchingEmployeeIds.Count == 0)
            {
                var empty = new PagedResponse<EmployeeSalaryStructureDto>(
                    Enumerable.Empty<EmployeeSalaryStructureDto>(),
                    0,
                    request.PageNumber,
                    request.PageSize);
                return Ok(empty);
            }

            filters["employee_id_in"] = matchingEmployeeIds;
        }

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            null,
            request.SortBy,
            request.SortDescending,
            filters);

        var dtos = _mapper.Map<IEnumerable<EmployeeSalaryStructureDto>>(items).ToList();
        
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
        
        var response = new PagedResponse<EmployeeSalaryStructureDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get salary structure by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeSalaryStructureDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var structure = await _repository.GetByIdAsync(id);
        if (structure == null)
            return NotFound($"Salary structure with ID {id} not found");

        return Ok(_mapper.Map<EmployeeSalaryStructureDto>(structure));
    }

    /// <summary>
    /// Get current salary structure for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    [ProducesResponseType(typeof(EmployeeSalaryStructureDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCurrentByEmployeeId(Guid employeeId)
    {
        var structure = await _repository.GetCurrentByEmployeeIdAsync(employeeId);
        if (structure == null)
            return NotFound($"No active salary structure found for employee {employeeId}");

        return Ok(_mapper.Map<EmployeeSalaryStructureDto>(structure));
    }

    /// <summary>
    /// Get salary structure history for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}/history")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeSalaryStructureDto>), 200)]
    public async Task<IActionResult> GetHistoryByEmployeeId(Guid employeeId)
    {
        var structures = await _repository.GetHistoryByEmployeeIdAsync(employeeId);
        var dtos = _mapper.Map<IEnumerable<EmployeeSalaryStructureDto>>(structures);
        return Ok(dtos);
    }

    /// <summary>
    /// Get effective salary structure as of a specific date
    /// </summary>
    [HttpGet("employee/{employeeId}/effective")]
    [ProducesResponseType(typeof(EmployeeSalaryStructureDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEffectiveAsOfDate(
        Guid employeeId,
        [FromQuery] DateTime asOfDate)
    {
        var structure = await _repository.GetEffectiveAsOfDateAsync(employeeId, asOfDate);
        if (structure == null)
            return NotFound($"No salary structure found for employee {employeeId} effective as of {asOfDate:yyyy-MM-dd}");

        return Ok(_mapper.Map<EmployeeSalaryStructureDto>(structure));
    }

    /// <summary>
    /// Get salary breakdown (for CTC calculator)
    /// </summary>
    [HttpGet("{id}/breakdown")]
    [ProducesResponseType(typeof(SalaryBreakdownDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBreakdown(Guid id)
    {
        var structure = await _repository.GetByIdAsync(id);
        if (structure == null)
            return NotFound($"Salary structure with ID {id} not found");

        var breakdown = new SalaryBreakdownDto
        {
            AnnualCtc = structure.AnnualCtc,
            MonthlyCtc = structure.AnnualCtc / 12,
            BasicSalary = structure.BasicSalary,
            Hra = structure.Hra,
            DearnessAllowance = structure.DearnessAllowance,
            ConveyanceAllowance = structure.ConveyanceAllowance,
            MedicalAllowance = structure.MedicalAllowance,
            SpecialAllowance = structure.SpecialAllowance,
            OtherAllowances = structure.OtherAllowances,
            MonthlyGross = structure.MonthlyGross,
            PfEmployer = structure.PfEmployerMonthly,
            EsiEmployer = structure.EsiEmployerMonthly,
            Gratuity = structure.GratuityMonthly,
            LtaAnnual = structure.LtaAnnual,
            BonusAnnual = structure.BonusAnnual
        };

        return Ok(breakdown);
    }

    /// <summary>
    /// Create a new salary structure
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeSalaryStructureDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeSalaryStructureDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check for overlapping structures
            var hasOverlap = await _repository.HasOverlappingStructureAsync(
                dto.EmployeeId,
                dto.EffectiveFrom,
                null,
                null);
            if (hasOverlap)
                return Conflict($"A salary structure already exists for this employee with overlapping effective dates");

            // Deactivate previous structures
            await _repository.DeactivatePreviousStructuresAsync(dto.EmployeeId, dto.EffectiveFrom);

            // Ensure payroll info exists for this employee (required for payroll processing)
            var payrollInfo = await _payrollInfoRepository.GetByEmployeeIdAsync(dto.EmployeeId);
            if (payrollInfo == null)
            {
                // Auto-create payroll info with sensible defaults when salary structure is created
                payrollInfo = new EmployeePayrollInfo
                {
                    EmployeeId = dto.EmployeeId,
                    CompanyId = dto.CompanyId,
                    PayrollType = "employee", // Default to employee
                    IsPfApplicable = true,
                    IsEsiApplicable = false,
                    IsPtApplicable = true,
                    TaxRegime = "new",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _payrollInfoRepository.AddAsync(payrollInfo);
            }

            var entity = _mapper.Map<EmployeeSalaryStructure>(dto);
            
            // Ensure EffectiveFrom is date-only (no time component) for DATE column
            entity.EffectiveFrom = entity.EffectiveFrom.Date;
            if (entity.EffectiveTo.HasValue)
            {
                entity.EffectiveTo = entity.EffectiveTo.Value.Date;
            }
            
            entity.IsActive = true;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Calculate monthly gross if not provided (LTA is paid monthly in Indian payroll)
            if (entity.MonthlyGross == 0)
            {
                entity.MonthlyGross = entity.BasicSalary + entity.Hra + entity.DearnessAllowance +
                                     entity.ConveyanceAllowance + entity.MedicalAllowance +
                                     entity.SpecialAllowance + entity.OtherAllowances +
                                     (entity.LtaAnnual / 12);
            }

            var created = await _repository.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<EmployeeSalaryStructureDto>(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, details = ex.ToString() });
        }
    }

    /// <summary>
    /// Update a salary structure
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeSalaryStructureDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Salary structure with ID {id} not found");

        // Check for overlapping structures if effective date is being changed
        if (dto.EffectiveFrom.HasValue && dto.EffectiveFrom.Value != existing.EffectiveFrom)
        {
            var hasOverlap = await _repository.HasOverlappingStructureAsync(
                existing.EmployeeId,
                dto.EffectiveFrom.Value,
                dto.EffectiveTo,
                id);
            if (hasOverlap)
                return Conflict($"A salary structure already exists for this employee with overlapping effective dates");
        }

        // Update fields
        if (dto.EffectiveFrom.HasValue)
            existing.EffectiveFrom = dto.EffectiveFrom.Value;
        if (dto.EffectiveTo.HasValue)
            existing.EffectiveTo = dto.EffectiveTo;
        if (dto.AnnualCtc.HasValue)
            existing.AnnualCtc = dto.AnnualCtc.Value;
        if (dto.BasicSalary.HasValue)
            existing.BasicSalary = dto.BasicSalary.Value;
        if (dto.Hra.HasValue)
            existing.Hra = dto.Hra.Value;
        if (dto.DearnessAllowance.HasValue)
            existing.DearnessAllowance = dto.DearnessAllowance.Value;
        if (dto.ConveyanceAllowance.HasValue)
            existing.ConveyanceAllowance = dto.ConveyanceAllowance.Value;
        if (dto.MedicalAllowance.HasValue)
            existing.MedicalAllowance = dto.MedicalAllowance.Value;
        if (dto.SpecialAllowance.HasValue)
            existing.SpecialAllowance = dto.SpecialAllowance.Value;
        if (dto.OtherAllowances.HasValue)
            existing.OtherAllowances = dto.OtherAllowances.Value;
        if (dto.LtaAnnual.HasValue)
            existing.LtaAnnual = dto.LtaAnnual.Value;
        if (dto.BonusAnnual.HasValue)
            existing.BonusAnnual = dto.BonusAnnual.Value;
        if (dto.PfEmployerMonthly.HasValue)
            existing.PfEmployerMonthly = dto.PfEmployerMonthly.Value;
        if (dto.EsiEmployerMonthly.HasValue)
            existing.EsiEmployerMonthly = dto.EsiEmployerMonthly.Value;
        if (dto.GratuityMonthly.HasValue)
            existing.GratuityMonthly = dto.GratuityMonthly.Value;
        if (dto.IsActive.HasValue)
            existing.IsActive = dto.IsActive.Value;
        if (!string.IsNullOrEmpty(dto.RevisionReason))
            existing.RevisionReason = dto.RevisionReason;
        if (!string.IsNullOrEmpty(dto.ApprovedBy))
        {
            existing.ApprovedBy = dto.ApprovedBy;
            existing.ApprovedAt = DateTime.UtcNow;
        }

        // Recalculate monthly gross (LTA is paid monthly in Indian payroll)
        existing.MonthlyGross = existing.BasicSalary + existing.Hra + existing.DearnessAllowance +
                               existing.ConveyanceAllowance + existing.MedicalAllowance +
                               existing.SpecialAllowance + existing.OtherAllowances +
                               (existing.LtaAnnual / 12);

        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Delete a salary structure
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var structure = await _repository.GetByIdAsync(id);
        if (structure == null)
            return NotFound($"Salary structure with ID {id} not found");

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}





