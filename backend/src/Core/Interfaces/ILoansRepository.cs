using Core.Entities;

namespace Core.Interfaces;

public interface ILoansRepository
{
    Task<Loan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Loan>> GetAllAsync(Guid? companyId = null);
    Task<(IEnumerable<Loan> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    Task<Loan> AddAsync(Loan entity);
    Task UpdateAsync(Loan entity);
    Task DeleteAsync(Guid id);

    Task<IEnumerable<LoanEmiSchedule>> GetEmiScheduleAsync(Guid loanId);
    Task<LoanEmiSchedule?> GetEmiScheduleItemAsync(Guid loanId, int emiNumber);
    Task<LoanEmiSchedule> AddEmiScheduleItemAsync(LoanEmiSchedule item);
    Task UpdateEmiScheduleItemAsync(LoanEmiSchedule item);
    Task<IEnumerable<LoanEmiSchedule>> GetPendingEmisAsync(Guid loanId, DateTime? upToDate = null);

    Task<IEnumerable<LoanTransaction>> GetTransactionsAsync(Guid loanId);
    Task<LoanTransaction> AddTransactionAsync(LoanTransaction transaction);
    Task<IEnumerable<LoanTransaction>> GetInterestPaymentsAsync(Guid? loanId, DateTime? fromDate, DateTime? toDate);
    Task<decimal> GetTotalInterestPaidAsync(Guid loanId, DateTime? fromDate, DateTime? toDate);
}




