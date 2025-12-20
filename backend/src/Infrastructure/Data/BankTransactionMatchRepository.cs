using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    public class BankTransactionMatchRepository : IBankTransactionMatchRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries
        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "bank_transaction_id",
            "matched_type", "matched_id", "matched_amount",
            "matched_at", "matched_by", "match_method", "confidence_score",
            "notes", "created_at"
        };

        public BankTransactionMatchRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<BankTransactionMatch?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankTransactionMatch>(
                "SELECT * FROM bank_transaction_matches WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<BankTransactionMatch>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransactionMatch>(
                "SELECT * FROM bank_transaction_matches ORDER BY matched_at DESC");
        }

        public async Task<(IEnumerable<BankTransactionMatch> Items, int TotalCount)> GetPagedAsync(
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
                    conditions.Add($"btm.{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add("(btm.notes ILIKE @searchTerm OR btm.matched_type ILIKE @searchTerm)");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Validate and set sort column
            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "matched_at";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM bank_transaction_matches btm
                {whereClause}
                ORDER BY btm.{orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM bank_transaction_matches btm
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<BankTransactionMatch>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<BankTransactionMatch> AddAsync(BankTransactionMatch entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO bank_transaction_matches (
                    company_id, bank_transaction_id,
                    matched_type, matched_id, matched_amount,
                    matched_at, matched_by, match_method, confidence_score,
                    notes, created_at
                )
                VALUES (
                    @CompanyId, @BankTransactionId,
                    @MatchedType, @MatchedId, @MatchedAmount,
                    @MatchedAt, @MatchedBy, @MatchMethod, @ConfidenceScore,
                    @Notes, NOW()
                )
                RETURNING *";

            return await connection.QuerySingleAsync<BankTransactionMatch>(sql, entity);
        }

        public async Task UpdateAsync(BankTransactionMatch entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE bank_transaction_matches SET
                    company_id = @CompanyId,
                    bank_transaction_id = @BankTransactionId,
                    matched_type = @MatchedType,
                    matched_id = @MatchedId,
                    matched_amount = @MatchedAmount,
                    matched_at = @MatchedAt,
                    matched_by = @MatchedBy,
                    match_method = @MatchMethod,
                    confidence_score = @ConfidenceScore,
                    notes = @Notes
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_transaction_matches WHERE id = @id",
                new { id });
        }

        // ==================== Query by Related Entities ====================

        public async Task<IEnumerable<BankTransactionMatch>> GetByBankTransactionIdAsync(Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransactionMatch>(
                "SELECT * FROM bank_transaction_matches WHERE bank_transaction_id = @bankTransactionId ORDER BY matched_at DESC",
                new { bankTransactionId });
        }

        public async Task<IEnumerable<BankTransactionMatch>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransactionMatch>(
                "SELECT * FROM bank_transaction_matches WHERE company_id = @companyId ORDER BY matched_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<BankTransactionMatch>> GetByMatchedRecordAsync(string matchedType, Guid matchedId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransactionMatch>(
                @"SELECT * FROM bank_transaction_matches
                  WHERE matched_type = @matchedType AND matched_id = @matchedId
                  ORDER BY matched_at DESC",
                new { matchedType, matchedId });
        }

        // ==================== Match Summary ====================

        public async Task<decimal> GetTotalMatchedForTransactionAsync(Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(matched_amount), 0) FROM bank_transaction_matches WHERE bank_transaction_id = @bankTransactionId",
                new { bankTransactionId });
        }

        public async Task<decimal> GetUnmatchedAmountAsync(Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT ABS(bt.amount) - COALESCE(SUM(btm.matched_amount), 0)
                FROM bank_transactions bt
                LEFT JOIN bank_transaction_matches btm ON btm.bank_transaction_id = bt.id
                WHERE bt.id = @bankTransactionId
                GROUP BY bt.amount";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { bankTransactionId });
        }

        public async Task<bool> IsTransactionFullyMatchedAsync(Guid bankTransactionId)
        {
            var unmatched = await GetUnmatchedAmountAsync(bankTransactionId);
            return unmatched <= 0.01m; // Allow for minor rounding differences
        }

        // ==================== Reconciliation Helpers ====================

        public async Task<IEnumerable<dynamic>> GetUnreconciledTransactionsAsync(Guid bankAccountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT bt.*,
                       COALESCE(SUM(btm.matched_amount), 0) as matched_total,
                       ABS(bt.amount) - COALESCE(SUM(btm.matched_amount), 0) as unmatched_amount
                FROM bank_transactions bt
                LEFT JOIN bank_transaction_matches btm ON btm.bank_transaction_id = bt.id
                WHERE bt.bank_account_id = @bankAccountId
                  AND bt.is_reconciled = false
                GROUP BY bt.id
                ORDER BY bt.transaction_date DESC";
            return await connection.QueryAsync(sql, new { bankAccountId });
        }

        public async Task<IEnumerable<dynamic>> GetPotentialMatchesAsync(Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get the bank transaction details first
            var txSql = "SELECT * FROM bank_transactions WHERE id = @bankTransactionId";
            var tx = await connection.QueryFirstOrDefaultAsync<BankTransaction>(txSql, new { bankTransactionId });

            if (tx == null) return Enumerable.Empty<dynamic>();

            // For credit transactions (money in), look for unallocated payments
            if (tx.TransactionType?.ToLower() == "credit")
            {
                var sql = @"
                    SELECT
                        'payment' as match_type,
                        p.id as match_id,
                        p.amount,
                        p.payment_date,
                        p.reference_number,
                        p.payment_method,
                        c.name as customer_name,
                        i.invoice_number,
                        p.amount - COALESCE(SUM(btm.matched_amount), 0) as unmatched_amount,
                        CASE
                            WHEN p.reference_number = @refNumber THEN 100
                            WHEN ABS(p.amount - @amount) < 0.01 THEN 90
                            WHEN p.payment_date = @txDate THEN 80
                            ELSE 50
                        END as confidence_score
                    FROM payments p
                    LEFT JOIN bank_transaction_matches btm ON btm.matched_type = 'payment' AND btm.matched_id = p.id
                    LEFT JOIN customers c ON p.customer_id = c.id
                    LEFT JOIN invoices i ON p.invoice_id = i.id
                    WHERE p.bank_account_id IS NULL OR p.is_reconciled = false
                    GROUP BY p.id, c.name, i.invoice_number
                    HAVING p.amount - COALESCE(SUM(btm.matched_amount), 0) > 0
                    ORDER BY confidence_score DESC
                    LIMIT 10";

                return await connection.QueryAsync(sql, new
                {
                    refNumber = tx.ReferenceNumber,
                    amount = tx.Amount,
                    txDate = tx.TransactionDate
                });
            }

            // For debit transactions (money out), return empty for now
            // Can be extended to match expenses, contractor payments, etc.
            return Enumerable.Empty<dynamic>();
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<BankTransactionMatch>> AddBulkAsync(IEnumerable<BankTransactionMatch> matches)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var results = new List<BankTransactionMatch>();
                var sql = @"INSERT INTO bank_transaction_matches (
                        company_id, bank_transaction_id,
                        matched_type, matched_id, matched_amount,
                        matched_at, matched_by, match_method, confidence_score,
                        notes, created_at
                    )
                    VALUES (
                        @CompanyId, @BankTransactionId,
                        @MatchedType, @MatchedId, @MatchedAmount,
                        @MatchedAt, @MatchedBy, @MatchMethod, @ConfidenceScore,
                        @Notes, NOW()
                    )
                    RETURNING *";

                foreach (var match in matches)
                {
                    var created = await connection.QuerySingleAsync<BankTransactionMatch>(sql, match, transaction);
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

        public async Task DeleteByBankTransactionIdAsync(Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_transaction_matches WHERE bank_transaction_id = @bankTransactionId",
                new { bankTransactionId });
        }
    }
}
