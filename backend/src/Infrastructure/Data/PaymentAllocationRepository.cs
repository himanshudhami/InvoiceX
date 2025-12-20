using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    public class PaymentAllocationRepository : IPaymentAllocationRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries
        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "payment_id", "invoice_id",
            "allocated_amount", "currency", "amount_in_inr", "exchange_rate",
            "allocation_date", "allocation_type", "tds_allocated",
            "notes", "created_by", "created_at", "updated_at"
        };

        // Base SELECT query with JOIN to get payment and invoice details
        private const string SelectWithDetails = @"
            SELECT pa.*,
                   p.payment_date, p.amount as payment_amount,
                   i.invoice_number, i.total_amount as invoice_total
            FROM payment_allocations pa
            LEFT JOIN payments p ON pa.payment_id = p.id
            LEFT JOIN invoices i ON pa.invoice_id = i.id";

        // Searchable columns
        private static readonly string[] SearchableColumns = new[]
        {
            "allocation_type", "notes"
        };

        public PaymentAllocationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<PaymentAllocation?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PaymentAllocation>(
                "SELECT * FROM payment_allocations WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<PaymentAllocation>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PaymentAllocation>(
                "SELECT * FROM payment_allocations ORDER BY allocation_date DESC");
        }

        public async Task<(IEnumerable<PaymentAllocation> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            // Apply filters
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    var paramName = filter.Key.Replace(".", "_");
                    conditions.Add($"pa.{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchConditions = SearchableColumns.Select(col => $"pa.{col} ILIKE @searchTerm");
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Validate and set sort column
            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "allocation_date";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM payment_allocations pa
                {whereClause}
                ORDER BY pa.{orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM payment_allocations pa
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<PaymentAllocation>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<PaymentAllocation> AddAsync(PaymentAllocation entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO payment_allocations (
                    company_id, payment_id, invoice_id,
                    allocated_amount, currency, amount_in_inr, exchange_rate,
                    allocation_date, allocation_type, tds_allocated,
                    notes, created_by, created_at, updated_at
                )
                VALUES (
                    @CompanyId, @PaymentId, @InvoiceId,
                    @AllocatedAmount, @Currency, @AmountInInr, @ExchangeRate,
                    @AllocationDate, @AllocationType, @TdsAllocated,
                    @Notes, @CreatedBy, NOW(), NOW()
                )
                RETURNING *";

            return await connection.QuerySingleAsync<PaymentAllocation>(sql, entity);
        }

        public async Task UpdateAsync(PaymentAllocation entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payment_allocations SET
                    company_id = @CompanyId,
                    payment_id = @PaymentId,
                    invoice_id = @InvoiceId,
                    allocated_amount = @AllocatedAmount,
                    currency = @Currency,
                    amount_in_inr = @AmountInInr,
                    exchange_rate = @ExchangeRate,
                    allocation_date = @AllocationDate,
                    allocation_type = @AllocationType,
                    tds_allocated = @TdsAllocated,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM payment_allocations WHERE id = @id",
                new { id });
        }

        // ==================== Query by Related Entities ====================

        public async Task<IEnumerable<PaymentAllocation>> GetByPaymentIdAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PaymentAllocation>(
                "SELECT * FROM payment_allocations WHERE payment_id = @paymentId ORDER BY allocation_date DESC",
                new { paymentId });
        }

        public async Task<IEnumerable<PaymentAllocation>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PaymentAllocation>(
                "SELECT * FROM payment_allocations WHERE invoice_id = @invoiceId ORDER BY allocation_date DESC",
                new { invoiceId });
        }

        public async Task<IEnumerable<PaymentAllocation>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PaymentAllocation>(
                "SELECT * FROM payment_allocations WHERE company_id = @companyId ORDER BY allocation_date DESC",
                new { companyId });
        }

        // ==================== Allocation Summary ====================

        public async Task<decimal> GetTotalAllocatedForPaymentAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(allocated_amount), 0) FROM payment_allocations WHERE payment_id = @paymentId",
                new { paymentId });
        }

        public async Task<decimal> GetTotalAllocatedForInvoiceAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(allocated_amount), 0) FROM payment_allocations WHERE invoice_id = @invoiceId",
                new { invoiceId });
        }

        public async Task<decimal> GetUnallocatedAmountAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT p.amount - COALESCE(SUM(pa.allocated_amount), 0)
                FROM payments p
                LEFT JOIN payment_allocations pa ON pa.payment_id = p.id
                WHERE p.id = @paymentId
                GROUP BY p.amount";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { paymentId });
        }

        // ==================== Invoice Payment Status ====================

        public async Task<(decimal TotalPaid, decimal BalanceDue, string Status)> GetInvoicePaymentStatusAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT
                    COALESCE(SUM(pa.allocated_amount), 0) as TotalPaid,
                    i.total_amount - COALESCE(SUM(pa.allocated_amount), 0) as BalanceDue,
                    CASE
                        WHEN COALESCE(SUM(pa.allocated_amount), 0) = 0 THEN 'unpaid'
                        WHEN COALESCE(SUM(pa.allocated_amount), 0) >= i.total_amount THEN 'paid'
                        ELSE 'partial'
                    END as Status
                FROM invoices i
                LEFT JOIN payment_allocations pa ON pa.invoice_id = i.id
                WHERE i.id = @invoiceId
                GROUP BY i.id, i.total_amount";

            var result = await connection.QueryFirstOrDefaultAsync<(decimal, decimal, string)>(sql, new { invoiceId });
            return result;
        }

        public async Task<IEnumerable<dynamic>> GetInvoicePaymentSummaryAsync(Guid companyId, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT * FROM v_invoice_payment_status
                WHERE company_id = @companyId";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += @" AND invoice_id IN (
                    SELECT id FROM invoices
                    WHERE company_id = @companyId
                    AND EXTRACT(YEAR FROM issue_date) || '-' || (EXTRACT(YEAR FROM issue_date) + 1)::TEXT = @financialYear
                )";
            }

            sql += " ORDER BY balance_due DESC";

            return await connection.QueryAsync(sql, new { companyId, financialYear });
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<PaymentAllocation>> AddBulkAsync(IEnumerable<PaymentAllocation> allocations)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var results = new List<PaymentAllocation>();
                var sql = @"INSERT INTO payment_allocations (
                        company_id, payment_id, invoice_id,
                        allocated_amount, currency, amount_in_inr, exchange_rate,
                        allocation_date, allocation_type, tds_allocated,
                        notes, created_by, created_at, updated_at
                    )
                    VALUES (
                        @CompanyId, @PaymentId, @InvoiceId,
                        @AllocatedAmount, @Currency, @AmountInInr, @ExchangeRate,
                        @AllocationDate, @AllocationType, @TdsAllocated,
                        @Notes, @CreatedBy, NOW(), NOW()
                    )
                    RETURNING *";

                foreach (var allocation in allocations)
                {
                    var created = await connection.QuerySingleAsync<PaymentAllocation>(sql, allocation, transaction);
                    results.Add(created);
                }

                await transaction.CommitAsync();
                return results;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteByPaymentIdAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM payment_allocations WHERE payment_id = @paymentId",
                new { paymentId });
        }
    }
}
