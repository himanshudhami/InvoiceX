using Application.DTOs.Expense;
using Application.Interfaces.Expense;
using Core.Common;
using Core.Entities.Expense;
using Core.Interfaces.Expense;
using Microsoft.Extensions.Logging;

namespace Application.Services.Expense
{
    /// <summary>
    /// Application service for expense category operations.
    /// </summary>
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly IExpenseCategoryRepository _repository;
        private readonly ILogger<ExpenseCategoryService> _logger;

        public ExpenseCategoryService(
            IExpenseCategoryRepository repository,
            ILogger<ExpenseCategoryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExpenseCategoryDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Category ID cannot be empty");
            }

            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Error.NotFound($"Expense category with ID {id} not found");
            }

            return Result<ExpenseCategoryDto>.Success(MapToDto(category));
        }

        public async Task<Result<IEnumerable<ExpenseCategoryDto>>> GetByCompanyAsync(
            Guid companyId, bool includeInactive = false)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var categories = await _repository.GetByCompanyAsync(companyId, includeInactive);
            return Result<IEnumerable<ExpenseCategoryDto>>.Success(
                categories.Select(MapToDto));
        }

        public async Task<Result<IEnumerable<ExpenseCategorySelectDto>>> GetSelectListAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var categories = await _repository.GetActiveByCompanyAsync(companyId);
            return Result<IEnumerable<ExpenseCategorySelectDto>>.Success(
                categories.Select(c => new ExpenseCategorySelectDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    MaxAmount = c.MaxAmount,
                    RequiresReceipt = c.RequiresReceipt,
                    // GST defaults
                    IsGstApplicable = c.IsGstApplicable,
                    DefaultGstRate = c.DefaultGstRate,
                    DefaultHsnSac = c.DefaultHsnSac,
                    ItcEligible = c.ItcEligible ?? true
                }));
        }

        public async Task<Result<(IEnumerable<ExpenseCategoryDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _repository.GetPagedAsync(
                companyId, pageNumber, pageSize, searchTerm, includeInactive);

            return Result<(IEnumerable<ExpenseCategoryDto>, int)>.Success(
                (items.Select(MapToDto), totalCount));
        }

        public async Task<Result<ExpenseCategoryDto>> CreateAsync(Guid companyId, CreateExpenseCategoryDto dto)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Error.Validation("Category name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return Error.Validation("Category code is required");
            }

            // Validate code format
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Code, @"^[a-zA-Z0-9_]+$"))
            {
                return Error.Validation("Category code can only contain letters, numbers, and underscores");
            }

            // Validate max amount
            if (dto.MaxAmount.HasValue && dto.MaxAmount <= 0)
            {
                return Error.Validation("Maximum amount must be greater than zero");
            }

            // Check for duplicate code
            if (await _repository.CodeExistsAsync(companyId, dto.Code))
            {
                return Error.Conflict($"A category with code '{dto.Code}' already exists");
            }

            var category = new ExpenseCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = dto.Name.Trim(),
                Code = dto.Code.ToLowerInvariant().Trim(),
                Description = dto.Description?.Trim(),
                IsActive = true,
                MaxAmount = dto.MaxAmount,
                RequiresReceipt = dto.RequiresReceipt,
                RequiresApproval = dto.RequiresApproval,
                GlAccountCode = dto.GlAccountCode?.Trim(),
                DisplayOrder = dto.DisplayOrder
            };

            var created = await _repository.AddAsync(category);

            _logger.LogInformation(
                "Expense category created: {CategoryId} ({Code}) for company {CompanyId}",
                created.Id, created.Code, companyId);

            return Result<ExpenseCategoryDto>.Success(MapToDto(created));
        }

        public async Task<Result<ExpenseCategoryDto>> UpdateAsync(Guid id, UpdateExpenseCategoryDto dto)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Category ID cannot be empty");
            }

            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Error.NotFound($"Expense category with ID {id} not found");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Error.Validation("Category name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return Error.Validation("Category code is required");
            }

            // Validate code format
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Code, @"^[a-zA-Z0-9_]+$"))
            {
                return Error.Validation("Category code can only contain letters, numbers, and underscores");
            }

            // Validate max amount
            if (dto.MaxAmount.HasValue && dto.MaxAmount <= 0)
            {
                return Error.Validation("Maximum amount must be greater than zero");
            }

            // Check for duplicate code (excluding current category)
            if (await _repository.CodeExistsAsync(category.CompanyId, dto.Code, id))
            {
                return Error.Conflict($"A category with code '{dto.Code}' already exists");
            }

            category.Name = dto.Name.Trim();
            category.Code = dto.Code.ToLowerInvariant().Trim();
            category.Description = dto.Description?.Trim();
            category.IsActive = dto.IsActive;
            category.MaxAmount = dto.MaxAmount;
            category.RequiresReceipt = dto.RequiresReceipt;
            category.RequiresApproval = dto.RequiresApproval;
            category.GlAccountCode = dto.GlAccountCode?.Trim();
            category.DisplayOrder = dto.DisplayOrder;

            await _repository.UpdateAsync(category);

            _logger.LogInformation(
                "Expense category updated: {CategoryId} ({Code})",
                category.Id, category.Code);

            return Result<ExpenseCategoryDto>.Success(MapToDto(category));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Category ID cannot be empty");
            }

            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Error.NotFound($"Expense category with ID {id} not found");
            }

            // TODO: Check if category is in use by any expense claims
            // For now, allow deletion

            await _repository.DeleteAsync(id);

            _logger.LogInformation(
                "Expense category deleted: {CategoryId} ({Code})",
                id, category.Code);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> SeedDefaultCategoriesAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            await _repository.SeedDefaultCategoriesAsync(companyId);

            _logger.LogInformation(
                "Default expense categories seeded for company {CompanyId}",
                companyId);

            return Result<bool>.Success(true);
        }

        private static ExpenseCategoryDto MapToDto(ExpenseCategory category)
        {
            return new ExpenseCategoryDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                Name = category.Name,
                Code = category.Code,
                Description = category.Description,
                IsActive = category.IsActive,
                MaxAmount = category.MaxAmount,
                RequiresReceipt = category.RequiresReceipt,
                RequiresApproval = category.RequiresApproval,
                GlAccountCode = category.GlAccountCode,
                DisplayOrder = category.DisplayOrder,
                // GST defaults
                IsGstApplicable = category.IsGstApplicable,
                DefaultGstRate = category.DefaultGstRate,
                DefaultHsnSac = category.DefaultHsnSac,
                ItcEligible = category.ItcEligible ?? true,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}
