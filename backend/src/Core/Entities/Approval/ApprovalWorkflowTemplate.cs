namespace Core.Entities.Approval
{
    /// <summary>
    /// Represents a configurable approval workflow template for a company and activity type
    /// </summary>
    public class ApprovalWorkflowTemplate
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string ActivityType { get; set; } = string.Empty;  // 'leave', 'asset_request', 'expense', etc.
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ApprovalWorkflowStep>? Steps { get; set; }
    }
}
