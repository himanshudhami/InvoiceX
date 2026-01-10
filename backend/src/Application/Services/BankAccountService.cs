using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Audit;
using Application.DTOs.BankAccounts;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for BankAccount operations
    /// </summary>
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;

        public BankAccountService(
            IBankAccountRepository repository,
            IAuditService auditService,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<BankAccount>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Bank account with ID {id} not found");

            return Result<BankAccount>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankAccount>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<BankAccount>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<BankAccount> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<BankAccount> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<BankAccount>> CreateAsync(CreateBankAccountDto dto)
        {
            if (dto == null)
                return Error.Validation("Bank account data is required");

            if (string.IsNullOrWhiteSpace(dto.AccountName))
                return Error.Validation("Account name is required");

            if (string.IsNullOrWhiteSpace(dto.AccountNumber))
                return Error.Validation("Account number is required");

            if (string.IsNullOrWhiteSpace(dto.BankName))
                return Error.Validation("Bank name is required");

            // Map DTO to entity
            var entity = _mapper.Map<BankAccount>(dto);

            // Set default values
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // If current balance not provided, use opening balance
            if (!dto.CurrentBalance.HasValue)
            {
                entity.CurrentBalance = entity.OpeningBalance;
            }

            // If as of date not provided, use today
            if (!dto.AsOfDate.HasValue)
            {
                entity.AsOfDate = DateOnly.FromDateTime(DateTime.Today);
            }

            var createdEntity = await _repository.AddAsync(entity);

            // Audit trail
            if (createdEntity.CompanyId.HasValue)
            {
                await _auditService.AuditCreateAsync(createdEntity, createdEntity.Id, createdEntity.CompanyId.Value, createdEntity.AccountName);
            }

            return Result<BankAccount>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateBankAccountDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            if (dto == null)
                return Error.Validation("Bank account data is required");

            // Check if entity exists
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Bank account with ID {id} not found");

            // Capture state before update for audit trail
            var oldEntity = _mapper.Map<BankAccount>(existingEntity);

            // Map DTO to existing entity (only update non-null values)
            if (!string.IsNullOrWhiteSpace(dto.AccountName))
                existingEntity.AccountName = dto.AccountName;
            if (!string.IsNullOrWhiteSpace(dto.AccountNumber))
                existingEntity.AccountNumber = dto.AccountNumber;
            if (!string.IsNullOrWhiteSpace(dto.BankName))
                existingEntity.BankName = dto.BankName;
            if (dto.IfscCode != null)
                existingEntity.IfscCode = dto.IfscCode;
            if (dto.BranchName != null)
                existingEntity.BranchName = dto.BranchName;
            if (!string.IsNullOrWhiteSpace(dto.AccountType))
                existingEntity.AccountType = dto.AccountType;
            if (!string.IsNullOrWhiteSpace(dto.Currency))
                existingEntity.Currency = dto.Currency;
            if (dto.OpeningBalance.HasValue)
                existingEntity.OpeningBalance = dto.OpeningBalance.Value;
            if (dto.CurrentBalance.HasValue)
                existingEntity.CurrentBalance = dto.CurrentBalance.Value;
            if (dto.AsOfDate.HasValue)
                existingEntity.AsOfDate = dto.AsOfDate.Value;
            if (dto.IsPrimary.HasValue)
                existingEntity.IsPrimary = dto.IsPrimary.Value;
            if (dto.IsActive.HasValue)
                existingEntity.IsActive = dto.IsActive.Value;
            if (dto.Notes != null)
                existingEntity.Notes = dto.Notes;

            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);

            // Audit trail
            if (existingEntity.CompanyId.HasValue)
            {
                await _auditService.AuditUpdateAsync(oldEntity, existingEntity, existingEntity.Id, existingEntity.CompanyId.Value, existingEntity.AccountName);
            }

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
                return Error.NotFound($"Bank account with ID {id} not found");

            // Audit trail before delete
            if (existingEntity.CompanyId.HasValue)
            {
                await _auditService.AuditDeleteAsync(existingEntity, existingEntity.Id, existingEntity.CompanyId.Value, existingEntity.AccountName);
            }

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            if (id == default(Guid))
                return Result<bool>.Success(false);

            var entity = await _repository.GetByIdAsync(id);
            return Result<bool>.Success(entity != null);
        }

        // ==================== Specialized Methods ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankAccount>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var accounts = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<BankAccount>>.Success(accounts);
        }

        /// <inheritdoc />
        public async Task<Result<BankAccount?>> GetPrimaryAccountAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var account = await _repository.GetPrimaryAccountAsync(companyId);
            return Result<BankAccount?>.Success(account);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<BankAccount>>> GetActiveAccountsAsync(Guid? companyId = null)
        {
            var accounts = await _repository.GetActiveAccountsAsync(companyId);
            return Result<IEnumerable<BankAccount>>.Success(accounts);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateBalanceAsync(Guid id, UpdateBalanceDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            if (dto == null)
                return Error.Validation("Balance update data is required");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Bank account with ID {id} not found");

            await _repository.UpdateBalanceAsync(id, dto.NewBalance, dto.AsOfDate);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> SetPrimaryAccountAsync(Guid companyId, Guid accountId)
        {
            var companyValidation = ServiceExtensions.ValidateGuid(companyId);
            if (companyValidation.IsFailure)
                return companyValidation.Error!;

            var accountValidation = ServiceExtensions.ValidateGuid(accountId);
            if (accountValidation.IsFailure)
                return accountValidation.Error!;

            var account = await _repository.GetByIdAsync(accountId);
            if (account == null)
                return Error.NotFound($"Bank account with ID {accountId} not found");

            if (account.CompanyId != companyId)
                return Error.Validation("Bank account does not belong to the specified company");

            await _repository.SetPrimaryAccountAsync(companyId, accountId);
            return Result.Success();
        }
    }
}
