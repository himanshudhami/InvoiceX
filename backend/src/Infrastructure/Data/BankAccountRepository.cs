using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly string _connectionString;

        // All columns for SELECT queries
        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "account_name", "account_number", "bank_name",
            "ifsc_code", "branch_name", "account_type", "currency",
            "opening_balance", "current_balance", "as_of_date",
            "is_primary", "is_active", "notes",
            "created_at", "updated_at"
        };

        // Searchable columns for full-text search
        private static readonly string[] SearchableColumns = new[]
        {
            "account_name", "account_number", "bank_name", "branch_name", "ifsc_code"
        };

        public BankAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<BankAccount?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Compute current_balance from opening_balance + credits - debits
            return await connection.QueryFirstOrDefaultAsync<BankAccount>(@"
                SELECT
                    ba.*,
                    ba.opening_balance + COALESCE(SUM(CASE WHEN bt.transaction_type = 'credit' THEN bt.amount ELSE 0 END), 0)
                        - COALESCE(SUM(CASE WHEN bt.transaction_type = 'debit' THEN bt.amount ELSE 0 END), 0) AS current_balance
                FROM bank_accounts ba
                LEFT JOIN bank_transactions bt ON bt.bank_account_id = ba.id
                WHERE ba.id = @id
                GROUP BY ba.id",
                new { id });
        }

        public async Task<IEnumerable<BankAccount>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Compute current_balance from opening_balance + credits - debits
            return await connection.QueryAsync<BankAccount>(@"
                SELECT
                    ba.*,
                    ba.opening_balance + COALESCE(SUM(CASE WHEN bt.transaction_type = 'credit' THEN bt.amount ELSE 0 END), 0)
                        - COALESCE(SUM(CASE WHEN bt.transaction_type = 'debit' THEN bt.amount ELSE 0 END), 0) AS current_balance
                FROM bank_accounts ba
                LEFT JOIN bank_transactions bt ON bt.bank_account_id = ba.id
                GROUP BY ba.id
                ORDER BY ba.bank_name, ba.account_name");
        }

        public async Task<(IEnumerable<BankAccount> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("bank_accounts", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "bank_name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<BankAccount>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<BankAccount> AddAsync(BankAccount entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO bank_accounts (
                    company_id, account_name, account_number, bank_name,
                    ifsc_code, branch_name, account_type, currency,
                    opening_balance, current_balance, as_of_date,
                    is_primary, is_active, notes,
                    created_at, updated_at
                )
                VALUES (
                    @CompanyId, @AccountName, @AccountNumber, @BankName,
                    @IfscCode, @BranchName, @AccountType, @Currency,
                    @OpeningBalance, @CurrentBalance, @AsOfDate,
                    @IsPrimary, @IsActive, @Notes,
                    NOW(), NOW()
                )
                RETURNING *";

            var createdEntity = await connection.QuerySingleAsync<BankAccount>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(BankAccount entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE bank_accounts SET
                    company_id = @CompanyId,
                    account_name = @AccountName,
                    account_number = @AccountNumber,
                    bank_name = @BankName,
                    ifsc_code = @IfscCode,
                    branch_name = @BranchName,
                    account_type = @AccountType,
                    currency = @Currency,
                    opening_balance = @OpeningBalance,
                    current_balance = @CurrentBalance,
                    as_of_date = @AsOfDate,
                    is_primary = @IsPrimary,
                    is_active = @IsActive,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM bank_accounts WHERE id = @id",
                new { id });
        }

        /// <summary>
        /// Get bank accounts by company ID
        /// </summary>
        public async Task<IEnumerable<BankAccount>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<BankAccount>(
                @"SELECT * FROM bank_accounts
                  WHERE company_id = @companyId
                  ORDER BY is_primary DESC, bank_name, account_name",
                new { companyId });
        }

        /// <summary>
        /// Get the primary bank account for a company
        /// </summary>
        public async Task<BankAccount?> GetPrimaryAccountAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankAccount>(
                @"SELECT * FROM bank_accounts
                  WHERE company_id = @companyId AND is_primary = true",
                new { companyId });
        }

        /// <summary>
        /// Get all active bank accounts, optionally filtered by company
        /// </summary>
        public async Task<IEnumerable<BankAccount>> GetActiveAccountsAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM bank_accounts WHERE is_active = true";
            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }
            sql += " ORDER BY is_primary DESC, bank_name, account_name";

            return await connection.QueryAsync<BankAccount>(sql, new { companyId });
        }

        /// <summary>
        /// Update the current balance of a bank account
        /// </summary>
        public async Task UpdateBalanceAsync(Guid id, decimal newBalance, DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE bank_accounts SET
                    current_balance = @newBalance,
                    as_of_date = @asOfDate,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, newBalance, asOfDate });
        }

        /// <summary>
        /// Set a bank account as the primary account for a company
        /// (Clears primary flag from all other accounts of the same company)
        /// </summary>
        public async Task SetPrimaryAccountAsync(Guid companyId, Guid accountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Clear primary flag from all accounts of this company
                await connection.ExecuteAsync(
                    @"UPDATE bank_accounts SET is_primary = false, updated_at = NOW()
                      WHERE company_id = @companyId",
                    new { companyId }, transaction);

                // Set the specified account as primary
                await connection.ExecuteAsync(
                    @"UPDATE bank_accounts SET is_primary = true, updated_at = NOW()
                      WHERE id = @accountId AND company_id = @companyId",
                    new { accountId, companyId }, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==================== Tally Integration ====================

        public async Task<BankAccount?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankAccount>(
                "SELECT * FROM bank_accounts WHERE company_id = @companyId AND tally_ledger_guid = @tallyLedgerGuid",
                new { companyId, tallyLedgerGuid });
        }

        public async Task<BankAccount?> GetByAccountNumberAsync(Guid companyId, string accountNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankAccount>(
                "SELECT * FROM bank_accounts WHERE company_id = @companyId AND account_number = @accountNumber",
                new { companyId, accountNumber });
        }

        public async Task<BankAccount?> GetByNameAsync(Guid companyId, string accountName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<BankAccount>(
                "SELECT * FROM bank_accounts WHERE company_id = @companyId AND account_name = @accountName",
                new { companyId, accountName });
        }
    }
}
