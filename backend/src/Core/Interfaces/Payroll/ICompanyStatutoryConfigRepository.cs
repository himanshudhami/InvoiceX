using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface ICompanyStatutoryConfigRepository
    {
        Task<CompanyStatutoryConfig?> GetByIdAsync(Guid id);
        Task<IEnumerable<CompanyStatutoryConfig>> GetAllAsync();
        Task<CompanyStatutoryConfig?> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<CompanyStatutoryConfig>> GetActiveConfigsAsync();
        Task<(IEnumerable<CompanyStatutoryConfig> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<CompanyStatutoryConfig> AddAsync(CompanyStatutoryConfig entity);
        Task UpdateAsync(CompanyStatutoryConfig entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsForCompanyAsync(Guid companyId, Guid? excludeId = null);
    }
}
