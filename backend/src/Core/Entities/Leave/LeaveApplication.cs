namespace Core.Entities.Leave
{
    /// <summary>
    /// Leave application submitted by an employee
    /// </summary>
    public class LeaveApplication
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
