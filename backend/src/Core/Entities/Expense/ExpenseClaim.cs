namespace Core.Entities.Expense
{
    /// <summary>
    /// Represents an employee expense reimbursement claim.
    /// </summary>
    public class ExpenseClaim
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = ExpenseClaimStatus.Draft;

        // Approval workflow
        public Guid? ApprovalRequestId { get; set; }

        // Submission
        public DateTime? SubmittedAt { get; set; }

        // Approval/Rejection
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public Guid? RejectedBy { get; set; }
        public string? RejectionReason { get; set; }

        // Reimbursement
        public DateTime? ReimbursedAt { get; set; }
        public string? ReimbursementReference { get; set; }
        public string? ReimbursementNotes { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties (populated by service layer)
        public string? EmployeeName { get; set; }
        public string? CategoryName { get; set; }
        public string? ApprovedByName { get; set; }
        public string? RejectedByName { get; set; }
    }

    /// <summary>
    /// Expense claim status constants.
    /// </summary>
    public static class ExpenseClaimStatus
    {
        public const string Draft = "draft";
        public const string Submitted = "submitted";
        public const string PendingApproval = "pending_approval";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Reimbursed = "reimbursed";
        public const string Cancelled = "cancelled";

        public static readonly string[] All = new[]
        {
            Draft, Submitted, PendingApproval, Approved, Rejected, Reimbursed, Cancelled
        };

        public static bool IsValid(string status) => All.Contains(status);
    }
}
