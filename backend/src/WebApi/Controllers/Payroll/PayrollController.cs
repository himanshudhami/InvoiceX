using Application.DTOs.Payroll;
using Application.Services.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Payroll management and processing endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollRunRepository _payrollRunRepository;
    private readonly IPayrollTransactionRepository _transactionRepository;
    private readonly IEmployeePayrollInfoRepository _payrollInfoRepository;
    private readonly IEmployeeSalaryStructureRepository _salaryStructureRepository;
    private readonly ICompanyStatutoryConfigRepository _companyConfigRepository;
    private readonly IEmployeesRepository _employeesRepository;
    private readonly ICompaniesRepository _companiesRepository;
    private readonly IPayrollCalculationLineRepository _calculationLineRepository;
    private readonly ITaxParameterRepository _taxParameterRepository;
    private readonly ISalaryComponentRepository _salaryComponentRepository;
    private readonly PayrollCalculationService _calculationService;
    private readonly IMapper _mapper;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollRunRepository payrollRunRepository,
        IPayrollTransactionRepository transactionRepository,
        IEmployeePayrollInfoRepository payrollInfoRepository,
        IEmployeeSalaryStructureRepository salaryStructureRepository,
        ICompanyStatutoryConfigRepository companyConfigRepository,
        IEmployeesRepository employeesRepository,
        ICompaniesRepository companiesRepository,
        IPayrollCalculationLineRepository calculationLineRepository,
        ITaxParameterRepository taxParameterRepository,
        ISalaryComponentRepository salaryComponentRepository,
        PayrollCalculationService calculationService,
        IMapper mapper,
        ILogger<PayrollController> logger)
    {
        _payrollRunRepository = payrollRunRepository;
        _transactionRepository = transactionRepository;
        _payrollInfoRepository = payrollInfoRepository;
        _salaryStructureRepository = salaryStructureRepository;
        _companyConfigRepository = companyConfigRepository;
        _employeesRepository = employeesRepository;
        _companiesRepository = companiesRepository;
        _calculationLineRepository = calculationLineRepository;
        _taxParameterRepository = taxParameterRepository;
        _salaryComponentRepository = salaryComponentRepository;
        _calculationService = calculationService;
        _mapper = mapper;
        _logger = logger;
    }

    // ==================== Payroll Runs ====================

    /// <summary>
    /// Get all payroll runs with pagination
    /// </summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(PagedResponse<PayrollRunDto>), 200)]
    public async Task<IActionResult> GetPayrollRuns([FromQuery] PayrollRunFilterRequest request)
    {
        var (items, totalCount) = await _payrollRunRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<PayrollRunDto>>(items).ToList();
        
        // Populate company names
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
        
        var response = new PagedResponse<PayrollRunDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all payroll runs with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("runs/paged")]
    [ProducesResponseType(typeof(PagedResponse<PayrollRunDto>), 200)]
    public async Task<IActionResult> GetPayrollRunsPaged([FromQuery] PayrollRunFilterRequest request)
    {
        var (items, totalCount) = await _payrollRunRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<PayrollRunDto>>(items).ToList();
        
        // Populate company names
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
        
        var response = new PagedResponse<PayrollRunDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get a payroll run by ID
    /// </summary>
    [HttpGet("runs/{id}")]
    [ProducesResponseType(typeof(PayrollRunDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayrollRunById(Guid id)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        var dto = _mapper.Map<PayrollRunDto>(payrollRun);
        
        // Populate company name
        var company = await _companiesRepository.GetByIdAsync(payrollRun.CompanyId);
        if (company != null)
        {
            dto.CompanyName = company.Name;
        }
        
        return Ok(dto);
    }

    /// <summary>
    /// Create a new payroll run (draft)
    /// </summary>
    [HttpPost("runs")]
    [ProducesResponseType(typeof(PayrollRunDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreatePayrollRun([FromBody] CreatePayrollRunDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if run already exists
        var exists = await _payrollRunRepository.ExistsForCompanyAndMonthAsync(
            dto.CompanyId, dto.PayrollMonth, dto.PayrollYear);
        if (exists)
            return Conflict($"Payroll run already exists for {dto.PayrollMonth}/{dto.PayrollYear}");

        var entity = _mapper.Map<PayrollRun>(dto);
        var created = await _payrollRunRepository.AddAsync(entity);

        return CreatedAtAction(nameof(GetPayrollRunById), new { id = created.Id }, _mapper.Map<PayrollRunDto>(created));
    }

    /// <summary>
    /// Get payroll preview (estimated totals before processing)
    /// </summary>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(PayrollPreviewDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetPayrollPreview(
        [FromQuery] Guid companyId,
        [FromQuery] int payrollMonth,
        [FromQuery] int payrollYear,
        [FromQuery] bool includeContractors = false)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required");

        // Get all active employees for this company
        var employees = await _payrollInfoRepository.GetActiveEmployeesForPayrollAsync(
            companyId, includeContractors ? null : "employee");

        var employeeCount = 0;
        var totalMonthlyGross = 0m;
        var totalPfEmployee = 0m;
        var totalPfEmployer = 0m;
        var totalEsiEmployee = 0m;
        var totalEsiEmployer = 0m;
        var totalPt = 0m;
        var totalTds = 0m;
        var employeesWithoutStructure = new List<string>();

        foreach (var empInfo in employees)
        {
            var salaryStructure = await _salaryStructureRepository.GetCurrentByEmployeeIdAsync(empInfo.EmployeeId);
            if (salaryStructure == null)
            {
                // Get employee name for the list
                var employee = await _employeesRepository.GetByIdAsync(empInfo.EmployeeId);
                if (employee != null)
                    employeesWithoutStructure.Add(employee.EmployeeName);
                continue;
            }

            employeeCount++;
            totalMonthlyGross += salaryStructure.MonthlyGross;
            totalPfEmployee += Math.Min(salaryStructure.BasicSalary, 15000) * 0.12m; // 12% of basic (capped)
            totalPfEmployer += salaryStructure.PfEmployerMonthly;
            totalEsiEmployee += salaryStructure.EsiEmployerMonthly > 0 ? salaryStructure.MonthlyGross * 0.0075m : 0; // 0.75% if applicable
            totalEsiEmployer += salaryStructure.EsiEmployerMonthly;
            totalPt += 200; // Approximate PT
        }

        var totalDeductions = totalPfEmployee + totalEsiEmployee + totalPt + totalTds;
        var totalNetPay = totalMonthlyGross - totalDeductions;

        return Ok(new PayrollPreviewDto
        {
            CompanyId = companyId,
            PayrollMonth = payrollMonth,
            PayrollYear = payrollYear,
            EmployeeCount = employeeCount,
            TotalMonthlyGross = totalMonthlyGross,
            TotalPfEmployee = totalPfEmployee,
            TotalPfEmployer = totalPfEmployer,
            TotalEsiEmployee = totalEsiEmployee,
            TotalEsiEmployer = totalEsiEmployer,
            TotalPt = totalPt,
            TotalTds = totalTds,
            TotalDeductions = totalDeductions,
            TotalNetPay = totalNetPay,
            EmployeesWithoutStructure = employeesWithoutStructure
        });
    }

    /// <summary>
    /// Process payroll for a company
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PayrollRunSummaryDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ProcessPayroll([FromBody] ProcessPayrollDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify company statutory config exists before processing
            var companyConfig = await _companyConfigRepository.GetByCompanyIdAsync(dto.CompanyId);
            if (companyConfig == null)
            {
                return BadRequest($"No active statutory configuration found for company {dto.CompanyId}. Please configure statutory settings first.");
            }

            // Get or create payroll run
            var payrollRun = await _payrollRunRepository.GetByCompanyAndMonthAsync(
                dto.CompanyId, dto.PayrollMonth, dto.PayrollYear);

        if (payrollRun == null)
        {
            payrollRun = new PayrollRun
            {
                CompanyId = dto.CompanyId,
                PayrollMonth = dto.PayrollMonth,
                PayrollYear = dto.PayrollYear,
                FinancialYear = GetFinancialYear(dto.PayrollMonth, dto.PayrollYear),
                Status = "processing",
                CreatedBy = dto.ProcessedBy
            };
            payrollRun = await _payrollRunRepository.AddAsync(payrollRun);
        }
        else if (payrollRun.Status == "paid")
        {
            return BadRequest("Cannot reprocess a paid payroll run");
        }
        else
        {
            await _payrollRunRepository.UpdateStatusAsync(payrollRun.Id, "processing", dto.ProcessedBy);
        }

        // Get all active employees for this company (must have payroll info)
        var employees = await _payrollInfoRepository.GetActiveEmployeesForPayrollAsync(
            dto.CompanyId, dto.IncludeContractors ? null : "employee");

        var totalGross = 0m;
        var totalDeductions = 0m;
        var totalNet = 0m;
        var totalEmployerPf = 0m;
        var totalEmployerEsi = 0m;
        var totalEmployerCost = 0m;
        var employeeCount = 0;
        var contractorCount = 0;

        foreach (var empInfo in employees)
        {
            // Get salary structure
            var salaryStructure = await _salaryStructureRepository.GetCurrentByEmployeeIdAsync(empInfo.EmployeeId);
            if (salaryStructure == null)
                continue;

            // Calculate payroll (returns transaction + calculation lines for auditability)
            // LTA is paid monthly in Indian payroll (tax exemption is claimed annually)
            var monthlyLta = salaryStructure.LtaAnnual / 12;
            var result = await _calculationService.CalculateEmployeePayrollAsync(
                empInfo,
                salaryStructure,
                dto.PayrollMonth,
                dto.PayrollYear,
                workingDays: 30, // TODO: Get from calendar
                presentDays: 30,
                ltaPaid: monthlyLta);

            var transaction = result.Transaction;
            transaction.PayrollRunId = payrollRun.Id;

            // Check if transaction already exists
            var existingTxn = await _transactionRepository.GetByEmployeeAndMonthAsync(
                empInfo.EmployeeId, dto.PayrollMonth, dto.PayrollYear);

            if (existingTxn != null)
            {
                transaction.Id = existingTxn.Id;
                await _transactionRepository.UpdateAsync(transaction);

                // Delete existing calculation lines and recreate
                await _calculationLineRepository.DeleteByTransactionIdAsync(transaction.Id);
            }
            else
            {
                await _transactionRepository.AddAsync(transaction);
            }

            // Save calculation lines for auditability (Rule 9)
            foreach (var line in result.CalculationLines)
            {
                line.TransactionId = transaction.Id;
                await _calculationLineRepository.AddAsync(line);
            }

            // Update totals
            totalGross += transaction.GrossEarnings;
            totalDeductions += transaction.TotalDeductions;
            totalNet += transaction.NetPayable;
            totalEmployerPf += transaction.PfEmployer;
            totalEmployerEsi += transaction.EsiEmployer;
            totalEmployerCost += transaction.TotalEmployerCost;

            if (empInfo.PayrollType == "contractor")
                contractorCount++;
            else
                employeeCount++;
        }

        // Update payroll run totals
        await _payrollRunRepository.UpdateTotalsAsync(
            payrollRun.Id,
            employeeCount,
            contractorCount,
            totalGross,
            totalDeductions,
            totalNet,
            totalEmployerPf,
            totalEmployerEsi,
            totalEmployerCost);

        await _payrollRunRepository.UpdateStatusAsync(payrollRun.Id, "computed", dto.ProcessedBy);

        return Ok(new PayrollRunSummaryDto
        {
            PayrollRunId = payrollRun.Id,
            MonthYear = $"{dto.PayrollMonth:D2}/{dto.PayrollYear}",
            Status = "computed",
            TotalEmployees = employeeCount,
            TotalContractors = contractorCount,
            TotalGross = totalGross,
            TotalDeductions = totalDeductions,
            TotalNet = totalNet,
            TotalEmployerCost = totalEmployerCost,
            TotalPfEmployer = totalEmployerPf,
            TotalEsiEmployer = totalEmployerEsi
        });
        }
        catch (Exception ex)
        {
            // Log the exception with full details
            _logger.LogError(ex, 
                "Error processing payroll for company {CompanyId}, month {Month}, year {Year}",
                dto.CompanyId, dto.PayrollMonth, dto.PayrollYear);
            
            // Re-throw to let ExceptionHandlerMiddleware handle it properly
            throw;
        }
    }

    /// <summary>
    /// Approve a payroll run
    /// </summary>
    [HttpPost("runs/{id}/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApprovePayrollRun(Guid id, [FromQuery] string? approvedBy)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        if (payrollRun.Status != "computed")
            return BadRequest("Only computed payroll runs can be approved");

        await _payrollRunRepository.UpdateStatusAsync(id, "approved", approvedBy);
        return NoContent();
    }

    /// <summary>
    /// Mark payroll as paid
    /// </summary>
    [HttpPost("runs/{id}/pay")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkPayrollAsPaid(Guid id, [FromBody] UpdatePayrollRunDto dto)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        if (payrollRun.Status != "approved")
            return BadRequest("Only approved payroll runs can be marked as paid");

        payrollRun.PaymentReference = dto.PaymentReference;
        payrollRun.PaymentMode = dto.PaymentMode;
        payrollRun.Remarks = dto.Remarks;
        await _payrollRunRepository.UpdateAsync(payrollRun);

        await _payrollRunRepository.UpdateStatusAsync(id, "paid", dto.UpdatedBy);

        // Update all transactions status
        var transactions = await _transactionRepository.GetByPayrollRunIdAsync(id);
        foreach (var txn in transactions)
        {
            await _transactionRepository.UpdateStatusAsync(txn.Id, "paid");
        }

        return NoContent();
    }

    // ==================== Transactions ====================

    /// <summary>
    /// Get payroll transactions with pagination
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PagedResponse<PayrollTransactionDto>), 200)]
    public async Task<IActionResult> GetTransactions([FromQuery] PayrollTransactionFilterRequest request)
    {
        var (items, totalCount) = await _transactionRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<PayrollTransactionDto>>(items).ToList();
        
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
        
        // Populate company names via payroll runs
        var payrollRunIds = dtos.Select(d => d.PayrollRunId).Distinct().ToList();
        var payrollRuns = new Dictionary<Guid, PayrollRun>();
        var companyIds = new HashSet<Guid>();
        foreach (var runId in payrollRunIds)
        {
            var run = await _payrollRunRepository.GetByIdAsync(runId);
            if (run != null)
            {
                payrollRuns[runId] = run;
                companyIds.Add(run.CompanyId);
            }
        }
        
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
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
            if (payrollRuns.TryGetValue(dto.PayrollRunId, out var run))
            {
                dto.CompanyName = companies.GetValueOrDefault(run.CompanyId);
            }
        }
        
        var response = new PagedResponse<PayrollTransactionDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get all payroll transactions with pagination (explicit paged route for frontend compatibility)
    /// </summary>
    [HttpGet("transactions/paged")]
    [ProducesResponseType(typeof(PagedResponse<PayrollTransactionDto>), 200)]
    public async Task<IActionResult> GetTransactionsPaged([FromQuery] PayrollTransactionFilterRequest request)
    {
        var (items, totalCount) = await _transactionRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            request.GetFilters());

        var dtos = _mapper.Map<IEnumerable<PayrollTransactionDto>>(items).ToList();
        
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
        
        // Populate company names via payroll runs
        var payrollRunIds = dtos.Select(d => d.PayrollRunId).Distinct().ToList();
        var payrollRuns = new Dictionary<Guid, PayrollRun>();
        var companyIds = new HashSet<Guid>();
        foreach (var runId in payrollRunIds)
        {
            var run = await _payrollRunRepository.GetByIdAsync(runId);
            if (run != null)
            {
                payrollRuns[runId] = run;
                companyIds.Add(run.CompanyId);
            }
        }
        
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
            dto.EmployeeName = employees.GetValueOrDefault(dto.EmployeeId);
            if (payrollRuns.TryGetValue(dto.PayrollRunId, out var run))
            {
                dto.CompanyName = companies.GetValueOrDefault(run.CompanyId);
            }
        }
        
        var response = new PagedResponse<PayrollTransactionDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get a single transaction
    /// </summary>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(typeof(PayrollTransactionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound($"Transaction with ID {id} not found");

        var dto = _mapper.Map<PayrollTransactionDto>(transaction);
        
        // Populate employee name
        var employee = await _employeesRepository.GetByIdAsync(transaction.EmployeeId);
        if (employee != null)
        {
            dto.EmployeeName = employee.EmployeeName;
        }
        
        // Populate company name via payroll run
        var payrollRun = await _payrollRunRepository.GetByIdAsync(transaction.PayrollRunId);
        if (payrollRun != null)
        {
            var company = await _companiesRepository.GetByIdAsync(payrollRun.CompanyId);
            if (company != null)
            {
                dto.CompanyName = company.Name;
            }
        }
        
        return Ok(dto);
    }

    /// <summary>
    /// Override TDS for a transaction
    /// </summary>
    [HttpPost("transactions/{id}/tds-override")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> OverrideTds(Guid id, [FromBody] TdsOverrideDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return NotFound($"Transaction with ID {id} not found");

        if (transaction.Status == "paid")
            return BadRequest("Cannot override TDS for a paid transaction");

        await _transactionRepository.UpdateTdsOverrideAsync(id, dto.TdsAmount, dto.Reason);
        return NoContent();
    }

    /// <summary>
    /// Get payroll run summary
    /// </summary>
    [HttpGet("runs/{id}/summary")]
    [ProducesResponseType(typeof(PayrollRunSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayrollRunSummary(Guid id)
    {
        var payrollRun = await _payrollRunRepository.GetByIdAsync(id);
        if (payrollRun == null)
            return NotFound($"Payroll run with ID {id} not found");

        var summary = await _transactionRepository.GetMonthlySummaryAsync(id);

        return Ok(new PayrollRunSummaryDto
        {
            PayrollRunId = id,
            MonthYear = $"{payrollRun.PayrollMonth:D2}/{payrollRun.PayrollYear}",
            Status = payrollRun.Status,
            TotalEmployees = payrollRun.TotalEmployees,
            TotalContractors = payrollRun.TotalContractors,
            TotalGross = summary.TryGetValue("TotalGross", out var g) ? g : 0,
            TotalDeductions = summary.TryGetValue("TotalDeductions", out var d) ? d : 0,
            TotalNet = summary.TryGetValue("TotalNet", out var n) ? n : 0,
            TotalEmployerCost = summary.TryGetValue("TotalEmployerCost", out var c) ? c : 0,
            TotalPfEmployee = summary.TryGetValue("TotalPfEmployee", out var pfe) ? pfe : 0,
            TotalPfEmployer = summary.TryGetValue("TotalPfEmployer", out var pfr) ? pfr : 0,
            TotalEsiEmployee = summary.TryGetValue("TotalEsiEmployee", out var esie) ? esie : 0,
            TotalEsiEmployer = summary.TryGetValue("TotalEsiEmployer", out var esir) ? esir : 0,
            TotalPt = summary.TryGetValue("TotalPt", out var pt) ? pt : 0,
            TotalTds = summary.TryGetValue("TotalTds", out var tds) ? tds : 0
        });
    }

    // ==================== Employee Payroll Info ====================

    /// <summary>
    /// Get employee payroll info by employee ID
    /// </summary>
    [HttpGet("payroll-info/employee/{employeeId}")]
    [ProducesResponseType(typeof(EmployeePayrollInfoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayrollInfoByEmployeeId(Guid employeeId)
    {
        var payrollInfo = await _payrollInfoRepository.GetByEmployeeIdAsync(employeeId);
        if (payrollInfo == null)
            return NotFound($"Payroll info not found for employee {employeeId}");

        return Ok(_mapper.Map<EmployeePayrollInfoDto>(payrollInfo));
    }

    /// <summary>
    /// Get employees by payroll type (employee or contractor)
    /// </summary>
    [HttpGet("payroll-info/by-type/{payrollType}")]
    [ProducesResponseType(typeof(IEnumerable<EmployeePayrollInfoDto>), 200)]
    public async Task<IActionResult> GetPayrollInfoByType(string payrollType)
    {
        if (payrollType != "employee" && payrollType != "contractor")
            return BadRequest("Payroll type must be 'employee' or 'contractor'");

        var payrollInfos = await _payrollInfoRepository.GetByPayrollTypeAsync(payrollType);
        var dtos = _mapper.Map<IEnumerable<EmployeePayrollInfoDto>>(payrollInfos);
        return Ok(dtos);
    }

    /// <summary>
    /// Create or update employee payroll info
    /// </summary>
    [HttpPost("payroll-info")]
    [ProducesResponseType(typeof(EmployeePayrollInfoDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateOrUpdatePayrollInfo([FromBody] CreateEmployeePayrollInfoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _payrollInfoRepository.GetByEmployeeIdAsync(dto.EmployeeId);
        
        if (existing != null)
        {
            // Update existing
            existing.CompanyId = dto.CompanyId;
            existing.Uan = dto.Uan;
            existing.PfAccountNumber = dto.PfAccountNumber;
            existing.EsiNumber = dto.EsiNumber;
            existing.BankAccountNumber = dto.BankAccountNumber;
            existing.BankName = dto.BankName;
            existing.BankIfsc = dto.BankIfsc;
            existing.TaxRegime = dto.TaxRegime;
            existing.PanNumber = dto.PanNumber;
            existing.PayrollType = dto.PayrollType;
            existing.IsPfApplicable = dto.IsPfApplicable;
            existing.IsEsiApplicable = dto.IsEsiApplicable;
            existing.IsPtApplicable = dto.IsPtApplicable;
            if (dto.DateOfJoining.HasValue)
                existing.DateOfJoining = dto.DateOfJoining;
            existing.UpdatedAt = DateTime.UtcNow;

            await _payrollInfoRepository.UpdateAsync(existing);
            return Ok(_mapper.Map<EmployeePayrollInfoDto>(existing));
        }
        else
        {
            // Create new
            var entity = _mapper.Map<EmployeePayrollInfo>(dto);
            entity.IsActive = true;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _payrollInfoRepository.AddAsync(entity);
            return Ok(_mapper.Map<EmployeePayrollInfoDto>(created));
        }
    }

    // ==================== Calculation Lines (Auditability) ====================

    /// <summary>
    /// Get calculation breakdown for a payroll transaction
    /// </summary>
    /// <param name="transactionId">The payroll transaction ID</param>
    /// <returns>Full calculation breakdown with earnings, deductions, and employer contributions</returns>
    [HttpGet("transactions/{transactionId}/calculation-lines")]
    [ProducesResponseType(typeof(PayrollCalculationSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCalculationLines(Guid transactionId)
    {
        // Verify transaction exists
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
            return NotFound($"Payroll transaction {transactionId} not found");

        // Get calculation lines
        var lines = await _calculationLineRepository.GetByTransactionIdAsync(transactionId);
        var lineDtos = _mapper.Map<List<PayrollCalculationLineDto>>(lines);

        // Get employee name
        var employee = await _employeesRepository.GetByIdAsync(transaction.EmployeeId);
        var employeeName = employee?.EmployeeName ?? "Unknown";

        // Build summary
        var summary = new PayrollCalculationSummaryDto
        {
            TransactionId = transactionId,
            EmployeeName = employeeName,
            MonthYear = $"{GetMonthName(transaction.PayrollMonth)} {transaction.PayrollYear}",
            Earnings = lineDtos.Where(l => l.LineType == "earning").ToList(),
            Deductions = lineDtos.Where(l => l.LineType == "deduction" || l.LineType == "statutory").ToList(),
            EmployerContributions = lineDtos.Where(l => l.LineType == "employer_contribution").ToList(),
            TotalEarnings = lineDtos.Where(l => l.LineType == "earning").Sum(l => l.ComputedAmount),
            TotalDeductions = lineDtos.Where(l => l.LineType == "deduction" || l.LineType == "statutory").Sum(l => l.ComputedAmount),
            TotalEmployerContributions = lineDtos.Where(l => l.LineType == "employer_contribution").Sum(l => l.ComputedAmount),
            NetPayable = transaction.NetPayable
        };

        return Ok(summary);
    }

    /// <summary>
    /// Check if calculation lines exist for a transaction
    /// </summary>
    [HttpGet("transactions/{transactionId}/calculation-lines/exists")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<IActionResult> CalculationLinesExist(Guid transactionId)
    {
        var exists = await _calculationLineRepository.ExistsForTransactionAsync(transactionId);
        return Ok(exists);
    }

    // ==================== Tax Parameters ====================

    /// <summary>
    /// Get all tax parameters
    /// </summary>
    [HttpGet("tax-parameters")]
    [ProducesResponseType(typeof(IEnumerable<TaxParameterDto>), 200)]
    public async Task<IActionResult> GetTaxParameters()
    {
        var parameters = await _taxParameterRepository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<TaxParameterDto>>(parameters);
        return Ok(dtos);
    }

    /// <summary>
    /// Get tax parameters for a specific financial year and regime
    /// </summary>
    [HttpGet("tax-parameters/{financialYear}/{regime}")]
    [ProducesResponseType(typeof(IEnumerable<TaxParameterDto>), 200)]
    public async Task<IActionResult> GetTaxParametersByRegime(string financialYear, string regime)
    {
        var parameters = await _taxParameterRepository.GetByRegimeAndYearAsync(regime, financialYear);
        var dtos = _mapper.Map<IEnumerable<TaxParameterDto>>(parameters);
        return Ok(dtos);
    }

    /// <summary>
    /// Get a specific tax parameter value
    /// </summary>
    [HttpGet("tax-parameters/{financialYear}/{regime}/{parameterCode}")]
    [ProducesResponseType(typeof(decimal), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTaxParameterValue(string financialYear, string regime, string parameterCode)
    {
        var param = await _taxParameterRepository.GetParameterAsync(parameterCode, regime, financialYear);
        if (param == null)
            return NotFound($"Tax parameter {parameterCode} not found for {regime} regime in {financialYear}");
        return Ok(param.ParameterValue);
    }

    // ==================== Salary Components ====================

    /// <summary>
    /// Get all salary components
    /// </summary>
    [HttpGet("salary-components")]
    [ProducesResponseType(typeof(IEnumerable<SalaryComponentDto>), 200)]
    public async Task<IActionResult> GetSalaryComponents([FromQuery] Guid? companyId = null)
    {
        var components = await _salaryComponentRepository.GetAllAsync();

        // Filter by company if specified (include global + company-specific)
        if (companyId.HasValue)
        {
            components = components.Where(c => c.CompanyId == null || c.CompanyId == companyId.Value);
        }

        var dtos = _mapper.Map<IEnumerable<SalaryComponentDto>>(components);
        return Ok(dtos);
    }

    /// <summary>
    /// Get salary components by type
    /// </summary>
    [HttpGet("salary-components/by-type/{componentType}")]
    [ProducesResponseType(typeof(IEnumerable<SalaryComponentDto>), 200)]
    public async Task<IActionResult> GetSalaryComponentsByType(string componentType)
    {
        var components = await _salaryComponentRepository.GetByTypeAsync(componentType);
        var dtos = _mapper.Map<IEnumerable<SalaryComponentDto>>(components);
        return Ok(dtos);
    }

    /// <summary>
    /// Get wage base flags for all components (for PF/ESI calculation)
    /// </summary>
    [HttpGet("salary-components/wage-flags")]
    [ProducesResponseType(typeof(IEnumerable<SalaryComponentWageFlagsDto>), 200)]
    public async Task<IActionResult> GetSalaryComponentWageFlags([FromQuery] Guid? companyId = null)
    {
        var components = await _salaryComponentRepository.GetAllAsync();

        if (companyId.HasValue)
        {
            components = components.Where(c => c.CompanyId == null || c.CompanyId == companyId.Value);
        }

        var flags = components.Select(c => new SalaryComponentWageFlagsDto
        {
            ComponentCode = c.ComponentCode,
            ComponentName = c.ComponentName,
            IsPfWage = c.IsPfWage,
            IsEsiWage = c.IsEsiWage,
            IsTaxable = c.IsTaxable,
            IsPtWage = c.IsPtWage,
            ApplyProration = c.ApplyProration
        });

        return Ok(flags);
    }

    private static string GetFinancialYear(int month, int year)
    {
        if (month >= 4)
            return $"{year}-{(year + 1) % 100:D2}";
        else
            return $"{year - 1}-{year % 100:D2}";
    }

    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => "Unknown"
        };
    }
}
