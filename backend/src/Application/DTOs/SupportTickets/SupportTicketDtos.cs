namespace Application.DTOs.SupportTickets;

public class CreateSupportTicketDto
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "medium";
}

public class UpdateSupportTicketDto
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public Guid? AssignedTo { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class SupportTicketSummaryDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupportTicketDetailDto : SupportTicketSummaryDto
{
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<TicketMessageDto> Messages { get; set; } = new();
}

public class TicketMessageDto
{
    public Guid Id { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTicketMessageDto
{
    public string Message { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

// FAQ DTOs
public class CreateFaqDto
{
    public Guid? CompanyId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateFaqDto
{
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FaqItemDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
