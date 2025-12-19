using Application.Interfaces;
using Application.DTOs.Employees;
using Application.DTOs.EmployeesBulk;
using Core.Entities;
using Core.Common;
using Core.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Controllers.Common;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Employees management endpoints.
    /// - Super Admin: Can access employees from ALL companies.
    /// - Company Admin/HR: Can access employees from ASSIGNED companies only.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "AdminOnly")]
    public class EmployeesController : CompanyAuthorizedController
    {
        private readonly IEmployeesService _service;
        private readonly IEmployeePayrollInfoRepository _payrollInfoRepository;

        /// <summary>
        /// Initializes a new instance of the EmployeesController
        /// </summary>
        public EmployeesController(IEmployeesService service, IEmployeePayrollInfoRepository payrollInfoRepository)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _payrollInfoRepository = payrollInfoRepository ?? throw new ArgumentNullException(nameof(payrollInfoRepository));
        }

        /// <summary>
        /// Get Employee by ID
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <returns>The Employee entity</returns>
        /// <response code="200">Returns the Employee entity</response>
        /// <response code="403">Forbidden - employee belongs to different company or user has no company access</response>
        /// <response code="404">Employee not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Employees), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            // Validate company access (Super Admin, Company Admin with assignment, or own company)
            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get Employee by Employee ID
        /// </summary>
        /// <param name="employeeId">The Employee ID (company-specific)</param>
        /// <returns>The Employee entity</returns>
        /// <response code="200">Returns the Employee entity</response>
        /// <response code="403">Forbidden - employee belongs to different company</response>
        /// <response code="404">Employee not found</response>
        [HttpGet("by-employee-id/{employeeId}")]
        [ProducesResponseType(typeof(Employees), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByEmployeeId(string employeeId)
        {
            var result = await _service.GetByEmployeeIdAsync(employeeId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            // Validate company access
            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Employees entities
        /// </summary>
        /// <param name="companyId">Optional company ID filter (Super Admin/Company Admin only)</param>
        /// <returns>List of Employees entities</returns>
        /// <response code="200">Returns the list of Employees entities</response>
        /// <response code="403">Forbidden - no company access or access denied to requested company</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Employees>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            // Get effective company ID with validation
            var (effectiveCompanyId, errorMessage) = GetEffectiveCompanyIdWithValidation(companyId);

            if (errorMessage != null)
                return StatusCode(403, new { error = errorMessage });

            if (!effectiveCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            var filters = new Dictionary<string, object> { { "company_id", effectiveCompanyId.Value } };
            var result = await _service.GetPagedAsync(1, 100, null, null, false, filters);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value.Items);
        }

        /// <summary>
        /// Get paginated Employees entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="companyId">Optional company ID filter (Super Admin/Company Admin only)</param>
        /// <returns>Paginated list of Employees entities</returns>
        /// <response code="200">Returns the paginated list of Employees entities</response>
        /// <response code="403">Forbidden - no company access or access denied to requested company</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Employees>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] EmployeesFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            // Get effective company ID with validation
            var (effectiveCompanyId, errorMessage) = GetEffectiveCompanyIdWithValidation(companyId);

            if (errorMessage != null)
                return StatusCode(403, new { error = errorMessage });

            if (!effectiveCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();
            // Set company filter based on effective company
            filters["company_id"] = effectiveCompanyId.Value;

            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                filters);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            var pagedResponse = new PagedResponse<Employees>(
                result.Value.Items,
                result.Value.TotalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(pagedResponse);
        }

        /// <summary>
        /// Create a new Employee
        /// </summary>
        /// <param name="dto">Employee creation data</param>
        /// <returns>The created Employee entity</returns>
        /// <response code="201">Employee created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Forbidden - cannot create employee for a company you don't have access to</response>
        /// <response code="409">Employee ID or email already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(Employees), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeesDto dto)
        {
            // Determine target company
            if (dto.CompanyId.HasValue)
            {
                // Validate user has access to the specified company
                if (!CanAccessCompany(dto.CompanyId.Value))
                    return StatusCode(403, new { error = "Cannot create employee for a company you don't have access to" });
            }
            else
            {
                // Use default company from JWT
                var defaultCompanyId = CurrentCompanyId;
                if (!defaultCompanyId.HasValue)
                    return CompanyIdNotFoundResponse();

                dto.CompanyId = defaultCompanyId.Value;
            }

            var result = await _service.CreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Bulk create employees
        /// </summary>
        /// <param name="dto">Bulk employees payload</param>
        /// <param name="companyId">Optional target company ID (Super Admin/Company Admin only)</param>
        /// <returns>Bulk upload summary</returns>
        /// <response code="200">Bulk upload processed</response>
        /// <response code="400">Validation errors</response>
        /// <response code="403">Forbidden - no access to target company</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(BulkEmployeesResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> BulkCreate([FromBody] BulkEmployeesDto dto, [FromQuery] Guid? companyId = null)
        {
            // Determine target company
            var (targetCompanyId, errorMessage) = GetEffectiveCompanyIdWithValidation(companyId);

            if (errorMessage != null)
                return StatusCode(403, new { error = errorMessage });

            if (!targetCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            // Set company_id for all employees to the target company
            if (dto.Employees != null)
            {
                foreach (var employee in dto.Employees)
                {
                    // If employee specifies a company, validate access
                    if (employee.CompanyId.HasValue && employee.CompanyId.Value != targetCompanyId.Value)
                    {
                        if (!CanAccessCompany(employee.CompanyId.Value))
                            return StatusCode(403, new { error = "Cannot create employees for a company you don't have access to" });
                    }
                    else
                    {
                        employee.CompanyId = targetCompanyId.Value;
                    }
                }
            }

            var result = await _service.BulkCreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Update an existing Employee
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <param name="dto">Employee update data</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Employee updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Forbidden - no access to employee's company</response>
        /// <response code="404">Employee not found</response>
        /// <response code="409">Employee ID or email already exists</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeesDto dto)
        {
            // Validate employee exists and user has access
            var employeeResult = await _service.GetByIdAsync(id);
            if (employeeResult.IsFailure)
            {
                return employeeResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(employeeResult.Error.Message),
                    _ => BadRequest(employeeResult.Error.Message)
                };
            }

            // Validate company access
            if (!HasCompanyAccess(employeeResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            // Prevent changing company to one user doesn't have access to
            if (dto.CompanyId.HasValue && dto.CompanyId.Value != employeeResult.Value.CompanyId)
            {
                if (!CanAccessCompany(dto.CompanyId.Value))
                    return StatusCode(403, new { error = "Cannot move employee to a company you don't have access to" });
            }

            var result = await _service.UpdateAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Delete an Employee
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Employee deleted successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Forbidden - no access to employee's company</response>
        /// <response code="404">Employee not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Validate employee exists and user has access
            var employeeResult = await _service.GetByIdAsync(id);
            if (employeeResult.IsFailure)
            {
                return employeeResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(employeeResult.Error.Message),
                    _ => BadRequest(employeeResult.Error.Message)
                };
            }

            // Validate company access
            if (!HasCompanyAccess(employeeResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            var result = await _service.DeleteAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Check if Employee exists
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <returns>True if exists and user has access, false otherwise</returns>
        /// <response code="200">Returns existence status</response>
        [HttpGet("{id}/exists")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> Exists(Guid id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return Ok(false);
            }

            // Return true only if user has access to the employee's company
            return Ok(HasCompanyAccess(result.Value!.CompanyId));
        }

        /// <summary>
        /// Check if Employee ID is unique
        /// </summary>
        /// <param name="employeeId">The Employee ID to check</param>
        /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
        /// <param name="companyId">Optional company ID (Super Admin/Company Admin only)</param>
        /// <returns>True if unique, false otherwise</returns>
        /// <response code="200">Returns uniqueness status</response>
        [HttpGet("check-employee-id-unique")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CheckEmployeeIdUnique([FromQuery] string employeeId, [FromQuery] Guid? excludeId = null, [FromQuery] Guid? companyId = null)
        {
            var (effectiveCompanyId, errorMessage) = GetEffectiveCompanyIdWithValidation(companyId);

            if (errorMessage != null)
                return StatusCode(403, new { error = errorMessage });

            if (!effectiveCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            var result = await _service.IsEmployeeIdUniqueAsync(employeeId, excludeId, effectiveCompanyId.Value);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Check if Email is unique
        /// </summary>
        /// <param name="email">The Email to check</param>
        /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
        /// <param name="companyId">Optional company ID (Super Admin/Company Admin only)</param>
        /// <returns>True if unique, false otherwise</returns>
        /// <response code="200">Returns uniqueness status</response>
        [HttpGet("check-email-unique")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CheckEmailUnique([FromQuery] string email, [FromQuery] Guid? excludeId = null, [FromQuery] Guid? companyId = null)
        {
            var (effectiveCompanyId, errorMessage) = GetEffectiveCompanyIdWithValidation(companyId);

            if (errorMessage != null)
                return StatusCode(403, new { error = errorMessage });

            if (!effectiveCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            var result = await _service.IsEmailUniqueAsync(email, excludeId, effectiveCompanyId.Value);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Resign an employee (set last working day, deactivate from payroll)
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <param name="dto">Resignation details</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Employee resigned successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Forbidden - no access to employee's company</response>
        /// <response code="404">Employee not found</response>
        [HttpPost("{id}/resign")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Resign(Guid id, [FromBody] ResignEmployeeDto dto)
        {
            // Validate employee exists
            var employeeResult = await _service.GetByIdAsync(id);
            if (employeeResult.IsFailure)
            {
                return employeeResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(employeeResult.Error.Message),
                    _ => BadRequest(employeeResult.Error.Message)
                };
            }

            var employee = employeeResult.Value!;

            // Validate company access
            if (!HasCompanyAccess(employee.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            // Check if employee is already resigned
            if (employee.Status == "resigned")
            {
                return BadRequest("Employee is already resigned");
            }

            // Update employee status to 'resigned' and store resignation details
            var updateDto = new UpdateEmployeesDto
            {
                EmployeeName = employee.EmployeeName,
                Email = employee.Email,
                Phone = employee.Phone,
                EmployeeId = employee.EmployeeId,
                Department = employee.Department,
                Designation = employee.Designation,
                HireDate = employee.HireDate,
                Status = "resigned",
                BankAccountNumber = employee.BankAccountNumber,
                BankName = employee.BankName,
                IfscCode = employee.IfscCode,
                PanNumber = employee.PanNumber,
                AddressLine1 = employee.AddressLine1,
                AddressLine2 = employee.AddressLine2,
                City = employee.City,
                State = employee.State,
                ZipCode = employee.ZipCode,
                Country = employee.Country,
                ContractType = employee.ContractType,
                Company = employee.Company,
                CompanyId = employee.CompanyId
            };

            var updateResult = await _service.UpdateAsync(id, updateDto);
            if (updateResult.IsFailure)
            {
                return StatusCode(500, updateResult.Error!.Message);
            }

            // Update payroll info (date_of_leaving, is_active=false)
            await _payrollInfoRepository.ResignEmployeeAsync(id, dto.LastWorkingDay);

            return NoContent();
        }

        /// <summary>
        /// Rejoin a previously resigned employee
        /// </summary>
        /// <param name="id">The Employee ID</param>
        /// <param name="dto">Optional rejoining details</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Employee rejoined successfully</response>
        /// <response code="400">Invalid request data or employee is not resigned</response>
        /// <response code="403">Forbidden - no access to employee's company</response>
        /// <response code="404">Employee not found</response>
        [HttpPost("{id}/rejoin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Rejoin(Guid id, [FromBody] RejoinEmployeeDto? dto = null)
        {
            // Validate employee exists
            var employeeResult = await _service.GetByIdAsync(id);
            if (employeeResult.IsFailure)
            {
                return employeeResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(employeeResult.Error.Message),
                    _ => BadRequest(employeeResult.Error.Message)
                };
            }

            var employee = employeeResult.Value!;

            // Validate company access
            if (!HasCompanyAccess(employee.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Employee");

            // Check if employee is resigned
            if (employee.Status != "resigned")
            {
                return BadRequest("Only resigned employees can be rejoined");
            }

            // Update employee status back to 'active'
            var updateDto = new UpdateEmployeesDto
            {
                EmployeeName = employee.EmployeeName,
                Email = employee.Email,
                Phone = employee.Phone,
                EmployeeId = employee.EmployeeId,
                Department = employee.Department,
                Designation = employee.Designation,
                HireDate = employee.HireDate,
                Status = "active",
                BankAccountNumber = employee.BankAccountNumber,
                BankName = employee.BankName,
                IfscCode = employee.IfscCode,
                PanNumber = employee.PanNumber,
                AddressLine1 = employee.AddressLine1,
                AddressLine2 = employee.AddressLine2,
                City = employee.City,
                State = employee.State,
                ZipCode = employee.ZipCode,
                Country = employee.Country,
                ContractType = employee.ContractType,
                Company = employee.Company,
                CompanyId = employee.CompanyId
            };

            var updateResult = await _service.UpdateAsync(id, updateDto);
            if (updateResult.IsFailure)
            {
                return StatusCode(500, updateResult.Error!.Message);
            }

            // Update payroll info (clear date_of_leaving, set is_active=true)
            await _payrollInfoRepository.RejoinEmployeeAsync(id, dto?.RejoiningDate);

            return NoContent();
        }
    }
}
