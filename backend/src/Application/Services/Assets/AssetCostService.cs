using Application.DTOs.Assets;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using AssetsEntity = Core.Entities.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Assets;

public class AssetCostService
{
    private readonly IAssetsRepository _repository;

    public AssetCostService(IAssetsRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<AssetCostSummaryDto>> GetCostSummaryAsync(Guid assetId)
    {
        if (assetId == Guid.Empty) return Error.Validation("AssetId required");
        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null) return Error.NotFound("Asset not found");

        var maintenanceTotals = await _repository.GetMaintenanceTotalsAsync();
        var maintenanceCost = maintenanceTotals.TryGetValue(assetId, out var m) ? m : 0;
        var disposal = await _repository.GetDisposalByAssetAsync(assetId);

        var summary = ComputeCostSummary(asset, maintenanceCost, disposal);
        return Result<AssetCostSummaryDto>.Success(summary);
    }

    public async Task<Result<AssetCostReportDto>> GetCostReportAsync(Guid? companyId = null)
    {
        var assets = await _repository.GetAllAsync();
        var assetList = companyId.HasValue
            ? assets.Where(a => a.CompanyId == companyId.Value).ToList()
            : assets.ToList();

        var maintenanceTotals = await _repository.GetMaintenanceTotalsAsync();
        var summaries = new List<AssetCostSummaryDto>();
        foreach (var asset in assetList)
        {
            var maintenance = maintenanceTotals.TryGetValue(asset.Id, out var mc) ? mc : 0;
            var disposal = await _repository.GetDisposalByAssetAsync(asset.Id);
            summaries.Add(ComputeCostSummary(asset, maintenance, disposal));
        }

        var report = BuildCostReport(assetList, summaries);
        return Result<AssetCostReportDto>.Success(report);
    }

    private AssetCostReportDto BuildCostReport(IReadOnlyCollection<AssetsEntity> assets, IReadOnlyCollection<AssetCostSummaryDto> summaries)
    {
        var summaryLookup = summaries.ToDictionary(s => s.AssetId, s => s);

        var byCompany = assets
            .GroupBy(a => a.CompanyId)
            .Select(g =>
            {
                var rows = g.Select(a => summaryLookup[a.Id]);
                return new AssetCostReportRow
                {
                    CompanyId = g.Key,
                    AssetCount = g.Count(),
                    PurchaseCost = rows.Sum(x => x.PurchaseCost),
                    MaintenanceCost = rows.Sum(x => x.MaintenanceCost),
                    AccumulatedDepreciation = rows.Sum(x => x.AccumulatedDepreciation),
                    NetBookValue = rows.Sum(x => x.NetBookValue)
                };
            })
            .ToList();

        var byCategory = assets
            .GroupBy(a => a.CategoryId)
            .Select(g =>
            {
                var rows = g.Select(a => summaryLookup[a.Id]);
                return new AssetCostReportRow
                {
                    CategoryId = g.Key,
                    AssetCount = g.Count(),
                    PurchaseCost = rows.Sum(x => x.PurchaseCost),
                    MaintenanceCost = rows.Sum(x => x.MaintenanceCost),
                    AccumulatedDepreciation = rows.Sum(x => x.AccumulatedDepreciation),
                    NetBookValue = rows.Sum(x => x.NetBookValue)
                };
            })
            .ToList();

        var byPurchaseType = assets
            .GroupBy(a => (a.PurchaseType ?? "capex").ToLowerInvariant())
            .Select(g =>
            {
                var rows = g.Select(a => summaryLookup[a.Id]);
                return new AssetCostReportRow
                {
                    PurchaseType = g.Key,
                    AssetCount = g.Count(),
                    PurchaseCost = rows.Sum(x => x.PurchaseCost),
                    MaintenanceCost = rows.Sum(x => x.MaintenanceCost),
                    AccumulatedDepreciation = rows.Sum(x => x.AccumulatedDepreciation),
                    NetBookValue = rows.Sum(x => x.NetBookValue)
                };
            })
            .ToList();

        var totals = summaries.Any()
            ? new
            {
                Purchase = summaries.Sum(s => s.PurchaseCost),
                Maintenance = summaries.Sum(s => s.MaintenanceCost),
                Dep = summaries.Sum(s => s.AccumulatedDepreciation),
                NbV = summaries.Sum(s => s.NetBookValue),
                Capex = summaries.Where(s => s.PurchaseType.Equals("capex", StringComparison.OrdinalIgnoreCase)).Sum(s => s.PurchaseCost),
                Opex = summaries.Where(s => s.PurchaseType.Equals("opex", StringComparison.OrdinalIgnoreCase)).Sum(s => s.PurchaseCost),
                Proceeds = summaries.Sum(s => s.DisposalProceeds),
                DisposalCosts = summaries.Sum(s => s.DisposalCost),
                GainLoss = summaries.Sum(s => s.DisposalGainLoss),
                AverageAge = summaries.Average(s => (double)s.AgeMonths)
            }
            : new { Purchase = 0m, Maintenance = 0m, Dep = 0m, NbV = 0m, Capex = 0m, Opex = 0m, Proceeds = 0m, DisposalCosts = 0m, GainLoss = 0m, AverageAge = 0d };

        var aging = BuildAgingBuckets(summaries);
        var topMaintenance = BuildTopMaintenanceSpend(assets, summaries);

        return new AssetCostReportDto
        {
            TotalPurchaseCost = totals.Purchase,
            TotalMaintenanceCost = totals.Maintenance,
            TotalAccumulatedDepreciation = totals.Dep,
            TotalNetBookValue = totals.NbV,
            TotalCapexPurchase = totals.Capex,
            TotalOpexSpend = totals.Opex,
            TotalDisposalProceeds = totals.Proceeds,
            TotalDisposalCosts = totals.DisposalCosts,
            TotalDisposalGainLoss = totals.GainLoss,
            AverageAgeMonths = (decimal)totals.AverageAge,
            ByCompany = byCompany,
            ByCategory = byCategory,
            ByPurchaseType = byPurchaseType,
            AgingBuckets = aging,
            TopMaintenanceSpend = topMaintenance
        };
    }

    private List<AssetAgingBucketDto> BuildAgingBuckets(IEnumerable<AssetCostSummaryDto> summaries)
    {
        var buckets = new (int Min, int Max, string Label)[]
        {
            (0, 12, "0-12m"),
            (13, 24, "13-24m"),
            (25, 60, "25-60m"),
            (61, int.MaxValue, "61m+")
        };

        var list = new List<AssetAgingBucketDto>();
        foreach (var bucket in buckets)
        {
            var items = bucket.Max == int.MaxValue
                ? summaries.Where(s => s.AgeMonths >= bucket.Min).ToList()
                : summaries.Where(s => s.AgeMonths >= bucket.Min && s.AgeMonths <= bucket.Max).ToList();

            list.Add(new AssetAgingBucketDto
            {
                Label = bucket.Label,
                AssetCount = items.Count,
                PurchaseCost = items.Sum(s => s.PurchaseCost),
                NetBookValue = items.Sum(s => s.NetBookValue)
            });
        }

        return list;
    }

    private List<AssetMaintenanceSpendDto> BuildTopMaintenanceSpend(IEnumerable<AssetsEntity> assets, IEnumerable<AssetCostSummaryDto> summaries, int take = 5)
    {
        var lookup = summaries.ToDictionary(s => s.AssetId, s => s);
        return assets
            .Select(a => new { Asset = a, Summary = lookup.TryGetValue(a.Id, out var summary) ? summary : null })
            .Where(x => x.Summary != null && x.Summary.MaintenanceCost > 0)
            .OrderByDescending(x => x!.Summary!.MaintenanceCost)
            .Take(take)
            .Select(x => new AssetMaintenanceSpendDto
            {
                AssetId = x!.Asset.Id,
                CompanyId = x.Asset.CompanyId,
                AssetTag = x.Asset.AssetTag,
                AssetName = x.Asset.Name,
                Status = x.Asset.Status,
                MaintenanceCost = x.Summary!.MaintenanceCost
            })
            .ToList();
    }

    private AssetCostSummaryDto ComputeCostSummary(AssetsEntity asset, decimal maintenanceCost, AssetDisposals? disposal)
    {
        var purchaseCost = asset.PurchaseCost ?? 0;
        var salvage = asset.SalvageValue ?? 0;
        var purchaseType = (asset.PurchaseType ?? "capex").ToLowerInvariant();
        var depBase = Math.Max(0, purchaseCost - salvage);
        var life = asset.UsefulLifeMonths ?? 0;
        var startDate = asset.DepreciationStartDate ?? asset.InServiceDate ?? asset.PurchaseDate ?? asset.CreatedAt;
        var now = DateTime.UtcNow;
        var ageMonths = MonthsBetween(startDate, now);

        decimal accumulated = 0;
        decimal monthlyDepreciation = 0;

        // OPEX assets are expensed immediately, no depreciation
        if (purchaseType == "opex")
        {
            depBase = 0;
            accumulated = 0;
        }
        else
        {
            var method = (asset.DepreciationMethod ?? "none").ToLowerInvariant();
            if (method != "none" && life > 0)
            {
                var monthsElapsed = Math.Min(life, ageMonths);
                (accumulated, monthlyDepreciation) = CalculateDepreciation(method, depBase, life, monthsElapsed, purchaseCost, salvage);
            }
        }

        accumulated = Math.Min(Math.Max(0, accumulated), depBase);
        var netBookValue = purchaseType == "opex" ? 0 : Math.Max(salvage, purchaseCost - accumulated);

        var proceeds = disposal?.Proceeds ?? 0;
        var disposalCost = disposal?.DisposalCost ?? 0;
        var gainLoss = proceeds - disposalCost - netBookValue;

        return new AssetCostSummaryDto
        {
            AssetId = asset.Id,
            PurchaseType = purchaseType,
            Currency = asset.Currency,
            PurchaseCost = purchaseCost,
            MaintenanceCost = maintenanceCost,
            DepreciationBase = depBase,
            AccumulatedDepreciation = accumulated,
            MonthlyDepreciation = monthlyDepreciation,
            NetBookValue = netBookValue,
            SalvageValue = salvage,
            DepreciationMethod = asset.DepreciationMethod ?? "none",
            UsefulLifeMonths = asset.UsefulLifeMonths,
            DepreciationStartDate = startDate,
            AgeMonths = ageMonths,
            RemainingLifeMonths = life > ageMonths ? life - ageMonths : 0,
            DisposalProceeds = proceeds,
            DisposalCost = disposalCost,
            DisposalGainLoss = gainLoss
        };
    }

    private (decimal accumulated, decimal monthly) CalculateDepreciation(string method, decimal depBase, int life, int monthsElapsed, decimal purchaseCost, decimal salvage)
    {
        if (depBase <= 0 || life <= 0 || monthsElapsed <= 0) return (0, 0);

        return method switch
        {
            "straight_line" => StraightLine(depBase, life, monthsElapsed),
            "double_declining" => DoubleDeclining(purchaseCost, salvage, life, monthsElapsed),
            "sum_of_years_digits" => SumOfYearsDigits(depBase, life, monthsElapsed),
            _ => (0, 0)
        };
    }

    private static (decimal accumulated, decimal monthly) StraightLine(decimal depBase, int life, int monthsElapsed)
    {
        var monthly = depBase / life;
        var accumulated = Math.Min(depBase, monthly * monthsElapsed);
        return (accumulated, monthly);
    }

    private static (decimal accumulated, decimal monthly) DoubleDeclining(decimal cost, decimal salvage, int life, int monthsElapsed)
    {
        if (life <= 0) return (0, 0);

        var rate = 2m / life;
        decimal bookValue = cost;
        decimal lastPeriodDep = 0;

        for (var i = 0; i < monthsElapsed; i++)
        {
            var depreciation = Math.Min(bookValue - salvage, bookValue * rate);
            if (depreciation < 0) depreciation = 0;

            bookValue -= depreciation;
            lastPeriodDep = depreciation;

            if (bookValue <= salvage)
            {
                bookValue = salvage;
                break;
            }
        }

        var accumulated = cost - bookValue;
        return (Math.Max(0, accumulated), lastPeriodDep);
    }

    private static (decimal accumulated, decimal monthly) SumOfYearsDigits(decimal depBase, int life, int monthsElapsed)
    {
        if (life <= 0) return (0, 0);

        var denominator = life * (life + 1) / 2m;
        decimal accumulated = 0;
        decimal lastMonthDep = 0;

        for (var i = 0; i < Math.Min(monthsElapsed, life); i++)
        {
            var weight = life - i;
            var depreciation = depBase * weight / denominator;
            accumulated += depreciation;
            lastMonthDep = depreciation;
        }

        accumulated = Math.Min(depBase, accumulated);
        return (accumulated, lastMonthDep);
    }

    private static int MonthsBetween(DateTime start, DateTime end)
    {
        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (end.Day < start.Day) months -= 1;
        return Math.Max(0, months);
    }
}
