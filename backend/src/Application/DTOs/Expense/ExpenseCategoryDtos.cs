namespace Application.DTOs.Expense
{
    /// <summary>
    /// DTO for expense category response.
    /// </summary>
    public class ExpenseCategoryDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; }
        public bool RequiresApproval { get; set; }
        public string? GlAccountCode { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new expense category.
    /// </summary>
    public class CreateExpenseCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; } = true;
        public bool RequiresApproval { get; set; } = true;
        public string? GlAccountCode { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO for updating an expense category.
    /// </summary>
    public class UpdateExpenseCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; }
        public bool RequiresApproval { get; set; }
        public string? GlAccountCode { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Simplified DTO for dropdowns.
    /// </summary>
    public class ExpenseCategorySelectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; }
    }
}
