using System;

namespace Application.DTOs.Assets;

public class UpdateAssetMaintenanceDto
{
    public string? Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Vendor { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}
