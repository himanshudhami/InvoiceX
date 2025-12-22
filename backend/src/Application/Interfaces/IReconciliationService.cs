using Application.DTOs.BankTransactions;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for bank transaction reconciliation operations
    /// </summary>
    public interface IReconciliationService
    {
        /// <summary>
        /// Reconcile a bank transaction with a record
        /// </summary>
        Task<Result> ReconcileTransactionAsync(Guid transactionId, ReconcileTransactionDto dto);

        /// <summary>
        /// Unreconcile a bank transaction
        /// </summary>
        Task<Result> UnreconcileTransactionAsync(Guid transactionId);

        /// <summary>
        /// Get credit reconciliation suggestions (incoming payments)
        /// </summary>
        Task<Result<IEnumerable<ReconciliationSuggestionDto>>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 1000m, int maxResults = 10);

        /// <summary>
        /// Search payments for reconciliation
        /// </summary>
        Task<Result<IEnumerable<ReconciliationSuggestionDto>>> SearchPaymentsAsync(
            Guid companyId,
            string? searchTerm = null,
            decimal? amountMin = null,
            decimal? amountMax = null,
            int maxResults = 20);

        /// <summary>
        /// Get debit reconciliation suggestions (outgoing payments)
        /// </summary>
        Task<Result<IEnumerable<DebitReconciliationSuggestionDto>>> GetDebitReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 1000m, int maxResults = 10);

        /// <summary>
        /// Search reconciliation candidates across all expense types
        /// </summary>
        Task<Result<(IEnumerable<DebitReconciliationSuggestionDto> Items, int TotalCount)>> SearchReconciliationCandidatesAsync(
            ReconciliationSearchRequest request);

        /// <summary>
        /// Automatically reconcile transactions based on matching rules
        /// </summary>
        Task<Result<AutoReconcileResultDto>> AutoReconcileAsync(
            Guid bankAccountId,
            int minMatchScore = 80,
            decimal amountTolerance = 100m,
            int dateTolerance = 3);
    }
}
