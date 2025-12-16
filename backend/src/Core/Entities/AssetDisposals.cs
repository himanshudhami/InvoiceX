using System;

namespace Core.Entities;

public class AssetDisposals
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateTime DisposedOn { get; set; }
    public string Method { get; set; } = "retired";
    public decimal? Proceeds { get; set; }
    public decimal? DisposalCost { get; set; }
    public string? Currency { get; set; }
    public string? Buyer { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
