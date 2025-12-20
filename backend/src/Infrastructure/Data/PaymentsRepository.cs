using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class PaymentsRepository : IPaymentsRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries (payments table)
        private static readonly string[] AllColumns = new[]
        {
            "id", "invoice_id", "company_id", "customer_id",
            "payment_date", "amount", "amount_in_inr", "currency",
            "payment_method", "reference_number", "notes", "description",
            "payment_type", "income_category",
            "tds_applicable", "tds_section", "tds_rate", "tds_amount", "gross_amount",
            "financial_year",
            "created_at", "updated_at"
        };

        // SQL for selecting payments with invoice_number from JOIN
        private const string SelectWithInvoiceNumber = @"
            SELECT p.*, i.invoice_number
            FROM payments p
            LEFT JOIN invoices i ON p.invoice_id = i.id";

        // Searchable columns for full-text search
        private static readonly string[] SearchableColumns = new[]
        {
            "payment_method", "reference_number", "notes", "description", "income_category"
        };

        public PaymentsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Payments?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Payments>(
                SelectWithInvoiceNumber + " WHERE p.id = @id",
                new { id });
        }

        public async Task<IEnumerable<Payments>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Payments>(
                SelectWithInvoiceNumber + " ORDER BY p.payment_date DESC");
        }

        public async Task<(IEnumerable<Payments> Items, int TotalCount)> GetPagedAsync(
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
                    conditions.Add($"p.{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchConditions = SearchableColumns.Select(col => $"p.{col} ILIKE @searchTerm");
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Validate and set sort column
            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "payment_date";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            // Calculate offset
            var offset = (pageNumber - 1) * pageSize;

            // Build data query with JOIN
            var dataSql = $@"
                SELECT p.*, i.invoice_number
                FROM payments p
                LEFT JOIN invoices i ON p.invoice_id = i.id
                {whereClause}
                ORDER BY p.{orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            // Build count query
            var countSql = $@"
                SELECT COUNT(*)
                FROM payments p
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Payments>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Payments> AddAsync(Payments entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO payments (
                    invoice_id, company_id, customer_id,
                    payment_date, amount, amount_in_inr, currency,
                    payment_method, reference_number, notes, description,
                    payment_type, income_category,
                    tds_applicable, tds_section, tds_rate, tds_amount, gross_amount,
                    financial_year,
                    created_at, updated_at
                )
                VALUES (
                    @InvoiceId, @CompanyId, @CustomerId,
                    @PaymentDate, @Amount, @AmountInInr, @Currency,
                    @PaymentMethod, @ReferenceNumber, @Notes, @Description,
                    @PaymentType, @IncomeCategory,
                    @TdsApplicable, @TdsSection, @TdsRate, @TdsAmount, @GrossAmount,
                    @FinancialYear,
                    NOW(), NOW()
                )
                RETURNING *";

            var createdEntity = await connection.QuerySingleAsync<Payments>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(Payments entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payments SET
                    invoice_id = @InvoiceId,
                    company_id = @CompanyId,
                    customer_id = @CustomerId,
                    payment_date = @PaymentDate,
                    amount = @Amount,
                    amount_in_inr = @AmountInInr,
                    currency = @Currency,
                    payment_method = @PaymentMethod,
                    reference_number = @ReferenceNumber,
                    notes = @Notes,
                    description = @Description,
                    payment_type = @PaymentType,
                    income_category = @IncomeCategory,
                    tds_applicable = @TdsApplicable,
                    tds_section = @TdsSection,
                    tds_rate = @TdsRate,
                    tds_amount = @TdsAmount,
                    gross_amount = @GrossAmount,
                    financial_year = @FinancialYear,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM payments WHERE id = @id",
                new { id });
        }

        /// <summary>
        /// Get payments by invoice ID
        /// </summary>
        public async Task<IEnumerable<Payments>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Payments>(
                SelectWithInvoiceNumber + " WHERE p.invoice_id = @invoiceId ORDER BY p.payment_date DESC",
                new { invoiceId });
        }

        /// <summary>
        /// Get payments by company ID
        /// </summary>
        public async Task<IEnumerable<Payments>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Payments>(
                SelectWithInvoiceNumber + " WHERE p.company_id = @companyId ORDER BY p.payment_date DESC",
                new { companyId });
        }

        /// <summary>
        /// Get payments by customer ID
        /// </summary>
        public async Task<IEnumerable<Payments>> GetByCustomerIdAsync(Guid customerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Payments>(
                SelectWithInvoiceNumber + " WHERE p.customer_id = @customerId ORDER BY p.payment_date DESC",
                new { customerId });
        }

        /// <summary>
        /// Get payments for a specific financial year
        /// </summary>
        public async Task<IEnumerable<Payments>> GetByFinancialYearAsync(string financialYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = SelectWithInvoiceNumber + " WHERE p.financial_year = @financialYear";
            if (companyId.HasValue)
            {
                sql += " AND p.company_id = @companyId";
            }
            sql += " ORDER BY p.payment_date DESC";

            return await connection.QueryAsync<Payments>(sql, new { financialYear, companyId });
        }

        /// <summary>
        /// Get income summary by company and period
        /// </summary>
        public async Task<(decimal TotalGross, decimal TotalTds, decimal TotalNet, decimal TotalInr)> GetIncomeSummaryAsync(
            Guid? companyId = null,
            string? financialYear = null,
            int? year = null,
            int? month = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (companyId.HasValue)
            {
                conditions.Add("company_id = @companyId");
                parameters.Add("companyId", companyId.Value);
            }

            if (!string.IsNullOrEmpty(financialYear))
            {
                conditions.Add("financial_year = @financialYear");
                parameters.Add("financialYear", financialYear);
            }

            if (year.HasValue)
            {
                conditions.Add("EXTRACT(YEAR FROM payment_date) = @year");
                parameters.Add("year", year.Value);
            }

            if (month.HasValue)
            {
                conditions.Add("EXTRACT(MONTH FROM payment_date) = @month");
                parameters.Add("month", month.Value);
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var sql = $@"
                SELECT
                    COALESCE(SUM(COALESCE(gross_amount, amount)), 0) as TotalGross,
                    COALESCE(SUM(COALESCE(tds_amount, 0)), 0) as TotalTds,
                    COALESCE(SUM(amount), 0) as TotalNet,
                    COALESCE(SUM(COALESCE(amount_in_inr, amount)), 0) as TotalInr
                FROM payments
                {whereClause}";

            var result = await connection.QueryFirstAsync<(decimal, decimal, decimal, decimal)>(sql, parameters);
            return result;
        }

        /// <summary>
        /// Get TDS summary for compliance reporting
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetTdsSummaryAsync(Guid? companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    c.name as customer_name,
                    c.tax_number as customer_pan,
                    p.tds_section,
                    COUNT(*) as payment_count,
                    SUM(COALESCE(p.gross_amount, p.amount)) as total_gross,
                    SUM(COALESCE(p.tds_amount, 0)) as total_tds,
                    SUM(p.amount) as total_net
                FROM payments p
                LEFT JOIN customers c ON p.customer_id = c.id
                WHERE p.tds_applicable = true
                    AND p.financial_year = @financialYear
                    AND (@companyId IS NULL OR p.company_id = @companyId)
                GROUP BY c.name, c.tax_number, p.tds_section
                ORDER BY total_tds DESC";

            return await connection.QueryAsync(sql, new { companyId, financialYear });
        }
    }
}
