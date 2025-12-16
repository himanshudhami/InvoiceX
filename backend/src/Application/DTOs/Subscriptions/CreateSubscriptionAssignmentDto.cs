using System;

namespace Application.DTOs.Subscriptions;

public class CreateSubscriptionAssignmentDto
{
    public string TargetType { get; set; } = "company";
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? SeatIdentifier { get; set; }
    public string? Role { get; set; }
    public DateTime? AssignedOn { get; set; }
    public string? Notes { get; set; }
}




