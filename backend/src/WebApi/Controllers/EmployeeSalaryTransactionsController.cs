using Application.Interfaces;
using Application.DTOs.EmployeeSalaryTransactions;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Employee Salary Transactions management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmployeeSalaryTransactionsController : ControllerBase
    {
        private readonly IEmployeeSalaryTransactionsService _service;

        /// <summary>
        /// Initializes a new instance of the EmployeeSalaryTransactionsController
        /// </summary>
        public EmployeeSalaryTransactionsController(IEmployeeSalaryTransactionsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Employee Salary Transaction by ID
        /// </summary>
        /// <param name="id">The Transaction ID</param>
        /// <returns>The Employee Salary Transaction entity</returns>
        /// <response code="200">Returns the Employee Salary Transaction entity</response>
        /// <response code="404">Transaction not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EmployeeSalaryTransactions), 200)]
        [ProducesResponseType(400)]
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
                    _ => BadRequest(result.Error.Message)
                };
            }
            
            return Ok(result.Value);
        }

        /// <summary>
        /// Get Employee Salary Transaction by Employee and Month
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <param name="salaryMonth">The salary month (1-12)</param>
        /// <param name="salaryYear">The salary year</param>
        /// <returns>The Employee Salary Transaction entity</returns>
        /// <response code="200">Returns the Employee Salary Transaction entity</response>
        /// <response code="404">Transaction not found</response>
        [HttpGet("by-employee-month")]
        [ProducesResponseType(typeof(EmployeeSalaryTransactions), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByEmployeeAndMonth(
            [FromQuery] Guid employeeId, 
            [FromQuery] int salaryMonth, 
            [FromQuery] int salaryYear)
        {
            var result = await _service.GetByEmployeeAndMonthAsync(employeeId, salaryMonth, salaryYear);
            
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }
            
            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Employee Salary Transactions
        /// </summary>
        /// <returns>List of Employee Salary Transactions</returns>
        /// <response code="200">Returns the list of Employee Salary Transactions</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmployeeSalaryTransactions>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            
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
        /// Get Employee Salary Transactions by Employee ID
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <returns>List of Employee Salary Transactions for the employee</returns>
        /// <response code="200">Returns the list of Employee Salary Transactions</response>
        [HttpGet("by-employee/{employeeId}")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeSalaryTransactions>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByEmployeeId(Guid employeeId)
        {
            var result = await _service.GetByEmployeeIdAsync(employeeId);
            
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
        /// Get Employee Salary Transactions by Month and Year
        /// </summary>
        /// <param name="salaryMonth">The salary month (1-12)</param>
        /// <param name="salaryYear">The salary year</param>
        /// <returns>List of Employee Salary Transactions for the month/year</returns>
        /// <response code="200">Returns the list of Employee Salary Transactions</response>
        [HttpGet("by-month")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeSalaryTransactions>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByMonthYear([FromQuery] int salaryMonth, [FromQuery] int salaryYear)
        {
            var result = await _service.GetByMonthYearAsync(salaryMonth, salaryYear);
            
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
        /// Get paginated Employee Salary Transactions with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Employee Salary Transactions</returns>
        /// <response code="200">Returns the paginated list of Employee Salary Transactions</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<EmployeeSalaryTransactions>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] EmployeeSalaryTransactionsFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());
            
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }
            
            var pagedResponse = new PagedResponse<EmployeeSalaryTransactions>(
                result.Value.Items,
                result.Value.TotalCount,
                request.PageNumber,
                request.PageSize);
            
            return Ok(pagedResponse);
        }

        /// <summary>
        /// Create a new Employee Salary Transaction
        /// </summary>
        /// <param name="dto">Employee Salary Transaction creation data</param>
        /// <returns>The created Employee Salary Transaction entity</returns>
        /// <response code="201">Transaction created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Employee not found</response>
        /// <response code="409">Salary record already exists for employee and month</response>
        [HttpPost]
        [ProducesResponseType(typeof(EmployeeSalaryTransactions), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeSalaryTransactionsDto dto)
        {
            var result = await _service.CreateAsync(dto);
            
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
            
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing Employee Salary Transaction
        /// </summary>
        /// <param name="id">The Transaction ID</param>
        /// <param name="dto">Employee Salary Transaction update data</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Transaction updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Transaction not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeSalaryTransactionsDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            
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
        /// Delete an Employee Salary Transaction
        /// </summary>
        /// <param name="id">The Transaction ID</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Transaction deleted successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Transaction not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
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
        /// Bulk create Employee Salary Transactions
        /// </summary>
        /// <param name="dto">Bulk upload data</param>
        /// <returns>Bulk upload results</returns>
        /// <response code="200">Bulk upload completed (may contain partial failures)</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(BulkUploadResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> BulkCreate([FromBody] BulkEmployeeSalaryTransactionsDto dto)
        {
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
        /// Copy salary transactions from one period to another
        /// </summary>
        /// <param name="dto">Copy operation parameters</param>
        /// <returns>Copy operation results</returns>
        /// <response code="200">Copy operation completed (may contain partial failures)</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("copy")]
        [ProducesResponseType(typeof(BulkUploadResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CopyTransactions([FromBody] CopySalaryTransactionsDto dto)
        {
            var result = await _service.CopyTransactionsAsync(dto);
            
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
        /// Get monthly salary summary
        /// </summary>
        /// <param name="salaryMonth">The salary month (1-12)</param>
        /// <param name="salaryYear">The salary year</param>
        /// <returns>Monthly salary summary</returns>
        /// <response code="200">Returns monthly summary</response>
        /// <response code="400">Invalid month or year</response>
        [HttpGet("summary/monthly")]
        [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int salaryMonth, [FromQuery] int salaryYear, [FromQuery] Guid? companyId = null)
        {
            var result = await _service.GetMonthlySummaryAsync(salaryMonth, salaryYear, companyId);
            
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
        /// Get yearly salary summary
        /// </summary>
        /// <param name="salaryYear">The salary year</param>
        /// <param name="companyId">Optional company ID to filter by</param>
        /// <returns>Yearly salary summary</returns>
        /// <response code="200">Returns yearly summary</response>
        /// <response code="400">Invalid year</response>
        [HttpGet("summary/yearly")]
        [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetYearlySummary([FromQuery] int salaryYear, [FromQuery] Guid? companyId = null)
        {
            var result = await _service.GetYearlySummaryAsync(salaryYear, companyId);
            
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
        /// Check if salary record exists for employee and month
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <param name="salaryMonth">The salary month (1-12)</param>
        /// <param name="salaryYear">The salary year</param>
        /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
        /// <returns>True if exists, false otherwise</returns>
        /// <response code="200">Returns existence status</response>
        [HttpGet("check-exists")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CheckSalaryRecordExists(
            [FromQuery] Guid employeeId,
            [FromQuery] int salaryMonth,
            [FromQuery] int salaryYear,
            [FromQuery] Guid? excludeId = null)
        {
            var result = await _service.SalaryRecordExistsAsync(employeeId, salaryMonth, salaryYear, excludeId);
            
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
    }
}