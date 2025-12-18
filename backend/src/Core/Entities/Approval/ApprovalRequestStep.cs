namespace Core.Entities.Approval
{
    /// <summary>
    /// Represents progress tracking for a single step in an approval request
    /// </summary>
    public class ApprovalRequestStep
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public Guid? AssignedToId { get; set; }  // Resolved approver
        public string Status { get; set; } = ApprovalStepStatus.Pending;
        public Guid? ActionById { get; set; }  // Who took action (may differ from AssignedToId if escalated)
        public DateTime? ActionAt { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ApprovalRequest? Request { get; set; }
        public Employees? AssignedTo { get; set; }
        public Employees? ActionBy { get; set; }
    }

    /// <summary>
    /// Constants for approval step statuses
    /// </summary>
    public static class ApprovalStepStatus
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Skipped = "skipped";
        public const string AutoApproved = "auto_approved";

        public static readonly string[] All = new[]
        {
            Pending,
            Approved,
            Rejected,
            Skipped,
            AutoApproved
        };

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);

        public static bool IsCompleted(string status) =>
            status != Pending;

        public static bool IsApproved(string status) =>
            status == Approved || status == AutoApproved || status == Skipped;
    }
}
