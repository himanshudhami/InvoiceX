using Application.DTOs.Document;
using Application.Interfaces.Document;
using Core.Common;
using Core.Entities.Document;
using Core.Interfaces.Document;
using Microsoft.Extensions.Logging;

namespace Application.Services.Document
{
    /// <summary>
    /// Application service for document category operations.
    /// </summary>
    public class DocumentCategoryService : IDocumentCategoryService
    {
        private readonly IDocumentCategoryRepository _repository;
        private readonly ILogger<DocumentCategoryService> _logger;

        public DocumentCategoryService(
            IDocumentCategoryRepository repository,
            ILogger<DocumentCategoryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<DocumentCategoryDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Category ID cannot be empty");
            }

            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Error.NotFound($"Document category with ID {id} not found");
            }

            return Result<DocumentCategoryDto>.Success(MapToDto(category));
        }

        public async Task<Result<IEnumerable<DocumentCategoryDto>>> GetByCompanyAsync(
            Guid companyId, bool includeInactive = false)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var categories = await _repository.GetByCompanyAsync(companyId, includeInactive);
            return Result<IEnumerable<DocumentCategoryDto>>.Success(
                categories.Select(MapToDto));
        }

        public async Task<Result<IEnumerable<DocumentCategorySelectDto>>> GetSelectListAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var categories = await _repository.GetActiveByCompanyAsync(companyId);
            return Result<IEnumerable<DocumentCategorySelectDto>>.Success(
                categories.Select(c => new DocumentCategorySelectDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    RequiresFinancialYear = c.RequiresFinancialYear
                }));
        }

        public async Task<Result<(IEnumerable<DocumentCategoryDto> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<DocumentCategoryDto>, int)>.Success(
                (items.Select(MapToDto), totalCount));
        }

        public async Task<Result<DocumentCategoryDto>> CreateAsync(Guid companyId, CreateDocumentCategoryDto dto)
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

            // Validate code format (alphanumeric and underscores only)
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Code, @"^[a-zA-Z0-9_]+$"))
            {
                return Error.Validation("Category code can only contain letters, numbers, and underscores");
            }

            // Check for duplicate code
            if (await _repository.CodeExistsAsync(companyId, dto.Code))
            {
                return Error.Conflict($"A category with code '{dto.Code}' already exists");
            }

            var category = new DocumentCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = dto.Name.Trim(),
                Code = dto.Code.ToLowerInvariant().Trim(),
                Description = dto.Description?.Trim(),
                IsSystem = false, // User-created categories are never system categories
                IsActive = true,
                RequiresFinancialYear = dto.RequiresFinancialYear,
                DisplayOrder = dto.DisplayOrder
            };

            var created = await _repository.AddAsync(category);

            _logger.LogInformation(
                "Document category created: {CategoryId} ({Code}) for company {CompanyId}",
                created.Id, created.Code, companyId);

            return Result<DocumentCategoryDto>.Success(MapToDto(created));
        }

        public async Task<Result<DocumentCategoryDto>> UpdateAsync(Guid id, UpdateDocumentCategoryDto dto)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Category ID cannot be empty");
            }

            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return Error.NotFound($"Document category with ID {id} not found");
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

            // Check for duplicate code (excluding current category)
            if (await _repository.CodeExistsAsync(category.CompanyId, dto.Code, id))
            {
                return Error.Conflict($"A category with code '{dto.Code}' already exists");
            }

            // System categories cannot have certain fields modified
            if (category.IsSystem)
            {
                // For system categories, only allow updating display order and active status
                category.IsActive = dto.IsActive;
                category.DisplayOrder = dto.DisplayOrder;
            }
            else
            {
                category.Name = dto.Name.Trim();
                category.Code = dto.Code.ToLowerInvariant().Trim();
                category.Description = dto.Description?.Trim();
                category.IsActive = dto.IsActive;
                category.RequiresFinancialYear = dto.RequiresFinancialYear;
                category.DisplayOrder = dto.DisplayOrder;
            }

            await _repository.UpdateAsync(category);

            _logger.LogInformation(
                "Document category updated: {CategoryId} ({Code})",
                category.Id, category.Code);

            return Result<DocumentCategoryDto>.Success(MapToDto(category));
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
                return Error.NotFound($"Document category with ID {id} not found");
            }

            if (category.IsSystem)
            {
                return Error.Forbidden("System categories cannot be deleted");
            }

            await _repository.DeleteAsync(id);

            _logger.LogInformation(
                "Document category deleted: {CategoryId} ({Code})",
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
                "Default document categories seeded for company {CompanyId}",
                companyId);

            return Result<bool>.Success(true);
        }

        private static DocumentCategoryDto MapToDto(DocumentCategory category)
        {
            return new DocumentCategoryDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                Name = category.Name,
                Code = category.Code,
                Description = category.Description,
                IsSystem = category.IsSystem,
                IsActive = category.IsActive,
                RequiresFinancialYear = category.RequiresFinancialYear,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}
