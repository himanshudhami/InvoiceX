using Application.DTOs.Loans;
using Core.Common;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LoanEntity = Core.Entities.Loan;

namespace Application.Interfaces;

public interface ILoansService
{
    Task<Result<LoanEntity>> GetByIdAsync(Guid id);
    Task<Result<(IEnumerable<LoanEntity> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<Result<LoanEntity>> CreateAsync(CreateLoanDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateLoanDto dto);
    Task<Result> DeleteAsync(Guid id);
    Task<Result<LoanScheduleDto>> GetScheduleAsync(Guid loanId);
    Task<Result<LoanEntity>> RecordEmiPaymentAsync(Guid loanId, CreateEmiPaymentDto dto);
    Task<Result<LoanEntity>> RecordPrepaymentAsync(Guid loanId, PrepaymentDto dto);
    Task<Result<LoanEntity>> ForecloseLoanAsync(Guid loanId, string? notes = null);
    Task<Result<IEnumerable<LoanEntity>>> GetOutstandingLoansAsync(Guid? companyId = null);
    Task<Result<decimal>> GetTotalInterestPaidAsync(Guid loanId, DateTime? fromDate, DateTime? toDate);
    Task<Result<IEnumerable<LoanTransaction>>> GetInterestPaymentsAsync(Guid? companyId, DateTime? fromDate, DateTime? toDate);
}

