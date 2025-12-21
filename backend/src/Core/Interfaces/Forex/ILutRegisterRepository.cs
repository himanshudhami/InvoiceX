using Core.Entities.Forex;

namespace Core.Interfaces.Forex
{
    public interface ILutRegisterRepository
    {
        Task<LutRegister?> GetByIdAsync(Guid id);
        Task<IEnumerable<LutRegister>> GetAllAsync();
        Task<(IEnumerable<LutRegister> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<LutRegister> AddAsync(LutRegister entity);
        Task UpdateAsync(LutRegister entity);
        Task DeleteAsync(Guid id);

        // Company-specific queries
        Task<IEnumerable<LutRegister>> GetByCompanyIdAsync(Guid companyId);
        Task<LutRegister?> GetActiveForCompanyAsync(Guid companyId, string financialYear);

        // Validation
        Task<LutRegister?> GetValidForDateAsync(Guid companyId, DateOnly date);
        Task<bool> IsLutValidAsync(Guid companyId, DateOnly invoiceDate);

        // Status management
        Task ExpireOldLutsAsync(DateOnly asOfDate);
        Task SupersedeAsync(Guid oldLutId, Guid newLutId);
    }
}
