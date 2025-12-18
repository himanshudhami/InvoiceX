namespace Application.DTOs.Announcements;

public class CreateAnnouncementDto
{
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "normal";
    public bool IsPinned { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class UpdateAnnouncementDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "normal";
    public bool IsPinned { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AnnouncementSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsRead { get; set; }
}

public class AnnouncementDetailDto : AnnouncementSummaryDto
{
    public DateTime? ExpiresAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
