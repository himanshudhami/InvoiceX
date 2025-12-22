namespace Core.Entities.Expense
{
    /// <summary>
    /// Attachment types for expense claims.
    /// </summary>
    public static class ExpenseAttachmentType
    {
        public const string EmployeeReceipt = "employee_receipt";
        public const string ReimbursementProof = "reimbursement_proof";
        public const string ApprovalNote = "approval_note";
    }

    /// <summary>
    /// Represents a receipt/invoice attachment for an expense claim.
    /// </summary>
    public class ExpenseAttachment
    {
        public Guid Id { get; set; }
        public Guid ExpenseId { get; set; }
        public Guid FileStorageId { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Type of attachment: employee_receipt, reimbursement_proof, approval_note
        /// </summary>
        public string AttachmentType { get; set; } = ExpenseAttachmentType.EmployeeReceipt;

        /// <summary>
        /// User who uploaded the attachment (employee or admin)
        /// </summary>
        public Guid? UploadedBy { get; set; }

        // Navigation properties (populated by service layer)
        public string? OriginalFilename { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public string? DownloadUrl { get; set; }
    }
}
