using Core.Entities.Ledger;

namespace Core.Interfaces.Ledger
{
    /// <summary>
    /// Repository interface for Chart of Accounts operations
    /// </summary>
    public interface IChartOfAccountRepository
    {
        // ==================== Basic CRUD ====================

        Task<ChartOfAccount?> GetByIdAsync(Guid id);
        Task<IEnumerable<ChartOfAccount>> GetAllAsync();
        Task<(IEnumerable<ChartOfAccount> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<ChartOfAccount> AddAsync(ChartOfAccount entity);
        Task UpdateAsync(ChartOfAccount entity);
        Task DeleteAsync(Guid id);

        // ==================== Company-Specific Queries ====================

        /// <summary>
        /// Get all accounts for a company
        /// </summary>
        Task<IEnumerable<ChartOfAccount>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get accounts by type (asset, liability, equity, income, expense)
        /// </summary>
        Task<IEnumerable<ChartOfAccount>> GetByTypeAsync(Guid companyId, string accountType);

        /// <summary>
        /// Get account by code within a company
        /// </summary>
        Task<ChartOfAccount?> GetByCodeAsync(Guid companyId, string accountCode);

        /// <summary>
        /// Get child accounts of a parent
        /// </summary>
        Task<IEnumerable<ChartOfAccount>> GetChildrenAsync(Guid parentAccountId);

        /// <summary>
        /// Get hierarchical tree structure for a company
        /// </summary>
        Task<IEnumerable<ChartOfAccount>> GetHierarchyAsync(Guid companyId);

        // ==================== Balance Queries ====================

        /// <summary>
        /// Get account with current balance
        /// </summary>
        Task<(ChartOfAccount Account, decimal Balance)?> GetWithBalanceAsync(Guid accountId);

        /// <summary>
        /// Update account balance (called by triggers/service)
        /// </summary>
        Task UpdateBalanceAsync(Guid accountId, decimal newBalance);

        // ==================== Initialization ====================

        /// <summary>
        /// Check if a company has chart of accounts initialized
        /// </summary>
        Task<bool> HasAccountsAsync(Guid companyId);

        /// <summary>
        /// Initialize default chart of accounts for a company
        /// </summary>
        Task InitializeDefaultAccountsAsync(Guid companyId, Guid? createdBy = null);
    }
}
