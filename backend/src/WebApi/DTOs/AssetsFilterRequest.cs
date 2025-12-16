using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace WebApi.DTOs;

public class AssetsFilterRequest
{
    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;

    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; set; }

    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; set; }

    [FromQuery(Name = "sortDescending")]
    public bool SortDescending { get; set; }

    [FromQuery(Name = "companyId")]
    public Guid? CompanyId { get; set; }

    [FromQuery(Name = "assetType")]
    public string? AssetType { get; set; }

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "categoryId")]
    public Guid? CategoryId { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (!string.IsNullOrWhiteSpace(AssetType)) filters["asset_type"] = AssetType!;
        if (!string.IsNullOrWhiteSpace(Status)) filters["status"] = Status!;
        if (CategoryId.HasValue) filters["category_id"] = CategoryId.Value;
        return filters;
    }
}




