using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace WebApi.DTOs;

public class LoansFilterRequest
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

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "loanType")]
    public string? LoanType { get; set; }

    [FromQuery(Name = "assetId")]
    public Guid? AssetId { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue)
            filters["company_id"] = CompanyId.Value;
        if (!string.IsNullOrWhiteSpace(Status))
            filters["status"] = Status;
        if (!string.IsNullOrWhiteSpace(LoanType))
            filters["loan_type"] = LoanType;
        if (AssetId.HasValue)
            filters["asset_id"] = AssetId.Value;
        return filters;
    }
}

