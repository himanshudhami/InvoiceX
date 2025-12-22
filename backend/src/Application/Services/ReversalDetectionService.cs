using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Application.Utilities;
using Core.Entities;
using Core.Interfaces;
using Core.Common;

namespace Application.Services
{
    /// <summary>
    /// Service for detecting and managing reversal transactions
    /// </summary>
    public class ReversalDetectionService : IReversalDetectionService
    {
        private readonly IBankTransactionRepository _repository;

        public ReversalDetectionService(IBankTransactionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public async Task<Result<ReversalDetectionResultDto>> DetectReversalAsync(Guid transactionId)
        {
            if (transactionId == Guid.Empty)
                return Error.Validation("Transaction ID cannot be empty");

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Transaction with ID {transactionId} not found");

            var (isReversal, pattern, confidence) = ReversalDetector.DetectReversal(transaction.Description);

            var result = new ReversalDetectionResultDto
            {
                IsReversal = isReversal,
                DetectedPattern = pattern,
                Confidence = confidence,
                ExtractedOriginalReference = ReversalDetector.ExtractOriginalReference(transaction.Description)
            };

            if (isReversal)
            {
                var potentialOriginals = await _repository.FindPotentialOriginalsForReversalAsync(transactionId, 90, 10);

                result.SuggestedOriginals = potentialOriginals.Select(orig =>
                {
                    var (score, reason) = ReversalDetector.CalculateMatchScore(
                        orig.Amount, orig.TransactionDate, orig.Description, orig.ReferenceNumber,
                        transaction.Amount, transaction.TransactionDate, transaction.Description, transaction.ReferenceNumber);

                    return new ReversalMatchSuggestionDto
                    {
                        TransactionId = orig.Id,
                        TransactionDate = orig.TransactionDate,
                        Description = orig.Description,
                        ReferenceNumber = orig.ReferenceNumber,
                        Amount = orig.Amount,
                        TransactionType = orig.TransactionType,
                        MatchScore = score,
                        MatchReason = reason,
                        IsReconciled = orig.IsReconciled,
                        ReconciledType = orig.ReconciledType,
                        ReconciledId = orig.ReconciledId
                    };
                })
                .OrderByDescending(s => s.MatchScore)
                .ToList();

                if (!transaction.IsReversalTransaction)
                {
                    await _repository.UpdateReversalFlagAsync(transactionId, true);
                }
            }

            return Result<ReversalDetectionResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ReversalMatchSuggestionDto>>> FindPotentialOriginalsAsync(
            Guid reversalTransactionId,
            int maxDaysBack = 90,
            int maxResults = 10)
        {
            if (reversalTransactionId == Guid.Empty)
                return Error.Validation("Reversal transaction ID cannot be empty");

            var reversal = await _repository.GetByIdAsync(reversalTransactionId);
            if (reversal == null)
                return Error.NotFound($"Transaction with ID {reversalTransactionId} not found");

            var potentialOriginals = await _repository.FindPotentialOriginalsForReversalAsync(
                reversalTransactionId, maxDaysBack, maxResults);

            var suggestions = potentialOriginals.Select(orig =>
            {
                var (score, reason) = ReversalDetector.CalculateMatchScore(
                    orig.Amount, orig.TransactionDate, orig.Description, orig.ReferenceNumber,
                    reversal.Amount, reversal.TransactionDate, reversal.Description, reversal.ReferenceNumber);

                return new ReversalMatchSuggestionDto
                {
                    TransactionId = orig.Id,
                    TransactionDate = orig.TransactionDate,
                    Description = orig.Description,
                    ReferenceNumber = orig.ReferenceNumber,
                    Amount = orig.Amount,
                    TransactionType = orig.TransactionType,
                    MatchScore = score,
                    MatchReason = reason,
                    IsReconciled = orig.IsReconciled,
                    ReconciledType = orig.ReconciledType,
                    ReconciledId = orig.ReconciledId
                };
            })
            .OrderByDescending(s => s.MatchScore)
            .ToList();

            return Result<IEnumerable<ReversalMatchSuggestionDto>>.Success(suggestions);
        }

        /// <inheritdoc />
        public async Task<Result<PairReversalResultDto>> PairReversalAsync(PairReversalRequestDto request)
        {
            if (request.ReversalTransactionId == Guid.Empty)
                return Error.Validation("Reversal transaction ID cannot be empty");
            if (request.OriginalTransactionId == Guid.Empty)
                return Error.Validation("Original transaction ID cannot be empty");

            var reversal = await _repository.GetByIdAsync(request.ReversalTransactionId);
            if (reversal == null)
                return Error.NotFound($"Reversal transaction with ID {request.ReversalTransactionId} not found");

            var original = await _repository.GetByIdAsync(request.OriginalTransactionId);
            if (original == null)
                return Error.NotFound($"Original transaction with ID {request.OriginalTransactionId} not found");

            if (reversal.TransactionType != "credit")
                return Error.Validation("Reversal transaction must be a credit (incoming)");
            if (original.TransactionType != "debit")
                return Error.Validation("Original transaction must be a debit (outgoing)");

            if (reversal.Amount != original.Amount)
                return Error.Validation($"Amount mismatch: reversal is {reversal.Amount}, original is {original.Amount}");

            if (reversal.BankAccountId != original.BankAccountId)
                return Error.Validation("Both transactions must be from the same bank account");

            if (reversal.PairedTransactionId.HasValue)
                return Error.Conflict("Reversal transaction is already paired");
            if (original.PairedTransactionId.HasValue)
                return Error.Conflict("Original transaction is already paired");

            Guid? reversalJournalEntryId = null;

            // TODO: If original was posted to ledger, create reversal journal entry
            // if (request.OriginalWasPostedToLedger && request.OriginalJournalEntryId.HasValue)
            // {
            //     reversalJournalEntryId = await CreateReversalJournalEntry(request.OriginalJournalEntryId.Value);
            // }

            await _repository.PairReversalAsync(
                request.OriginalTransactionId,
                request.ReversalTransactionId,
                reversalJournalEntryId);

            var result = new PairReversalResultDto
            {
                Success = true,
                OriginalTransactionId = request.OriginalTransactionId,
                ReversalTransactionId = request.ReversalTransactionId,
                ReversalJournalEntryId = reversalJournalEntryId,
                Message = reversalJournalEntryId.HasValue
                    ? "Transactions paired and reversal journal entry created"
                    : "Transactions paired (no ledger impact)"
            };

            return Result<PairReversalResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result> UnpairReversalAsync(Guid transactionId)
        {
            if (transactionId == Guid.Empty)
                return Error.Validation("Transaction ID cannot be empty");

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Transaction with ID {transactionId} not found");

            if (!transaction.PairedTransactionId.HasValue)
                return Error.Validation("Transaction is not paired with any reversal");

            // TODO: If there's a reversal journal entry, reverse it
            // if (transaction.ReversalJournalEntryId.HasValue)
            // {
            //     await _journalEntryService.DeleteAsync(transaction.ReversalJournalEntryId.Value);
            // }

            await _repository.UnpairReversalAsync(transactionId);

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetUnpairedReversalsAsync(Guid? bankAccountId = null)
        {
            var reversals = await _repository.GetUnpairedReversalsAsync(bankAccountId);
            return Result<IEnumerable<BankTransaction>>.Success(reversals);
        }
    }
}
