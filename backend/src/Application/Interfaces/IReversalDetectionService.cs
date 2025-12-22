using Application.DTOs.BankTransactions;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for detecting and managing reversal transactions
    /// </summary>
    public interface IReversalDetectionService
    {
        /// <summary>
        /// Detect if a transaction is a reversal and get suggested originals
        /// </summary>
        Task<Result<ReversalDetectionResultDto>> DetectReversalAsync(Guid transactionId);

        /// <summary>
        /// Find potential original transactions for a reversal
        /// </summary>
        Task<Result<IEnumerable<ReversalMatchSuggestionDto>>> FindPotentialOriginalsAsync(
            Guid reversalTransactionId,
            int maxDaysBack = 90,
            int maxResults = 10);

        /// <summary>
        /// Pair a reversal transaction with its original
        /// </summary>
        Task<Result<PairReversalResultDto>> PairReversalAsync(PairReversalRequestDto request);

        /// <summary>
        /// Unpair a reversal from its original
        /// </summary>
        Task<Result> UnpairReversalAsync(Guid transactionId);

        /// <summary>
        /// Get all unpaired reversal transactions
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetUnpairedReversalsAsync(Guid? bankAccountId = null);
    }
}
