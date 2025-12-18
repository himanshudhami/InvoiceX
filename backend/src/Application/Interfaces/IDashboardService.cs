using Application.DTOs.Dashboard;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for dashboard-related operations
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Get comprehensive dashboard data including statistics and recent invoices
        /// </summary>
        /// <param name="companyId">The company ID to filter by for multi-tenancy</param>
        /// <returns>Dashboard data with statistics and recent invoices</returns>
        Task<Result<DashboardDataDto>> GetDashboardDataAsync(Guid companyId);
    }
}
