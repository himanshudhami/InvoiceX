using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface ITaxSlabRepository
    {
        Task<TaxSlab?> GetByIdAsync(Guid id);
        Task<IEnumerable<TaxSlab>> GetAllAsync();
        Task<IEnumerable<TaxSlab>> GetByFinancialYearAsync(string financialYear);
        Task<IEnumerable<TaxSlab>> GetByRegimeAndYearAsync(string regime, string financialYear);

        /// <summary>
        /// Get tax slabs by regime, financial year, and taxpayer category (for senior citizen support)
        /// </summary>
        /// <param name="regime">Tax regime ('old' or 'new')</param>
        /// <param name="financialYear">Financial year (e.g., '2024-25')</param>
        /// <param name="category">Taxpayer category ('all', 'senior', 'super_senior')</param>
        Task<IEnumerable<TaxSlab>> GetByRegimeYearAndCategoryAsync(string regime, string financialYear, string category);

        Task<TaxSlab?> GetSlabForIncomeAsync(decimal income, string regime, string financialYear);
        Task<(IEnumerable<TaxSlab> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<TaxSlab> AddAsync(TaxSlab entity);
        Task UpdateAsync(TaxSlab entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<TaxSlab>> BulkAddAsync(IEnumerable<TaxSlab> entities);
    }
}
