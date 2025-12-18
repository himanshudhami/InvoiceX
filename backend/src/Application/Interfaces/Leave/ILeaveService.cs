using Application.DTOs.Leave;
using Core.Common;

namespace Application.Interfaces.Leave
{
    /// <summary>
    /// Service interface for leave management operations
    /// </summary>
    public interface ILeaveService
    {
        // ==================== Leave Types (Admin) ====================

        Task<Result<IEnumerable<LeaveTypeDto>>> GetLeaveTypesAsync(Guid companyId, bool activeOnly = true);
        Task<Result<LeaveTypeDto>> GetLeaveTypeByIdAsync(Guid id);
        Task<Result<LeaveTypeDto>> CreateLeaveTypeAsync(Guid companyId, CreateLeaveTypeDto dto, string? createdBy = null);
        Task<Result<LeaveTypeDto>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto, string? updatedBy = null);
        Task<Result> DeleteLeaveTypeAsync(Guid id);

        // ==================== Leave Balances ====================

        Task<Result<IEnumerable<LeaveBalanceDto>>> GetCompanyBalancesAsync(Guid companyId, string financialYear);
        Task<Result<IEnumerable<LeaveBalanceDto>>> GetEmployeeBalancesAsync(Guid employeeId, string? financialYear = null);
        Task<Result> InitializeEmployeeBalancesAsync(Guid employeeId, Guid companyId, string financialYear);
        Task<Result> AdjustBalanceAsync(Guid employeeId, AdjustLeaveBalanceDto dto);
        Task<Result> CarryForwardBalancesAsync(Guid employeeId, string fromYear, string toYear);

        // ==================== Leave Applications ====================

        /// <summary>
        /// Apply for leave (employee action)
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> ApplyLeaveAsync(Guid employeeId, Guid companyId, ApplyLeaveDto dto);

        /// <summary>
        /// Get leave application by ID
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> GetLeaveApplicationAsync(Guid applicationId);

        /// <summary>
        /// Get employee's leave applications
        /// </summary>
        Task<Result<IEnumerable<LeaveApplicationSummaryDto>>> GetEmployeeApplicationsAsync(Guid employeeId, string? status = null);

        /// <summary>
        /// Get pending applications for approval (for managers/HR)
        /// </summary>
        Task<Result<IEnumerable<LeaveApplicationSummaryDto>>> GetPendingApprovalsAsync(Guid companyId);

        /// <summary>
        /// Update leave application (only for pending status)
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> UpdateLeaveApplicationAsync(Guid employeeId, Guid applicationId, UpdateLeaveApplicationDto dto);

        /// <summary>
        /// Approve leave application (manager/HR action)
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> ApproveLeaveAsync(Guid applicationId, Guid approvedBy, ApproveLeaveDto dto);

        /// <summary>
        /// Reject leave application (manager/HR action)
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> RejectLeaveAsync(Guid applicationId, Guid rejectedBy, RejectLeaveDto dto);

        /// <summary>
        /// Cancel approved leave (manager/HR action)
        /// </summary>
        Task<Result<LeaveApplicationDetailDto>> CancelLeaveAsync(Guid applicationId, CancelLeaveDto dto);

        /// <summary>
        /// Withdraw pending leave application (employee action)
        /// </summary>
        Task<Result> WithdrawLeaveAsync(Guid employeeId, Guid applicationId, string? reason = null);

        /// <summary>
        /// Calculate leave days between two dates
        /// </summary>
        Task<Result<LeaveCalculationDto>> CalculateLeaveDaysAsync(Guid companyId, DateTime fromDate, DateTime toDate);

        // ==================== Holidays ====================

        Task<Result<IEnumerable<HolidayDto>>> GetHolidaysAsync(int year);
        Task<Result<IEnumerable<HolidayDto>>> GetHolidaysAsync(Guid companyId, int year);
        Task<Result<HolidayDto>> GetHolidayByIdAsync(Guid id);
        Task<Result<HolidayDto>> CreateHolidayAsync(Guid companyId, CreateHolidayDto dto);
        Task<Result<HolidayDto>> UpdateHolidayAsync(Guid id, UpdateHolidayDto dto);
        Task<Result> DeleteHolidayAsync(Guid id);

        // ==================== Portal ====================

        /// <summary>
        /// Get leave dashboard for employee portal
        /// </summary>
        Task<Result<LeaveDashboardDto>> GetEmployeeLeaveDashboardAsync(Guid employeeId, Guid companyId);

        /// <summary>
        /// Get leave calendar events for a date range
        /// </summary>
        Task<Result<IEnumerable<LeaveCalendarEventDto>>> GetCalendarEventsAsync(Guid companyId, DateTime fromDate, DateTime toDate);

        // ==================== Manager Portal ====================

        /// <summary>
        /// Get leave applications from the manager's team (direct reports)
        /// </summary>
        Task<Result<IEnumerable<TeamLeaveApplicationDto>>> GetTeamLeaveApplicationsAsync(Guid managerId, string? status = null);

        /// <summary>
        /// Update leave application status (called by approval workflow)
        /// </summary>
        Task<Result> UpdateLeaveStatusAsync(Guid applicationId, string status, string? reason = null);
    }
}
