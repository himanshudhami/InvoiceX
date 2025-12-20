using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Ledger
{
    public class ChartOfAccountRepository : IChartOfAccountRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "account_code", "account_name",
            "parent_account_id", "depth_level", "sort_order",
            "account_type", "account_subtype", "schedule_reference",
            "normal_balance", "gst_treatment", "is_control_account",
            "is_system_account", "opening_balance", "current_balance",
            "is_active", "created_by", "created_at", "updated_at"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "account_code", "account_name", "account_type", "account_subtype"
        };

        public ChartOfAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<ChartOfAccount?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ChartOfAccount>(
                "SELECT * FROM chart_of_accounts WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<ChartOfAccount>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ChartOfAccount>(
                "SELECT * FROM chart_of_accounts ORDER BY account_code");
        }

        public async Task<(IEnumerable<ChartOfAccount> Items, int TotalCount)> GetPagedAsync(
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
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "account_code";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM chart_of_accounts
                {whereClause}
                ORDER BY {orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM chart_of_accounts
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<ChartOfAccount>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<ChartOfAccount> AddAsync(ChartOfAccount entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO chart_of_accounts (
                    company_id, account_code, account_name,
                    parent_account_id, depth_level, sort_order,
                    account_type, account_subtype, schedule_reference,
                    normal_balance, gst_treatment, is_control_account,
                    is_system_account, opening_balance, current_balance,
                    is_active, created_by, created_at, updated_at
                )
                VALUES (
                    @CompanyId, @AccountCode, @AccountName,
                    @ParentAccountId, @DepthLevel, @SortOrder,
                    @AccountType, @AccountSubtype, @ScheduleReference,
                    @NormalBalance, @GstTreatment, @IsControlAccount,
                    @IsSystemAccount, @OpeningBalance, @CurrentBalance,
                    @IsActive, @CreatedBy, NOW(), NOW()
                )
                RETURNING *";

            return await connection.QuerySingleAsync<ChartOfAccount>(sql, entity);
        }

        public async Task UpdateAsync(ChartOfAccount entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE chart_of_accounts SET
                    account_name = @AccountName,
                    parent_account_id = @ParentAccountId,
                    depth_level = @DepthLevel,
                    sort_order = @SortOrder,
                    account_subtype = @AccountSubtype,
                    schedule_reference = @ScheduleReference,
                    gst_treatment = @GstTreatment,
                    is_control_account = @IsControlAccount,
                    opening_balance = @OpeningBalance,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM chart_of_accounts WHERE id = @id AND is_system_account = FALSE",
                new { id });
        }

        // ==================== Company-Specific Queries ====================

        public async Task<IEnumerable<ChartOfAccount>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ChartOfAccount>(
                @"SELECT * FROM chart_of_accounts
                  WHERE company_id = @companyId
                  ORDER BY account_code",
                new { companyId });
        }

        public async Task<IEnumerable<ChartOfAccount>> GetByTypeAsync(Guid companyId, string accountType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ChartOfAccount>(
                @"SELECT * FROM chart_of_accounts
                  WHERE company_id = @companyId AND account_type = @accountType
                  ORDER BY account_code",
                new { companyId, accountType });
        }

        public async Task<ChartOfAccount?> GetByCodeAsync(Guid companyId, string accountCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ChartOfAccount>(
                @"SELECT * FROM chart_of_accounts
                  WHERE company_id = @companyId AND account_code = @accountCode",
                new { companyId, accountCode });
        }

        public async Task<IEnumerable<ChartOfAccount>> GetChildrenAsync(Guid parentAccountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ChartOfAccount>(
                @"SELECT * FROM chart_of_accounts
                  WHERE parent_account_id = @parentAccountId
                  ORDER BY sort_order, account_code",
                new { parentAccountId });
        }

        public async Task<IEnumerable<ChartOfAccount>> GetHierarchyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ChartOfAccount>(
                @"WITH RECURSIVE account_tree AS (
                    SELECT *, 1 as tree_level
                    FROM chart_of_accounts
                    WHERE company_id = @companyId AND parent_account_id IS NULL

                    UNION ALL

                    SELECT c.*, at.tree_level + 1
                    FROM chart_of_accounts c
                    INNER JOIN account_tree at ON c.parent_account_id = at.id
                )
                SELECT * FROM account_tree ORDER BY account_code",
                new { companyId });
        }

        // ==================== Balance Queries ====================

        public async Task<(ChartOfAccount Account, decimal Balance)?> GetWithBalanceAsync(Guid accountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var account = await connection.QueryFirstOrDefaultAsync<ChartOfAccount>(
                "SELECT * FROM chart_of_accounts WHERE id = @accountId",
                new { accountId });

            if (account == null) return null;

            return (account, account.CurrentBalance);
        }

        public async Task UpdateBalanceAsync(Guid accountId, decimal newBalance)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE chart_of_accounts
                  SET current_balance = @newBalance, updated_at = NOW()
                  WHERE id = @accountId",
                new { accountId, newBalance });
        }

        // ==================== Initialization ====================

        public async Task<bool> HasAccountsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM chart_of_accounts WHERE company_id = @companyId",
                new { companyId });
            return count > 0;
        }

        public async Task InitializeDefaultAccountsAsync(Guid companyId, Guid? createdBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Call the database function to create default chart of accounts
            await connection.ExecuteAsync(
                "SELECT create_default_chart_of_accounts(@companyId, @createdBy)",
                new { companyId, createdBy });
        }
    }
}
