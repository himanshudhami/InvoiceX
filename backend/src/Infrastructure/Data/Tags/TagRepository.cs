using Core.Entities.Tags;
using Core.Interfaces.Tags;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Tags
{
    public class TagRepository : ITagRepository
    {
        private readonly string _connectionString;

        public TagRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<Tag?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Tag>(
                "SELECT * FROM tags WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                "SELECT * FROM tags ORDER BY tag_group, sort_order, name");
        }

        public async Task<IEnumerable<Tag>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE company_id = @companyId
                  ORDER BY tag_group, sort_order, name",
                new { companyId });
        }

        public async Task<Tag> AddAsync(Tag entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO tags (
                    id, company_id, name, code, tag_group, description,
                    parent_tag_id, color, icon, sort_order,
                    budget_amount, budget_period, budget_year,
                    tally_cost_center_guid, tally_cost_center_name,
                    is_active, created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @CompanyId, @Name, @Code, @TagGroup, @Description,
                    @ParentTagId, @Color, @Icon, @SortOrder,
                    @BudgetAmount, @BudgetPeriod, @BudgetYear,
                    @TallyCostCenterGuid, @TallyCostCenterName,
                    @IsActive, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<Tag>(sql, entity);
        }

        public async Task UpdateAsync(Tag entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE tags SET
                    name = @Name,
                    code = @Code,
                    tag_group = @TagGroup,
                    description = @Description,
                    parent_tag_id = @ParentTagId,
                    color = @Color,
                    icon = @Icon,
                    sort_order = @SortOrder,
                    budget_amount = @BudgetAmount,
                    budget_period = @BudgetPeriod,
                    budget_year = @BudgetYear,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM tags WHERE id = @id", new { id });
        }

        // ==================== Paged/Filtered ====================

        public async Task<(IEnumerable<Tag> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "code", "tag_group", "description",
                "parent_tag_id", "full_path", "level", "color", "icon", "sort_order",
                "budget_amount", "budget_period", "budget_year", "is_active",
                "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("tags", allowedColumns)
                .SearchAcross(new[] { "name", "code", "description", "full_path" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "sort_order";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Tag>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        // ==================== By Group ====================

        public async Task<IEnumerable<Tag>> GetByGroupAsync(Guid companyId, string tagGroup)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE company_id = @companyId AND tag_group = @tagGroup
                  ORDER BY sort_order, name",
                new { companyId, tagGroup });
        }

        public async Task<IEnumerable<Tag>> GetActiveByGroupAsync(Guid companyId, string tagGroup)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE company_id = @companyId AND tag_group = @tagGroup AND is_active = TRUE
                  ORDER BY sort_order, name",
                new { companyId, tagGroup });
        }

        // ==================== Hierarchy ====================

        public async Task<IEnumerable<Tag>> GetChildrenAsync(Guid parentTagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE parent_tag_id = @parentTagId
                  ORDER BY sort_order, name",
                new { parentTagId });
        }

        public async Task<IEnumerable<Tag>> GetRootTagsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE company_id = @companyId AND parent_tag_id IS NULL
                  ORDER BY tag_group, sort_order, name",
                new { companyId });
        }

        public async Task<IEnumerable<Tag>> GetTagHierarchyAsync(Guid companyId, string? tagGroup = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                WITH RECURSIVE tag_tree AS (
                    -- Base case: root tags
                    SELECT *, 0 as tree_level
                    FROM tags
                    WHERE company_id = @companyId
                      AND parent_tag_id IS NULL
                      AND (@tagGroup IS NULL OR tag_group = @tagGroup)

                    UNION ALL

                    -- Recursive case: children
                    SELECT t.*, tt.tree_level + 1
                    FROM tags t
                    INNER JOIN tag_tree tt ON t.parent_tag_id = tt.id
                )
                SELECT * FROM tag_tree
                ORDER BY tag_group, tree_level, sort_order, name";

            return await connection.QueryAsync<Tag>(sql, new { companyId, tagGroup });
        }

        // ==================== Lookups ====================

        public async Task<Tag?> GetByNameAsync(Guid companyId, string name, Guid? parentTagId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = parentTagId.HasValue
                ? "SELECT * FROM tags WHERE company_id = @companyId AND name = @name AND parent_tag_id = @parentTagId"
                : "SELECT * FROM tags WHERE company_id = @companyId AND name = @name AND parent_tag_id IS NULL";
            return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { companyId, name, parentTagId });
        }

        public async Task<Tag?> GetByNameAndGroupAsync(Guid companyId, string name, string tagGroup)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Tag>(
                @"SELECT * FROM tags
                  WHERE company_id = @companyId
                    AND name = @name
                    AND tag_group = @tagGroup
                    AND is_active = true",
                new { companyId, name, tagGroup });
        }

        public async Task<Tag?> GetByCodeAsync(Guid companyId, string code)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Tag>(
                "SELECT * FROM tags WHERE company_id = @companyId AND code = @code",
                new { companyId, code });
        }

        public async Task<Tag?> GetByTallyGuidAsync(string tallyCostCenterGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Tag>(
                "SELECT * FROM tags WHERE tally_cost_center_guid = @tallyCostCenterGuid",
                new { tallyCostCenterGuid });
        }

        // ==================== Validation ====================

        public async Task<bool> ExistsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM tags WHERE id = @id)",
                new { id });
        }

        public async Task<bool> NameExistsAsync(Guid companyId, string name, Guid? parentTagId = null, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM tags
                    WHERE company_id = @companyId
                      AND name = @name
                      AND (parent_tag_id = @parentTagId OR (parent_tag_id IS NULL AND @parentTagId IS NULL))
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, name, parentTagId, excludeId });
        }

        public async Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM tags
                    WHERE company_id = @companyId
                      AND code = @code
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, code, excludeId });
        }

        public async Task<bool> HasChildrenAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM tags WHERE parent_tag_id = @tagId)",
                new { tagId });
        }

        public async Task<bool> HasTransactionsAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM transaction_tags WHERE tag_id = @tagId)",
                new { tagId });
        }

        // ==================== Statistics ====================

        public async Task<int> GetTransactionCountAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM transaction_tags WHERE tag_id = @tagId",
                new { tagId });
        }

        public async Task<decimal> GetTotalAllocatedAmountAsync(Guid tagId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(allocated_amount), 0)
                FROM transaction_tags
                WHERE tag_id = @tagId
                  AND (@fromDate IS NULL OR created_at >= @fromDate)
                  AND (@toDate IS NULL OR created_at <= @toDate)";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { tagId, fromDate, toDate });
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<Tag>> AddManyAsync(IEnumerable<Tag> tags)
        {
            var results = new List<Tag>();
            foreach (var tag in tags)
            {
                results.Add(await AddAsync(tag));
            }
            return results;
        }

        public async Task UpdateManyAsync(IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                await UpdateAsync(tag);
            }
        }

        public async Task SeedDefaultTagsAsync(Guid companyId, Guid? createdBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "SELECT seed_default_tags(@companyId, @createdBy)",
                new { companyId, createdBy });
        }

        public async Task<Tag?> GetByTallyCostCenterGuidAsync(Guid companyId, string tallyCostCenterGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Tag>(
                "SELECT * FROM tags WHERE company_id = @companyId AND tally_cost_center_guid = @tallyCostCenterGuid",
                new { companyId, tallyCostCenterGuid });
        }
    }
}
