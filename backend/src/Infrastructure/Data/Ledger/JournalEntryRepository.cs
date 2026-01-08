using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Ledger
{
    public class JournalEntryRepository : IJournalEntryRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "journal_number", "journal_date",
            "financial_year", "period_month", "entry_type",
            "source_type", "source_id", "source_number",
            "description", "total_debit", "total_credit",
            "status", "posted_at", "posted_by",
            "is_reversed", "reversal_of_id", "reversed_by_id",
            "rule_pack_version", "rule_code",
            "created_by", "created_at", "updated_at"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "journal_number", "description", "source_number", "entry_type"
        };

        public JournalEntryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<JournalEntry?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<JournalEntry>(
                "SELECT * FROM journal_entries WHERE id = @id",
                new { id });
        }

        public async Task<JournalEntry?> GetByIdWithLinesAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM journal_entries WHERE id = @id;
                SELECT * FROM journal_entry_lines WHERE journal_entry_id = @id ORDER BY line_number;";

            using var multi = await connection.QueryMultipleAsync(sql, new { id });
            var entry = await multi.ReadFirstOrDefaultAsync<JournalEntry>();
            if (entry != null)
            {
                entry.Lines = (await multi.ReadAsync<JournalEntryLine>()).ToList();
            }
            return entry;
        }

        public async Task<IEnumerable<JournalEntry>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                "SELECT * FROM journal_entries ORDER BY journal_date DESC, journal_number DESC");
        }

        public async Task<(IEnumerable<JournalEntry> Items, int TotalCount)> GetPagedAsync(
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

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    var paramName = filter.Key.Replace(".", "_");
                    conditions.Add($"{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchConditions = SearchableColumns.Select(col => $"{col} ILIKE @searchTerm");
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "journal_date";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM journal_entries
                {whereClause}
                ORDER BY {orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM journal_entries
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<JournalEntry>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<JournalEntry> AddAsync(JournalEntry entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Generate journal number if not provided
                if (string.IsNullOrEmpty(entity.JournalNumber))
                {
                    entity.JournalNumber = await GenerateNextNumberAsync(entity.CompanyId, entity.FinancialYear);
                }

                // Use provided ID if set, otherwise let database generate one
                var hasId = entity.Id != Guid.Empty;

                var sql = hasId
                    ? @"INSERT INTO journal_entries (
                        id, company_id, journal_number, journal_date,
                        financial_year, period_month, entry_type,
                        source_type, source_id, source_number,
                        description, narration, total_debit, total_credit,
                        status, posted_at, posted_by,
                        rule_pack_version, rule_code, idempotency_key,
                        tally_voucher_guid, tally_voucher_number, tally_voucher_type, tally_migration_batch_id,
                        created_by, created_at, updated_at
                    )
                    VALUES (
                        @Id, @CompanyId, @JournalNumber, @JournalDate,
                        @FinancialYear, @PeriodMonth, @EntryType,
                        @SourceType, @SourceId, @SourceNumber,
                        @Description, @Narration, @TotalDebit, @TotalCredit,
                        @Status, @PostedAt, @PostedBy,
                        @RulePackVersion, @RuleCode, @IdempotencyKey,
                        @TallyVoucherGuid, @TallyVoucherNumber, @TallyVoucherType, @TallyMigrationBatchId,
                        @CreatedBy, NOW(), NOW()
                    )
                    RETURNING *"
                    : @"INSERT INTO journal_entries (
                        company_id, journal_number, journal_date,
                        financial_year, period_month, entry_type,
                        source_type, source_id, source_number,
                        description, narration, total_debit, total_credit,
                        status, posted_at, posted_by,
                        rule_pack_version, rule_code, idempotency_key,
                        created_by, created_at, updated_at
                    )
                    VALUES (
                        @CompanyId, @JournalNumber, @JournalDate,
                        @FinancialYear, @PeriodMonth, @EntryType,
                        @SourceType, @SourceId, @SourceNumber,
                        @Description, @Narration, @TotalDebit, @TotalCredit,
                        @Status, @PostedAt, @PostedBy,
                        @RulePackVersion, @RuleCode, @IdempotencyKey,
                        @CreatedBy, NOW(), NOW()
                    )
                    RETURNING *";

                var entry = await connection.QuerySingleAsync<JournalEntry>(sql, entity, transaction);

                // Insert lines if provided
                if (entity.Lines?.Any() == true)
                {
                    await AddLinesInternalAsync(connection, transaction, entry.Id, entity.Lines);
                }

                await transaction.CommitAsync();
                return entry;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(JournalEntry entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Only allow updates to draft entries
            var existing = await GetByIdAsync(entity.Id);
            if (existing?.Status != "draft")
            {
                throw new InvalidOperationException("Can only update draft journal entries");
            }

            var sql = @"UPDATE journal_entries SET
                    journal_date = @JournalDate,
                    description = @Description,
                    total_debit = @TotalDebit,
                    total_credit = @TotalCredit,
                    updated_at = NOW()
                WHERE id = @Id AND status = 'draft'";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Only delete draft entries
            await connection.ExecuteAsync(
                "DELETE FROM journal_entries WHERE id = @id AND status = 'draft'",
                new { id });
        }

        // ==================== Company-Specific Queries ====================

        public async Task<IEnumerable<JournalEntry>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE company_id = @companyId
                  ORDER BY journal_date DESC, journal_number DESC",
                new { companyId });
        }

        public async Task<JournalEntry?> GetByNumberAsync(Guid companyId, string journalNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE company_id = @companyId AND journal_number = @journalNumber",
                new { companyId, journalNumber });
        }

        public async Task<IEnumerable<JournalEntry>> GetByPeriodAsync(
            Guid companyId,
            string financialYear,
            int? periodMonth = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"SELECT * FROM journal_entries
                        WHERE company_id = @companyId
                        AND financial_year = @financialYear";

            if (periodMonth.HasValue)
            {
                sql += " AND period_month = @periodMonth";
            }

            sql += " ORDER BY journal_date, journal_number";

            return await connection.QueryAsync<JournalEntry>(
                sql, new { companyId, financialYear, periodMonth });
        }

        public async Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE company_id = @companyId
                  AND journal_date >= @fromDate
                  AND journal_date <= @toDate
                  ORDER BY journal_date, journal_number",
                new { companyId, fromDate, toDate });
        }

        // ==================== Source Tracking ====================

        public async Task<IEnumerable<JournalEntry>> GetBySourceAsync(
            Guid companyId,
            string sourceType,
            Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE company_id = @companyId
                  AND source_type = @sourceType
                  AND source_id = @sourceId
                  ORDER BY journal_date DESC",
                new { companyId, sourceType, sourceId });
        }

        public async Task<bool> HasEntriesForSourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM journal_entries
                  WHERE source_type = @sourceType AND source_id = @sourceId",
                new { sourceType, sourceId });
            return count > 0;
        }

        public async Task<IEnumerable<JournalEntry>> GetBySourceAsync(
            string sourceType,
            Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE source_type = @sourceType
                  AND source_id = @sourceId
                  ORDER BY created_at",
                new { sourceType, sourceId });
        }

        // ==================== Idempotency ====================

        public async Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE idempotency_key = @idempotencyKey",
                new { idempotencyKey });
        }

        // ==================== Status Operations ====================

        public async Task PostAsync(Guid id, Guid postedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var entry = await GetByIdWithLinesAsync(id);
            if (entry == null)
            {
                throw new InvalidOperationException("Journal entry not found");
            }
            if (entry.Status != "draft")
            {
                throw new InvalidOperationException("Only draft entries can be posted");
            }
            if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
            {
                throw new InvalidOperationException("Journal entry is not balanced");
            }

            await connection.ExecuteAsync(
                @"UPDATE journal_entries SET
                    status = 'posted',
                    posted_at = NOW(),
                    posted_by = @postedBy,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, postedBy });
        }

        public async Task<IEnumerable<JournalEntry>> GetDraftEntriesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntry>(
                @"SELECT * FROM journal_entries
                  WHERE company_id = @companyId AND status = 'draft'
                  ORDER BY journal_date DESC",
                new { companyId });
        }

        // ==================== Reversal ====================

        public async Task<JournalEntry> CreateReversalAsync(Guid originalId, Guid createdBy, string? reason = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var original = await GetByIdWithLinesAsync(originalId);
                if (original == null)
                {
                    throw new InvalidOperationException("Original journal entry not found");
                }
                if (original.Status != "posted")
                {
                    throw new InvalidOperationException("Only posted entries can be reversed");
                }
                if (original.IsReversed)
                {
                    throw new InvalidOperationException("Entry has already been reversed");
                }

                // Create reversal entry
                var reversal = new JournalEntry
                {
                    CompanyId = original.CompanyId,
                    JournalNumber = await GenerateNextNumberAsync(original.CompanyId, original.FinancialYear),
                    JournalDate = DateOnly.FromDateTime(DateTime.Today),
                    FinancialYear = original.FinancialYear,
                    PeriodMonth = original.PeriodMonth,
                    EntryType = "reversal",
                    SourceType = original.SourceType,
                    SourceId = original.SourceId,
                    SourceNumber = original.SourceNumber,
                    Description = $"Reversal of {original.JournalNumber}" + (reason != null ? $": {reason}" : ""),
                    TotalDebit = original.TotalCredit,
                    TotalCredit = original.TotalDebit,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = createdBy,
                    ReversalOfId = originalId,
                    CreatedBy = createdBy
                };

                var reversalSql = @"INSERT INTO journal_entries (
                        company_id, journal_number, journal_date,
                        financial_year, period_month, entry_type,
                        source_type, source_id, source_number,
                        description, total_debit, total_credit,
                        status, posted_at, posted_by, reversal_of_id,
                        created_by, created_at, updated_at
                    )
                    VALUES (
                        @CompanyId, @JournalNumber, @JournalDate,
                        @FinancialYear, @PeriodMonth, @EntryType,
                        @SourceType, @SourceId, @SourceNumber,
                        @Description, @TotalDebit, @TotalCredit,
                        @Status, @PostedAt, @PostedBy, @ReversalOfId,
                        @CreatedBy, NOW(), NOW()
                    )
                    RETURNING *";

                var newReversal = await connection.QuerySingleAsync<JournalEntry>(reversalSql, reversal, transaction);

                // Create reversed lines (swap debit/credit)
                if (original.Lines?.Any() == true)
                {
                    var reversedLines = original.Lines.Select(l => new JournalEntryLine
                    {
                        JournalEntryId = newReversal.Id,
                        AccountId = l.AccountId,
                        LineNumber = l.LineNumber,
                        DebitAmount = l.CreditAmount,
                        CreditAmount = l.DebitAmount,
                        Currency = l.Currency,
                        ExchangeRate = l.ExchangeRate,
                        SubledgerType = l.SubledgerType,
                        SubledgerId = l.SubledgerId,
                        Description = $"Reversal: {l.Description}"
                    });

                    await AddLinesInternalAsync(connection, transaction, newReversal.Id, reversedLines);
                }

                // Mark original as reversed
                await connection.ExecuteAsync(
                    @"UPDATE journal_entries SET
                        is_reversed = TRUE,
                        reversed_by_id = @reversedById,
                        updated_at = NOW()
                      WHERE id = @originalId",
                    new { originalId, reversedById = newReversal.Id },
                    transaction);

                await transaction.CommitAsync();
                return newReversal;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CanReverseAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var entry = await GetByIdAsync(id);
            return entry != null && entry.Status == "posted" && !entry.IsReversed;
        }

        // ==================== Number Generation ====================

        public async Task<string> GenerateNextNumberAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var number = await connection.ExecuteScalarAsync<string>(
                "SELECT generate_journal_number(@companyId, @financialYear)",
                new { companyId, financialYear });
            return number ?? $"JV/{financialYear}/0001";
        }

        // ==================== Lines ====================

        public async Task<IEnumerable<JournalEntryLine>> GetLinesAsync(Guid journalEntryId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<JournalEntryLine>(
                @"SELECT * FROM journal_entry_lines
                  WHERE journal_entry_id = @journalEntryId
                  ORDER BY line_number",
                new { journalEntryId });
        }

        public async Task AddLinesAsync(Guid journalEntryId, IEnumerable<JournalEntryLine> lines)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await AddLinesInternalAsync(connection, transaction, journalEntryId, lines);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateLinesAsync(Guid journalEntryId, IEnumerable<JournalEntryLine> lines)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Verify entry is draft
                var entry = await connection.QueryFirstOrDefaultAsync<JournalEntry>(
                    "SELECT * FROM journal_entries WHERE id = @journalEntryId",
                    new { journalEntryId },
                    transaction);

                if (entry?.Status != "draft")
                {
                    throw new InvalidOperationException("Can only update lines of draft entries");
                }

                // Delete existing lines
                await connection.ExecuteAsync(
                    "DELETE FROM journal_entry_lines WHERE journal_entry_id = @journalEntryId",
                    new { journalEntryId },
                    transaction);

                // Insert new lines
                await AddLinesInternalAsync(connection, transaction, journalEntryId, lines);

                // Update totals
                var totals = await connection.QueryFirstAsync<(decimal TotalDebit, decimal TotalCredit)>(
                    @"SELECT
                        COALESCE(SUM(debit_amount), 0) as TotalDebit,
                        COALESCE(SUM(credit_amount), 0) as TotalCredit
                      FROM journal_entry_lines WHERE journal_entry_id = @journalEntryId",
                    new { journalEntryId },
                    transaction);

                await connection.ExecuteAsync(
                    @"UPDATE journal_entries SET
                        total_debit = @TotalDebit,
                        total_credit = @TotalCredit,
                        updated_at = NOW()
                      WHERE id = @journalEntryId",
                    new { journalEntryId, totals.TotalDebit, totals.TotalCredit },
                    transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task AddLinesInternalAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid journalEntryId,
            IEnumerable<JournalEntryLine> lines)
        {
            var lineSql = @"INSERT INTO journal_entry_lines (
                    journal_entry_id, account_id, line_number,
                    debit_amount, credit_amount, currency, exchange_rate,
                    foreign_debit, foreign_credit, subledger_type, subledger_id,
                    description, reference_type, reference_id
                )
                VALUES (
                    @JournalEntryId, @AccountId, @LineNumber,
                    @DebitAmount, @CreditAmount, @Currency, @ExchangeRate,
                    @ForeignDebit, @ForeignCredit, @SubledgerType, @SubledgerId,
                    @Description, @ReferenceType, @ReferenceId
                )";

            int lineNum = 1;
            foreach (var line in lines)
            {
                line.JournalEntryId = journalEntryId;
                line.LineNumber = lineNum++;
                await connection.ExecuteAsync(lineSql, line, transaction);
            }
        }

        // ==================== Balance Queries ====================

        /// <summary>
        /// Get the balance for a specific account as of a date.
        /// Calculates sum of debits - sum of credits from posted journal entries.
        /// Used for BRS generation to get book balance from ledger perspective.
        /// </summary>
        public async Task<decimal> GetAccountBalanceAsync(Guid companyId, Guid accountId, DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT COALESCE(SUM(jel.debit_amount) - SUM(jel.credit_amount), 0)
                FROM journal_entry_lines jel
                INNER JOIN journal_entries je ON jel.journal_entry_id = je.id
                WHERE je.company_id = @companyId
                  AND jel.account_id = @accountId
                  AND je.status = 'posted'
                  AND je.journal_date <= @asOfDate";

            return await connection.ExecuteScalarAsync<decimal>(sql, new { companyId, accountId, asOfDate });
        }

        // ==================== Tally Integration ====================

        public async Task<JournalEntry?> GetByTallyGuidAsync(Guid companyId, string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<JournalEntry>(
                "SELECT * FROM journal_entries WHERE company_id = @companyId AND tally_voucher_guid = @tallyVoucherGuid",
                new { companyId, tallyVoucherGuid });
        }
    }
}
