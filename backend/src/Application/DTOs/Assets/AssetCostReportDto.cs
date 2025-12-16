using System;
using System.Collections.Generic;

namespace Application.DTOs.Assets;

public class AssetCostReportRow
{
    public Guid? CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? PurchaseType { get; set; }
    public int AssetCount { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
}

public class AssetAgingBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal NetBookValue { get; set; }
}

public class AssetMaintenanceSpendDto
{
    public Guid AssetId { get; set; }
    public Guid CompanyId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Status { get; set; } = "available";
    public decimal MaintenanceCost { get; set; }
}

public class AssetCostReportDto
{
    public decimal TotalPurchaseCost { get; set; }
    public decimal TotalMaintenanceCost { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public decimal TotalNetBookValue { get; set; }
    public decimal TotalCapexPurchase { get; set; }
    public decimal TotalOpexSpend { get; set; }
    public decimal TotalDisposalProceeds { get; set; }
    public decimal TotalDisposalCosts { get; set; }
    public decimal TotalDisposalGainLoss { get; set; }
    public decimal AverageAgeMonths { get; set; }
    public IEnumerable<AssetCostReportRow> ByCompany { get; set; } = Array.Empty<AssetCostReportRow>();
    public IEnumerable<AssetCostReportRow> ByCategory { get; set; } = Array.Empty<AssetCostReportRow>();
    public IEnumerable<AssetCostReportRow> ByPurchaseType { get; set; } = Array.Empty<AssetCostReportRow>();
    public IEnumerable<AssetAgingBucketDto> AgingBuckets { get; set; } = Array.Empty<AssetAgingBucketDto>();
    public IEnumerable<AssetMaintenanceSpendDto> TopMaintenanceSpend { get; set; } = Array.Empty<AssetMaintenanceSpendDto>();
}
