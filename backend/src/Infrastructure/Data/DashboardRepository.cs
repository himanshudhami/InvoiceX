using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    /// <summary>
    /// Dashboard repository implementation using optimized queries
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<(decimal TotalRevenue, decimal OutstandingAmount, decimal ThisMonthAmount, decimal OverdueAmount, int OutstandingCount, int ThisMonthCount, int OverdueCount)> GetDashboardStatsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get revenue stats from payments table (actual income received)
            // Filter by company_id for multi-tenancy
            var paymentsSql = @"
                SELECT
                    -- Total Revenue: Sum of all payments received (using INR amount for accurate totals)
                    COALESCE(SUM(COALESCE(amount_in_inr, amount)), 0) as TotalRevenue,

                    -- This Month Income: Payments received this month
                    COALESCE(SUM(CASE WHEN DATE_TRUNC('month', payment_date::timestamp) = DATE_TRUNC('month', CURRENT_DATE::timestamp)
                        THEN COALESCE(amount_in_inr, amount) ELSE 0 END), 0) as ThisMonthAmount
                FROM payments
                WHERE company_id = @companyId OR company_id IS NULL";

            // Get outstanding/overdue stats from invoices table (amounts not yet received)
            // Filter by company_id for multi-tenancy
            var invoicesSql = @"
                SELECT
                    -- Outstanding Amount (sent, viewed, overdue - not draft, paid, or cancelled)
                    COALESCE(SUM(CASE WHEN status IN ('sent', 'viewed', 'overdue') THEN total_amount ELSE 0 END), 0) as OutstandingAmount,

                    -- Overdue Amount (past due date and not paid)
                    COALESCE(SUM(CASE WHEN due_date < CURRENT_DATE AND status != 'paid' AND status != 'cancelled' THEN total_amount ELSE 0 END), 0) as OverdueAmount,

                    -- Outstanding Count
                    COUNT(CASE WHEN status IN ('sent', 'viewed', 'overdue') THEN 1 END) as OutstandingCount,

                    -- This Month Count (invoices created this month)
                    COUNT(CASE WHEN DATE_TRUNC('month', invoice_date::timestamp) = DATE_TRUNC('month', CURRENT_DATE::timestamp) THEN 1 END) as ThisMonthCount,

                    -- Overdue Count
                    COUNT(CASE WHEN due_date < CURRENT_DATE AND status != 'paid' AND status != 'cancelled' THEN 1 END) as OverdueCount
                FROM invoices
                WHERE status != 'cancelled'
                AND company_id = @companyId";

            var paymentsResult = await connection.QueryFirstOrDefaultAsync<(decimal TotalRevenue, decimal ThisMonthAmount)>(paymentsSql, new { companyId });
            var invoicesResult = await connection.QueryFirstOrDefaultAsync<(decimal OutstandingAmount, decimal OverdueAmount, int OutstandingCount, int ThisMonthCount, int OverdueCount)>(invoicesSql, new { companyId });

            return (
                paymentsResult.TotalRevenue,
                invoicesResult.OutstandingAmount,
                paymentsResult.ThisMonthAmount,
                invoicesResult.OverdueAmount,
                invoicesResult.OutstandingCount,
                invoicesResult.ThisMonthCount,
                invoicesResult.OverdueCount
            );
        }

        public async Task<List<(Guid Id, string InvoiceNumber, string CustomerName, decimal TotalAmount, string Status, DateOnly InvoiceDate, DateOnly DueDate)>> GetRecentInvoicesAsync(Guid companyId, int count = 10)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Filter by company_id for multi-tenancy
            var sql = @"
                SELECT
                    i.id as Id,
                    i.invoice_number as InvoiceNumber,
                    COALESCE(p.display_name, p.name, 'Unknown Customer') as CustomerName,
                    i.total_amount as TotalAmount,
                    i.status as Status,
                    i.invoice_date as InvoiceDate,
                    i.due_date as DueDate
                FROM invoices i
                LEFT JOIN parties p ON i.party_id = p.id
                WHERE i.status != 'cancelled'
                AND i.company_id = @companyId
                ORDER BY i.created_at DESC
                LIMIT @count";

            var results = await connection.QueryAsync<(Guid, string, string, decimal, string, DateOnly, DateOnly)>(sql, new { companyId, count });
            return results.ToList();
        }
    }
}
