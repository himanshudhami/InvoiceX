using Core.Abstractions;

namespace Core.Entities.Leave
{
    /// <summary>
    /// Leave application submitted by an employee
    /// Implements IApprovableActivity to participate in the generic approval workflow
    /// </summary>
    public class LeaveApplication : IApprovableActivity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public Guid CompanyId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public bool IsHalfDay { get; set; }

        /// <summary>
        /// 'first_half' or 'second_half' if IsHalfDay is true
        /// </summary>
        public string? HalfDayType { get; set; }

        public string? Reason { get; set; }

        /// <summary>
        /// Status: pending, approved, rejected, cancelled, withdrawn
        /// </summary>
        public string Status { get; set; } = "pending";

        public DateTime AppliedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? EmergencyContact { get; set; }
        public string? HandoverNotes { get; set; }
        public string? AttachmentUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Computed properties

        /// <summary>
        /// Whether the application can be edited (only in pending status)
        /// </summary>
        public bool CanEdit => Status == "pending";

        /// <summary>
        /// Whether the application can be cancelled
        /// </summary>
        public bool CanCancel => Status == "pending" || Status == "approved";

        /// <summary>
        /// Whether the application can be withdrawn (by employee)
        /// </summary>
        public bool CanWithdraw => Status == "pending";

        /// <summary>
        /// Whether the leave is upcoming
        /// </summary>
        public bool IsUpcoming => FromDate > DateTime.UtcNow.Date && (Status == "approved" || Status == "pending");

        /// <summary>
        /// Whether the leave is currently active
        /// </summary>
        public bool IsActive =>
            Status == "approved" &&
            FromDate <= DateTime.UtcNow.Date &&
            ToDate >= DateTime.UtcNow.Date;

        // Navigation properties
        public Employees? Employee { get; set; }
        public LeaveType? LeaveType { get; set; }
        public Employees? Approver { get; set; }

        // ==================== IApprovableActivity Implementation ====================

        /// <summary>
        /// Activity type identifier for the approval workflow
        /// </summary>
        public string ActivityType => ActivityTypes.Leave;

        /// <summary>
        /// The unique identifier for this leave application
        /// </summary>
        public Guid ActivityId => Id;

        /// <summary>
        /// The employee requesting the leave
        /// </summary>
        public Guid RequestorId => EmployeeId;

        /// <summary>
        /// Gets a display-friendly title for the leave request
        /// </summary>
        public string GetDisplayTitle()
        {
            var leaveTypeName = LeaveType?.Name ?? "Leave";
            var days = TotalDays == 1 ? "1 day" : $"{TotalDays} days";
            return $"{leaveTypeName} - {days} ({FromDate:MMM dd} - {ToDate:MMM dd})";
        }

        /// <summary>
        /// Called when the leave application is fully approved
        /// </summary>
        public Task OnApprovedAsync()
        {
            Status = LeaveStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the leave application is rejected
        /// </summary>
        public Task OnRejectedAsync(string reason, Guid rejectedBy)
        {
            Status = LeaveStatus.Rejected;
            RejectionReason = reason;
            ApprovedBy = rejectedBy; // Store who rejected it
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the leave application is cancelled
        /// </summary>
        public Task OnCancelledAsync()
        {
            Status = LeaveStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns context data for evaluating workflow step conditions
        /// </summary>
        public Dictionary<string, object> GetConditionContext()
        {
            return new Dictionary<string, object>
            {
                { "total_days", TotalDays },
                { "leave_type_id", LeaveTypeId },
                { "is_half_day", IsHalfDay },
                { "days_from_now", (FromDate.Date - DateTime.UtcNow.Date).Days }
            };
        }
    }

    /// <summary>
    /// Leave application status constants
    /// </summary>
    public static class LeaveStatus
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Cancelled = "cancelled";
        public const string Withdrawn = "withdrawn";
    }

    /// <summary>
    /// Half day type constants
    /// </summary>
    public static class HalfDayType
    {
        public const string FirstHalf = "first_half";
        public const string SecondHalf = "second_half";
    }
}
