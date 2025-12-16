using System;

namespace Core.Entities;

public class AssetDepreciation
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
    public DateTime RunAt { get; set; }
    public string? Notes { get; set; }
}




