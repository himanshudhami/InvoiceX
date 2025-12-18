namespace Core.Entities;

public class Announcement
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "general"; // general, hr, policy, event, celebration
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent
    public bool IsPinned { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AnnouncementRead
{
    public Guid Id { get; set; }
    public Guid AnnouncementId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime ReadAt { get; set; }
}
