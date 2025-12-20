using Core.Entities.Document;

namespace Core.Interfaces.Document
{
    /// <summary>
    /// Repository interface for document category operations.
    /// </summary>
    public interface IDocumentCategoryRepository
    {
        /// <summary>
        /// Get a document category by ID.
        /// </summary>
        Task<DocumentCategory?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get a document category by company and code.
        /// </summary>
        Task<DocumentCategory?> GetByCodeAsync(Guid companyId, string code);

        /// <summary>
        /// Get all document categories for a company.
        /// </summary>
        Task<IEnumerable<DocumentCategory>> GetByCompanyAsync(Guid companyId, bool includeInactive = false);

        /// <summary>
        /// Get active document categories for a company, ordered by display order.
        /// </summary>
        Task<IEnumerable<DocumentCategory>> GetActiveByCompanyAsync(Guid companyId);

        /// <summary>
        /// Get paginated document categories for a company.
        /// </summary>
        Task<(IEnumerable<DocumentCategory> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false);

        /// <summary>
        /// Add a new document category.
        /// </summary>
        Task<DocumentCategory> AddAsync(DocumentCategory category);

        /// <summary>
        /// Update an existing document category.
        /// </summary>
        Task UpdateAsync(DocumentCategory category);

        /// <summary>
        /// Delete a document category (hard delete - use with caution).
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Check if a code already exists for a company.
        /// </summary>
        Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null);

        /// <summary>
        /// Seed default categories for a company.
        /// </summary>
        Task SeedDefaultCategoriesAsync(Guid companyId);
    }
}
