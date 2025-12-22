using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    /// <summary>
    /// Service for importing bank statements
    /// </summary>
    public class BankStatementImportService : IBankStatementImportService
    {
        private readonly IBankTransactionRepository _repository;
        private readonly IBankAccountRepository _bankAccountRepository;

        public BankStatementImportService(
            IBankTransactionRepository repository,
            IBankAccountRepository bankAccountRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
        }

        /// <inheritdoc />
        public async Task<Result<ImportBankTransactionsResult>> ImportTransactionsAsync(ImportBankTransactionsRequest request)
        {
            if (request == null)
                return Error.Validation("Import request is required");

            if (request.Transactions == null || !request.Transactions.Any())
                return Error.Validation("At least one transaction is required");

            var bankAccount = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {request.BankAccountId} not found");

            var result = new ImportBankTransactionsResult
            {
                BatchId = Guid.NewGuid()
            };

            var transactionHashes = request.Transactions
                .Select(t => GenerateTransactionHash(t.TransactionDate, t.Amount, t.Description))
                .ToList();

            var existingHashes = request.SkipDuplicates
                ? (await _repository.GetExistingHashesAsync(request.BankAccountId, transactionHashes)).ToHashSet()
                : new HashSet<string>();

            var transactionsToImport = new List<BankTransaction>();

            for (int i = 0; i < request.Transactions.Count; i++)
            {
                var dto = request.Transactions[i];
                var hash = transactionHashes[i];

                if (request.SkipDuplicates && existingHashes.Contains(hash))
                {
                    result.SkippedCount++;
                    continue;
                }

                if (dto.TransactionType != "credit" && dto.TransactionType != "debit")
                {
                    result.FailedCount++;
                    result.Errors.Add($"Row {i + 1}: Transaction type must be 'credit' or 'debit'");
                    continue;
                }

                var entity = new BankTransaction
                {
                    BankAccountId = request.BankAccountId,
                    TransactionDate = dto.TransactionDate,
                    ValueDate = dto.ValueDate,
                    Description = dto.Description,
                    ReferenceNumber = dto.ReferenceNumber,
                    ChequeNumber = dto.ChequeNumber,
                    TransactionType = dto.TransactionType,
                    Amount = dto.Amount,
                    BalanceAfter = dto.BalanceAfter,
                    ImportSource = "csv",
                    ImportBatchId = result.BatchId,
                    RawData = dto.RawData,
                    TransactionHash = hash,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                transactionsToImport.Add(entity);
            }

            if (transactionsToImport.Any())
            {
                await _repository.BulkAddAsync(transactionsToImport);
                result.ImportedCount = transactionsToImport.Count;

                var lastTransaction = transactionsToImport
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.CreatedAt)
                    .FirstOrDefault();

                if (lastTransaction?.BalanceAfter.HasValue == true)
                {
                    await _bankAccountRepository.UpdateBalanceAsync(
                        request.BankAccountId,
                        lastTransaction.BalanceAfter.Value,
                        lastTransaction.TransactionDate);
                }
            }

            return Result<ImportBankTransactionsResult>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetByImportBatchIdAsync(Guid batchId)
        {
            var validation = ServiceExtensions.ValidateGuid(batchId);
            if (validation.IsFailure)
                return validation.Error!;

            var transactions = await _repository.GetByImportBatchIdAsync(batchId);
            return Result<IEnumerable<BankTransaction>>.Success(transactions);
        }

        /// <inheritdoc />
        public async Task<Result> DeleteImportBatchAsync(Guid batchId)
        {
            var validation = ServiceExtensions.ValidateGuid(batchId);
            if (validation.IsFailure)
                return validation.Error!;

            var transactions = await _repository.GetByImportBatchIdAsync(batchId);
            if (!transactions.Any())
                return Error.NotFound($"No transactions found for batch ID {batchId}");

            var reconciledCount = transactions.Count(t => t.IsReconciled);
            if (reconciledCount > 0)
                return Error.Validation($"Cannot delete batch: {reconciledCount} transactions are reconciled");

            await _repository.DeleteByImportBatchIdAsync(batchId);
            return Result.Success();
        }

        private static string GenerateTransactionHash(DateOnly date, decimal amount, string? description)
        {
            var input = $"{date:yyyy-MM-dd}|{amount:F2}|{description ?? ""}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
