using Application.Common;
using Application.Interfaces;
using Application.DTOs.TdsReceivable;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for TDS Receivable operations
    /// </summary>
    public class TdsReceivableService : ITdsReceivableService
    {
        private readonly ITdsReceivableRepository _repository;
        private readonly IMapper _mapper;

        public TdsReceivableService(
            ITdsReceivableRepository repository,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<TdsReceivable>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"TDS receivable with ID {id} not found");

            return Result<TdsReceivable>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<TdsReceivable>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<TdsReceivable> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<TdsReceivable> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<TdsReceivable>> CreateAsync(CreateTdsReceivableDto dto)
        {
            if (dto == null)
                return Error.Validation("TDS receivable data is required");

            if (dto.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(dto.FinancialYear))
                return Error.Validation("Financial year is required");

            if (string.IsNullOrWhiteSpace(dto.Quarter))
                return Error.Validation("Quarter is required");

            if (string.IsNullOrWhiteSpace(dto.DeductorName))
                return Error.Validation("Deductor name is required");

            if (string.IsNullOrWhiteSpace(dto.TdsSection))
                return Error.Validation("TDS section is required");

            // Validate quarter format
            var validQuarters = new[] { "Q1", "Q2", "Q3", "Q4" };
            if (!validQuarters.Contains(dto.Quarter.ToUpper()))
                return Error.Validation("Quarter must be Q1, Q2, Q3, or Q4");

            // Validate TDS amounts
            if (dto.GrossAmount <= 0)
                return Error.Validation("Gross amount must be positive");

            if (dto.TdsAmount < 0)
                return Error.Validation("TDS amount cannot be negative");

            if (dto.NetReceived < 0)
                return Error.Validation("Net received cannot be negative");

            // Validate that amounts are consistent
            var calculatedNet = dto.GrossAmount - dto.TdsAmount;
            if (Math.Abs(calculatedNet - dto.NetReceived) > 0.01m)
                return Error.Validation($"Net received ({dto.NetReceived}) should equal Gross ({dto.GrossAmount}) - TDS ({dto.TdsAmount}) = {calculatedNet}");

            // Map DTO to entity
            var entity = _mapper.Map<TdsReceivable>(dto);
            entity.Status = "pending";
            entity.MatchedWith26As = false;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);
            return Result<TdsReceivable>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateTdsReceivableDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            if (dto == null)
                return Error.Validation("TDS receivable data is required");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"TDS receivable with ID {id} not found");

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(dto.FinancialYear))
                existingEntity.FinancialYear = dto.FinancialYear;
            if (!string.IsNullOrWhiteSpace(dto.Quarter))
                existingEntity.Quarter = dto.Quarter;
            if (dto.CustomerId.HasValue)
                existingEntity.CustomerId = dto.CustomerId;
            if (!string.IsNullOrWhiteSpace(dto.DeductorName))
                existingEntity.DeductorName = dto.DeductorName;
            if (dto.DeductorTan != null)
                existingEntity.DeductorTan = dto.DeductorTan;
            if (dto.DeductorPan != null)
                existingEntity.DeductorPan = dto.DeductorPan;
            if (dto.PaymentDate.HasValue)
                existingEntity.PaymentDate = dto.PaymentDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.TdsSection))
                existingEntity.TdsSection = dto.TdsSection;
            if (dto.GrossAmount.HasValue)
                existingEntity.GrossAmount = dto.GrossAmount.Value;
            if (dto.TdsRate.HasValue)
                existingEntity.TdsRate = dto.TdsRate.Value;
            if (dto.TdsAmount.HasValue)
                existingEntity.TdsAmount = dto.TdsAmount.Value;
            if (dto.NetReceived.HasValue)
                existingEntity.NetReceived = dto.NetReceived.Value;
            if (dto.CertificateNumber != null)
                existingEntity.CertificateNumber = dto.CertificateNumber;
            if (dto.CertificateDate.HasValue)
                existingEntity.CertificateDate = dto.CertificateDate;
            if (dto.CertificateDownloaded.HasValue)
                existingEntity.CertificateDownloaded = dto.CertificateDownloaded.Value;
            if (dto.PaymentId.HasValue)
                existingEntity.PaymentId = dto.PaymentId;
            if (dto.InvoiceId.HasValue)
                existingEntity.InvoiceId = dto.InvoiceId;
            if (dto.Notes != null)
                existingEntity.Notes = dto.Notes;

            existingEntity.UpdatedAt = DateTime.UtcNow;

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
                return Error.NotFound($"TDS receivable with ID {id} not found");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Specialized Methods ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetByCompanyAndFYAsync(Guid companyId, string financialYear)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var entries = await _repository.GetByCompanyAndFYAsync(companyId, financialYear);
            return Result<IEnumerable<TdsReceivable>>.Success(entries);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            if (string.IsNullOrWhiteSpace(quarter))
                return Error.Validation("Quarter is required");

            var entries = await _repository.GetByCompanyFYQuarterAsync(companyId, financialYear, quarter);
            return Result<IEnumerable<TdsReceivable>>.Success(entries);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetByCustomerAsync(Guid customerId)
        {
            var validation = ServiceExtensions.ValidateGuid(customerId);
            if (validation.IsFailure)
                return validation.Error!;

            var entries = await _repository.GetByCustomerAsync(customerId);
            return Result<IEnumerable<TdsReceivable>>.Success(entries);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetUnmatchedAsync(Guid companyId, string? financialYear = null)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entries = await _repository.GetUnmatchedAsync(companyId, financialYear);
            return Result<IEnumerable<TdsReceivable>>.Success(entries);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsReceivable>>> GetByStatusAsync(Guid companyId, string status)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(status))
                return Error.Validation("Status is required");

            var entries = await _repository.GetByStatusAsync(companyId, status);
            return Result<IEnumerable<TdsReceivable>>.Success(entries);
        }

        /// <inheritdoc />
        public async Task<Result<TdsSummary>> GetSummaryAsync(Guid companyId, string financialYear)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var summary = await _repository.GetSummaryAsync(companyId, financialYear);
            return Result<TdsSummary>.Success(summary);
        }

        /// <inheritdoc />
        public async Task<Result> MatchWith26AsAsync(Guid id, Match26AsDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            if (dto == null)
                return Error.Validation("Match data is required");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"TDS receivable with ID {id} not found");

            // Calculate difference
            var difference = existingEntity.TdsAmount - dto.Form26AsAmount;

            await _repository.MatchWith26AsAsync(id, dto.Form26AsAmount, difference != 0 ? difference : null);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UpdateStatusAsync(Guid id, UpdateStatusDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            if (dto == null)
                return Error.Validation("Status data is required");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return Error.Validation("Status is required");

            var validStatuses = new[] { "pending", "matched", "claimed", "disputed", "written_off" };
            if (!validStatuses.Contains(dto.Status.ToLower()))
                return Error.Validation($"Status must be one of: {string.Join(", ", validStatuses)}");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"TDS receivable with ID {id} not found");

            await _repository.UpdateStatusAsync(id, dto.Status, dto.ClaimedInReturn);
            return Result.Success();
        }
    }
}
