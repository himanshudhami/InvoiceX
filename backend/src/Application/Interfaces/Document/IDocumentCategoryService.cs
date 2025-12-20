using Application.DTOs.Document;
using Core.Common;

namespace Application.Interfaces.Document
{
    /// <summary>
    /// Service interface for document category operations.
    /// </summary>
    public interface IDocumentCategoryService
    {
        /// <summary>
        /// Get a document category by ID.
        /// </summary>
        Task<Result<DocumentCategoryDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all document categories for a company.
        /// </summary>
        Task<Result<IEnumerable<DocumentCategoryDto>>> GetByCompanyAsync(
            Guid companyId, bool includeInactive = false);

        /// <summary>
        /// Get active categories for dropdown selection.
        /// </summary>
        Task<Result<IEnumerable<DocumentCategorySelectDto>>> GetSelectListAsync(Guid companyId);

        /// <summary>
        /// Get paginated document categories.
        /// </summary>
        Task<Result<(IEnumerable<DocumentCategoryDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false);

        /// <summary>
        /// Create a new document category.
        /// </summary>
        Task<Result<DocumentCategoryDto>> CreateAsync(Guid companyId, CreateDocumentCategoryDto dto);

        /// <summary>
        /// Update an existing document category.
        /// </summary>
        Task<Result<DocumentCategoryDto>> UpdateAsync(Guid id, UpdateDocumentCategoryDto dto);

        /// <summary>
        /// Delete a document category.
        /// </summary>
        Task<Result<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// Seed default categories for a new company.
        /// </summary>
        Task<Result<bool>> SeedDefaultCategoriesAsync(Guid companyId);
    }
}
