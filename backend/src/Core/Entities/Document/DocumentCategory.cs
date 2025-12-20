namespace Core.Entities.Document
{
    /// <summary>
    /// Represents a document category for HR documents.
    /// Categories are company-specific and can be admin-defined or system-defined.
    /// </summary>
    public class DocumentCategory
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; } = true;
        public bool RequiresFinancialYear { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
