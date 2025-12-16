using Application.Common;
using Application.DTOs.Loans;
using Application.Interfaces;
using Application.Services.Loans;
using Application.Validators.Loans;
using AutoMapper;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoanEntity = Core.Entities.Loan;

namespace Application.Services;

public class LoansService : ILoansService
{
    private readonly ILoansRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateLoanDto> _createValidator;
    private readonly IValidator<UpdateLoanDto> _updateValidator;
    private readonly IValidator<CreateEmiPaymentDto> _emiPaymentValidator;
    private readonly IValidator<PrepaymentDto> _prepaymentValidator;
    private readonly EmiCalculationService _emiCalculator;

    public LoansService(
        ILoansRepository repository,
        IMapper mapper,
        IValidator<CreateLoanDto> createValidator,
        IValidator<UpdateLoanDto> updateValidator,
        IValidator<CreateEmiPaymentDto> emiPaymentValidator,
        IValidator<PrepaymentDto> prepaymentValidator,
        EmiCalculationService emiCalculator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _emiPaymentValidator = emiPaymentValidator ?? throw new ArgumentNullException(nameof(emiPaymentValidator));
        _prepaymentValidator = prepaymentValidator ?? throw new ArgumentNullException(nameof(prepaymentValidator));
        _emiCalculator = emiCalculator ?? throw new ArgumentNullException(nameof(emiCalculator));
    }

    public async Task<Result<LoanEntity>> GetByIdAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Loan {id} not found");

        return Result<LoanEntity>.Success(entity);
    }

    public async Task<Result<(IEnumerable<LoanEntity> Items, int TotalCount)>> GetPagedAsync(
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

        var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
        return Result<(IEnumerable<LoanEntity> Items, int TotalCount)>.Success(result);
    }

    public async Task<Result<LoanEntity>> CreateAsync(CreateLoanDto dto)
    {
        var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        // Calculate EMI amount
        var emiAmount = _emiCalculator.CalculateEmi(dto.PrincipalAmount, dto.InterestRate, dto.TenureMonths);
        if (emiAmount <= 0)
            return Error.Validation("Invalid loan parameters. Unable to calculate EMI.");

        // Calculate loan end date if not provided
        var loanEndDate = dto.LoanEndDate ?? dto.LoanStartDate.AddMonths(dto.TenureMonths);

        var entity = _mapper.Map<LoanEntity>(dto);
        entity.EmiAmount = emiAmount;
        entity.OutstandingPrincipal = dto.PrincipalAmount;
        entity.LoanEndDate = loanEndDate;
        entity.Status = "active";
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(entity);

        // Generate EMI schedule
        var schedule = _emiCalculator.GenerateSchedule(
            dto.PrincipalAmount,
            dto.InterestRate,
            dto.TenureMonths,
            dto.LoanStartDate,
            emiAmount);

        foreach (var scheduleItemDto in schedule)
        {
            var scheduleItem = new LoanEmiSchedule
            {
                Id = Guid.NewGuid(),
                LoanId = created.Id,
                EmiNumber = scheduleItemDto.EmiNumber,
                DueDate = scheduleItemDto.DueDate,
                PrincipalAmount = scheduleItemDto.PrincipalAmount,
                InterestAmount = scheduleItemDto.InterestAmount,
                TotalEmi = scheduleItemDto.TotalEmi,
                OutstandingPrincipalAfter = scheduleItemDto.OutstandingPrincipalAfter,
                Status = scheduleItemDto.Status,
                PaidDate = scheduleItemDto.PaidDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.AddEmiScheduleItemAsync(scheduleItem);
        }

        // Record disbursement transaction
        var disbursement = new LoanTransaction
        {
            LoanId = created.Id,
            TransactionType = "disbursement",
            TransactionDate = dto.LoanStartDate,
            Amount = dto.PrincipalAmount,
            PrincipalAmount = dto.PrincipalAmount,
            InterestAmount = 0,
            Description = $"Loan disbursement for {dto.LoanName}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddTransactionAsync(disbursement);

        return Result<LoanEntity>.Success(created);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateLoanDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Loan not found");

        // If principal, interest rate, or tenure changed, recalculate EMI
        if (dto.PrincipalAmount.HasValue || dto.InterestRate.HasValue || dto.TenureMonths.HasValue)
        {
            var principal = dto.PrincipalAmount ?? existing.PrincipalAmount;
            var rate = dto.InterestRate ?? existing.InterestRate;
            var tenure = dto.TenureMonths ?? existing.TenureMonths;
            var newEmi = _emiCalculator.CalculateEmi(principal, rate, tenure);
            dto.EmiAmount = newEmi;
        }

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existing);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Loan not found");

        // Only allow deletion if loan is closed or foreclosed
        if (existing.Status != "closed" && existing.Status != "foreclosed")
            return Error.Validation("Cannot delete active loan. Please close or foreclose the loan first.");

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result<LoanScheduleDto>> GetScheduleAsync(Guid loanId)
    {
        var validation = ServiceExtensions.ValidateGuid(loanId);
        if (validation.IsFailure)
            return validation.Error!;

        var loan = await _repository.GetByIdAsync(loanId);
        if (loan == null)
            return Error.NotFound("Loan not found");

        var scheduleItems = await _repository.GetEmiScheduleAsync(loanId);
        var scheduleDtos = scheduleItems.Select(s => new LoanEmiScheduleItemDto
        {
            Id = s.Id,
            EmiNumber = s.EmiNumber,
            DueDate = s.DueDate,
            PrincipalAmount = s.PrincipalAmount,
            InterestAmount = s.InterestAmount,
            TotalEmi = s.TotalEmi,
            OutstandingPrincipalAfter = s.OutstandingPrincipalAfter,
            Status = s.Status,
            PaidDate = s.PaidDate
        }).ToList();

        var schedule = new LoanScheduleDto
        {
            LoanId = loan.Id,
            LoanName = loan.LoanName,
            PrincipalAmount = loan.PrincipalAmount,
            InterestRate = loan.InterestRate,
            TenureMonths = loan.TenureMonths,
            EmiAmount = loan.EmiAmount,
            ScheduleItems = scheduleDtos
        };

        return Result<LoanScheduleDto>.Success(schedule);
    }

    public async Task<Result<LoanEntity>> RecordEmiPaymentAsync(Guid loanId, CreateEmiPaymentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(loanId);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_emiPaymentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var loan = await _repository.GetByIdAsync(loanId);
        if (loan == null)
            return Error.NotFound("Loan not found");

        if (loan.Status != "active")
            return Error.Validation("Cannot record payment for non-active loan");

        // If EMI number specified, update that specific EMI
        if (dto.EmiNumber.HasValue)
        {
            var emiItem = await _repository.GetEmiScheduleItemAsync(loanId, dto.EmiNumber.Value);
            if (emiItem == null)
                return Error.NotFound($"EMI number {dto.EmiNumber.Value} not found");

            if (emiItem.Status == "paid")
                return Error.Validation($"EMI number {dto.EmiNumber.Value} is already paid");

            emiItem.Status = "paid";
            emiItem.PaidDate = dto.PaymentDate;
            emiItem.PrincipalAmount = dto.PrincipalAmount;
            emiItem.InterestAmount = dto.InterestAmount;
            emiItem.TotalEmi = dto.Amount;
            await _repository.UpdateEmiScheduleItemAsync(emiItem);
        }

        // Update outstanding principal
        loan.OutstandingPrincipal -= dto.PrincipalAmount;
        loan.OutstandingPrincipal = Math.Max(0, loan.OutstandingPrincipal);

        // If fully paid, close the loan
        if (loan.OutstandingPrincipal <= 0)
        {
            loan.Status = "closed";
            loan.LoanEndDate = dto.PaymentDate;
        }

        loan.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(loan);

        // Record transaction
        var transaction = new LoanTransaction
        {
            LoanId = loanId,
            TransactionType = "emi_payment",
            TransactionDate = dto.PaymentDate,
            Amount = dto.Amount,
            PrincipalAmount = dto.PrincipalAmount,
            InterestAmount = dto.InterestAmount,
            PaymentMethod = dto.PaymentMethod,
            BankAccountId = dto.BankAccountId,
            VoucherReference = dto.VoucherReference,
            Description = $"EMI payment for loan {loan.LoanName}",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddTransactionAsync(transaction);

        return Result<LoanEntity>.Success(loan);
    }

    public async Task<Result<LoanEntity>> RecordPrepaymentAsync(Guid loanId, PrepaymentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(loanId);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_prepaymentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var loan = await _repository.GetByIdAsync(loanId);
        if (loan == null)
            return Error.NotFound("Loan not found");

        if (loan.Status != "active")
            return Error.Validation("Cannot record prepayment for non-active loan");

        if (dto.Amount > loan.OutstandingPrincipal)
            return Error.Validation("Prepayment amount cannot exceed outstanding principal");

        // Update outstanding principal
        loan.OutstandingPrincipal -= dto.Amount;
        loan.OutstandingPrincipal = Math.Max(0, loan.OutstandingPrincipal);

        // If fully paid, close the loan
        if (loan.OutstandingPrincipal <= 0)
        {
            loan.Status = "closed";
            loan.LoanEndDate = dto.PrepaymentDate;
        }
        else if (dto.ReduceEmi)
        {
            // Recalculate EMI with reduced principal
            var newEmi = _emiCalculator.CalculateEmi(loan.OutstandingPrincipal, loan.InterestRate, loan.TenureMonths);
            loan.EmiAmount = newEmi;
        }

        loan.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(loan);

        // Record transaction
        var transaction = new LoanTransaction
        {
            LoanId = loanId,
            TransactionType = "prepayment",
            TransactionDate = dto.PrepaymentDate,
            Amount = dto.Amount,
            PrincipalAmount = dto.Amount,
            InterestAmount = 0,
            PaymentMethod = dto.PaymentMethod,
            BankAccountId = dto.BankAccountId,
            VoucherReference = dto.VoucherReference,
            Description = $"Prepayment for loan {loan.LoanName}",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddTransactionAsync(transaction);

        return Result<LoanEntity>.Success(loan);
    }

    public async Task<Result<LoanEntity>> ForecloseLoanAsync(Guid loanId, string? notes = null)
    {
        var idValidation = ServiceExtensions.ValidateGuid(loanId);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var loan = await _repository.GetByIdAsync(loanId);
        if (loan == null)
            return Error.NotFound("Loan not found");

        if (loan.Status != "active")
            return Error.Validation("Only active loans can be foreclosed");

        loan.Status = "foreclosed";
        loan.LoanEndDate = DateTime.UtcNow.Date;
        loan.OutstandingPrincipal = 0;
        loan.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(loan);

        // Record transaction
        var transaction = new LoanTransaction
        {
            LoanId = loanId,
            TransactionType = "foreclosure",
            TransactionDate = DateTime.UtcNow.Date,
            Amount = loan.OutstandingPrincipal,
            PrincipalAmount = loan.OutstandingPrincipal,
            InterestAmount = 0,
            Description = $"Loan foreclosure for {loan.LoanName}",
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddTransactionAsync(transaction);

        return Result<LoanEntity>.Success(loan);
    }

    public async Task<Result<IEnumerable<LoanEntity>>> GetOutstandingLoansAsync(Guid? companyId = null)
    {
        var allLoans = await _repository.GetAllAsync(companyId);
        var outstanding = allLoans.Where(l => l.Status == "active" && l.OutstandingPrincipal > 0);
        return Result<IEnumerable<LoanEntity>>.Success(outstanding);
    }

    public async Task<Result<decimal>> GetTotalInterestPaidAsync(Guid loanId, DateTime? fromDate, DateTime? toDate)
    {
        var idValidation = ServiceExtensions.ValidateGuid(loanId);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var loan = await _repository.GetByIdAsync(loanId);
        if (loan == null)
            return Error.NotFound("Loan not found");

        var totalInterest = await _repository.GetTotalInterestPaidAsync(loanId, fromDate, toDate);
        return Result<decimal>.Success(totalInterest);
    }

    public async Task<Result<IEnumerable<LoanTransaction>>> GetInterestPaymentsAsync(Guid? companyId, DateTime? fromDate, DateTime? toDate)
    {
        // Get all loans for the company
        var loans = await _repository.GetAllAsync(companyId);
        var loanIds = loans.Select(l => l.Id).ToList();

        if (!loanIds.Any())
            return Result<IEnumerable<LoanTransaction>>.Success(new List<LoanTransaction>());

        // Get interest payments for all loans
        var allInterestPayments = new List<LoanTransaction>();
        foreach (var loanId in loanIds)
        {
            var payments = await _repository.GetInterestPaymentsAsync(loanId, fromDate, toDate);
            allInterestPayments.AddRange(payments);
        }

        return Result<IEnumerable<LoanTransaction>>.Success(allInterestPayments);
    }
}

