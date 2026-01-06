using Core.Entities;

namespace Core.Interfaces
{
    public interface IVendorsRepository
    {
        Task<Vendors?> GetByIdAsync(Guid id);
        Task<IEnumerable<Vendors>> GetAllAsync();
        Task<IEnumerable<Vendors>> GetByCompanyIdAsync(Guid companyId);
        Task<(IEnumerable<Vendors> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Vendors?> GetByGstinAsync(Guid companyId, string gstin);
        Task<Vendors?> GetByPanAsync(Guid companyId, string panNumber);
        Task<IEnumerable<Vendors>> GetMsmeVendorsAsync(Guid companyId);
        Task<IEnumerable<Vendors>> GetTdsApplicableVendorsAsync(Guid companyId);
        Task<Vendors?> GetByTallyGuidAsync(string tallyLedgerGuid);
        Task<Vendors> AddAsync(Vendors entity);
        Task UpdateAsync(Vendors entity);
        Task DeleteAsync(Guid id);
        Task<decimal> GetOutstandingBalanceAsync(Guid vendorId);
    }
}
