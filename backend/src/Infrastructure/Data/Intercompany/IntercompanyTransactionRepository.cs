using Core.Entities.Intercompany;
using Core.Interfaces.Intercompany;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Intercompany
{
    public class IntercompanyTransactionRepository : IIntercompanyTransactionRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "counterparty_company_id", "transaction_date", "financial_year",
            "transaction_type", "transaction_direction", "source_document_type",
            "source_document_id", "source_document_number", "amount", "currency",
            "exchange_rate", "amount_in_inr", "gst_amount", "is_gst_applicable",
            "journal_entry_id", "is_reconciled", "reconciled_at", "reconciled_by",
            "counterparty_transaction_id", "reconciliation_notes", "description",
            "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "source_document_number", "description", "transaction_type"
        };

        public IntercompanyTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IntercompanyTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<IntercompanyTransaction>(
                "SELECT * FROM intercompany_transactions WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<IntercompanyTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<IntercompanyTransaction>(
                "SELECT * FROM intercompany_transactions ORDER BY transaction_date DESC");
        }

        public async Task<(IEnumerable<IntercompanyTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("intercompany_transactions", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "transaction_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<IntercompanyTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<IntercompanyTransaction> AddAsync(IntercompanyTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO intercompany_transactions (
                    company_id, counterparty_company_id, transaction_date, financial_year,
                    transaction_type, transaction_direction, source_document_type,
                    source_document_id, source_document_number, amount, currency,
                    exchange_rate, amount_in_inr, gst_amount, is_gst_applicable,
                    journal_entry_id, is_reconciled, counterparty_transaction_id,
                    description, created_at, updated_at, created_by
                ) VALUES (
                    @CompanyId, @CounterpartyCompanyId, @TransactionDate, @FinancialYear,
                    @TransactionType, @TransactionDirection, @SourceDocumentType,
                    @SourceDocumentId, @SourceDocumentNumber, @Amount, @Currency,
                    @ExchangeRate, @AmountInInr, @GstAmount, @IsGstApplicable,
                    @JournalEntryId, @IsReconciled, @CounterpartyTransactionId,
                    @Description, NOW(), NOW(), @CreatedBy
                ) RETURNING *";
            return await connection.QuerySingleAsync<IntercompanyTransaction>(sql, entity);
        }

        public async Task UpdateAsync(IntercompanyTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE intercompany_transactions SET
                    company_id = @CompanyId,
                    counterparty_company_id = @CounterpartyCompanyId,
                    transaction_date = @TransactionDate,
                    financial_year = @FinancialYear,
                    transaction_type = @TransactionType,
                    transaction_direction = @TransactionDirection,
                    source_document_type = @SourceDocumentType,
                    source_document_id = @SourceDocumentId,
                    source_document_number = @SourceDocumentNumber,
                    amount = @Amount,
                    currency = @Currency,
                    exchange_rate = @ExchangeRate,
                    amount_in_inr = @AmountInInr,
                    gst_amount = @GstAmount,
                    is_gst_applicable = @IsGstApplicable,
                    journal_entry_id = @JournalEntryId,
                    is_reconciled = @IsReconciled,
                    reconciled_at = @ReconciledAt,
                    reconciled_by = @ReconciledBy,
                    counterparty_transaction_id = @CounterpartyTransactionId,
                    reconciliation_notes = @ReconciliationNotes,
                    description = @Description,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM intercompany_transactions WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<IntercompanyTransaction>> GetTransactionsBetweenCompaniesAsync(
            Guid companyId, Guid counterpartyId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM intercompany_transactions
                  WHERE company_id = @companyId
                    AND counterparty_company_id = @counterpartyId
                    AND (@fromDate IS NULL OR transaction_date >= @fromDate)
                    AND (@toDate IS NULL OR transaction_date <= @toDate)
                  ORDER BY transaction_date DESC";
            return await connection.QueryAsync<IntercompanyTransaction>(sql, new { companyId, counterpartyId, fromDate, toDate });
        }

        public async Task<IEnumerable<IntercompanyTransaction>> GetByCompanyIdAsync(Guid companyId, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM intercompany_transactions
                  WHERE company_id = @companyId
                    AND (@financialYear IS NULL OR financial_year = @financialYear)
                  ORDER BY transaction_date DESC";
            return await connection.QueryAsync<IntercompanyTransaction>(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<IntercompanyTransaction>> GetUnreconciledAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM intercompany_transactions
                  WHERE is_reconciled = false
                    AND (@companyId IS NULL OR company_id = @companyId)
                  ORDER BY transaction_date DESC";
            return await connection.QueryAsync<IntercompanyTransaction>(sql, new { companyId });
        }

        public async Task<IntercompanyTransaction?> GetBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<IntercompanyTransaction>(
                @"SELECT * FROM intercompany_transactions
                  WHERE source_document_type = @sourceType
                    AND source_document_id = @sourceId",
                new { sourceType, sourceId });
        }

        public async Task ReconcileTransactionsAsync(Guid transactionId, Guid counterpartyTransactionId, Guid reconciledBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE intercompany_transactions SET
                    is_reconciled = true,
                    reconciled_at = NOW(),
                    reconciled_by = @reconciledBy,
                    counterparty_transaction_id = CASE
                        WHEN id = @transactionId THEN @counterpartyTransactionId
                        ELSE @transactionId
                    END,
                    updated_at = NOW()
                WHERE id IN (@transactionId, @counterpartyTransactionId)";
            await connection.ExecuteAsync(sql, new { transactionId, counterpartyTransactionId, reconciledBy });
        }
    }
}
