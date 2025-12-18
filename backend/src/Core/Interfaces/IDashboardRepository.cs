namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for dashboard-specific data operations
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Get dashboard statistics in a single query for optimal performance
        /// </summary>
        /// <param name="companyId">The company ID to filter by for multi-tenancy</param>
        /// <returns>Tuple containing (TotalRevenue, OutstandingAmount, ThisMonthAmount, OverdueAmount, OutstandingCount, ThisMonthCount, OverdueCount)</returns>
        Task<(decimal TotalRevenue, decimal OutstandingAmount, decimal ThisMonthAmount, decimal OverdueAmount, int OutstandingCount, int ThisMonthCount, int OverdueCount)> GetDashboardStatsAsync(Guid companyId);

        /// <summary>
        /// Get recent invoices with customer information
        /// </summary>
        /// <param name="companyId">The company ID to filter by for multi-tenancy</param>
        /// <param name="count">Number of recent invoices to retrieve</param>
        /// <returns>List of recent invoices with customer names</returns>
        Task<List<(Guid Id, string InvoiceNumber, string CustomerName, decimal TotalAmount, string Status, DateOnly InvoiceDate, DateOnly DueDate)>> GetRecentInvoicesAsync(Guid companyId, int count = 10);
    }
}
