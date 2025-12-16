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
        /// <returns>Dashboard data with statistics and recent invoices</returns>
        Task<Result<DashboardDataDto>> GetDashboardDataAsync();
    }
}