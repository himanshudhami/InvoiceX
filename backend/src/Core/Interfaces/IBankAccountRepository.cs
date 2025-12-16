using Core.Entities;

namespace Core.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<BankAccount?> GetByIdAsync(Guid id);
        Task<IEnumerable<BankAccount>> GetAllAsync();
        Task<(IEnumerable<BankAccount> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<BankAccount> AddAsync(BankAccount entity);
        Task UpdateAsync(BankAccount entity);
        Task DeleteAsync(Guid id);

        // Company-specific queries
        Task<IEnumerable<BankAccount>> GetByCompanyIdAsync(Guid companyId);
        Task<BankAccount?> GetPrimaryAccountAsync(Guid companyId);
        Task<IEnumerable<BankAccount>> GetActiveAccountsAsync(Guid? companyId = null);

        // Balance operations
        Task UpdateBalanceAsync(Guid id, decimal newBalance, DateOnly asOfDate);

        // Primary account management
        Task SetPrimaryAccountAsync(Guid companyId, Guid accountId);
    }
}
