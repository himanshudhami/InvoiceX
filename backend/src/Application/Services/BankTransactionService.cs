using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for BankTransaction operations
    /// </summary>
    public class BankTransactionService : IBankTransactionService
    {
        private readonly IBankTransactionRepository _repository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IMapper _mapper;

        public BankTransactionService(
            IBankTransactionRepository repository,
            IBankAccountRepository bankAccountRepository,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<BankTransaction>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Bank transaction with ID {id} not found");

            return Result<BankTransaction>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<BankTransaction>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<BankTransaction> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (validation.IsFailure)
                return validation.Error!;

            var result = await _repository.GetPagedAsync(
                pageNumber,
                pageSize,
                searchTerm,
                sortBy,
                sortDescending,
                filters);

            return Result<(IEnumerable<BankTransaction> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<BankTransaction>> CreateAsync(CreateBankTransactionDto dto)
        {
            if (dto == null)
                return Error.Validation("Bank transaction data is required");

            // Validate bank account exists
            var bankAccount = await _bankAccountRepository.GetByIdAsync(dto.BankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {dto.BankAccountId} not found");

            // Validate transaction type
            if (dto.TransactionType != "credit" && dto.TransactionType != "debit")
                return Error.Validation("Transaction type must be 'credit' or 'debit'");

            // Map DTO to entity
            var entity = _mapper.Map<BankTransaction>(dto);

            // Generate transaction hash for duplicate detection
            entity.TransactionHash = GenerateTransactionHash(dto.TransactionDate, dto.Amount, dto.Description);
            entity.ImportSource = "manual";
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);
            return Result<BankTransaction>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateBankTransactionDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            if (dto == null)
                return Error.Validation("Bank transaction data is required");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Bank transaction with ID {id} not found");

            // Update non-null values
            if (dto.TransactionDate.HasValue)
                existingEntity.TransactionDate = dto.TransactionDate.Value;
            if (dto.ValueDate.HasValue)
                existingEntity.ValueDate = dto.ValueDate.Value;
            if (dto.Description != null)
                existingEntity.Description = dto.Description;
            if (dto.ReferenceNumber != null)
                existingEntity.ReferenceNumber = dto.ReferenceNumber;
            if (dto.ChequeNumber != null)
                existingEntity.ChequeNumber = dto.ChequeNumber;
            if (!string.IsNullOrWhiteSpace(dto.TransactionType))
            {
                if (dto.TransactionType != "credit" && dto.TransactionType != "debit")
                    return Error.Validation("Transaction type must be 'credit' or 'debit'");
                existingEntity.TransactionType = dto.TransactionType;
            }
            if (dto.Amount.HasValue)
                existingEntity.Amount = dto.Amount.Value;
            if (dto.BalanceAfter.HasValue)
                existingEntity.BalanceAfter = dto.BalanceAfter.Value;
            if (dto.Category != null)
                existingEntity.Category = dto.Category;

            existingEntity.UpdatedAt = DateTime.UtcNow;

            // Regenerate hash if relevant fields changed
            existingEntity.TransactionHash = GenerateTransactionHash(
                existingEntity.TransactionDate, existingEntity.Amount, existingEntity.Description);

            await _repository.UpdateAsync(existingEntity);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Bank transaction with ID {id} not found");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Bank Account Specific Methods ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetByBankAccountIdAsync(Guid bankAccountId)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var transactions = await _repository.GetByBankAccountIdAsync(bankAccountId);
            return Result<IEnumerable<BankTransaction>>.Success(transactions);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetByDateRangeAsync(
            Guid bankAccountId, DateOnly fromDate, DateOnly toDate)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            if (fromDate > toDate)
                return Error.Validation("From date must be before or equal to to date");

            var transactions = await _repository.GetByDateRangeAsync(bankAccountId, fromDate, toDate);
            return Result<IEnumerable<BankTransaction>>.Success(transactions);
        }

        // ==================== Reconciliation Methods ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetUnreconciledAsync(Guid? bankAccountId = null)
        {
            var transactions = await _repository.GetUnreconciledAsync(bankAccountId);
            return Result<IEnumerable<BankTransaction>>.Success(transactions);
        }

        /// <inheritdoc />
        public async Task<Result> ReconcileTransactionAsync(Guid transactionId, ReconcileTransactionDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            if (dto == null)
                return Error.Validation("Reconciliation data is required");

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            if (transaction.IsReconciled)
                return Error.Validation("Transaction is already reconciled");

            // Validate reconciled type
            var validTypes = new[] { "payment", "expense", "payroll", "tax_payment", "transfer", "contractor" };
            if (!validTypes.Contains(dto.ReconciledType.ToLower()))
                return Error.Validation($"Invalid reconciled type. Must be one of: {string.Join(", ", validTypes)}");

            await _repository.ReconcileTransactionAsync(
                transactionId, dto.ReconciledType, dto.ReconciledId, dto.ReconciledBy);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UnreconcileTransactionAsync(Guid transactionId)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            if (!transaction.IsReconciled)
                return Error.Validation("Transaction is not reconciled");

            await _repository.UnreconcileTransactionAsync(transactionId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ReconciliationSuggestionDto>>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            var payments = await _repository.GetReconciliationSuggestionsAsync(transactionId, tolerance, maxResults);

            var suggestions = payments.Select(p => new ReconciliationSuggestionDto
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                ReferenceNumber = p.ReferenceNumber,
                MatchScore = CalculateMatchScore(transaction, p),
                AmountDifference = Math.Abs(transaction.Amount - p.Amount),
                DateDifferenceInDays = Math.Abs((transaction.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                    p.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days)
            }).OrderByDescending(s => s.MatchScore).ToList();

            return Result<IEnumerable<ReconciliationSuggestionDto>>.Success(suggestions);
        }

        // ==================== Import Methods ====================

        /// <inheritdoc />
        public async Task<Result<ImportBankTransactionsResult>> ImportTransactionsAsync(ImportBankTransactionsRequest request)
        {
            if (request == null)
                return Error.Validation("Import request is required");

            if (request.Transactions == null || !request.Transactions.Any())
                return Error.Validation("At least one transaction is required");

            // Validate bank account exists
            var bankAccount = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {request.BankAccountId} not found");

            var result = new ImportBankTransactionsResult
            {
                BatchId = Guid.NewGuid()
            };

            // Generate hashes for all transactions
            var transactionHashes = request.Transactions
                .Select(t => GenerateTransactionHash(t.TransactionDate, t.Amount, t.Description))
                .ToList();

            // Get existing hashes for duplicate detection
            var existingHashes = request.SkipDuplicates
                ? (await _repository.GetExistingHashesAsync(request.BankAccountId, transactionHashes)).ToHashSet()
                : new HashSet<string>();

            var transactionsToImport = new List<BankTransaction>();

            for (int i = 0; i < request.Transactions.Count; i++)
            {
                var dto = request.Transactions[i];
                var hash = transactionHashes[i];

                // Skip duplicates
                if (request.SkipDuplicates && existingHashes.Contains(hash))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Validate transaction type
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

            // Bulk insert
            if (transactionsToImport.Any())
            {
                await _repository.BulkAddAsync(transactionsToImport);
                result.ImportedCount = transactionsToImport.Count;

                // Update bank account balance from the last transaction
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

            // Check if any transactions are reconciled
            var reconciledCount = transactions.Count(t => t.IsReconciled);
            if (reconciledCount > 0)
                return Error.Validation($"Cannot delete batch: {reconciledCount} transactions are reconciled");

            await _repository.DeleteByImportBatchIdAsync(batchId);
            return Result.Success();
        }

        // ==================== Summary Methods ====================

        /// <inheritdoc />
        public async Task<Result<BankTransactionSummaryDto>> GetSummaryAsync(
            Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var (totalCount, reconciledCount, totalCredits, totalDebits) =
                await _repository.GetSummaryAsync(bankAccountId, fromDate, toDate);

            return Result<BankTransactionSummaryDto>.Success(new BankTransactionSummaryDto
            {
                TotalCount = totalCount,
                ReconciledCount = reconciledCount,
                TotalCredits = totalCredits,
                TotalDebits = totalDebits
            });
        }

        // ==================== Auto-Reconciliation Methods ====================

        /// <inheritdoc />
        public async Task<Result<AutoReconcileResultDto>> AutoReconcileAsync(
            Guid bankAccountId,
            int minMatchScore = 80,
            decimal amountTolerance = 0.01m,
            int dateTolerance = 3)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var result = new AutoReconcileResultDto();

            // Get unreconciled bank transactions
            var unreconciledTxns = await _repository.GetUnreconciledAsync(bankAccountId);
            var transactions = unreconciledTxns.ToList();
            result.TransactionsProcessed = transactions.Count;

            foreach (var txn in transactions)
            {
                // Get suggestions for this transaction
                var suggestions = await _repository.GetReconciliationSuggestionsAsync(txn.Id, amountTolerance, 5);
                var suggestionsList = suggestions.ToList();

                if (!suggestionsList.Any())
                {
                    result.TransactionsSkipped++;
                    continue;
                }

                // Find best match above threshold
                Payments? bestMatch = null;
                int bestScore = 0;
                string matchReason = "";

                foreach (var payment in suggestionsList)
                {
                    var score = CalculateMatchScore(txn, payment);
                    if (score >= minMatchScore && score > bestScore)
                    {
                        // Additional date tolerance check
                        var dateDiff = Math.Abs((txn.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                            payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
                        if (dateDiff <= dateTolerance)
                        {
                            bestMatch = payment;
                            bestScore = score;
                            matchReason = BuildMatchReason(txn, payment, score);
                        }
                    }
                }

                if (bestMatch != null)
                {
                    // Reconcile the transaction
                    await _repository.ReconcileTransactionAsync(txn.Id, "payment", bestMatch.Id, "auto-reconcile");

                    result.TransactionsReconciled++;
                    result.TotalAmountReconciled += txn.Amount;
                    result.Matches.Add(new AutoReconcileMatchDto
                    {
                        BankTransactionId = txn.Id,
                        PaymentId = bestMatch.Id,
                        Amount = txn.Amount,
                        MatchScore = bestScore,
                        MatchReason = matchReason
                    });
                }
                else
                {
                    result.TransactionsSkipped++;
                }
            }

            return Result<AutoReconcileResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<BankReconciliationStatementDto>> GenerateBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {bankAccountId} not found");

            var brs = new BankReconciliationStatementDto
            {
                BankAccountId = bankAccountId,
                BankAccountName = bankAccount.AccountName ?? $"{bankAccount.BankName} - {bankAccount.AccountNumber}",
                AsOfDate = asOfDate,
                BankStatementBalance = bankAccount.CurrentBalance
            };

            // Get all transactions up to the as-of date
            var allTxns = await _repository.GetByDateRangeAsync(bankAccountId, DateOnly.MinValue, asOfDate);
            var transactions = allTxns.ToList();

            brs.TotalTransactions = transactions.Count;
            brs.ReconciledTransactions = transactions.Count(t => t.IsReconciled);
            brs.UnreconciledTransactions = transactions.Count(t => !t.IsReconciled);

            // Unreconciled bank credits (in bank but not in books)
            var unreconciledCredits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "credit").ToList();
            brs.BankCreditsNotInBooks = unreconciledCredits.Sum(t => t.Amount);
            brs.BankCreditsNotInBooksItems = unreconciledCredits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Unreconciled bank debits (in bank but not in books)
            var unreconciledDebits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "debit").ToList();
            brs.BankDebitsNotInBooks = unreconciledDebits.Sum(t => t.Amount);
            brs.BankDebitsNotInBooksItems = unreconciledDebits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Calculate book balance from reconciled transactions
            var reconciledCredits = transactions.Where(t => t.IsReconciled && t.TransactionType == "credit").Sum(t => t.Amount);
            var reconciledDebits = transactions.Where(t => t.IsReconciled && t.TransactionType == "debit").Sum(t => t.Amount);
            brs.BookBalance = reconciledCredits - reconciledDebits;

            // For now, deposits in transit and outstanding cheques are set to 0
            // These would need integration with payment/expense systems to track
            brs.DepositsInTransit = 0;
            brs.OutstandingCheques = 0;

            // Calculate adjusted balances
            brs.AdjustedBankBalance = brs.BankStatementBalance + brs.DepositsInTransit - brs.OutstandingCheques;
            brs.AdjustedBookBalance = brs.BookBalance + brs.BankCreditsNotInBooks - brs.BankDebitsNotInBooks;

            return Result<BankReconciliationStatementDto>.Success(brs);
        }

        private static string BuildMatchReason(BankTransaction txn, Payments payment, int score)
        {
            var reasons = new List<string>();

            if (Math.Abs(txn.Amount - payment.Amount) < 0.01m)
                reasons.Add("Exact amount match");
            else
                reasons.Add($"Amount within tolerance (diff: {Math.Abs(txn.Amount - payment.Amount):F2})");

            var dateDiff = Math.Abs((txn.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
            if (dateDiff == 0)
                reasons.Add("Same date");
            else
                reasons.Add($"Date within {dateDiff} days");

            if (!string.IsNullOrWhiteSpace(txn.ReferenceNumber) &&
                !string.IsNullOrWhiteSpace(payment.ReferenceNumber) &&
                txn.ReferenceNumber.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                reasons.Add("Reference number match");

            return $"Score: {score}% - {string.Join(", ", reasons)}";
        }

        // ==================== Helper Methods ====================

        private static string GenerateTransactionHash(DateOnly date, decimal amount, string? description)
        {
            var input = $"{date:yyyy-MM-dd}|{amount:F2}|{description ?? ""}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }

        private static int CalculateMatchScore(BankTransaction transaction, Payments payment)
        {
            int score = 0;

            // Amount match (40 points)
            var amountDiff = Math.Abs(transaction.Amount - payment.Amount);
            if (amountDiff == 0) score += 40;
            else if (amountDiff <= 0.01m) score += 35;
            else if (amountDiff <= 1m) score += 25;
            else if (amountDiff <= 10m) score += 15;

            // Date match (30 points)
            var dateDiff = Math.Abs((transaction.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
            if (dateDiff == 0) score += 30;
            else if (dateDiff <= 1) score += 25;
            else if (dateDiff <= 3) score += 15;
            else if (dateDiff <= 7) score += 5;

            // Reference match (30 points)
            if (!string.IsNullOrWhiteSpace(transaction.ReferenceNumber) &&
                !string.IsNullOrWhiteSpace(payment.ReferenceNumber))
            {
                if (transaction.ReferenceNumber.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 30;
                else if (transaction.ReferenceNumber.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                         payment.ReferenceNumber.Contains(transaction.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 20;
            }

            return Math.Min(score, 100);
        }
    }
}
