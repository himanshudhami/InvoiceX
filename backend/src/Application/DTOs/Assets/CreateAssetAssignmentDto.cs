using System;

namespace Application.DTOs.Assets;

public class CreateAssetAssignmentDto
{
    public string TargetType { get; set; } = "company";
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? AssignedOn { get; set; }
    public string? ConditionOut { get; set; }
    public string? Notes { get; set; }
}




