using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IProfessionalTaxSlabRepository
    {
        Task<ProfessionalTaxSlab?> GetByIdAsync(Guid id);
        Task<IEnumerable<ProfessionalTaxSlab>> GetAllAsync();
        Task<IEnumerable<ProfessionalTaxSlab>> GetByStateAsync(string state);
        Task<ProfessionalTaxSlab?> GetSlabForIncomeAsync(decimal monthlyIncome, string state);
        Task<(IEnumerable<ProfessionalTaxSlab> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<ProfessionalTaxSlab> AddAsync(ProfessionalTaxSlab entity);
        Task UpdateAsync(ProfessionalTaxSlab entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ProfessionalTaxSlab>> BulkAddAsync(IEnumerable<ProfessionalTaxSlab> entities);
        Task<IEnumerable<string>> GetDistinctStatesAsync();
        Task<bool> ExistsForStateAndRangeAsync(string state, decimal minIncome, decimal? maxIncome, Guid? excludeId = null);
    }
}
