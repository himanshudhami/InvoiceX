using Core.Entities.Leave;

namespace Core.Interfaces.Leave
{
    /// <summary>
    /// Repository interface for leave type operations
    /// </summary>
    public interface ILeaveTypeRepository
    {
        Task<LeaveType?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaveType>> GetAllByCompanyAsync(Guid companyId, bool activeOnly = true);
        Task<LeaveType?> GetByCodeAsync(Guid companyId, string code);
        Task<LeaveType> AddAsync(LeaveType entity);
        Task UpdateAsync(LeaveType entity);
        Task DeleteAsync(Guid id);
        Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null);
    }

    /// <summary>
    /// Repository interface for employee leave balance operations
    /// </summary>
    public interface IEmployeeLeaveBalanceRepository
    {
        Task<EmployeeLeaveBalance?> GetByIdAsync(Guid id);
        Task<EmployeeLeaveBalance?> GetByEmployeeTypeYearAsync(Guid employeeId, Guid leaveTypeId, string financialYear);
        Task<IEnumerable<EmployeeLeaveBalance>> GetByEmployeeAsync(Guid employeeId, string? financialYear = null);
        Task<IEnumerable<EmployeeLeaveBalance>> GetByCompanyAndYearAsync(Guid companyId, string financialYear);
        Task<EmployeeLeaveBalance> AddAsync(EmployeeLeaveBalance entity);
        Task UpdateAsync(EmployeeLeaveBalance entity);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Update the 'taken' field when leave is approved
        /// </summary>
        Task IncrementTakenAsync(Guid employeeId, Guid leaveTypeId, string financialYear, decimal days);

        /// <summary>
        /// Decrease the 'taken' field when leave is cancelled
        /// </summary>
        Task DecrementTakenAsync(Guid employeeId, Guid leaveTypeId, string financialYear, decimal days);

        /// <summary>
        /// Initialize leave balances for an employee for a financial year
        /// </summary>
        Task InitializeBalancesAsync(Guid employeeId, Guid companyId, string financialYear);

        /// <summary>
        /// Carry forward balances from previous year
        /// </summary>
        Task CarryForwardBalancesAsync(Guid employeeId, string fromYear, string toYear);
    }

    /// <summary>
    /// Repository interface for leave application operations
    /// </summary>
    public interface ILeaveApplicationRepository
    {
        Task<LeaveApplication?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaveApplication>> GetByEmployeeAsync(Guid employeeId, string? status = null);
        Task<IEnumerable<LeaveApplication>> GetByCompanyAsync(Guid companyId, string? status = null);
        Task<(IEnumerable<LeaveApplication> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Get pending applications for approval (for managers/HR)
        /// </summary>
        Task<IEnumerable<LeaveApplication>> GetPendingApprovalAsync(Guid companyId);

        /// <summary>
        /// Get applications overlapping with a date range
        /// </summary>
        Task<IEnumerable<LeaveApplication>> GetOverlappingAsync(
            Guid employeeId,
            DateTime fromDate,
            DateTime toDate,
            Guid? excludeId = null);

        /// <summary>
        /// Get approved leaves for a date range (for calendar view)
        /// </summary>
        Task<IEnumerable<LeaveApplication>> GetApprovedForDateRangeAsync(
            Guid companyId,
            DateTime fromDate,
            DateTime toDate);

        Task<LeaveApplication> AddAsync(LeaveApplication entity);
        Task UpdateAsync(LeaveApplication entity);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Update application status
        /// </summary>
        Task UpdateStatusAsync(Guid id, string status, Guid? approvedBy = null, string? reason = null);
    }

    /// <summary>
    /// Repository interface for holiday operations
    /// </summary>
    public interface IHolidayRepository
    {
        Task<Holiday?> GetByIdAsync(Guid id);
        Task<IEnumerable<Holiday>> GetByCompanyAndYearAsync(Guid companyId, int year);
        Task<IEnumerable<Holiday>> GetByCompanyAndDateRangeAsync(Guid companyId, DateTime fromDate, DateTime toDate);
        Task<Holiday?> GetByCompanyAndDateAsync(Guid companyId, DateTime date);
        Task<Holiday> AddAsync(Holiday entity);
        Task UpdateAsync(Holiday entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid companyId, DateTime date, Guid? excludeId = null);

        /// <summary>
        /// Get count of holidays between two dates (for leave calculation)
        /// </summary>
        Task<int> GetHolidayCountAsync(Guid companyId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Check if a specific date is a holiday
        /// </summary>
        Task<bool> IsHolidayAsync(Guid companyId, DateTime date);
    }
}
