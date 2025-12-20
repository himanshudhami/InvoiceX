namespace Application.DTOs.Document
{
    /// <summary>
    /// DTO for document category response.
    /// </summary>
    public class DocumentCategoryDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresFinancialYear { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document category.
    /// </summary>
    public class CreateDocumentCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequiresFinancialYear { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO for updating a document category.
    /// </summary>
    public class UpdateDocumentCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresFinancialYear { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Simplified DTO for dropdowns and lists.
    /// </summary>
    public class DocumentCategorySelectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool RequiresFinancialYear { get; set; }
    }
}
