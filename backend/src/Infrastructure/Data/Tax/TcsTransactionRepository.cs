using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Tax
{
    /// <summary>
    /// Repository implementation for TCS (Tax Collected at Source) transactions
    /// </summary>
    public class TcsTransactionRepository : ITcsTransactionRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "transaction_type", "section_code", "section_id",
            "transaction_date", "financial_year", "quarter",
            "party_type", "party_id", "party_name", "party_pan", "party_gstin",
            "transaction_value", "tcs_rate", "tcs_amount",
            "cumulative_value_fy", "threshold_amount",
            "invoice_id", "payment_id", "journal_entry_id",
            "status", "collected_at", "remitted_at", "challan_number", "bsr_code",
            "form_27eq_quarter", "form_27eq_filed", "form_27eq_acknowledgement",
            "notes", "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "party_name", "party_pan", "section_code", "challan_number"
        };

        public TcsTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TcsTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TcsTransaction>(
                "SELECT * FROM tcs_transactions WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TcsTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                "SELECT * FROM tcs_transactions ORDER BY transaction_date DESC");
        }

        public async Task<(IEnumerable<TcsTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("tcs_transactions", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "transaction_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<TcsTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<TcsTransaction> AddAsync(TcsTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO tcs_transactions (
                    id, company_id, transaction_type, section_code, section_id,
                    transaction_date, financial_year, quarter,
                    party_type, party_id, party_name, party_pan, party_gstin,
                    transaction_value, tcs_rate, tcs_amount,
                    cumulative_value_fy, threshold_amount,
                    invoice_id, payment_id, journal_entry_id,
                    status, collected_at, remitted_at, challan_number, bsr_code,
                    form_27eq_quarter, form_27eq_filed, form_27eq_acknowledgement,
                    notes, created_at, updated_at, created_by
                )
                VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CompanyId, @TransactionType, @SectionCode, @SectionId,
                    @TransactionDate, @FinancialYear, @Quarter,
                    @PartyType, @PartyId, @PartyName, @PartyPan, @PartyGstin,
                    @TransactionValue, @TcsRate, @TcsAmount,
                    @CumulativeValueFy, @ThresholdAmount,
                    @InvoiceId, @PaymentId, @JournalEntryId,
                    @Status, @CollectedAt, @RemittedAt, @ChallanNumber, @BsrCode,
                    @Form27EqQuarter, @Form27EqFiled, @Form27EqAcknowledgement,
                    @Notes, NOW(), NOW(), @CreatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<TcsTransaction>(sql, entity);
        }

        public async Task UpdateAsync(TcsTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE tcs_transactions SET
                    company_id = @CompanyId,
                    transaction_type = @TransactionType,
                    section_code = @SectionCode,
                    section_id = @SectionId,
                    transaction_date = @TransactionDate,
                    financial_year = @FinancialYear,
                    quarter = @Quarter,
                    party_type = @PartyType,
                    party_id = @PartyId,
                    party_name = @PartyName,
                    party_pan = @PartyPan,
                    party_gstin = @PartyGstin,
                    transaction_value = @TransactionValue,
                    tcs_rate = @TcsRate,
                    tcs_amount = @TcsAmount,
                    cumulative_value_fy = @CumulativeValueFy,
                    threshold_amount = @ThresholdAmount,
                    invoice_id = @InvoiceId,
                    payment_id = @PaymentId,
                    journal_entry_id = @JournalEntryId,
                    status = @Status,
                    collected_at = @CollectedAt,
                    remitted_at = @RemittedAt,
                    challan_number = @ChallanNumber,
                    bsr_code = @BsrCode,
                    form_27eq_quarter = @Form27EqQuarter,
                    form_27eq_filed = @Form27EqFiled,
                    form_27eq_acknowledgement = @Form27EqAcknowledgement,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tcs_transactions WHERE id = @id",
                new { id });
        }

        // ==================== Company & Period Queries ====================

        public async Task<IEnumerable<TcsTransaction>> GetByCompanyAsync(Guid companyId, string? quarter = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM tcs_transactions WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(quarter))
            {
                sql += " AND quarter = @quarter";
            }
            sql += " ORDER BY transaction_date DESC";

            return await connection.QueryAsync<TcsTransaction>(sql, new { companyId, quarter });
        }

        public async Task<IEnumerable<TcsTransaction>> GetByCompanyAndFYAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                @"SELECT * FROM tcs_transactions
                  WHERE company_id = @companyId AND financial_year = @financialYear
                  ORDER BY transaction_date DESC",
                new { companyId, financialYear });
        }

        public async Task<IEnumerable<TcsTransaction>> GetByCompanyFYQuarterAsync(Guid companyId, string financialYear, string quarter)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                @"SELECT * FROM tcs_transactions
                  WHERE company_id = @companyId AND financial_year = @financialYear AND quarter = @quarter
                  ORDER BY transaction_date DESC",
                new { companyId, financialYear, quarter });
        }

        // ==================== Transaction Type Queries ====================

        public async Task<IEnumerable<TcsTransaction>> GetCollectedAsync(Guid companyId, string? quarter = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM tcs_transactions WHERE company_id = @companyId AND transaction_type = 'collected'";
            if (!string.IsNullOrEmpty(quarter))
            {
                sql += " AND quarter = @quarter";
            }
            sql += " ORDER BY transaction_date DESC";

            return await connection.QueryAsync<TcsTransaction>(sql, new { companyId, quarter });
        }

        public async Task<IEnumerable<TcsTransaction>> GetPaidAsync(Guid companyId, string? quarter = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM tcs_transactions WHERE company_id = @companyId AND transaction_type = 'paid'";
            if (!string.IsNullOrEmpty(quarter))
            {
                sql += " AND quarter = @quarter";
            }
            sql += " ORDER BY transaction_date DESC";

            return await connection.QueryAsync<TcsTransaction>(sql, new { companyId, quarter });
        }

        // ==================== Status-based Queries ====================

        public async Task<IEnumerable<TcsTransaction>> GetPendingRemittanceAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                @"SELECT * FROM tcs_transactions
                  WHERE company_id = @companyId
                    AND transaction_type = 'collected'
                    AND status IN ('pending', 'collected')
                    AND remitted_at IS NULL
                  ORDER BY transaction_date ASC",
                new { companyId });
        }

        public async Task<IEnumerable<TcsTransaction>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                @"SELECT * FROM tcs_transactions
                  WHERE company_id = @companyId AND status = @status
                  ORDER BY transaction_date DESC",
                new { companyId, status });
        }

        // ==================== Party Queries ====================

        public async Task<IEnumerable<TcsTransaction>> GetByPartyAsync(Guid companyId, string partyPan)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TcsTransaction>(
                @"SELECT * FROM tcs_transactions
                  WHERE company_id = @companyId AND party_pan = @partyPan
                  ORDER BY transaction_date DESC",
                new { companyId, partyPan });
        }

        public async Task<decimal> GetCumulativePartyValueAsync(Guid companyId, string partyPan, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(transaction_value), 0)
                  FROM tcs_transactions
                  WHERE company_id = @companyId
                    AND party_pan = @partyPan
                    AND financial_year = @financialYear
                    AND transaction_type = 'collected'",
                new { companyId, partyPan, financialYear });
        }

        // ==================== Invoice/Payment Queries ====================

        public async Task<TcsTransaction?> GetByInvoiceAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TcsTransaction>(
                "SELECT * FROM tcs_transactions WHERE invoice_id = @invoiceId",
                new { invoiceId });
        }

        public async Task<TcsTransaction?> GetByPaymentAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TcsTransaction>(
                "SELECT * FROM tcs_transactions WHERE payment_id = @paymentId",
                new { paymentId });
        }

        // ==================== Aggregations ====================

        public async Task<decimal> GetTotalTcsCollectedAsync(Guid companyId, string quarter)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(tcs_amount), 0)
                  FROM tcs_transactions
                  WHERE company_id = @companyId
                    AND quarter = @quarter
                    AND transaction_type = 'collected'",
                new { companyId, quarter });
        }

        public async Task<decimal> GetTotalPendingRemittanceAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(tcs_amount), 0)
                  FROM tcs_transactions
                  WHERE company_id = @companyId
                    AND transaction_type = 'collected'
                    AND status IN ('pending', 'collected')
                    AND remitted_at IS NULL",
                new { companyId });
        }

        public async Task<TcsQuarterlySummary> GetQuarterlySummaryAsync(Guid companyId, string financialYear, string quarter)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var summaryQuery = @"
                SELECT
                    @financialYear as FinancialYear,
                    @quarter as Quarter,
                    COALESCE(SUM(transaction_value), 0) as TotalTransactionValue,
                    COALESCE(SUM(tcs_amount), 0) as TotalTcsAmount,
                    COALESCE(SUM(tcs_amount) FILTER (WHERE status IN ('collected', 'remitted', 'filed')), 0) as TcsCollected,
                    COALESCE(SUM(tcs_amount) FILTER (WHERE status IN ('remitted', 'filed')), 0) as TcsRemitted,
                    COALESCE(SUM(tcs_amount) FILTER (WHERE status NOT IN ('remitted', 'filed', 'cancelled')), 0) as TcsPending,
                    COUNT(*) as TotalTransactions
                FROM tcs_transactions
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND quarter = @quarter
                  AND transaction_type = 'collected'";

            var summary = await connection.QuerySingleAsync<TcsQuarterlySummary>(summaryQuery, new { companyId, financialYear, quarter });

            // Get section breakdown
            var sectionQuery = @"
                SELECT
                    section_code as SectionCode,
                    COALESCE(SUM(transaction_value), 0) as TransactionValue,
                    COALESCE(SUM(tcs_amount), 0) as TcsAmount,
                    COUNT(*) as TransactionCount
                FROM tcs_transactions
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND quarter = @quarter
                  AND transaction_type = 'collected'
                GROUP BY section_code";

            var sections = await connection.QueryAsync<TcsSectionSummary>(sectionQuery, new { companyId, financialYear, quarter });
            summary.SectionBreakdown = sections.ToList();

            return summary;
        }

        // ==================== Status Updates ====================

        public async Task MarkCollectedAsync(Guid id, Guid? journalEntryId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tcs_transactions SET
                    status = 'collected',
                    collected_at = NOW(),
                    journal_entry_id = COALESCE(@journalEntryId, journal_entry_id),
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, journalEntryId });
        }

        public async Task MarkRemittedAsync(Guid id, string challanNumber, string? bsrCode = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tcs_transactions SET
                    status = 'remitted',
                    remitted_at = NOW(),
                    challan_number = @challanNumber,
                    bsr_code = @bsrCode,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, challanNumber, bsrCode });
        }

        public async Task MarkForm27EqFiledAsync(Guid id, string acknowledgement)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tcs_transactions SET
                    status = 'filed',
                    form_27eq_filed = true,
                    form_27eq_acknowledgement = @acknowledgement,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, acknowledgement });
        }
    }
}
