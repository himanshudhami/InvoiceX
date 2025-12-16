using System;

namespace Core.Entities;

public class AssetEvents
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; } // JSONB
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}




