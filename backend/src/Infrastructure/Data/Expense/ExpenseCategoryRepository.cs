using Core.Entities.Expense;
using Core.Interfaces.Expense;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Expense
{
    /// <summary>
    /// Dapper implementation of expense category repository.
    /// </summary>
    public class ExpenseCategoryRepository : IExpenseCategoryRepository
    {
        private readonly string _connectionString;

        public ExpenseCategoryRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<ExpenseCategory?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExpenseCategory>(
                @"SELECT id, company_id, name, code, description, is_active,
                         max_amount, requires_receipt, requires_approval,
                         gl_account_code, display_order, created_at, updated_at
                  FROM expense_categories
                  WHERE id = @id",
                new { id });
        }

        public async Task<ExpenseCategory?> GetByCodeAsync(Guid companyId, string code)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExpenseCategory>(
                @"SELECT id, company_id, name, code, description, is_active,
                         max_amount, requires_receipt, requires_approval,
                         gl_account_code, display_order, created_at, updated_at
                  FROM expense_categories
                  WHERE company_id = @companyId AND LOWER(code) = LOWER(@code)",
                new { companyId, code });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetByCompanyAsync(Guid companyId, bool includeInactive = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id, name, code, description, is_active,
                               max_amount, requires_receipt, requires_approval,
                               gl_account_code, display_order, created_at, updated_at
                        FROM expense_categories
                        WHERE company_id = @companyId";

            if (!includeInactive)
            {
                sql += " AND is_active = TRUE";
            }

            sql += " ORDER BY display_order, name";

            return await connection.QueryAsync<ExpenseCategory>(sql, new { companyId });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetActiveByCompanyAsync(Guid companyId)
        {
            return await GetByCompanyAsync(companyId, includeInactive: false);
        }

        public async Task<(IEnumerable<ExpenseCategory> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool includeInactive = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE company_id = @companyId";
            if (!includeInactive)
            {
                whereClause += " AND is_active = TRUE";
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClause += " AND (LOWER(name) LIKE LOWER(@searchTerm) OR LOWER(code) LIKE LOWER(@searchTerm) OR LOWER(description) LIKE LOWER(@searchTerm))";
            }

            var countSql = $"SELECT COUNT(*) FROM expense_categories {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new
            {
                companyId,
                searchTerm = $"%{searchTerm}%"
            });

            var offset = (pageNumber - 1) * pageSize;
            var dataSql = $@"SELECT id, company_id, name, code, description, is_active,
                                    max_amount, requires_receipt, requires_approval,
                                    gl_account_code, display_order, created_at, updated_at
                             FROM expense_categories
                             {whereClause}
                             ORDER BY display_order, name
                             LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<ExpenseCategory>(dataSql, new
            {
                companyId,
                searchTerm = $"%{searchTerm}%",
                pageSize,
                offset
            });

            return (items, totalCount);
        }

        public async Task<ExpenseCategory> AddAsync(ExpenseCategory category)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"INSERT INTO expense_categories
                        (id, company_id, name, code, description, is_active,
                         max_amount, requires_receipt, requires_approval,
                         gl_account_code, display_order, created_at, updated_at)
                        VALUES
                        (@Id, @CompanyId, @Name, @Code, @Description, @IsActive,
                         @MaxAmount, @RequiresReceipt, @RequiresApproval,
                         @GlAccountCode, @DisplayOrder, @CreatedAt, @UpdatedAt)
                        RETURNING *";

            category.Id = category.Id == Guid.Empty ? Guid.NewGuid() : category.Id;
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            return await connection.QuerySingleAsync<ExpenseCategory>(sql, category);
        }

        public async Task UpdateAsync(ExpenseCategory category)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            category.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE expense_categories
                  SET name = @Name,
                      code = @Code,
                      description = @Description,
                      is_active = @IsActive,
                      max_amount = @MaxAmount,
                      requires_receipt = @RequiresReceipt,
                      requires_approval = @RequiresApproval,
                      gl_account_code = @GlAccountCode,
                      display_order = @DisplayOrder,
                      updated_at = @UpdatedAt
                  WHERE id = @Id",
                category);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM expense_categories WHERE id = @id",
                new { id });
        }

        public async Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = "SELECT EXISTS(SELECT 1 FROM expense_categories WHERE company_id = @companyId AND LOWER(code) = LOWER(@code)";
            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
            }
            sql += ")";

            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, code, excludeId });
        }

        public async Task SeedDefaultCategoriesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "SELECT seed_default_expense_categories(@companyId)",
                new { companyId });
        }
    }
}
