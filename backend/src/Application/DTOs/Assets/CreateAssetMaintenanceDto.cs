using System;

namespace Application.DTOs.Assets;

public class CreateAssetMaintenanceDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? OpenedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Vendor { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}
