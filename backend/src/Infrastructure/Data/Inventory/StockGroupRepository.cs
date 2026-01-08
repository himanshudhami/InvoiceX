using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class StockGroupRepository : IStockGroupRepository
    {
        private readonly string _connectionString;

        public StockGroupRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockGroup?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<StockGroup>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE company_id = @companyId ORDER BY name",
                new { companyId });
        }

        public async Task<(IEnumerable<StockGroup> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "parent_stock_group_id",
                "is_active", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("stock_groups", allowedColumns)
                .SearchAcross(new[] { "name" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<StockGroup>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<StockGroup> AddAsync(StockGroup entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_groups (
                    id, company_id, name, parent_stock_group_id,
                    tally_stock_group_guid, tally_stock_group_name,
                    is_active, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @Name, @ParentStockGroupId,
                    @TallyStockGroupGuid, @TallyStockGroupName,
                    @IsActive, @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockGroup>(sql, entity);
        }

        public async Task UpdateAsync(StockGroup entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE stock_groups SET
                    name = @Name,
                    parent_stock_group_id = @ParentStockGroupId,
                    tally_stock_group_guid = @TallyStockGroupGuid,
                    tally_stock_group_name = @TallyStockGroupName,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_groups WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<StockGroup>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockGroup>("SELECT * FROM stock_groups ORDER BY name");
        }

        public async Task<IEnumerable<StockGroup>> GetChildGroupsAsync(Guid parentStockGroupId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE parent_stock_group_id = @parentStockGroupId ORDER BY name",
                new { parentStockGroupId });
        }

        public async Task<IEnumerable<StockGroup>> GetRootGroupsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockGroup>(
                @"SELECT * FROM stock_groups
                  WHERE company_id = @companyId AND parent_stock_group_id IS NULL
                  ORDER BY name",
                new { companyId });
        }

        public async Task<IEnumerable<StockGroup>> GetHierarchyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                WITH RECURSIVE group_tree AS (
                    SELECT *, 0 as level
                    FROM stock_groups
                    WHERE company_id = @companyId AND parent_stock_group_id IS NULL

                    UNION ALL

                    SELECT sg.*, gt.level + 1
                    FROM stock_groups sg
                    INNER JOIN group_tree gt ON sg.parent_stock_group_id = gt.id
                )
                SELECT * FROM group_tree ORDER BY level, name";

            return await connection.QueryAsync<StockGroup>(sql, new { companyId });
        }

        public async Task<IEnumerable<StockGroup>> GetActiveAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE company_id = @companyId AND is_active = TRUE ORDER BY name",
                new { companyId });
        }

        public async Task<bool> ExistsAsync(Guid companyId, string name, Guid? parentGroupId = null, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM stock_groups
                    WHERE company_id = @companyId
                      AND LOWER(name) = LOWER(@name)
                      AND (parent_stock_group_id = @parentGroupId OR (parent_stock_group_id IS NULL AND @parentGroupId IS NULL))
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, name, parentGroupId, excludeId });
        }

        public async Task<bool> HasItemsAsync(Guid stockGroupId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_items WHERE stock_group_id = @stockGroupId)",
                new { stockGroupId });
        }

        public async Task<bool> HasChildrenAsync(Guid stockGroupId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_groups WHERE parent_stock_group_id = @stockGroupId)",
                new { stockGroupId });
        }

        public async Task<string> GetFullPathAsync(Guid stockGroupId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                WITH RECURSIVE path AS (
                    SELECT id, name, parent_stock_group_id, name::TEXT as full_path
                    FROM stock_groups
                    WHERE id = @stockGroupId

                    UNION ALL

                    SELECT sg.id, sg.name, sg.parent_stock_group_id, sg.name || ' > ' || p.full_path
                    FROM stock_groups sg
                    INNER JOIN path p ON sg.id = p.parent_stock_group_id
                )
                SELECT full_path FROM path WHERE parent_stock_group_id IS NULL";

            return await connection.ExecuteScalarAsync<string>(sql, new { stockGroupId }) ?? "";
        }

        public async Task<StockGroup?> GetByTallyGuidAsync(string tallyStockGroupGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE tally_stock_group_guid = @tallyStockGroupGuid",
                new { tallyStockGroupGuid });
        }

        public async Task<StockGroup?> GetByTallyGuidAsync(Guid companyId, string tallyStockGroupGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE company_id = @companyId AND tally_stock_group_guid = @tallyStockGroupGuid",
                new { companyId, tallyStockGroupGuid });
        }

        public async Task<StockGroup?> GetByNameAsync(Guid companyId, string name)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockGroup>(
                "SELECT * FROM stock_groups WHERE company_id = @companyId AND LOWER(name) = LOWER(@name)",
                new { companyId, name });
        }
    }
}
