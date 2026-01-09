using System;

namespace Core.Entities;

public class AssetMaintenance
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "open";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Vendor { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public Guid? BankTransactionId { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



