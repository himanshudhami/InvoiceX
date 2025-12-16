using Application.DTOs.BankAccounts;
using Core.Entities;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for BankAccount operations
    /// </summary>
    public interface IBankAccountService
    {
        /// <summary>
        /// Get bank account by ID
        /// </summary>
        Task<Result<BankAccount>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all bank accounts
        /// </summary>
        Task<Result<IEnumerable<BankAccount>>> GetAllAsync();

        /// <summary>
        /// Get paginated bank accounts with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<BankAccount> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new bank account
        /// </summary>
        Task<Result<BankAccount>> CreateAsync(CreateBankAccountDto dto);

        /// <summary>
        /// Update an existing bank account
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateBankAccountDto dto);

        /// <summary>
        /// Delete a bank account by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Check if bank account exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);

        // ==================== Specialized Methods ====================

        /// <summary>
        /// Get bank accounts by company ID
        /// </summary>
        Task<Result<IEnumerable<BankAccount>>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get the primary bank account for a company
        /// </summary>
        Task<Result<BankAccount?>> GetPrimaryAccountAsync(Guid companyId);

        /// <summary>
        /// Get all active bank accounts, optionally filtered by company
        /// </summary>
        Task<Result<IEnumerable<BankAccount>>> GetActiveAccountsAsync(Guid? companyId = null);

        /// <summary>
        /// Update the current balance of a bank account
        /// </summary>
        Task<Result> UpdateBalanceAsync(Guid id, UpdateBalanceDto dto);

        /// <summary>
        /// Set a bank account as the primary account for a company
        /// </summary>
        Task<Result> SetPrimaryAccountAsync(Guid companyId, Guid accountId);
    }
}
