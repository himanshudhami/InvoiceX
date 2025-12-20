using Application.DTOs.Expense;
using Core.Common;

namespace Application.Interfaces.Expense
{
    /// <summary>
    /// Service interface for expense category operations.
    /// </summary>
    public interface IExpenseCategoryService
    {
        /// <summary>
        /// Get an expense category by ID.
        /// </summary>
        Task<Result<ExpenseCategoryDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all expense categories for a company.
        /// </summary>
        Task<Result<IEnumerable<ExpenseCategoryDto>>> GetByCompanyAsync(
            Guid companyId, bool includeInactive = false);

        /// <summary>
        /// Get active categories for dropdown selection.
        /// </summary>
        Task<Result<IEnumerable<ExpenseCategorySelectDto>>> GetSelectListAsync(Guid companyId);

        /// <summary>
        /// Get paginated expense categories.
        /// </summary>
        Task<Result<(IEnumerable<ExpenseCategoryDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false);

        /// <summary>
        /// Create a new expense category.
        /// </summary>
        Task<Result<ExpenseCategoryDto>> CreateAsync(Guid companyId, CreateExpenseCategoryDto dto);

        /// <summary>
        /// Update an existing expense category.
        /// </summary>
        Task<Result<ExpenseCategoryDto>> UpdateAsync(Guid id, UpdateExpenseCategoryDto dto);

        /// <summary>
        /// Delete an expense category.
        /// </summary>
        Task<Result<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// Seed default categories for a new company.
        /// </summary>
        Task<Result<bool>> SeedDefaultCategoriesAsync(Guid companyId);
    }
}
