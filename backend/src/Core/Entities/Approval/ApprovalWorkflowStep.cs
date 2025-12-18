namespace Core.Entities.Approval
{
    /// <summary>
    /// Represents a single step in an approval workflow template
    /// </summary>
    public class ApprovalWorkflowStep
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public int StepOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;  // 'direct_manager', 'skip_level_manager', 'role', 'specific_user', 'department_head'
        public string? ApproverRole { get; set; }  // if ApproverType='role': 'HR', 'Finance', 'Admin', etc.
        public Guid? ApproverUserId { get; set; }  // if ApproverType='specific_user'
        public bool IsRequired { get; set; } = true;
        public bool CanSkip { get; set; } = false;
        public int? AutoApproveAfterDays { get; set; }
        public string? ConditionsJson { get; set; }  // JSON conditions for conditional steps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ApprovalWorkflowTemplate? Template { get; set; }
    }

    /// <summary>
    /// Constants for approver types
    /// </summary>
    public static class ApproverTypes
    {
        public const string DirectManager = "direct_manager";
        public const string SkipLevelManager = "skip_level_manager";
        public const string Role = "role";
        public const string SpecificUser = "specific_user";
        public const string DepartmentHead = "department_head";

        public static readonly string[] All = new[]
        {
            DirectManager,
            SkipLevelManager,
            Role,
            SpecificUser,
            DepartmentHead
        };

        public static bool IsValid(string approverType) =>
            All.Contains(approverType, StringComparer.OrdinalIgnoreCase);
    }
}
