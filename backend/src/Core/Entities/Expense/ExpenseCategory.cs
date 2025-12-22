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

        // GST settings
        public bool IsGstApplicable { get; set; } = true;
        public decimal DefaultGstRate { get; set; } = 18;
        public string? DefaultHsnSac { get; set; }
        public bool? ItcEligible { get; set; } = true; // Some categories have blocked ITC

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
