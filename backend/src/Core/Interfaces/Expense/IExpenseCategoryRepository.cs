using Core.Entities.Expense;

namespace Core.Interfaces.Expense
{
    /// <summary>
    /// Repository interface for expense category operations.
    /// </summary>
    public interface IExpenseCategoryRepository
    {
        /// <summary>
        /// Get an expense category by ID.
        /// </summary>
        Task<ExpenseCategory?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get an expense category by company and code.
        /// </summary>
        Task<ExpenseCategory?> GetByCodeAsync(Guid companyId, string code);

        /// <summary>
        /// Get all expense categories for a company.
        /// </summary>
        Task<IEnumerable<ExpenseCategory>> GetByCompanyAsync(Guid companyId, bool includeInactive = false);

        /// <summary>
        /// Get active expense categories for a company, ordered by display order.
        /// </summary>
        Task<IEnumerable<ExpenseCategory>> GetActiveByCompanyAsync(Guid companyId);

        /// <summary>
        /// Get paginated expense categories for a company.
        /// </summary>
        Task<(IEnumerable<ExpenseCategory> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false);

        /// <summary>
        /// Add a new expense category.
        /// </summary>
        Task<ExpenseCategory> AddAsync(ExpenseCategory category);

        /// <summary>
        /// Update an existing expense category.
        /// </summary>
        Task UpdateAsync(ExpenseCategory category);

        /// <summary>
        /// Delete an expense category.
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
