using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    /// <summary>
    /// Repository implementation for TDS Receivable operations
    /// </summary>
    public class TdsReceivableRepository : ITdsReceivableRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries
        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "financial_year", "quarter",
            "customer_id", "deductor_name", "deductor_tan", "deductor_pan",
            "payment_date", "tds_section", "gross_amount", "tds_rate", "tds_amount", "net_received",
            "certificate_number", "certificate_date", "certificate_downloaded",
            "payment_id", "invoice_id",
            "matched_with_26as", "form_26as_amount", "amount_difference", "matched_at",
            "status", "claimed_in_return", "notes",
            "created_at", "updated_at"
        };

        // Searchable columns for full-text search
        private static readonly string[] SearchableColumns = new[]
        {
            "deductor_name", "deductor_tan", "deductor_pan", "tds_section", "certificate_number"
        };

        public TdsReceivableRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TdsReceivable?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TdsReceivable>(
                "SELECT * FROM tds_receivable WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TdsReceivable>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TdsReceivable>(
                "SELECT * FROM tds_receivable ORDER BY payment_date DESC");
        }

        public async Task<(IEnumerable<TdsReceivable> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("tds_receivable", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "payment_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<TdsReceivable>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<TdsReceivable> AddAsync(TdsReceivable entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO tds_receivable (
                    company_id, financial_year, quarter,
                    customer_id, deductor_name, deductor_tan, deductor_pan,
                    payment_date, tds_section, gross_amount, tds_rate, tds_amount, net_received,
                    certificate_number, certificate_date, certificate_downloaded,
                    payment_id, invoice_id,
                    matched_with_26as, form_26as_amount, amount_difference, matched_at,
                    status, claimed_in_return, notes,
                    created_at, updated_at
                )
                VALUES (
                    @CompanyId, @FinancialYear, @Quarter,
                    @CustomerId, @DeductorName, @DeductorTan, @DeductorPan,
                    @PaymentDate, @TdsSection, @GrossAmount, @TdsRate, @TdsAmount, @NetReceived,
                    @CertificateNumber, @CertificateDate, @CertificateDownloaded,
                    @PaymentId, @InvoiceId,
                    @MatchedWith26As, @Form26AsAmount, @AmountDifference, @MatchedAt,
                    @Status, @ClaimedInReturn, @Notes,
                    NOW(), NOW()
                )
                RETURNING *";

            var createdEntity = await connection.QuerySingleAsync<TdsReceivable>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(TdsReceivable entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE tds_receivable SET
                    company_id = @CompanyId,
                    financial_year = @FinancialYear,
                    quarter = @Quarter,
                    customer_id = @CustomerId,
                    deductor_name = @DeductorName,
                    deductor_tan = @DeductorTan,
                    deductor_pan = @DeductorPan,
                    payment_date = @PaymentDate,
                    tds_section = @TdsSection,
                    gross_amount = @GrossAmount,
                    tds_rate = @TdsRate,
                    tds_amount = @TdsAmount,
                    net_received = @NetReceived,
                    certificate_number = @CertificateNumber,
                    certificate_date = @CertificateDate,
                    certificate_downloaded = @CertificateDownloaded,
                    payment_id = @PaymentId,
                    invoice_id = @InvoiceId,
                    matched_with_26as = @MatchedWith26As,
                    form_26as_amount = @Form26AsAmount,
                    amount_difference = @AmountDifference,
                    matched_at = @MatchedAt,
                    status = @Status,
                    claimed_in_return = @ClaimedInReturn,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tds_receivable WHERE id = @id",
                new { id });
        }

        // ==================== Specialized Queries ====================

        public async Task<IEnumerable<TdsReceivable>> GetByCompanyAndFYAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TdsReceivable>(
                @"SELECT * FROM tds_receivable
                  WHERE company_id = @companyId AND financial_year = @financialYear
                  ORDER BY payment_date DESC",
                new { companyId, financialYear });
        }

        public async Task<IEnumerable<TdsReceivable>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TdsReceivable>(
                @"SELECT * FROM tds_receivable
                  WHERE company_id = @companyId AND financial_year = @financialYear AND quarter = @quarter
                  ORDER BY payment_date DESC",
                new { companyId, financialYear, quarter });
        }

        public async Task<IEnumerable<TdsReceivable>> GetByCustomerAsync(Guid customerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TdsReceivable>(
                @"SELECT * FROM tds_receivable
                  WHERE customer_id = @customerId
                  ORDER BY payment_date DESC",
                new { customerId });
        }

        public async Task<IEnumerable<TdsReceivable>> GetUnmatchedAsync(Guid companyId, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM tds_receivable
                        WHERE company_id = @companyId AND matched_with_26as = false";
            if (!string.IsNullOrWhiteSpace(financialYear))
            {
                sql += " AND financial_year = @financialYear";
            }
            sql += " ORDER BY payment_date DESC";

            return await connection.QueryAsync<TdsReceivable>(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<TdsReceivable>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TdsReceivable>(
                @"SELECT * FROM tds_receivable
                  WHERE company_id = @companyId AND status = @status
                  ORDER BY payment_date DESC",
                new { companyId, status });
        }

        public async Task<TdsSummary> GetSummaryAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get overall summary
            var summaryQuery = @"
                SELECT
                    @financialYear as FinancialYear,
                    COALESCE(SUM(gross_amount), 0) as TotalGrossAmount,
                    COALESCE(SUM(tds_amount), 0) as TotalTdsAmount,
                    COALESCE(SUM(net_received), 0) as TotalNetReceived,
                    COUNT(*) as TotalEntries,
                    COUNT(*) FILTER (WHERE matched_with_26as = true) as MatchedEntries,
                    COUNT(*) FILTER (WHERE matched_with_26as = false) as UnmatchedEntries,
                    COALESCE(SUM(tds_amount) FILTER (WHERE matched_with_26as = true), 0) as MatchedAmount,
                    COALESCE(SUM(tds_amount) FILTER (WHERE matched_with_26as = false), 0) as UnmatchedAmount
                FROM tds_receivable
                WHERE company_id = @companyId AND financial_year = @financialYear";

            var summary = await connection.QuerySingleAsync<TdsSummary>(summaryQuery, new { companyId, financialYear });

            // Get quarterly summary
            var quarterlyQuery = @"
                SELECT
                    quarter as Quarter,
                    COALESCE(SUM(tds_amount), 0) as TdsAmount,
                    COUNT(*) as EntryCount
                FROM tds_receivable
                WHERE company_id = @companyId AND financial_year = @financialYear
                GROUP BY quarter
                ORDER BY quarter";

            var quarterly = await connection.QueryAsync<TdsQuarterlySummary>(quarterlyQuery, new { companyId, financialYear });
            summary.QuarterlySummary = quarterly.ToList();

            return summary;
        }

        public async Task MatchWith26AsAsync(Guid id, decimal form26AsAmount, decimal? difference)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tds_receivable SET
                    matched_with_26as = true,
                    form_26as_amount = @form26AsAmount,
                    amount_difference = @difference,
                    matched_at = NOW(),
                    status = CASE WHEN @difference IS NULL OR @difference = 0 THEN 'matched' ELSE 'disputed' END,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, form26AsAmount, difference });
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? claimedInReturn = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tds_receivable SET
                    status = @status,
                    claimed_in_return = COALESCE(@claimedInReturn, claimed_in_return),
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, status, claimedInReturn });
        }
    }
}
