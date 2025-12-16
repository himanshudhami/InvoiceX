using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace WebApi.DTOs;

public class SubscriptionsFilterRequest
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

    [FromQuery(Name = "renewalPeriod")]
    public string? RenewalPeriod { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (!string.IsNullOrWhiteSpace(Status)) filters["status"] = Status!;
        if (!string.IsNullOrWhiteSpace(RenewalPeriod)) filters["renewal_period"] = RenewalPeriod!;
        return filters;
    }
}




