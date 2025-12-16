using System;

namespace Application.DTOs.Assets;

public class AssetCostSummaryDto
{
    public Guid AssetId { get; set; }
    public string PurchaseType { get; set; } = "capex";
    public string? Currency { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal DepreciationBase { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal MonthlyDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
    public decimal SalvageValue { get; set; }
    public string DepreciationMethod { get; set; } = "none";
    public int? UsefulLifeMonths { get; set; }
    public DateTime? DepreciationStartDate { get; set; }
    public int AgeMonths { get; set; }
    public int RemainingLifeMonths { get; set; }
    public decimal DisposalProceeds { get; set; }
    public decimal DisposalCost { get; set; }
    public decimal DisposalGainLoss { get; set; }
}
