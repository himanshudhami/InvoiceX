using Core.Entities.Expense;

namespace Core.Interfaces.Expense
{
    /// <summary>
    /// Repository interface for expense claim operations.
    /// </summary>
    public interface IExpenseClaimRepository
    {
        /// <summary>
        /// Get an expense claim by ID with related data.
        /// </summary>
        Task<ExpenseClaim?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get an expense claim by claim number.
        /// </summary>
        Task<ExpenseClaim?> GetByClaimNumberAsync(Guid companyId, string claimNumber);

        /// <summary>
        /// Get all expense claims for an employee.
        /// </summary>
        Task<IEnumerable<ExpenseClaim>> GetByEmployeeAsync(Guid employeeId);

        /// <summary>
        /// Get expense claims by status.
        /// </summary>
        Task<IEnumerable<ExpenseClaim>> GetByStatusAsync(Guid companyId, string status);

        /// <summary>
        /// Get pending approval claims for a manager (claims from their direct reports).
        /// </summary>
        Task<IEnumerable<ExpenseClaim>> GetPendingForManagerAsync(Guid managerId);

        /// <summary>
        /// Get paginated expense claims for a company.
        /// </summary>
        Task<(IEnumerable<ExpenseClaim> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            Guid? employeeId = null,
            Guid? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Get paginated expense claims for an employee.
        /// </summary>
        Task<(IEnumerable<ExpenseClaim> Items, int TotalCount)> GetPagedByEmployeeAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? status = null);

        /// <summary>
        /// Add a new expense claim.
        /// </summary>
        Task<ExpenseClaim> AddAsync(ExpenseClaim claim);

        /// <summary>
        /// Update an existing expense claim.
        /// </summary>
        Task UpdateAsync(ExpenseClaim claim);

        /// <summary>
        /// Delete an expense claim (hard delete - only for drafts).
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Mark an expense claim as reconciled with a bank transaction.
        /// </summary>
        Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy);

        /// <summary>
        /// Clear reconciliation fields for an expense claim.
        /// </summary>
        Task ClearReconciliationAsync(Guid id);

        /// <summary>
        /// Generate a new claim number for a company.
        /// </summary>
        Task<string> GenerateClaimNumberAsync(Guid companyId);

        /// <summary>
        /// Get expense summary for reporting.
        /// </summary>
        Task<ExpenseSummary> GetSummaryAsync(
            Guid companyId,
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }

    /// <summary>
    /// Expense summary for reporting.
    /// </summary>
    public class ExpenseSummary
    {
        public int TotalClaims { get; set; }
        public int DraftClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int ReimbursedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal ReimbursedAmount { get; set; }
        public Dictionary<string, decimal> AmountByCategory { get; set; } = new();
    }
}
