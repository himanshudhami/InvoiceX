using Application.Interfaces;
using Application.DTOs.EmployeeSalaryTransactions;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Employee Salary Transactions operations
    /// </summary>
    public class EmployeeSalaryTransactionsService : IEmployeeSalaryTransactionsService
    {
        private readonly IEmployeeSalaryTransactionsRepository _repository;
        private readonly IEmployeesRepository _employeeRepository;
        private readonly IMapper _mapper;

        public EmployeeSalaryTransactionsService(
            IEmployeeSalaryTransactionsRepository repository,
            IEmployeesRepository employeeRepository,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<EmployeeSalaryTransactions>> GetByIdAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Employee salary transaction with ID {id} not found");

            return Result<EmployeeSalaryTransactions>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<EmployeeSalaryTransactions>> GetByEmployeeAndMonthAsync(Guid employeeId, int salaryMonth, int salaryYear)
        {
            if (employeeId == default(Guid))
                return Error.Validation("Employee ID cannot be default value");

            if (salaryMonth < 1 || salaryMonth > 12)
                return Error.Validation("Salary month must be between 1 and 12");

            if (salaryYear < 2000 || salaryYear > 2100)
                return Error.Validation("Salary year must be between 2000 and 2100");

            var entity = await _repository.GetByEmployeeAndMonthAsync(employeeId, salaryMonth, salaryYear);
            if (entity == null)
                return Error.NotFound($"Salary transaction for employee {employeeId} for {salaryMonth}/{salaryYear} not found");

            return Result<EmployeeSalaryTransactions>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return Result<IEnumerable<EmployeeSalaryTransactions>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve salary transactions: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetByEmployeeIdAsync(Guid employeeId)
        {
            if (employeeId == default(Guid))
                return Error.Validation("Employee ID cannot be default value");

            try
            {
                var entities = await _repository.GetByEmployeeIdAsync(employeeId);
                return Result<IEnumerable<EmployeeSalaryTransactions>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve salary transactions for employee: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<EmployeeSalaryTransactions>>> GetByMonthYearAsync(int salaryMonth, int salaryYear)
        {
            if (salaryMonth < 1 || salaryMonth > 12)
                return Error.Validation("Salary month must be between 1 and 12");

            if (salaryYear < 2000 || salaryYear > 2100)
                return Error.Validation("Salary year must be between 2000 and 2100");

            try
            {
                var entities = await _repository.GetByMonthYearAsync(salaryMonth, salaryYear);
                return Result<IEnumerable<EmployeeSalaryTransactions>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve salary transactions for {salaryMonth}/{salaryYear}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<EmployeeSalaryTransactions> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            try
            {
                if (pageNumber < 1)
                    return Error.Validation("Page number must be greater than 0");

                if (pageSize < 1 || pageSize > 100)
                    return Error.Validation("Page size must be between 1 and 100");

                var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
                return Result<(IEnumerable<EmployeeSalaryTransactions>, int)>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve paged salary transactions: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EmployeeSalaryTransactions>> CreateAsync(CreateEmployeeSalaryTransactionsDto dto)
        {
            try
            {
                // Validate employee exists
                var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId);
                if (employee == null)
                    return Error.NotFound($"Employee with ID {dto.EmployeeId} not found");

                // Allow multiple payments per month for all transaction types
                // No duplicate check - users can have multiple bonuses, reimbursements, gifts, etc. in the same month

                var entity = _mapper.Map<EmployeeSalaryTransactions>(dto);
                
                // Populate company_id from employee if not provided
                if (!entity.CompanyId.HasValue)
                {
                    entity.CompanyId = employee.CompanyId;
                }
                
                // Calculate gross and net salary
                CalculateSalaryTotals(entity);

                var createdEntity = await _repository.AddAsync(entity);
                
                return Result<EmployeeSalaryTransactions>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to create salary transaction: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateEmployeeSalaryTransactionsDto dto)
        {
            try
            {
                if (id == default(Guid))
                    return Error.Validation("ID cannot be default value");

                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return Error.NotFound($"Salary transaction with ID {id} not found");

                _mapper.Map(dto, existingEntity);
                
                // Recalculate gross and net salary
                CalculateSalaryTotals(existingEntity);

                await _repository.UpdateAsync(existingEntity);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to update salary transaction: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                if (id == default(Guid))
                    return Error.Validation("ID cannot be default value");

                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return Error.NotFound($"Salary transaction with ID {id} not found");

                await _repository.DeleteAsync(id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to delete salary transaction: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            try
            {
                if (id == default(Guid))
                    return Error.Validation("ID cannot be default value");

                var entity = await _repository.GetByIdAsync(id);
                return Result<bool>.Success(entity != null);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to check if salary transaction exists: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> SalaryRecordExistsAsync(Guid employeeId, int salaryMonth, int salaryYear, Guid? excludeId = null)
        {
            try
            {
                if (employeeId == default(Guid))
                    return Error.Validation("Employee ID cannot be default value");

                if (salaryMonth < 1 || salaryMonth > 12)
                    return Error.Validation("Salary month must be between 1 and 12");

                if (salaryYear < 2000 || salaryYear > 2100)
                    return Error.Validation("Salary year must be between 2000 and 2100");

                var exists = await _repository.SalaryRecordExistsAsync(employeeId, salaryMonth, salaryYear, excludeId);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to check salary record existence: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BulkUploadResultDto>> BulkCreateAsync(BulkEmployeeSalaryTransactionsDto dto)
        {
            try
            {
                var result = new BulkUploadResultDto
                {
                    TotalCount = dto.SalaryTransactions.Count
                };

                var validTransactions = new List<EmployeeSalaryTransactions>();
                var errors = new List<BulkUploadErrorDto>();

                for (int i = 0; i < dto.SalaryTransactions.Count; i++)
                {
                    var transaction = dto.SalaryTransactions[i];
                    var rowNumber = i + 1;
                    var hasErrors = false;

                    try
                    {
                        // Validate employee exists
                        var employee = await _employeeRepository.GetByIdAsync(transaction.EmployeeId);
                        if (employee == null)
                        {
                            errors.Add(new BulkUploadErrorDto
                            {
                                RowNumber = rowNumber,
                                EmployeeReference = transaction.EmployeeId.ToString(),
                                ErrorMessage = "Employee not found",
                                FieldName = "EmployeeId"
                            });
                            hasErrors = true;
                        }

                        // Allow multiple payments per month for all transaction types
                        // No duplicate check - users can have multiple bonuses, reimbursements, gifts, consulting payments, etc. in the same month
                        // Only check for exact duplicates (same employee, month, year, type, and payment date) if needed

                        if (!hasErrors || dto.SkipValidationErrors)
                        {
                            if (!hasErrors)
                            {
                                var entity = _mapper.Map<EmployeeSalaryTransactions>(transaction);
                                entity.CreatedBy = dto.CreatedBy;
                                
                                // Ensure TransactionType is set and valid (default to salary if not provided)
                                if (string.IsNullOrWhiteSpace(entity.TransactionType))
                                {
                                    entity.TransactionType = "salary";
                                }
                                
                                // Validate transaction type matches allowed values
                                var allowedTypes = new[] { "salary", "consulting", "bonus", "reimbursement", "gift" };
                                if (!allowedTypes.Contains(entity.TransactionType.ToLowerInvariant()))
                                {
                                    entity.TransactionType = "salary"; // Default to salary if invalid
                                }
                                else
                                {
                                    entity.TransactionType = entity.TransactionType.ToLowerInvariant(); // Normalize to lowercase
                                }
                                
                                // Populate company_id from employee if not provided
                                if (!entity.CompanyId.HasValue && employee != null)
                                {
                                    entity.CompanyId = employee.CompanyId;
                                }
                                
                                CalculateSalaryTotals(entity);
                                validTransactions.Add(entity);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            EmployeeReference = transaction.EmployeeId.ToString(),
                            ErrorMessage = $"Validation error: {ex.Message}"
                        });
                    }
                }

                // Process valid transactions
                if (validTransactions.Any())
                {
                    try
                    {
                        var createdTransactions = await _repository.BulkAddAsync(validTransactions);
                        result.SuccessCount = createdTransactions.Count();
                        result.CreatedIds = createdTransactions.Select(t => t.Id).ToList();
                    }
                    catch (Exception ex)
                    {
                        // Add database error to errors list
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = 0,
                            EmployeeReference = "Bulk Insert",
                            ErrorMessage = $"Database error: {ex.Message}. Inner exception: {ex.InnerException?.Message ?? "None"}",
                            FieldName = "Database"
                        });
                        result.FailureCount += validTransactions.Count;
                    }
                }

                result.FailureCount = result.TotalCount - result.SuccessCount;
                result.Errors = errors;

                return Result<BulkUploadResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to process bulk upload: {ex.Message}. Inner exception: {ex.InnerException?.Message ?? "None"}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Dictionary<string, decimal>>> GetMonthlySummaryAsync(int salaryMonth, int salaryYear, Guid? companyId = null)
        {
            try
            {
                if (salaryMonth < 1 || salaryMonth > 12)
                    return Error.Validation("Salary month must be between 1 and 12");

                if (salaryYear < 2000 || salaryYear > 2100)
                    return Error.Validation("Salary year must be between 2000 and 2100");

                var summary = await _repository.GetMonthlySummaryAsync(salaryMonth, salaryYear, companyId);
                return Result<Dictionary<string, decimal>>.Success(summary);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to get monthly summary: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Dictionary<string, decimal>>> GetYearlySummaryAsync(int salaryYear, Guid? companyId = null)
        {
            try
            {
                if (salaryYear < 2000 || salaryYear > 2100)
                    return Error.Validation("Salary year must be between 2000 and 2100");

                var summary = await _repository.GetYearlySummaryAsync(salaryYear, companyId);
                return Result<Dictionary<string, decimal>>.Success(summary);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to get yearly summary: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BulkUploadResultDto>> CopyTransactionsAsync(CopySalaryTransactionsDto dto)
        {
            try
            {
                // Validate source and target periods
                if (dto.SourceMonth == dto.TargetMonth && dto.SourceYear == dto.TargetYear)
                {
                    return Error.Validation("Source and target periods cannot be the same");
                }

                // Get source transactions
                var sourceResult = await GetByMonthYearAsync(dto.SourceMonth, dto.SourceYear);
                if (sourceResult.IsFailure)
                {
                    return Error.Internal($"Failed to retrieve source transactions: {sourceResult.Error!.Message}");
                }

                var sourceTransactions = sourceResult.Value!.ToList();

                // Filter by company if specified
                if (dto.CompanyId.HasValue)
                {
                    sourceTransactions = sourceTransactions
                        .Where(t => t.CompanyId == dto.CompanyId.Value)
                        .ToList();
                }

                if (!sourceTransactions.Any())
                {
                    return Error.Validation("No transactions found to copy for the specified criteria");
                }

                var result = new BulkUploadResultDto
                {
                    TotalCount = sourceTransactions.Count
                };

                var validTransactions = new List<EmployeeSalaryTransactions>();
                var errors = new List<BulkUploadErrorDto>();

                for (int i = 0; i < sourceTransactions.Count; i++)
                {
                    var sourceTransaction = sourceTransactions[i];
                    var rowNumber = i + 1;
                    
                    try
                    {
                        // Check if target transaction already exists
                        var existsResult = await SalaryRecordExistsAsync(
                            sourceTransaction.EmployeeId, 
                            dto.TargetMonth, 
                            dto.TargetYear);

                        if (existsResult.IsSuccess && existsResult.Value)
                        {
                            if (dto.DuplicateHandling == "skip" || dto.DuplicateHandling == "skip_and_report")
                            {
                                if (dto.DuplicateHandling == "skip_and_report")
                                {
                                    var employee = await _employeeRepository.GetByIdAsync(sourceTransaction.EmployeeId);
                                    errors.Add(new BulkUploadErrorDto
                                    {
                                        RowNumber = rowNumber,
                                        EmployeeReference = employee?.EmployeeName ?? sourceTransaction.EmployeeId.ToString(),
                                        ErrorMessage = $"Transaction already exists for {dto.TargetMonth}/{dto.TargetYear}",
                                        FieldName = "Month/Year"
                                    });
                                }
                                result.FailureCount++;
                                continue;
                            }
                            else if (dto.DuplicateHandling == "overwrite")
                            {
                                // Delete existing transaction
                                var existingResult = await GetByEmployeeAndMonthAsync(
                                    sourceTransaction.EmployeeId, 
                                    dto.TargetMonth, 
                                    dto.TargetYear);
                                
                                if (existingResult.IsSuccess && existingResult.Value != null)
                                {
                                    var deleteResult = await DeleteAsync(existingResult.Value.Id);
                                    if (deleteResult.IsFailure)
                                    {
                                        errors.Add(new BulkUploadErrorDto
                                        {
                                            RowNumber = rowNumber,
                                            EmployeeReference = sourceTransaction.EmployeeId.ToString(),
                                            ErrorMessage = $"Failed to delete existing transaction: {deleteResult.Error!.Message}",
                                            FieldName = "Delete"
                                        });
                                        result.FailureCount++;
                                        continue;
                                    }
                                }
                            }
                        }

                        // Create new transaction from source
                        var newTransaction = new EmployeeSalaryTransactions
                        {
                            EmployeeId = sourceTransaction.EmployeeId,
                            SalaryMonth = dto.TargetMonth,
                            SalaryYear = dto.TargetYear,
                            BasicSalary = sourceTransaction.BasicSalary,
                            Hra = sourceTransaction.Hra,
                            Conveyance = sourceTransaction.Conveyance,
                            MedicalAllowance = sourceTransaction.MedicalAllowance,
                            SpecialAllowance = sourceTransaction.SpecialAllowance,
                            Lta = sourceTransaction.Lta,
                            OtherAllowances = sourceTransaction.OtherAllowances,
                            PfEmployee = sourceTransaction.PfEmployee,
                            PfEmployer = sourceTransaction.PfEmployer,
                            Pt = sourceTransaction.Pt,
                            IncomeTax = sourceTransaction.IncomeTax,
                            OtherDeductions = sourceTransaction.OtherDeductions,
                            GrossSalary = sourceTransaction.GrossSalary,
                            NetSalary = sourceTransaction.NetSalary,
                            Currency = sourceTransaction.Currency,
                            Remarks = sourceTransaction.Remarks,
                            CreatedBy = dto.CreatedBy,
                            Status = dto.ResetPaymentInfo ? "pending" : sourceTransaction.Status,
                            PaymentDate = dto.ResetPaymentInfo ? null : sourceTransaction.PaymentDate,
                            PaymentMethod = dto.ResetPaymentInfo ? "bank_transfer" : sourceTransaction.PaymentMethod,
                            PaymentReference = dto.ResetPaymentInfo ? null : sourceTransaction.PaymentReference
                        };

                        CalculateSalaryTotals(newTransaction);
                        validTransactions.Add(newTransaction);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            EmployeeReference = sourceTransaction.EmployeeId.ToString(),
                            ErrorMessage = $"Error copying transaction: {ex.Message}",
                            FieldName = "Copy"
                        });
                        result.FailureCount++;
                    }
                }

                // Bulk create valid transactions
                if (validTransactions.Any())
                {
                    var createdTransactions = await _repository.BulkAddAsync(validTransactions);
                    result.SuccessCount = createdTransactions.Count();
                    result.CreatedIds = createdTransactions.Select(t => t.Id).ToList();
                }

                result.FailureCount = result.TotalCount - result.SuccessCount;
                result.Errors = errors;

                return Result<BulkUploadResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to copy transactions: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate gross and net salary totals based on transaction type
        /// </summary>
        private static void CalculateSalaryTotals(EmployeeSalaryTransactions entity)
        {
            var transactionType = entity.TransactionType?.ToLowerInvariant() ?? "salary";

            switch (transactionType)
            {
                case "consulting":
                case "bonus":
                case "reimbursement":
                case "gift":
                    // For simplified payment types, gross is typically in OtherAllowances or already set
                    if (entity.GrossSalary == 0 && entity.OtherAllowances > 0)
                    {
                        entity.GrossSalary = entity.OtherAllowances;
                    }
                    // For consulting/bonus/gift: typically no PF/PT
                    if (transactionType == "consulting" || transactionType == "bonus" || transactionType == "gift")
                    {
                        entity.PfEmployee = 0;
                        entity.PfEmployer = 0;
                        entity.Pt = 0;
                    }
                    // For reimbursement: no TDS, no PF/PT
                    if (transactionType == "reimbursement")
                    {
                        entity.PfEmployee = 0;
                        entity.PfEmployer = 0;
                        entity.Pt = 0;
                        entity.IncomeTax = 0; // Reimbursements are tax-free
                    }
                    break;

                case "salary":
                default:
                    // Standard salary calculation
                    // Calculate gross salary (sum of all allowances)
                    entity.GrossSalary = entity.BasicSalary + entity.Hra + entity.Conveyance + 
                                        entity.MedicalAllowance + entity.SpecialAllowance + 
                                        entity.Lta + entity.OtherAllowances;
                    break;
            }

            // Calculate total deductions
            var totalDeductions = entity.PfEmployee + entity.Pt + entity.IncomeTax + entity.OtherDeductions;

            // Calculate net salary (gross - deductions)
            entity.NetSalary = entity.GrossSalary - totalDeductions;

            // Ensure net salary is not negative
            if (entity.NetSalary < 0)
                entity.NetSalary = 0;
        }

        /// <summary>
        /// Calculate TDS based on transaction type and amount
        /// </summary>
        private static decimal CalculateTDSForTransactionType(string transactionType, decimal grossAmount, Employees? employee)
        {
            var type = transactionType?.ToLowerInvariant() ?? "salary";

            return type switch
            {
                "consulting" => Math.Round(grossAmount * 0.10m, 2), // Section 194J: 10% flat rate
                "bonus" => CalculateBonusTDS(grossAmount, employee), // Section 192: Slab rate (simplified)
                "reimbursement" => 0, // Tax-free
                "gift" => CalculateGiftTDS(grossAmount), // May apply if exceeds threshold
                "salary" => 0, // Calculated separately based on salary structure
                _ => 0
            };
        }

        /// <summary>
        /// Calculate TDS for bonus (simplified - uses flat 30% for high amounts, otherwise 0)
        /// Note: In production, this should use actual tax slab calculation
        /// </summary>
        private static decimal CalculateBonusTDS(decimal amount, Employees? employee)
        {
            // Simplified: For bonuses, typically taxed at slab rate (Section 192)
            // This is a placeholder - actual implementation should use employee's tax slab
            // For now, return 0 and let it be calculated/manually entered
            return 0;
        }

        /// <summary>
        /// Calculate TDS for gifts (taxable if annual aggregate exceeds ₹5,000)
        /// Note: This requires tracking annual aggregate, which is not implemented here
        /// </summary>
        private static decimal CalculateGiftTDS(decimal amount)
        {
            // Gifts are taxable if annual aggregate exceeds ₹5,000
            // This requires tracking annual aggregate per employee
            // For now, return 0 and let it be calculated/manually entered
            return 0;
        }
    }
}