namespace Core.Entities.Expense
{
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

        // Navigation properties (populated by service layer)
        public string? OriginalFilename { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public string? DownloadUrl { get; set; }
    }
}
