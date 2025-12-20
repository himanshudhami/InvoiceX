namespace Core.Entities.Expense
{
    /// <summary>
    /// Represents an expense category for reimbursement claims.
    /// Categories are company-specific and admin-defined.
    /// </summary>
    public class ExpenseCategory
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; } = true;
        public bool RequiresApproval { get; set; } = true;
        public string? GlAccountCode { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
