namespace Application.DTOs.Dashboard
{
    public class DashboardDataDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
    }
}