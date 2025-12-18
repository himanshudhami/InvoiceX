namespace Core.Entities.Approval
{
    /// <summary>
    /// Represents an active approval request instance for a submitted activity
    /// </summary>
    public class ApprovalRequest
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid TemplateId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public Guid ActivityId { get; set; }  // Reference to the actual entity being approved
        public Guid RequestorId { get; set; }
        public int CurrentStep { get; set; } = 1;
        public string Status { get; set; } = ApprovalRequestStatus.InProgress;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public ApprovalWorkflowTemplate? Template { get; set; }
        public Employees? Requestor { get; set; }
        public ICollection<ApprovalRequestStep>? Steps { get; set; }
    }

    /// <summary>
    /// Constants for approval request statuses
    /// </summary>
    public static class ApprovalRequestStatus
    {
        public const string InProgress = "in_progress";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Cancelled = "cancelled";

        public static readonly string[] All = new[]
        {
            InProgress,
            Approved,
            Rejected,
            Cancelled
        };

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);

        public static bool IsTerminal(string status) =>
            status == Approved || status == Rejected || status == Cancelled;
    }
}
