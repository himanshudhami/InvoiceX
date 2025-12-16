using Application.Interfaces;
using Application.DTOs.Employees;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.EmployeesBulk;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Employees operations
    /// </summary>
    public class EmployeesService : IEmployeesService
    {
        private readonly IEmployeesRepository _repository;
        private readonly IMapper _mapper;

        public EmployeesService(IEmployeesRepository repository, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<Employees>> GetByIdAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Employee with ID {id} not found");

            return Result<Employees>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<Employees>> GetByEmployeeIdAsync(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                return Error.Validation("Employee ID cannot be empty");

            var entity = await _repository.GetByEmployeeIdAsync(employeeId);
            if (entity == null)
                return Error.NotFound($"Employee with Employee ID {employeeId} not found");

            return Result<Employees>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Employees>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return Result<IEnumerable<Employees>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve employees: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Employees> Items, int TotalCount)>> GetPagedAsync(
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
                return Result<(IEnumerable<Employees>, int)>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve paged employees: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Employees>> CreateAsync(CreateEmployeesDto dto)
        {
            try
            {
                // Validate Employee ID uniqueness if provided
                if (!string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    var employeeIdExists = await _repository.EmployeeIdExistsAsync(dto.EmployeeId);
                    if (employeeIdExists)
                        return Error.Conflict($"Employee ID {dto.EmployeeId} already exists");
                }

                // Validate email uniqueness if provided
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    var emailExists = await _repository.EmailExistsAsync(dto.Email);
                    if (emailExists)
                        return Error.Conflict($"Email {dto.Email} already exists");
                }

                var entity = _mapper.Map<Employees>(dto);
                var createdEntity = await _repository.AddAsync(entity);
                
                return Result<Employees>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to create employee: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateEmployeesDto dto)
        {
            try
            {
                if (id == default(Guid))
                    return Error.Validation("ID cannot be default value");

                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return Error.NotFound($"Employee with ID {id} not found");

                // Validate Employee ID uniqueness if provided and changed
                if (!string.IsNullOrWhiteSpace(dto.EmployeeId) && dto.EmployeeId != existingEntity.EmployeeId)
                {
                    var employeeIdExists = await _repository.EmployeeIdExistsAsync(dto.EmployeeId, id);
                    if (employeeIdExists)
                        return Error.Conflict($"Employee ID {dto.EmployeeId} already exists");
                }

                // Validate email uniqueness if provided and changed
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != existingEntity.Email)
                {
                    var emailExists = await _repository.EmailExistsAsync(dto.Email, id);
                    if (emailExists)
                        return Error.Conflict($"Email {dto.Email} already exists");
                }

                _mapper.Map(dto, existingEntity);
                await _repository.UpdateAsync(existingEntity);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to update employee: {ex.Message}");
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
                    return Error.NotFound($"Employee with ID {id} not found");

                await _repository.DeleteAsync(id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to delete employee: {ex.Message}");
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
                return Error.Internal($"Failed to check if employee exists: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId))
                    return Result<bool>.Success(true); // Empty employee ID is considered unique

                var exists = await _repository.EmployeeIdExistsAsync(employeeId, excludeId);
                return Result<bool>.Success(!exists);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to check employee ID uniqueness: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return Result<bool>.Success(true); // Empty email is considered unique

                var exists = await _repository.EmailExistsAsync(email, excludeId);
                return Result<bool>.Success(!exists);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to check email uniqueness: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BulkEmployeesResultDto>> BulkCreateAsync(BulkEmployeesDto dto)
        {
            try
            {
                if (dto.Employees == null || dto.Employees.Count == 0)
                    return Error.Validation("No employees provided for bulk upload");

                var result = new BulkEmployeesResultDto
                {
                    TotalCount = dto.Employees.Count
                };

                for (int i = 0; i < dto.Employees.Count; i++)
                {
                    var employeeDto = dto.Employees[i];
                    var rowNumber = i + 1;

                    try
                    {
                        // Basic validation
                        if (string.IsNullOrWhiteSpace(employeeDto.EmployeeName))
                            throw new ArgumentException("Employee name is required", nameof(employeeDto.EmployeeName));

                        // Employee ID uniqueness if provided
                        if (!string.IsNullOrWhiteSpace(employeeDto.EmployeeId))
                        {
                            var exists = await _repository.EmployeeIdExistsAsync(employeeDto.EmployeeId);
                            if (exists)
                                throw new ArgumentException($"Employee ID {employeeDto.EmployeeId} already exists", nameof(employeeDto.EmployeeId));
                        }

                        // Email uniqueness if provided
                        if (!string.IsNullOrWhiteSpace(employeeDto.Email))
                        {
                            var exists = await _repository.EmailExistsAsync(employeeDto.Email);
                            if (exists)
                                throw new ArgumentException($"Email {employeeDto.Email} already exists", nameof(employeeDto.Email));
                        }

                        var entity = _mapper.Map<Employees>(employeeDto);
                        var created = await _repository.AddAsync(entity);

                        result.SuccessCount++;
                        result.CreatedIds.Add(created.Id);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BulkEmployeesErrorDto
                        {
                            RowNumber = rowNumber,
                            EmployeeReference = employeeDto.EmployeeId ?? employeeDto.EmployeeName,
                            ErrorMessage = ex.Message
                        });

                        if (!dto.SkipValidationErrors)
                        {
                            return Result<BulkEmployeesResultDto>.Success(result);
                        }
                    }
                }

                return Result<BulkEmployeesResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to bulk create employees: {ex.Message}");
            }
        }
    }
}
