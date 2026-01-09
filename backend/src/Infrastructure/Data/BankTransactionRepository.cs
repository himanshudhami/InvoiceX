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
                    source_voucher_type, matched_entity_type, matched_entity_id,
                    tally_voucher_guid, tally_voucher_number, tally_migration_batch_id,
                    created_at, updated_at
                )
                VALUES (
                    @BankAccountId, @TransactionDate, @ValueDate,
                    @Description, @ReferenceNumber, @ChequeNumber,
                    @TransactionType, @Amount, @BalanceAfter, @Category,
                    @IsReconciled, @ReconciledType, @ReconciledId,
                    @ReconciledAt, @ReconciledBy,
                    @ImportSource, @ImportBatchId, @RawData::jsonb, @TransactionHash,
                    @SourceVoucherType, @MatchedEntityType, @MatchedEntityId,
                    @TallyVoucherGuid, @TallyVoucherNumber, @TallyMigrationBatchId,
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
                    source_voucher_type = @SourceVoucherType,
                    matched_entity_type = @MatchedEntityType,
                    matched_entity_id = @MatchedEntityId,
                    tally_voucher_guid = @TallyVoucherGuid,
                    tally_voucher_number = @TallyVoucherNumber,
                    tally_migration_batch_id = @TallyMigrationBatchId,
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
        /// Optionally stores difference information and adjustment journal reference
        /// </summary>
        public async Task ReconcileTransactionAsync(
            Guid transactionId,
            string reconciledType,
            Guid reconciledId,
            string? reconciledBy = null,
            decimal? differenceAmount = null,
            string? differenceType = null,
            string? differenceNotes = null,
            string? tdsSection = null,
            Guid? adjustmentJournalId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE bank_transactions SET
                    is_reconciled = true,
                    reconciled_type = @reconciledType,
                    reconciled_id = @reconciledId,
                    reconciled_at = NOW(),
                    reconciled_by = @reconciledBy,
                    reconciliation_difference_amount = @differenceAmount,
                    reconciliation_difference_type = @differenceType,
                    reconciliation_difference_notes = @differenceNotes,
                    reconciliation_tds_section = @tdsSection,
                    reconciliation_adjustment_journal_id = @adjustmentJournalId,
                    updated_at = NOW()
                  WHERE id = @transactionId",
                new {
                    transactionId,
                    reconciledType,
                    reconciledId,
                    reconciledBy,
                    differenceAmount,
                    differenceType,
                    differenceNotes,
                    tdsSection,
                    adjustmentJournalId
                });
        }

        /// <summary>
        /// Remove reconciliation from a transaction
        /// Clears all reconciliation fields including difference tracking
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
                    reconciliation_difference_amount = NULL,
                    reconciliation_difference_type = NULL,
                    reconciliation_difference_notes = NULL,
                    reconciliation_tds_section = NULL,
                    reconciliation_adjustment_journal_id = NULL,
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
        /// Get potential payment matches for reconciliation (credit transactions)
        /// </summary>
        public async Task<IEnumerable<PaymentWithDetails>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First get the bank transaction details
            var transaction = await GetByIdAsync(transactionId);
            if (transaction == null)
                return Enumerable.Empty<PaymentWithDetails>();

            // Only suggest matches for credit transactions (money coming in)
            if (transaction.TransactionType != "credit")
                return Enumerable.Empty<PaymentWithDetails>();

            // Find payments with similar amount, including customer and invoice info
            // Use wider date range (±30 days) for better matching
            // Use amount_in_inr for multi-currency payments (bank transactions are always in INR)
            var sql = @"
                SELECT
                    p.id,
                    p.payment_date,
                    COALESCE(p.amount_in_inr, p.amount) as amount,
                    p.reference_number,
                    p.payment_method,
                    p.notes,
                    c.name as customer_name,
                    c.company_name as customer_company,
                    i.invoice_number
                FROM payments p
                LEFT JOIN customers c ON c.id = p.customer_id
                LEFT JOIN invoices i ON i.id = p.invoice_id
                WHERE ABS(COALESCE(p.amount_in_inr, p.amount) - @amount) <= @tolerance
                  AND p.payment_date >= @fromDate
                  AND p.payment_date <= @toDate
                  AND NOT EXISTS (
                      SELECT 1 FROM bank_transactions bt
                      WHERE bt.reconciled_id = p.id AND bt.is_reconciled = true
                  )
                ORDER BY ABS(COALESCE(p.amount_in_inr, p.amount) - @amount), ABS(p.payment_date::date - @transactionDate::date)
                LIMIT @maxResults";

            return await connection.QueryAsync<PaymentWithDetails>(sql, new
            {
                amount = transaction.Amount,
                tolerance,
                fromDate = transaction.TransactionDate.AddDays(-30),
                toDate = transaction.TransactionDate.AddDays(30),
                transactionDate = transaction.TransactionDate,
                maxResults
            });
        }

        /// <summary>
        /// Search payments for credit reconciliation with text search
        /// </summary>
        public async Task<IEnumerable<PaymentWithDetails>> SearchPaymentsAsync(
            Guid companyId,
            string? searchTerm = null,
            decimal? amountMin = null,
            decimal? amountMax = null,
            int maxResults = 20)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string> { "p.company_id = @companyId" };

            // Exclude already reconciled payments
            conditions.Add(@"NOT EXISTS (
                SELECT 1 FROM bank_transactions bt
                WHERE bt.reconciled_id = p.id AND bt.is_reconciled = true
            )");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add(@"(
                    c.name ILIKE @searchPattern OR
                    c.company_name ILIKE @searchPattern OR
                    i.invoice_number ILIKE @searchPattern OR
                    p.reference_number ILIKE @searchPattern OR
                    p.notes ILIKE @searchPattern
                )");
            }

            // Use amount_in_inr for filtering to handle multi-currency payments correctly
            // Bank transactions are always in INR, so we need to compare against INR amounts
            if (amountMin.HasValue)
                conditions.Add("COALESCE(p.amount_in_inr, p.amount) >= @amountMin");
            if (amountMax.HasValue)
                conditions.Add("COALESCE(p.amount_in_inr, p.amount) <= @amountMax");

            var sql = $@"
                SELECT
                    p.id,
                    p.payment_date,
                    COALESCE(p.amount_in_inr, p.amount) as amount,
                    p.reference_number,
                    p.payment_method,
                    p.notes,
                    c.name as customer_name,
                    c.company_name as customer_company,
                    i.invoice_number
                FROM payments p
                LEFT JOIN customers c ON c.id = p.customer_id
                LEFT JOIN invoices i ON i.id = p.invoice_id
                WHERE {string.Join(" AND ", conditions)}
                ORDER BY p.payment_date DESC
                LIMIT @maxResults";

            return await connection.QueryAsync<PaymentWithDetails>(sql, new
            {
                companyId,
                searchPattern = $"%{searchTerm}%",
                amountMin,
                amountMax,
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

        // ==================== Debit Reconciliation (Outgoing Payments) Methods ====================

        /// <summary>
        /// Get debit reconciliation candidates by querying across all expense tables
        /// </summary>
        public async Task<IEnumerable<OutgoingPaymentRecord>> GetDebitReconciliationCandidatesAsync(
            Guid companyId, decimal amount, DateOnly date, decimal amountTolerance, int dateTolerance, int maxResults)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var fromDate = date.AddDays(-dateTolerance);
            var toDate = date.AddDays(dateTolerance);

            // Union query across all 6 expense tables
            var sql = @"
                WITH candidates AS (
                    -- Salary transactions
                    SELECT
                        est.id,
                        'salary' as type,
                        est.payment_date as payment_date,
                        est.net_salary as amount,
                        e.employee_name as payee_name,
                        CONCAT('Salary for ', est.salary_month, '/', est.salary_year) as description,
                        est.payment_reference as reference_number,
                        est.bank_transaction_id IS NOT NULL as is_reconciled,
                        est.bank_transaction_id,
                        est.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Salary' as category,
                        est.status
                    FROM employee_salary_transactions est
                    JOIN employees e ON e.id = est.employee_id
                    WHERE e.company_id = @companyId
                      AND est.status = 'paid'
                      AND est.payment_date >= @fromDate
                      AND est.payment_date <= @toDate
                      AND ABS(est.net_salary - @amount) <= @amountTolerance

                    UNION ALL

                    -- Contractor payments
                    SELECT
                        cp.id,
                        'contractor' as type,
                        cp.payment_date,
                        cp.net_payable as amount,
                        p.name as payee_name,
                        cp.remarks as description,
                        cp.payment_reference as reference_number,
                        cp.bank_transaction_id IS NOT NULL as is_reconciled,
                        cp.bank_transaction_id,
                        cp.reconciled_at,
                        cp.tds_amount,
                        cp.tds_section,
                        'Contractor' as category,
                        cp.status
                    FROM contractor_payments cp
                    LEFT JOIN parties p ON p.id = cp.party_id
                    WHERE cp.company_id = @companyId
                      AND cp.status = 'paid'
                      AND cp.payment_date >= @fromDate
                      AND cp.payment_date <= @toDate
                      AND ABS(cp.net_payable - @amount) <= @amountTolerance

                    UNION ALL

                    -- Expense claims
                    SELECT
                        ec.id,
                        'expense_claim' as type,
                        ec.approved_at::date as payment_date,
                        ec.amount,
                        e.employee_name as payee_name,
                        ec.description,
                        ec.claim_number as reference_number,
                        ec.bank_transaction_id IS NOT NULL as is_reconciled,
                        ec.bank_transaction_id,
                        ec.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        cat.name as category,
                        ec.status
                    FROM expense_claims ec
                    JOIN employees e ON e.id = ec.employee_id
                    LEFT JOIN expense_categories cat ON cat.id = ec.category_id
                    WHERE e.company_id = @companyId
                      AND ec.status IN ('approved', 'reimbursed')
                      AND ec.approved_at IS NOT NULL
                      AND ec.approved_at::date >= @fromDate
                      AND ec.approved_at::date <= @toDate
                      AND ABS(ec.amount - @amount) <= @amountTolerance

                    UNION ALL

                    -- Subscriptions (recurring)
                    SELECT
                        s.id,
                        'subscription' as type,
                        s.renewal_date as payment_date,
                        s.cost_per_period as amount,
                        COALESCE(s.vendor, s.name) as payee_name,
                        COALESCE(s.plan_name, s.notes) as description,
                        s.name as reference_number,
                        s.bank_transaction_id IS NOT NULL as is_reconciled,
                        s.bank_transaction_id,
                        s.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        s.category,
                        s.status
                    FROM subscriptions s
                    WHERE s.company_id = @companyId
                      AND s.status = 'active'
                      AND s.renewal_date >= @fromDate
                      AND s.renewal_date <= @toDate
                      AND ABS(s.cost_per_period - @amount) <= @amountTolerance

                    UNION ALL

                    -- Loan EMI payments
                    SELECT
                        lt.id,
                        'loan_payment' as type,
                        lt.transaction_date as payment_date,
                        lt.amount,
                        l.lender_name as payee_name,
                        CONCAT(lt.transaction_type, ' - ', l.loan_name) as description,
                        lt.voucher_reference as reference_number,
                        lt.bank_transaction_id IS NOT NULL as is_reconciled,
                        lt.bank_transaction_id,
                        lt.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Loan' as category,
                        'completed' as status
                    FROM loan_transactions lt
                    JOIN loans l ON l.id = lt.loan_id
                    WHERE l.company_id = @companyId
                      AND lt.transaction_type IN ('emi_payment', 'prepayment', 'foreclosure')
                      AND lt.transaction_date >= @fromDate
                      AND lt.transaction_date <= @toDate
                      AND ABS(lt.amount - @amount) <= @amountTolerance

                    UNION ALL

                    -- Asset maintenance
                    SELECT
                        am.id,
                        'asset_maintenance' as type,
                        am.opened_at::date as payment_date,
                        am.cost as amount,
                        am.vendor as payee_name,
                        am.title as description,
                        NULL::text as reference_number,
                        am.bank_transaction_id IS NOT NULL as is_reconciled,
                        am.bank_transaction_id,
                        am.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Asset Maintenance' as category,
                        am.status
                    FROM asset_maintenance am
                    JOIN assets a ON a.id = am.asset_id
                    WHERE a.company_id = @companyId
                      AND am.status IN ('resolved', 'closed')
                      AND am.opened_at::date >= @fromDate
                      AND am.opened_at::date <= @toDate
                      AND ABS(am.cost - @amount) <= @amountTolerance
                )
                SELECT * FROM candidates
                WHERE is_reconciled = false
                ORDER BY ABS(amount - @amount), ABS(payment_date::date - @targetDate::date)
                LIMIT @maxResults";

            return await connection.QueryAsync<OutgoingPaymentRecord>(sql, new
            {
                companyId,
                amount,
                amountTolerance,
                fromDate,
                toDate,
                targetDate = date,
                maxResults
            });
        }

        /// <summary>
        /// Get unified list of outgoing payments across all expense types
        /// </summary>
        public async Task<(IEnumerable<OutgoingPaymentRecord> Items, int TotalCount)> GetOutgoingPaymentsAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            bool? reconciled = null,
            List<string>? types = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string? searchTerm = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var offset = (pageNumber - 1) * pageSize;

            // Build the base union query
            var unionClauses = new List<string>();

            // Only include requested types, or all if none specified
            var includeAll = types == null || !types.Any();
            var typeSet = types?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            if (includeAll || typeSet.Contains("salary"))
            {
                unionClauses.Add(@"
                    SELECT
                        est.id,
                        'salary' as type,
                        est.payment_date as payment_date,
                        est.net_salary as amount,
                        e.employee_name as payee_name,
                        CONCAT('Salary for ', est.salary_month, '/', est.salary_year) as description,
                        est.payment_reference as reference_number,
                        est.bank_transaction_id IS NOT NULL as is_reconciled,
                        est.bank_transaction_id,
                        est.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Salary' as category,
                        est.status
                    FROM employee_salary_transactions est
                    JOIN employees e ON e.id = est.employee_id
                    WHERE e.company_id = @companyId
                      AND est.status = 'paid'");
            }

            if (includeAll || typeSet.Contains("contractor"))
            {
                unionClauses.Add(@"
                    SELECT
                        cp.id,
                        'contractor' as type,
                        cp.payment_date,
                        cp.net_payable as amount,
                        p.name as payee_name,
                        cp.remarks as description,
                        cp.payment_reference as reference_number,
                        cp.bank_transaction_id IS NOT NULL as is_reconciled,
                        cp.bank_transaction_id,
                        cp.reconciled_at,
                        cp.tds_amount,
                        cp.tds_section,
                        'Contractor' as category,
                        cp.status
                    FROM contractor_payments cp
                    LEFT JOIN parties p ON p.id = cp.party_id
                    WHERE cp.company_id = @companyId
                      AND cp.status = 'paid'");
            }

            if (includeAll || typeSet.Contains("expense_claim"))
            {
                unionClauses.Add(@"
                    SELECT
                        ec.id,
                        'expense_claim' as type,
                        ec.approved_at::date as payment_date,
                        ec.amount,
                        e.employee_name as payee_name,
                        ec.description,
                        ec.claim_number as reference_number,
                        ec.bank_transaction_id IS NOT NULL as is_reconciled,
                        ec.bank_transaction_id,
                        ec.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        cat.name as category,
                        ec.status
                    FROM expense_claims ec
                    JOIN employees e ON e.id = ec.employee_id
                    LEFT JOIN expense_categories cat ON cat.id = ec.category_id
                    WHERE e.company_id = @companyId
                      AND ec.status IN ('approved', 'reimbursed')
                      AND ec.approved_at IS NOT NULL");
            }

            if (includeAll || typeSet.Contains("subscription"))
            {
                unionClauses.Add(@"
                    SELECT
                        s.id,
                        'subscription' as type,
                        s.renewal_date as payment_date,
                        s.cost_per_period as amount,
                        COALESCE(s.vendor, s.name) as payee_name,
                        COALESCE(s.plan_name, s.notes) as description,
                        s.name as reference_number,
                        s.bank_transaction_id IS NOT NULL as is_reconciled,
                        s.bank_transaction_id,
                        s.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        s.category,
                        s.status
                    FROM subscriptions s
                    WHERE s.company_id = @companyId");
            }

            if (includeAll || typeSet.Contains("loan_payment"))
            {
                unionClauses.Add(@"
                    SELECT
                        lt.id,
                        'loan_payment' as type,
                        lt.transaction_date as payment_date,
                        lt.amount,
                        l.lender_name as payee_name,
                        CONCAT(lt.transaction_type, ' - ', l.loan_name) as description,
                        lt.voucher_reference as reference_number,
                        lt.bank_transaction_id IS NOT NULL as is_reconciled,
                        lt.bank_transaction_id,
                        lt.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Loan' as category,
                        'completed' as status
                    FROM loan_transactions lt
                    JOIN loans l ON l.id = lt.loan_id
                    WHERE l.company_id = @companyId
                      AND lt.transaction_type IN ('emi_payment', 'prepayment', 'foreclosure')");
            }

            if (includeAll || typeSet.Contains("asset_maintenance"))
            {
                unionClauses.Add(@"
                    SELECT
                        am.id,
                        'asset_maintenance' as type,
                        am.opened_at::date as payment_date,
                        am.cost as amount,
                        am.vendor as payee_name,
                        am.title as description,
                        NULL::text as reference_number,
                        am.bank_transaction_id IS NOT NULL as is_reconciled,
                        am.bank_transaction_id,
                        am.reconciled_at,
                        NULL::decimal as tds_amount,
                        NULL::text as tds_section,
                        'Asset Maintenance' as category,
                        am.status
                    FROM asset_maintenance am
                    JOIN assets a ON a.id = am.asset_id
                    WHERE a.company_id = @companyId
                      AND am.status IN ('resolved', 'closed')");
            }

            if (!unionClauses.Any())
            {
                return (Enumerable.Empty<OutgoingPaymentRecord>(), 0);
            }

            var baseSql = string.Join(" UNION ALL ", unionClauses);

            // Build filter conditions
            var filters = new List<string>();
            if (reconciled.HasValue)
                filters.Add(reconciled.Value ? "is_reconciled = true" : "is_reconciled = false");
            if (fromDate.HasValue)
                filters.Add("payment_date >= @fromDate");
            if (toDate.HasValue)
                filters.Add("payment_date <= @toDate");
            if (!string.IsNullOrWhiteSpace(searchTerm))
                filters.Add("(payee_name ILIKE @searchPattern OR description ILIKE @searchPattern OR reference_number ILIKE @searchPattern)");

            var filterClause = filters.Any() ? "WHERE " + string.Join(" AND ", filters) : "";

            var dataSql = $@"
                WITH all_payments AS ({baseSql})
                SELECT * FROM all_payments
                {filterClause}
                ORDER BY payment_date DESC
                OFFSET @offset LIMIT @pageSize";

            var countSql = $@"
                WITH all_payments AS ({baseSql})
                SELECT COUNT(*) FROM all_payments
                {filterClause}";

            var searchPattern = $"%{searchTerm}%";

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, new
            {
                companyId,
                reconciled,
                fromDate,
                toDate,
                searchPattern,
                offset,
                pageSize
            });

            var items = await multi.ReadAsync<OutgoingPaymentRecord>();
            var totalCount = await multi.ReadSingleAsync<int>();

            return (items, totalCount);
        }

        /// <summary>
        /// Get summary of outgoing payments for dashboard
        /// </summary>
        public async Task<OutgoingPaymentsSummary> GetOutgoingPaymentsSummaryAsync(
            Guid companyId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var dateFilter = "";
            if (fromDate.HasValue)
                dateFilter += " AND payment_date >= @fromDate";
            if (toDate.HasValue)
                dateFilter += " AND payment_date <= @toDate";

            var sql = $@"
                WITH all_payments AS (
                    -- Salary transactions
                    SELECT
                        'salary' as type,
                        est.net_salary as amount,
                        est.bank_transaction_id IS NOT NULL as is_reconciled,
                        est.payment_date
                    FROM employee_salary_transactions est
                    JOIN employees e ON e.id = est.employee_id
                    WHERE e.company_id = @companyId AND est.status = 'paid'

                    UNION ALL

                    -- Contractor payments
                    SELECT
                        'contractor' as type,
                        cp.net_payable as amount,
                        cp.bank_transaction_id IS NOT NULL as is_reconciled,
                        cp.payment_date
                    FROM contractor_payments cp
                    WHERE cp.company_id = @companyId AND cp.status = 'paid'

                    UNION ALL

                    -- Expense claims
                    SELECT
                        'expense_claim' as type,
                        ec.amount,
                        ec.bank_transaction_id IS NOT NULL as is_reconciled,
                        ec.approved_at::date as payment_date
                    FROM expense_claims ec
                    JOIN employees e ON e.id = ec.employee_id
                    WHERE e.company_id = @companyId AND ec.status IN ('approved', 'reimbursed') AND ec.approved_at IS NOT NULL

                    UNION ALL

                    -- Subscriptions
                    SELECT
                        'subscription' as type,
                        s.cost_per_period as amount,
                        s.bank_transaction_id IS NOT NULL as is_reconciled,
                        s.renewal_date as payment_date
                    FROM subscriptions s
                    WHERE s.company_id = @companyId

                    UNION ALL

                    -- Loan payments
                    SELECT
                        'loan_payment' as type,
                        lt.amount,
                        lt.bank_transaction_id IS NOT NULL as is_reconciled,
                        lt.transaction_date as payment_date
                    FROM loan_transactions lt
                    JOIN loans l ON l.id = lt.loan_id
                    WHERE l.company_id = @companyId AND lt.transaction_type IN ('emi_payment', 'prepayment', 'foreclosure')

                    UNION ALL

                    -- Asset maintenance
                    SELECT
                        'asset_maintenance' as type,
                        am.cost as amount,
                        am.bank_transaction_id IS NOT NULL as is_reconciled,
                        am.opened_at::date as payment_date
                    FROM asset_maintenance am
                    JOIN assets a ON a.id = am.asset_id
                    WHERE a.company_id = @companyId AND am.status IN ('resolved', 'closed')
                ),
                filtered AS (
                    SELECT * FROM all_payments WHERE 1=1 {dateFilter}
                )
                SELECT
                    COUNT(*) as total_count,
                    COUNT(*) FILTER (WHERE is_reconciled = true) as reconciled_count,
                    COUNT(*) FILTER (WHERE is_reconciled = false) as unreconciled_count,
                    COALESCE(SUM(amount), 0) as total_amount,
                    COALESCE(SUM(amount) FILTER (WHERE is_reconciled = true), 0) as reconciled_amount,
                    COALESCE(SUM(amount) FILTER (WHERE is_reconciled = false), 0) as unreconciled_amount,
                    type,
                    COUNT(*) FILTER (WHERE type = type) as type_count,
                    COALESCE(SUM(amount) FILTER (WHERE type = type), 0) as type_amount,
                    COUNT(*) FILTER (WHERE type = type AND is_reconciled = true) as type_reconciled_count
                FROM filtered
                GROUP BY GROUPING SETS ((), (type))";

            var results = await connection.QueryAsync<dynamic>(sql, new { companyId, fromDate, toDate });
            var rows = results.ToList();

            var summary = new OutgoingPaymentsSummary();

            // First row is the total (no type)
            var totalRow = rows.FirstOrDefault(r => r.type == null);
            if (totalRow != null)
            {
                summary.TotalCount = (int)(totalRow.total_count ?? 0);
                summary.ReconciledCount = (int)(totalRow.reconciled_count ?? 0);
                summary.UnreconciledCount = (int)(totalRow.unreconciled_count ?? 0);
                summary.TotalAmount = (decimal)(totalRow.total_amount ?? 0m);
                summary.ReconciledAmount = (decimal)(totalRow.reconciled_amount ?? 0m);
                summary.UnreconciledAmount = (decimal)(totalRow.unreconciled_amount ?? 0m);
            }

            // Other rows are by type
            foreach (var row in rows.Where(r => r.type != null))
            {
                var typeName = (string)row.type;
                summary.ByType[typeName] = (
                    Count: (int)(row.type_count ?? 0),
                    Amount: (decimal)(row.type_amount ?? 0m),
                    ReconciledCount: (int)(row.type_reconciled_count ?? 0)
                );
            }

            return summary;
        }

        // ==================== Reversal Pairing Methods ====================

        /// <summary>
        /// Find potential original transactions for a reversal
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> FindPotentialOriginalsForReversalAsync(
            Guid reversalTransactionId,
            int maxDaysBack = 90,
            int maxResults = 10)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First get the reversal transaction
            var reversalSql = @"
                SELECT * FROM bank_transactions
                WHERE id = @reversalTransactionId";

            var reversal = await connection.QueryFirstOrDefaultAsync<BankTransaction>(reversalSql, new { reversalTransactionId });
            if (reversal == null)
                return Enumerable.Empty<BankTransaction>();

            // Find potential originals: opposite type (debit), same amount, within date range, same bank account
            var sql = @"
                SELECT *
                FROM bank_transactions
                WHERE bank_account_id = @bankAccountId
                  AND transaction_type = 'debit'
                  AND amount = @amount
                  AND transaction_date >= @minDate
                  AND transaction_date <= @maxDate
                  AND id != @reversalTransactionId
                  AND paired_transaction_id IS NULL
                ORDER BY ABS(transaction_date::date - @reversalDate::date), transaction_date DESC
                LIMIT @maxResults";

            var minDate = reversal.TransactionDate.AddDays(-maxDaysBack);
            var maxDate = reversal.TransactionDate;

            return await connection.QueryAsync<BankTransaction>(sql, new
            {
                bankAccountId = reversal.BankAccountId,
                amount = reversal.Amount,
                minDate,
                maxDate,
                reversalDate = reversal.TransactionDate,
                reversalTransactionId,
                maxResults
            });
        }

        /// <summary>
        /// Pair a reversal transaction with its original
        /// </summary>
        public async Task PairReversalAsync(
            Guid originalTransactionId,
            Guid reversalTransactionId,
            Guid? reversalJournalEntryId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // Update original transaction
                var updateOriginalSql = @"
                    UPDATE bank_transactions
                    SET paired_transaction_id = @reversalTransactionId,
                        pair_type = 'original',
                        is_reconciled = true,
                        reconciled_type = 'reversal',
                        reconciled_id = @reversalTransactionId,
                        reconciled_at = @now,
                        updated_at = @now
                    WHERE id = @originalTransactionId";

                await connection.ExecuteAsync(updateOriginalSql, new
                {
                    reversalTransactionId,
                    originalTransactionId,
                    now
                }, transaction);

                // Update reversal transaction
                var updateReversalSql = @"
                    UPDATE bank_transactions
                    SET paired_transaction_id = @originalTransactionId,
                        pair_type = 'reversal',
                        reversal_journal_entry_id = @reversalJournalEntryId,
                        is_reconciled = true,
                        reconciled_type = 'reversal',
                        reconciled_id = @originalTransactionId,
                        reconciled_at = @now,
                        updated_at = @now
                    WHERE id = @reversalTransactionId";

                await connection.ExecuteAsync(updateReversalSql, new
                {
                    originalTransactionId,
                    reversalTransactionId,
                    reversalJournalEntryId,
                    now
                }, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Unpair a reversal from its original
        /// </summary>
        public async Task UnpairReversalAsync(Guid transactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // Get the paired transaction ID first
                var getPairedSql = "SELECT paired_transaction_id FROM bank_transactions WHERE id = @transactionId";
                var pairedId = await connection.QueryFirstOrDefaultAsync<Guid?>(getPairedSql, new { transactionId }, transaction);

                // Clear pairing on this transaction
                var clearSql = @"
                    UPDATE bank_transactions
                    SET paired_transaction_id = NULL,
                        pair_type = NULL,
                        reversal_journal_entry_id = NULL,
                        is_reconciled = false,
                        reconciled_type = NULL,
                        reconciled_id = NULL,
                        reconciled_at = NULL,
                        updated_at = @now
                    WHERE id = @transactionId";

                await connection.ExecuteAsync(clearSql, new { transactionId, now }, transaction);

                // Clear pairing on the paired transaction
                if (pairedId.HasValue)
                {
                    await connection.ExecuteAsync(clearSql, new { transactionId = pairedId.Value, now }, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Update the is_reversal_transaction flag
        /// </summary>
        public async Task UpdateReversalFlagAsync(Guid transactionId, bool isReversal)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                UPDATE bank_transactions
                SET is_reversal_transaction = @isReversal,
                    updated_at = @now
                WHERE id = @transactionId";

            await connection.ExecuteAsync(sql, new { transactionId, isReversal, now = DateTime.UtcNow });
        }

        /// <summary>
        /// Get all unpaired reversal transactions
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetUnpairedReversalsAsync(Guid? bankAccountId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM bank_transactions
                WHERE is_reversal_transaction = true
                  AND paired_transaction_id IS NULL";

            if (bankAccountId.HasValue)
                sql += " AND bank_account_id = @bankAccountId";

            sql += " ORDER BY transaction_date DESC";

            return await connection.QueryAsync<BankTransaction>(sql, new { bankAccountId });
        }

        // ==================== Journal Entry Linking (Hybrid Reconciliation) ====================

        /// <summary>
        /// Update the journal entry link for a bank transaction.
        /// Called during reconciliation to link bank txn to JE line.
        /// </summary>
        public async Task UpdateJournalEntryLinkAsync(
            Guid transactionId,
            Guid journalEntryId,
            Guid journalEntryLineId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                UPDATE bank_transactions
                SET reconciled_journal_entry_id = @journalEntryId,
                    reconciled_je_line_id = @journalEntryLineId,
                    updated_at = @now
                WHERE id = @transactionId";

            await connection.ExecuteAsync(sql, new
            {
                transactionId,
                journalEntryId,
                journalEntryLineId,
                now = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Reconcile a bank transaction directly to a journal entry (for manual JEs without source documents).
        /// </summary>
        public async Task ReconcileToJournalAsync(
            Guid transactionId,
            Guid journalEntryId,
            Guid journalEntryLineId,
            Guid? adjustmentJournalId = null,
            string? reconciledBy = null,
            string? notes = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                UPDATE bank_transactions
                SET is_reconciled = true,
                    reconciled_type = 'journal_entry',
                    reconciled_id = @journalEntryId,
                    reconciled_at = @now,
                    reconciled_by = @reconciledBy,
                    reconciled_journal_entry_id = @journalEntryId,
                    reconciled_je_line_id = @journalEntryLineId,
                    reconciliation_adjustment_journal_id = @adjustmentJournalId,
                    reconciliation_difference_notes = @notes,
                    updated_at = @now
                WHERE id = @transactionId";

            await connection.ExecuteAsync(sql, new
            {
                transactionId,
                journalEntryId,
                journalEntryLineId,
                adjustmentJournalId,
                reconciledBy,
                notes,
                now = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get reconciled transactions that don't have a JE link yet.
        /// Used for backfill migration.
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetReconciledWithoutJeLinkAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT bt.*
                FROM bank_transactions bt
                INNER JOIN bank_accounts ba ON bt.bank_account_id = ba.id
                WHERE ba.company_id = @companyId
                  AND bt.is_reconciled = true
                  AND bt.reconciled_journal_entry_id IS NULL
                  AND bt.reconciled_type IS NOT NULL
                  AND bt.reconciled_type != 'journal_entry'
                ORDER BY bt.transaction_date";

            return await connection.QueryAsync<BankTransaction>(sql, new { companyId });
        }

        // ==================== Tally Migration ====================

        /// <summary>
        /// Get bank transaction by Tally voucher GUID for deduplication
        /// </summary>
        public async Task<BankTransaction?> GetByTallyGuidAsync(string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankTransaction>(
                "SELECT * FROM bank_transactions WHERE tally_voucher_guid = @tallyVoucherGuid",
                new { tallyVoucherGuid });
        }

        /// <summary>
        /// Get all bank transactions for a Tally migration batch
        /// </summary>
        public async Task<IEnumerable<BankTransaction>> GetByTallyBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankTransaction>(
                "SELECT * FROM bank_transactions WHERE tally_migration_batch_id = @batchId ORDER BY transaction_date",
                new { batchId });
        }

        /// <summary>
        /// Delete all bank transactions for a Tally migration batch (for rollback)
        /// </summary>
        public async Task DeleteByTallyBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_transactions WHERE tally_migration_batch_id = @batchId",
                new { batchId });
        }
    }
}
