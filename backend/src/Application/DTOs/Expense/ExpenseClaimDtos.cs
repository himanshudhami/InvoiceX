namespace Application.DTOs.Expense
{
    /// <summary>
    /// DTO for expense claim response.
    /// </summary>
    public class ExpenseClaimDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public string StatusDisplayName { get; set; } = string.Empty;

        // Workflow
        public Guid? ApprovalRequestId { get; set; }
        public DateTime? SubmittedAt { get; set; }

        // Approval
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? RejectedAt { get; set; }
        public Guid? RejectedBy { get; set; }
        public string? RejectedByName { get; set; }
        public string? RejectionReason { get; set; }

        // Reimbursement
        public DateTime? ReimbursedAt { get; set; }
        public string? ReimbursementReference { get; set; }
        public string? ReimbursementNotes { get; set; }

        // Attachments
        public IEnumerable<ExpenseAttachmentDto>? Attachments { get; set; }
        public int AttachmentCount { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new expense claim.
    /// </summary>
    public class CreateExpenseClaimDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }

    /// <summary>
    /// DTO for updating an expense claim (draft only).
    /// </summary>
    public class UpdateExpenseClaimDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }

    /// <summary>
    /// DTO for rejecting an expense claim.
    /// </summary>
    public class RejectExpenseClaimDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for marking an expense as reimbursed.
    /// </summary>
    public class ReimburseExpenseClaimDto
    {
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for expense attachment.
    /// </summary>
    public class ExpenseAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid ExpenseId { get; set; }
        public Guid FileStorageId { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; }
        public string? OriginalFilename { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for adding an attachment.
    /// </summary>
    public class AddExpenseAttachmentDto
    {
        public Guid FileStorageId { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// DTO for expense summary.
    /// </summary>
    public class ExpenseSummaryDto
    {
        public int TotalClaims { get; set; }
        public int DraftClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int ReimbursedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal ReimbursedAmount { get; set; }
        public Dictionary<string, decimal> AmountByCategory { get; set; } = new();
    }

    /// <summary>
    /// Filter request for expense claims.
    /// </summary>
    public class ExpenseClaimFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Status display mapping.
    /// </summary>
    public static class ExpenseStatusDisplay
    {
        private static readonly Dictionary<string, string> DisplayNames = new()
        {
            { "draft", "Draft" },
            { "submitted", "Submitted" },
            { "pending_approval", "Pending Approval" },
            { "approved", "Approved" },
            { "rejected", "Rejected" },
            { "reimbursed", "Reimbursed" },
            { "cancelled", "Cancelled" }
        };

        public static string GetDisplayName(string status)
        {
            return DisplayNames.TryGetValue(status, out var name) ? name : status;
        }
    }
}
