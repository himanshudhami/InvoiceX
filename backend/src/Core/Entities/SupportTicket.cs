namespace Core.Entities;

public class SupportTicket
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general"; // payroll, leave, it, hr, assets, general
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent
    public string Status { get; set; } = "open"; // open, in_progress, waiting_on_employee, resolved, closed
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties (for read)
    public string? EmployeeName { get; set; }
    public string? AssignedToName { get; set; }
}

public class SupportTicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderType { get; set; } = "employee"; // employee, admin
    public string Message { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public string? SenderName { get; set; }
}

public class FaqItem
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // NULL = global FAQ
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
