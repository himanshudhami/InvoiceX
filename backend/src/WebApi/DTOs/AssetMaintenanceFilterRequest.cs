using System;
using System.Collections.Generic;

namespace WebApi.DTOs;

public class AssetMaintenanceFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CompanyId { get; set; }
    public Guid? AssetId { get; set; }
    public string? Status { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (AssetId.HasValue) filters["asset_id"] = AssetId.Value;
        if (!string.IsNullOrWhiteSpace(Status)) filters["status"] = Status!;
        return filters;
    }
}
