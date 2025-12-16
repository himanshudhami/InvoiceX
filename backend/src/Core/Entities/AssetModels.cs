using System;

namespace Core.Entities;

public class AssetModels
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Manufacturer { get; set; }
    public string? ModelName { get; set; }
    public string? ModelNumber { get; set; }
    public string? Specs { get; set; } // stored as JSONB
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




