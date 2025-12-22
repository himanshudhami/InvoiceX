using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for BankTransaction CRUD operations
    /// For reconciliation, use ReconciliationService
    /// For import, use BankStatementImportService
    /// For BRS, use BrsService
    /// For reversals, use ReversalDetectionService
    /// For outgoing payments, use OutgoingPaymentsService
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

            var bankAccount = await _bankAccountRepository.GetByIdAsync(dto.BankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {dto.BankAccountId} not found");

            if (dto.TransactionType != "credit" && dto.TransactionType != "debit")
                return Error.Validation("Transaction type must be 'credit' or 'debit'");

            var entity = _mapper.Map<BankTransaction>(dto);

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

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankTransaction>>> GetUnreconciledAsync(Guid? bankAccountId = null)
        {
            var transactions = await _repository.GetUnreconciledAsync(bankAccountId);
            return Result<IEnumerable<BankTransaction>>.Success(transactions);
        }

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

        private static string GenerateTransactionHash(DateOnly date, decimal amount, string? description)
        {
            var input = $"{date:yyyy-MM-dd}|{amount:F2}|{description ?? ""}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
