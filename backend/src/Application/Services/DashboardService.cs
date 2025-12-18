using Application.DTOs.Dashboard;
using Application.Interfaces;
using Core.Common;
using Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Dashboard service implementation
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
        }

        public async Task<Result<DashboardDataDto>> GetDashboardDataAsync(Guid companyId)
        {
            try
            {
                // Validate company ID
                if (companyId == Guid.Empty)
                    return Error.Validation("Company ID is required");

                // Get statistics and recent invoices in parallel for better performance
                // Pass companyId for multi-tenancy filtering
                var statsTask = _dashboardRepository.GetDashboardStatsAsync(companyId);
                var recentInvoicesTask = _dashboardRepository.GetRecentInvoicesAsync(companyId, 10);

                await Task.WhenAll(statsTask, recentInvoicesTask);

                var stats = await statsTask;
                var recentInvoicesData = await recentInvoicesTask;

                // Build dashboard statistics
                var dashboardStats = new DashboardStatsDto
                {
                    TotalRevenue = stats.TotalRevenue,
                    OutstandingAmount = stats.OutstandingAmount,
                    ThisMonthAmount = stats.ThisMonthAmount,
                    OverdueAmount = stats.OverdueAmount,
                    OutstandingCount = stats.OutstandingCount,
                    ThisMonthCount = stats.ThisMonthCount,
                    OverdueCount = stats.OverdueCount
                };

                // Build recent invoices with overdue calculation
                var recentInvoices = recentInvoicesData.Select(invoice =>
                {
                    var daysOverdue = CalculateDaysOverdue(invoice.DueDate, invoice.Status);

                    return new RecentInvoiceDto
                    {
                        Id = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        CustomerName = invoice.CustomerName,
                        TotalAmount = invoice.TotalAmount,
                        Status = invoice.Status,
                        InvoiceDate = invoice.InvoiceDate,
                        DueDate = invoice.DueDate,
                        DaysOverdue = daysOverdue
                    };
                }).ToList();

                var dashboardData = new DashboardDataDto
                {
                    Stats = dashboardStats,
                    RecentInvoices = recentInvoices
                };

                return Result<DashboardDataDto>.Success(dashboardData);
            }
            catch (Exception)
            {
                return Error.Internal("Failed to retrieve dashboard data");
            }
        }

        private static int? CalculateDaysOverdue(DateOnly dueDate, string status)
        {
            // Only calculate overdue days if the invoice is not paid or cancelled
            if (status is "paid" or "cancelled")
                return null;

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dueDate < today)
            {
                return today.DayNumber - dueDate.DayNumber;
            }

            return null;
        }
    }
}
