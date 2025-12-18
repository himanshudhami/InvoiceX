namespace Application.DTOs.Approval
{
    #region Template DTOs

    public class ApprovalWorkflowTemplateDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int StepCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ApprovalWorkflowTemplateDetailDto : ApprovalWorkflowTemplateDto
    {
        public List<ApprovalWorkflowStepDto> Steps { get; set; } = new();
    }

    public class CreateApprovalTemplateDto
    {
        public Guid CompanyId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateApprovalTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }

    #endregion

    #region Step DTOs

    public class ApprovalWorkflowStepDto
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public int StepOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public string? ApproverRole { get; set; }
        public Guid? ApproverUserId { get; set; }
        public string? ApproverUserName { get; set; }
        public bool IsRequired { get; set; }
        public bool CanSkip { get; set; }
        public int? AutoApproveAfterDays { get; set; }
        public string? ConditionsJson { get; set; }
    }

    public class CreateApprovalStepDto
    {
        public string Name { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public string? ApproverRole { get; set; }
        public Guid? ApproverUserId { get; set; }
        public bool IsRequired { get; set; } = true;
        public bool CanSkip { get; set; } = false;
        public int? AutoApproveAfterDays { get; set; }
        public string? ConditionsJson { get; set; }
    }

    public class UpdateApprovalStepDto
    {
        public string Name { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public string? ApproverRole { get; set; }
        public Guid? ApproverUserId { get; set; }
        public bool IsRequired { get; set; }
        public bool CanSkip { get; set; }
        public int? AutoApproveAfterDays { get; set; }
        public string? ConditionsJson { get; set; }
    }

    public class ReorderStepsDto
    {
        public List<Guid> StepIds { get; set; } = new();
    }

    #endregion

    #region Request DTOs

    public class ApprovalRequestDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public Guid RequestorId { get; set; }
        public string RequestorName { get; set; } = string.Empty;
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class ApprovalRequestDetailDto : ApprovalRequestDto
    {
        public List<ApprovalRequestStepDto> Steps { get; set; } = new();
    }

    public class ApprovalRequestStepDto
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ApproverType { get; set; } = string.Empty;
        public Guid? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? ActionById { get; set; }
        public string? ActionByName { get; set; }
        public DateTime? ActionAt { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Pending Approval DTOs

    public class PendingApprovalDto
    {
        public Guid RequestId { get; set; }
        public Guid StepId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public Guid RequestorId { get; set; }
        public string RequestorName { get; set; } = string.Empty;
        public string RequestorDepartment { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public int StepOrder { get; set; }
        public int TotalSteps { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    #endregion

    #region Action DTOs

    public class ApproveRequestDto
    {
        public string? Comments { get; set; }
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    #endregion
}
