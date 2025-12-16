using System;

namespace Core.Entities;

public class AssetAssignments
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string TargetType { get; set; } = "company";
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime AssignedOn { get; set; }
    public DateTime? ReturnedOn { get; set; }
    public string? ConditionOut { get; set; }
    public string? ConditionIn { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




