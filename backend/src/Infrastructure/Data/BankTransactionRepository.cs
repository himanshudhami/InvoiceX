using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class BankTransactionRepository : IBankTransactionRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries
        private static readonly string[] AllColumns = new[]
        {
            "id", "bank_account_id", "transaction_date", "value_date",
            "description", "reference_number", "cheque_number",
            "transaction_type", "amount", "balance_after", "category",
            "is_reconciled", "reconciled_type", "reconciled_id",
            "reconciled_at", "reconciled_by",
            "import_source", "import_batch_id", "raw_data", "transaction_hash",
            "created_at", "updated_at"
        };

        // Searchable columns for full-text search
        private static readonly string[] SearchableColumns = new[]
        {
            "description", "reference_number", "cheque_number", "category"
        };

        public BankTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<BankTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankTransaction>(
                "SELECT * FROM bank_transactions WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<BankTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                "SELECT * FROM bank_transactions ORDER BY transaction_date DESC, created_at DESC");
        }

        public async Task<(IEnumerable<BankTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("bank_transactions", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "transaction_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<BankTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<BankTransaction> AddAsync(BankTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO bank_transactions (
                    bank_account_id, transaction_date, value_date,
                    description, reference_number, cheque_number,
                    transaction_type, amount, balance_after, category,
                    is_reconciled, reconciled_type, reconciled_id,
                    reconciled_at, reconciled_by,
                    import_source, import_batch_id, raw_data, transaction_hash,
                    created_at, updated_at
                )
                VALUES (
                    @BankAccountId, @TransactionDate, @ValueDate,
                    @Description, @ReferenceNumber, @ChequeNumber,
                    @TransactionType, @Amount, @BalanceAfter, @Category,
                    @IsReconciled, @ReconciledType, @ReconciledId,
                    @ReconciledAt, @ReconciledBy,
                    @ImportSource, @ImportBatchId, @RawData::jsonb, @TransactionHash,
                    NOW(), NOW()
                )
                RETURNING *";

            var createdEntity = await connection.QuerySingleAsync<BankTransaction>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(BankTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE bank_transactions SET
                    bank_account_id = @BankAccountId,
                    transaction_date = @TransactionDate,
                    value_date = @ValueDate,
                    description = @Description,
                    reference_number = @ReferenceNumber,
                    cheque_number = @ChequeNumber,
                    transaction_type = @TransactionType,
                    amount = @Amount,
                    balance_after = @BalanceAfter,
                    category = @Category,
                    is_reconciled = @IsReconciled,
                    reconciled_type = @ReconciledType,
                    reconciled_id = @ReconciledId,
                    reconciled_at = @ReconciledAt,
                    reconciled_by = @ReconciledBy,
                    import_source = @ImportSource,
                    import_batch_id = @ImportBatchId,
                    raw_data = @RawData::jsonb,
                    transaction_hash = @TransactionHash,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_transactions WHERE id = @id",
                new { id });
        }

        /// <summary>
        /// Get transactions for a specific bank account
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetByBankAccountIdAsync(Guid bankAccountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                @"SELECT * FROM bank_transactions
                  WHERE bank_account_id = @bankAccountId
                  ORDER BY transaction_date DESC, created_at DESC",
                new { bankAccountId });
        }

        /// <summary>
        /// Get transactions within a date range for a specific bank account
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetByDateRangeAsync(
            Guid bankAccountId, DateOnly fromDate, DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                @"SELECT * FROM bank_transactions
                  WHERE bank_account_id = @bankAccountId
                    AND transaction_date >= @fromDate
                    AND transaction_date <= @toDate
                  ORDER BY transaction_date DESC, created_at DESC",
                new { bankAccountId, fromDate, toDate });
        }

        /// <summary>
        /// Get unreconciled transactions, optionally filtered by bank account
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetUnreconciledAsync(Guid? bankAccountId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM bank_transactions WHERE is_reconciled = false";
            if (bankAccountId.HasValue)
            {
                sql += " AND bank_account_id = @bankAccountId";
            }
            sql += " ORDER BY transaction_date DESC, created_at DESC";

            return await connection.QueryAsync<BankTransaction>(sql, new { bankAccountId });
        }

        /// <summary>
        /// Get reconciled transactions for a bank account
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetReconciledAsync(Guid bankAccountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                @"SELECT * FROM bank_transactions
                  WHERE bank_account_id = @bankAccountId AND is_reconciled = true
                  ORDER BY reconciled_at DESC",
                new { bankAccountId });
        }

        /// <summary>
        /// Mark a transaction as reconciled with a linked record
        /// </summary>
        public async Task ReconcileTransactionAsync(
            Guid transactionId, string reconciledType, Guid reconciledId, string? reconciledBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE bank_transactions SET
                    is_reconciled = true,
                    reconciled_type = @reconciledType,
                    reconciled_id = @reconciledId,
                    reconciled_at = NOW(),
                    reconciled_by = @reconciledBy,
                    updated_at = NOW()
                  WHERE id = @transactionId",
                new { transactionId, reconciledType, reconciledId, reconciledBy });
        }

        /// <summary>
        /// Remove reconciliation from a transaction
        /// </summary>
        public async Task UnreconcileTransactionAsync(Guid transactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE bank_transactions SET
                    is_reconciled = false,
                    reconciled_type = NULL,
                    reconciled_id = NULL,
                    reconciled_at = NULL,
                    reconciled_by = NULL,
                    updated_at = NOW()
                  WHERE id = @transactionId",
                new { transactionId });
        }

        /// <summary>
        /// Bulk add transactions (for CSV import)
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> BulkAddAsync(IEnumerable<BankTransaction> transactions)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var results = new List<BankTransaction>();

            foreach (var transaction in transactions)
            {
                var sql = @"INSERT INTO bank_transactions (
                        bank_account_id, transaction_date, value_date,
                        description, reference_number, cheque_number,
                        transaction_type, amount, balance_after, category,
                        is_reconciled, import_source, import_batch_id,
                        raw_data, transaction_hash,
                        created_at, updated_at
                    )
                    VALUES (
                        @BankAccountId, @TransactionDate, @ValueDate,
                        @Description, @ReferenceNumber, @ChequeNumber,
                        @TransactionType, @Amount, @BalanceAfter, @Category,
                        false, @ImportSource, @ImportBatchId,
                        @RawData::jsonb, @TransactionHash,
                        NOW(), NOW()
                    )
                    RETURNING *";

                var created = await connection.QuerySingleAsync<BankTransaction>(sql, transaction);
                results.Add(created);
            }

            return results;
        }

        /// <summary>
        /// Get transactions by import batch ID
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetByImportBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                @"SELECT * FROM bank_transactions
                  WHERE import_batch_id = @batchId
                  ORDER BY transaction_date DESC",
                new { batchId });
        }

        /// <summary>
        /// Delete all transactions from a specific import batch (for rollback)
        /// </summary>
        public async Task DeleteByImportBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_transactions WHERE import_batch_id = @batchId",
                new { batchId });
        }

        /// <summary>
        /// Check if a transaction with the given hash already exists
        /// </summary>
        public async Task<bool> ExistsByHashAsync(string transactionHash, Guid bankAccountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM bank_transactions
                  WHERE transaction_hash = @transactionHash AND bank_account_id = @bankAccountId",
                new { transactionHash, bankAccountId });
            return count > 0;
        }

        /// <summary>
        /// Get existing hashes from a list of hashes (for bulk duplicate detection)
        /// </summary>
        public async Task<IEnumerable<string>> GetExistingHashesAsync(Guid bankAccountId, IEnumerable<string> hashes)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<string>(
                @"SELECT transaction_hash FROM bank_transactions
                  WHERE bank_account_id = @bankAccountId
                    AND transaction_hash = ANY(@hashes)",
                new { bankAccountId, hashes = hashes.ToArray() });
        }

        /// <summary>
        /// Get potential payment matches for reconciliation
        /// </summary>
        public async Task<IEnumerable<Payments>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First get the bank transaction details
            var transaction = await GetByIdAsync(transactionId);
            if (transaction == null)
                return Enumerable.Empty<Payments>();

            // Only suggest matches for credit transactions (money coming in)
            if (transaction.TransactionType != "credit")
                return Enumerable.Empty<Payments>();

            // Find payments with similar amount and date range
            var sql = @"
                SELECT p.* FROM payments p
                WHERE ABS(p.amount - @amount) <= @tolerance
                  AND p.payment_date >= @fromDate
                  AND p.payment_date <= @toDate
                  AND NOT EXISTS (
                      SELECT 1 FROM bank_transactions bt
                      WHERE bt.reconciled_id = p.id AND bt.is_reconciled = true
                  )
                ORDER BY ABS(p.amount - @amount), ABS(p.payment_date - @transactionDate)
                LIMIT @maxResults";

            return await connection.QueryAsync<Payments>(sql, new
            {
                amount = transaction.Amount,
                tolerance,
                fromDate = transaction.TransactionDate.AddDays(-7),
                toDate = transaction.TransactionDate.AddDays(7),
                transactionDate = transaction.TransactionDate,
                maxResults
            });
        }

        /// <summary>
        /// Get summary statistics for a bank account
        /// </summary>
        public async Task<(int TotalCount, int ReconciledCount, decimal TotalCredits, decimal TotalDebits)>
            GetSummaryAsync(Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string> { "bank_account_id = @bankAccountId" };
            if (fromDate.HasValue)
                conditions.Add("transaction_date >= @fromDate");
            if (toDate.HasValue)
                conditions.Add("transaction_date <= @toDate");

            var whereClause = string.Join(" AND ", conditions);

            var sql = $@"
                SELECT
                    COUNT(*) as TotalCount,
                    COUNT(*) FILTER (WHERE is_reconciled = true) as ReconciledCount,
                    COALESCE(SUM(amount) FILTER (WHERE transaction_type = 'credit'), 0) as TotalCredits,
                    COALESCE(SUM(amount) FILTER (WHERE transaction_type = 'debit'), 0) as TotalDebits
                FROM bank_transactions
                WHERE {whereClause}";

            var result = await connection.QueryFirstAsync<(int, int, decimal, decimal)>(
                sql, new { bankAccountId, fromDate, toDate });
            return result;
        }
    }
}
