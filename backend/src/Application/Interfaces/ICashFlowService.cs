using Application.DTOs.CashFlow;
using Core.Common;

namespace Application.Interfaces;

/// <summary>
/// Service interface for cash flow operations
/// </summary>
public interface ICashFlowService
{
    /// <summary>
    /// Get cash flow statement for a given period
    /// </summary>
    /// <param name="companyId">Optional company ID to filter by</param>
    /// <param name="year">Year for the cash flow statement</param>
    /// <param name="month">Optional month (1-12) to filter by specific month</param>
    /// <returns>Cash flow statement DTO</returns>
    Task<Result<CashFlowStatementDto>> GetCashFlowStatementAsync(Guid? companyId, int year, int? month = null);
}





