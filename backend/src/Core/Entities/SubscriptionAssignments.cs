using System;

namespace Core.Entities;

public class SubscriptionAssignments
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string TargetType { get; set; } = "company";
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? SeatIdentifier { get; set; }
    public string? Role { get; set; }
    public DateTime AssignedOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




