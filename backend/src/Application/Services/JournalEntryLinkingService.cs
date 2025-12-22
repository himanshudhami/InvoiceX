using Application.Interfaces;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Service for linking bank transactions to journal entries.
    /// Enables hybrid reconciliation (source documents + JE lines) for:
    /// - Proper BRS generation from ledger perspective
    /// - Complete audit trail: Bank txn → JE line → Source doc
    /// - CA compliance per ICAI standards
    /// </summary>
    public class JournalEntryLinkingService : IJournalEntryLinkingService
    {
        private readonly IBankTransactionRepository _bankTransactionRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly ILogger<JournalEntryLinkingService> _logger;

        public JournalEntryLinkingService(
            IBankTransactionRepository bankTransactionRepository,
            IBankAccountRepository bankAccountRepository,
            IJournalEntryRepository journalEntryRepository,
            ILogger<JournalEntryLinkingService> logger)
        {
            _bankTransactionRepository = bankTransactionRepository ?? throw new ArgumentNullException(nameof(bankTransactionRepository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
            _journalEntryRepository = journalEntryRepository ?? throw new ArgumentNullException(nameof(journalEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result<(Guid JournalEntryId, Guid JournalEntryLineId)?>> FindBankJournalEntryLineAsync(
            string sourceType,
            Guid sourceId,
            Guid bankAccountId)
        {
            if (string.IsNullOrEmpty(sourceType))
                return Error.Validation("Source type is required");

            if (sourceId == Guid.Empty)
                return Error.Validation("Source ID is required");

            if (bankAccountId == Guid.Empty)
                return Error.Validation("Bank account ID is required");

            try
            {
                // 1. Get the bank account to find its linked ledger account
                var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
                if (bankAccount == null)
                {
                    _logger.LogWarning("Bank account {BankAccountId} not found", bankAccountId);
                    return Result<(Guid, Guid)?>.Success(null);
                }

                if (!bankAccount.LinkedAccountId.HasValue)
                {
                    _logger.LogWarning("Bank account {BankAccountId} has no linked ledger account", bankAccountId);
                    return Result<(Guid, Guid)?>.Success(null);
                }

                var linkedAccountId = bankAccount.LinkedAccountId.Value;

                // 2. Get the company ID from bank account
                if (!bankAccount.CompanyId.HasValue)
                {
                    _logger.LogWarning("Bank account {BankAccountId} has no company ID", bankAccountId);
                    return Result<(Guid, Guid)?>.Success(null);
                }

                var companyId = bankAccount.CompanyId.Value;

                // 3. Find journal entries for this source document
                var journalEntries = await _journalEntryRepository.GetBySourceAsync(
                    companyId,
                    sourceType,
                    sourceId);

                var jeList = journalEntries.ToList();
                if (!jeList.Any())
                {
                    _logger.LogDebug("No journal entries found for {SourceType}/{SourceId}",
                        sourceType, sourceId);
                    return Result<(Guid, Guid)?>.Success(null);
                }

                // 4. Find the JE that's posted (prefer posted over draft)
                var postedJe = jeList.FirstOrDefault(je => je.Status == "posted") ?? jeList.First();

                // 5. Get lines and find the one affecting the bank account
                var lines = await _journalEntryRepository.GetLinesAsync(postedJe.Id);
                var bankLine = lines.FirstOrDefault(l => l.AccountId == linkedAccountId);

                if (bankLine == null)
                {
                    _logger.LogDebug("No JE line found affecting bank account {LinkedAccountId} in JE {JeId}",
                        linkedAccountId, postedJe.Id);
                    return Result<(Guid, Guid)?>.Success(null);
                }

                _logger.LogDebug("Found JE link: JE {JeId}, Line {LineId} for {SourceType}/{SourceId}",
                    postedJe.Id, bankLine.Id, sourceType, sourceId);

                return Result<(Guid, Guid)?>.Success((postedJe.Id, bankLine.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding JE link for {SourceType}/{SourceId}",
                    sourceType, sourceId);
                return Error.Internal($"Error finding journal entry link: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> AutoLinkJournalEntryAsync(Guid bankTransactionId)
        {
            if (bankTransactionId == Guid.Empty)
                return Error.Validation("Bank transaction ID is required");

            try
            {
                var transaction = await _bankTransactionRepository.GetByIdAsync(bankTransactionId);
                if (transaction == null)
                    return Error.NotFound($"Bank transaction {bankTransactionId} not found");

                if (!transaction.IsReconciled)
                    return Error.Validation("Transaction is not reconciled");

                if (string.IsNullOrEmpty(transaction.ReconciledType) || !transaction.ReconciledId.HasValue)
                    return Error.Validation("Transaction has no source document link");

                // Already has JE link?
                if (transaction.ReconciledJournalEntryId.HasValue)
                {
                    _logger.LogDebug("Transaction {TxnId} already has JE link", bankTransactionId);
                    return Result.Success();
                }

                // Find the JE link
                var linkResult = await FindBankJournalEntryLineAsync(
                    transaction.ReconciledType,
                    transaction.ReconciledId.Value,
                    transaction.BankAccountId);

                if (linkResult.IsFailure)
                    return linkResult.Error!;

                if (!linkResult.Value.HasValue)
                {
                    _logger.LogDebug("No JE link found for transaction {TxnId}", bankTransactionId);
                    return Result.Success(); // Not an error, just no JE yet
                }

                // Update the transaction with JE link
                await _bankTransactionRepository.UpdateJournalEntryLinkAsync(
                    bankTransactionId,
                    linkResult.Value.Value.JournalEntryId,
                    linkResult.Value.Value.JournalEntryLineId);

                _logger.LogInformation("Linked transaction {TxnId} to JE {JeId}, Line {LineId}",
                    bankTransactionId,
                    linkResult.Value.Value.JournalEntryId,
                    linkResult.Value.Value.JournalEntryLineId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-linking JE for transaction {TxnId}", bankTransactionId);
                return Error.Internal($"Error auto-linking journal entry: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BackfillResult>> BackfillJournalEntryLinksAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            var result = new BackfillResult();

            try
            {
                // Get all reconciled transactions without JE link for this company
                var transactions = await _bankTransactionRepository.GetReconciledWithoutJeLinkAsync(companyId);
                var txnList = transactions.ToList();

                _logger.LogInformation("Backfilling JE links for {Count} transactions in company {CompanyId}",
                    txnList.Count, companyId);

                foreach (var txn in txnList)
                {
                    // Skip if no source document info
                    if (string.IsNullOrEmpty(txn.ReconciledType) || !txn.ReconciledId.HasValue)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    try
                    {
                        var linkResult = await FindBankJournalEntryLineAsync(
                            txn.ReconciledType,
                            txn.ReconciledId.Value,
                            txn.BankAccountId);

                        if (linkResult.IsFailure)
                        {
                            result.FailedCount++;
                            result.FailedItems.Add(new BackfillFailure
                            {
                                TransactionId = txn.Id,
                                Reason = linkResult.Error?.Message ?? "Unknown error"
                            });
                            continue;
                        }

                        if (!linkResult.Value.HasValue)
                        {
                            result.FailedCount++;
                            result.FailedItems.Add(new BackfillFailure
                            {
                                TransactionId = txn.Id,
                                Reason = "No journal entry found for source document"
                            });
                            continue;
                        }

                        // Update the transaction
                        await _bankTransactionRepository.UpdateJournalEntryLinkAsync(
                            txn.Id,
                            linkResult.Value.Value.JournalEntryId,
                            linkResult.Value.Value.JournalEntryLineId);

                        result.LinkedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.FailedItems.Add(new BackfillFailure
                        {
                            TransactionId = txn.Id,
                            Reason = ex.Message
                        });
                    }
                }

                _logger.LogInformation(
                    "Backfill complete for company {CompanyId}: {Linked} linked, {Skipped} skipped, {Failed} failed",
                    companyId, result.LinkedCount, result.SkippedCount, result.FailedCount);

                return Result<BackfillResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during backfill for company {CompanyId}", companyId);
                return Error.Internal($"Error during backfill: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<(int Count, IEnumerable<Guid> TransactionIds)>> GetUnlinkedTransactionsAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            try
            {
                var transactions = await _bankTransactionRepository.GetReconciledWithoutJeLinkAsync(companyId);
                var ids = transactions.Select(t => t.Id).ToList();

                return Result<(int, IEnumerable<Guid>)>.Success((ids.Count, ids));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unlinked transactions for company {CompanyId}", companyId);
                return Error.Internal($"Error getting unlinked transactions: {ex.Message}");
            }
        }
    }
}
