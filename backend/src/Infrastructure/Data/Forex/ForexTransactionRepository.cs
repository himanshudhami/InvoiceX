using Core.Entities.Forex;
using Core.Interfaces.Forex;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Forex
{
    public class ForexTransactionRepository : IForexTransactionRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "transaction_date", "financial_year",
            "source_type", "source_id", "source_number",
            "currency", "foreign_amount", "exchange_rate", "inr_amount",
            "transaction_type", "forex_gain_loss", "gain_loss_type",
            "related_forex_id", "journal_entry_id", "is_posted",
            "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "source_number", "currency", "transaction_type"
        };

        public ForexTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ForexTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ForexTransaction>(
                "SELECT * FROM forex_transactions WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<ForexTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ForexTransaction>(
                "SELECT * FROM forex_transactions ORDER BY transaction_date DESC");
        }

        public async Task<(IEnumerable<ForexTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("forex_transactions", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "transaction_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<ForexTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<ForexTransaction> AddAsync(ForexTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO forex_transactions (
                    company_id, transaction_date, financial_year,
                    source_type, source_id, source_number,
                    currency, foreign_amount, exchange_rate, inr_amount,
                    transaction_type, forex_gain_loss, gain_loss_type,
                    related_forex_id, journal_entry_id, is_posted,
                    created_at, updated_at, created_by
                ) VALUES (
                    @CompanyId, @TransactionDate, @FinancialYear,
                    @SourceType, @SourceId, @SourceNumber,
                    @Currency, @ForeignAmount, @ExchangeRate, @InrAmount,
                    @TransactionType, @ForexGainLoss, @GainLossType,
                    @RelatedForexId, @JournalEntryId, @IsPosted,
                    NOW(), NOW(), @CreatedBy
                ) RETURNING *";
            return await connection.QuerySingleAsync<ForexTransaction>(sql, entity);
        }

        public async Task UpdateAsync(ForexTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE forex_transactions SET
                    company_id = @CompanyId, transaction_date = @TransactionDate,
                    financial_year = @FinancialYear, source_type = @SourceType,
                    source_id = @SourceId, source_number = @SourceNumber,
                    currency = @Currency, foreign_amount = @ForeignAmount,
                    exchange_rate = @ExchangeRate, inr_amount = @InrAmount,
                    transaction_type = @TransactionType, forex_gain_loss = @ForexGainLoss,
                    gain_loss_type = @GainLossType, related_forex_id = @RelatedForexId,
                    journal_entry_id = @JournalEntryId, is_posted = @IsPosted,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM forex_transactions WHERE id = @id", new { id });
        }

        public async Task<ForexTransaction?> GetBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ForexTransaction>(
                @"SELECT * FROM forex_transactions
                  WHERE source_type = @sourceType AND source_id = @sourceId
                  ORDER BY transaction_date DESC LIMIT 1",
                new { sourceType, sourceId });
        }

        public async Task<IEnumerable<ForexTransaction>> GetBySourceTypeAsync(Guid companyId, string sourceType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ForexTransaction>(
                @"SELECT * FROM forex_transactions
                  WHERE company_id = @companyId AND source_type = @sourceType
                  ORDER BY transaction_date DESC",
                new { companyId, sourceType });
        }

        public async Task<IEnumerable<ForexTransaction>> GetUnpostedAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM forex_transactions WHERE is_posted = false";
            if (companyId.HasValue)
                sql += " AND company_id = @companyId";
            sql += " ORDER BY transaction_date";
            return await connection.QueryAsync<ForexTransaction>(sql, new { companyId });
        }

        public async Task<IEnumerable<ForexTransaction>> GetBookingsForSettlementAsync(
            Guid companyId, string currency, decimal? amount = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM forex_transactions
                        WHERE company_id = @companyId AND currency = @currency
                          AND transaction_type = 'booking'
                          AND related_forex_id IS NULL";
            if (amount.HasValue)
                sql += " AND foreign_amount = @amount";
            sql += " ORDER BY transaction_date";
            return await connection.QueryAsync<ForexTransaction>(sql, new { companyId, currency, amount });
        }

        public async Task<IEnumerable<ForexTransaction>> GetOutstandingBookingsAsync(
            Guid companyId, string currency, DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ForexTransaction>(
                @"SELECT b.* FROM forex_transactions b
                  WHERE b.company_id = @companyId AND b.currency = @currency
                    AND b.transaction_type = 'booking'
                    AND b.transaction_date <= @asOfDate
                    AND NOT EXISTS (
                        SELECT 1 FROM forex_transactions s
                        WHERE s.related_forex_id = b.id AND s.transaction_type = 'settlement'
                    )
                  ORDER BY b.transaction_date",
                new { companyId, currency, asOfDate });
        }

        public async Task<(decimal RealizedGain, decimal UnrealizedGain)> GetGainLossSummaryAsync(
            Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<(decimal? Realized, decimal? Unrealized)>(
                @"SELECT
                    COALESCE(SUM(forex_gain_loss) FILTER (WHERE gain_loss_type = 'realized'), 0) as Realized,
                    COALESCE(SUM(forex_gain_loss) FILTER (WHERE gain_loss_type = 'unrealized'), 0) as Unrealized
                  FROM forex_transactions
                  WHERE company_id = @companyId AND financial_year = @financialYear",
                new { companyId, financialYear });
            return (result.Realized ?? 0, result.Unrealized ?? 0);
        }

        public async Task<IEnumerable<ForexTransaction>> BulkAddAsync(IEnumerable<ForexTransaction> transactions)
        {
            var results = new List<ForexTransaction>();
            foreach (var txn in transactions)
            {
                results.Add(await AddAsync(txn));
            }
            return results;
        }

        public async Task MarkAsPostedAsync(Guid id, Guid journalEntryId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE forex_transactions SET
                    journal_entry_id = @journalEntryId, is_posted = true, updated_at = NOW()
                  WHERE id = @id",
                new { id, journalEntryId });
        }
    }
}
