using System;

namespace Core.Entities;

public class AssetDocuments
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Notes { get; set; }
}




