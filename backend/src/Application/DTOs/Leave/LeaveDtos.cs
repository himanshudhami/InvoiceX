namespace Application.DTOs.Leave
{
    // ==================== Leave Type DTOs ====================

    /// <summary>
    /// Leave type for display
    /// </summary>
    public class LeaveTypeDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForwardAllowed { get; set; }
        public decimal MaxCarryForwardDays { get; set; }
        public bool EncashmentAllowed { get; set; }
        public decimal MaxEncashmentDays { get; set; }
        public bool RequiresApproval { get; set; }
        public int MinDaysNotice { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public bool IsActive { get; set; }
        public string ColorCode { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Create leave type request
    /// </summary>
    public class CreateLeaveTypeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForwardAllowed { get; set; }
        public decimal MaxCarryForwardDays { get; set; }
        public bool EncashmentAllowed { get; set; }
        public decimal MaxEncashmentDays { get; set; }
        public bool RequiresApproval { get; set; } = true;
        public int MinDaysNotice { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public string ColorCode { get; set; } = "#3B82F6";
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Update leave type request
    /// </summary>
    public class UpdateLeaveTypeDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public decimal? DaysPerYear { get; set; }
        public bool? CarryForwardAllowed { get; set; }
        public decimal? MaxCarryForwardDays { get; set; }
        public bool? EncashmentAllowed { get; set; }
        public decimal? MaxEncashmentDays { get; set; }
        public bool? RequiresApproval { get; set; }
        public int? MinDaysNotice { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public bool? IsActive { get; set; }
        public string? ColorCode { get; set; }
        public int? SortOrder { get; set; }
    }

    // ==================== Leave Balance DTOs ====================

    /// <summary>
    /// Employee leave balance summary
    /// </summary>
    public class LeaveBalanceDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string ColorCode { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal Accrued { get; set; }
        public decimal Taken { get; set; }
        public decimal CarryForwarded { get; set; }
        public decimal Adjusted { get; set; }
        public decimal Encashed { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal TotalCredited { get; set; }
    }

    /// <summary>
    /// Adjust leave balance request
    /// </summary>
    public class AdjustLeaveBalanceDto
    {
        public Guid LeaveTypeId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public decimal AdjustmentDays { get; set; }
        public string? Reason { get; set; }
    }

    // ==================== Leave Application DTOs ====================

    /// <summary>
    /// Leave application summary for list views
    /// </summary>
    public class LeaveApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public bool IsHalfDay { get; set; }
        public string? HalfDayType { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    /// <summary>
    /// Leave application detail
    /// </summary>
    public class LeaveApplicationDetailDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public bool IsHalfDay { get; set; }
        public string? HalfDayType { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? EmergencyContact { get; set; }
        public string? HandoverNotes { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool CanWithdraw { get; set; }

        /// <summary>
        /// The approval workflow request ID if using workflow-based approval
        /// </summary>
        public Guid? ApprovalRequestId { get; set; }

        /// <summary>
        /// Whether this leave application uses the approval workflow system
        /// </summary>
        public bool HasApprovalWorkflow { get; set; }

        /// <summary>
        /// Current step in the approval workflow (if applicable)
        /// </summary>
        public int? CurrentApprovalStep { get; set; }

        /// <summary>
        /// Total steps in the approval workflow (if applicable)
        /// </summary>
        public int? TotalApprovalSteps { get; set; }
    }

    /// <summary>
    /// Apply for leave request
    /// </summary>
    public class ApplyLeaveDto
    {
        public Guid LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsHalfDay { get; set; }
        public string? HalfDayType { get; set; }
        public string? Reason { get; set; }
        public string? EmergencyContact { get; set; }
        public string? HandoverNotes { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    /// <summary>
    /// Update leave application (only for pending status)
    /// </summary>
    public class UpdateLeaveApplicationDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsHalfDay { get; set; }
        public string? HalfDayType { get; set; }
        public string? Reason { get; set; }
        public string? EmergencyContact { get; set; }
        public string? HandoverNotes { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    /// <summary>
    /// Approve leave request
    /// </summary>
    public class ApproveLeaveDto
    {
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Reject leave request
    /// </summary>
    public class RejectLeaveDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cancel leave request
    /// </summary>
    public class CancelLeaveDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Leave calculation result
    /// </summary>
    public class LeaveCalculationDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public int WorkingDays { get; set; }
        public int WeekendDays { get; set; }
        public List<HolidayDto> Holidays { get; set; } = new();
    }

    // ==================== Holiday DTOs ====================

    /// <summary>
    /// Holiday for display
    /// </summary>
    public class HolidayDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public bool IsOptional { get; set; }
        public string? Description { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
    }

    /// <summary>
    /// Create holiday request
    /// </summary>
    public class CreateHolidayDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsOptional { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Update holiday request
    /// </summary>
    public class UpdateHolidayDto
    {
        public string? Name { get; set; }
        public DateTime? Date { get; set; }
        public bool? IsOptional { get; set; }
        public string? Description { get; set; }
    }

    // ==================== Portal DTOs ====================

    /// <summary>
    /// Leave dashboard for employee portal
    /// </summary>
    public class LeaveDashboardDto
    {
        public List<LeaveBalanceDto> Balances { get; set; } = new();
        public List<LeaveApplicationSummaryDto> UpcomingLeaves { get; set; } = new();
        public List<LeaveApplicationSummaryDto> PendingApplications { get; set; } = new();
        public List<HolidayDto> UpcomingHolidays { get; set; } = new();
    }

    /// <summary>
    /// Leave calendar event
    /// </summary>
    public class LeaveCalendarEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "leave" or "holiday"
        public string? EmployeeName { get; set; }
        public string? LeaveTypeCode { get; set; }
    }

    /// <summary>
    /// Team leave application for manager view
    /// </summary>
    public class TeamLeaveApplicationDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string LeaveTypeColor { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public bool IsHalfDay { get; set; }
        public string? HalfDayType { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        // Approval workflow details
        public Guid? ApprovalRequestId { get; set; }
        public bool HasApprovalWorkflow { get; set; }
        public bool IsCurrentApprover { get; set; }
    }
}
