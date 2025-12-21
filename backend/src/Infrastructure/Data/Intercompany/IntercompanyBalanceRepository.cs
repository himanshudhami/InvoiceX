using Core.Entities.Intercompany;
using Core.Interfaces.Intercompany;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Intercompany
{
    public class IntercompanyBalanceRepository : IIntercompanyBalanceRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "from_company_id", "to_company_id", "as_of_date", "financial_year",
            "balance_amount", "currency", "balance_in_inr", "opening_balance",
            "total_debits", "total_credits", "transaction_count", "last_transaction_date",
            "is_reconciled", "counterparty_balance", "difference",
            "created_at", "updated_at"
        };

        public IntercompanyBalanceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IntercompanyBalance?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<IntercompanyBalance>(
                "SELECT * FROM intercompany_balances WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<IntercompanyBalance>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<IntercompanyBalance>(
                "SELECT * FROM intercompany_balances ORDER BY as_of_date DESC");
        }

        public async Task<(IEnumerable<IntercompanyBalance> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("intercompany_balances", AllColumns)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "as_of_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<IntercompanyBalance>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<IntercompanyBalance> AddAsync(IntercompanyBalance entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO intercompany_balances (
                    from_company_id, to_company_id, as_of_date, financial_year,
                    balance_amount, currency, balance_in_inr, opening_balance,
                    total_debits, total_credits, transaction_count, last_transaction_date,
                    is_reconciled, counterparty_balance, difference,
                    created_at, updated_at
                ) VALUES (
                    @FromCompanyId, @ToCompanyId, @AsOfDate, @FinancialYear,
                    @BalanceAmount, @Currency, @BalanceInInr, @OpeningBalance,
                    @TotalDebits, @TotalCredits, @TransactionCount, @LastTransactionDate,
                    @IsReconciled, @CounterpartyBalance, @Difference,
                    NOW(), NOW()
                ) RETURNING *";
            return await connection.QuerySingleAsync<IntercompanyBalance>(sql, entity);
        }

        public async Task UpdateAsync(IntercompanyBalance entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE intercompany_balances SET
                    balance_amount = @BalanceAmount,
                    balance_in_inr = @BalanceInInr,
                    total_debits = @TotalDebits,
                    total_credits = @TotalCredits,
                    transaction_count = @TransactionCount,
                    last_transaction_date = @LastTransactionDate,
                    is_reconciled = @IsReconciled,
                    counterparty_balance = @CounterpartyBalance,
                    difference = @Difference,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM intercompany_balances WHERE id = @id", new { id });
        }

        public async Task<IntercompanyBalance?> GetBalanceAsync(Guid fromCompanyId, Guid toCompanyId, DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<IntercompanyBalance>(
                @"SELECT * FROM intercompany_balances
                  WHERE from_company_id = @fromCompanyId
                    AND to_company_id = @toCompanyId
                    AND as_of_date = @asOfDate",
                new { fromCompanyId, toCompanyId, asOfDate });
        }

        public async Task<IntercompanyBalance?> GetLatestBalanceAsync(Guid fromCompanyId, Guid toCompanyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<IntercompanyBalance>(
                @"SELECT * FROM intercompany_balances
                  WHERE from_company_id = @fromCompanyId
                    AND to_company_id = @toCompanyId
                  ORDER BY as_of_date DESC
                  LIMIT 1",
                new { fromCompanyId, toCompanyId });
        }

        public async Task<IEnumerable<IntercompanyBalance>> GetBalancesForCompanyAsync(Guid companyId, DateOnly? asOfDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            if (asOfDate.HasValue)
            {
                return await connection.QueryAsync<IntercompanyBalance>(
                    @"SELECT * FROM intercompany_balances
                      WHERE (from_company_id = @companyId OR to_company_id = @companyId)
                        AND as_of_date = @asOfDate
                      ORDER BY balance_amount DESC",
                    new { companyId, asOfDate });
            }
            else
            {
                // Get latest balance for each counterparty
                return await connection.QueryAsync<IntercompanyBalance>(
                    @"SELECT DISTINCT ON (from_company_id, to_company_id) *
                      FROM intercompany_balances
                      WHERE from_company_id = @companyId OR to_company_id = @companyId
                      ORDER BY from_company_id, to_company_id, as_of_date DESC",
                    new { companyId });
            }
        }

        public async Task<IntercompanyBalance> GetOrCreateBalanceAsync(
            Guid fromCompanyId, Guid toCompanyId, DateOnly asOfDate, string financialYear)
        {
            var existing = await GetBalanceAsync(fromCompanyId, toCompanyId, asOfDate);
            if (existing != null)
                return existing;

            // Get previous balance for opening balance
            var previousBalance = await GetLatestBalanceAsync(fromCompanyId, toCompanyId);

            var newBalance = new IntercompanyBalance
            {
                FromCompanyId = fromCompanyId,
                ToCompanyId = toCompanyId,
                AsOfDate = asOfDate,
                FinancialYear = financialYear,
                OpeningBalance = previousBalance?.BalanceAmount ?? 0,
                BalanceAmount = previousBalance?.BalanceAmount ?? 0,
                Currency = "INR"
            };

            return await AddAsync(newBalance);
        }

        public async Task UpdateBalanceAsync(
            Guid fromCompanyId, Guid toCompanyId, DateOnly transactionDate, decimal amount, bool isDebit)
        {
            // Get financial year
            var year = transactionDate.Month >= 4 ? transactionDate.Year : transactionDate.Year - 1;
            var financialYear = $"{year}-{(year + 1) % 100:D2}";

            var balance = await GetOrCreateBalanceAsync(fromCompanyId, toCompanyId, transactionDate, financialYear);

            if (isDebit)
            {
                balance.TotalDebits += amount;
                balance.BalanceAmount += amount; // Receivable increases
            }
            else
            {
                balance.TotalCredits += amount;
                balance.BalanceAmount -= amount; // Receivable decreases
            }

            balance.TransactionCount++;
            balance.LastTransactionDate = transactionDate;
            balance.BalanceInInr = balance.BalanceAmount; // Assuming INR for now

            await UpdateAsync(balance);
        }
    }
}
