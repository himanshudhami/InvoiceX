namespace Application.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal ThisMonthAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public int OutstandingCount { get; set; }
        public int ThisMonthCount { get; set; }
        public int OverdueCount { get; set; }
    }
}