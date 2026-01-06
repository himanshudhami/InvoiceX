using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class StockItemRepository : IStockItemRepository
    {
        private readonly string _connectionString;

        public StockItemRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockItem?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockItem>(
                "SELECT * FROM stock_items WHERE id = @id",
                new { id });
        }

        public async Task<StockItem?> GetByIdWithDetailsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT si.*, sg.name as StockGroupName, uom.name as UnitName, uom.symbol as UnitSymbol
                FROM stock_items si
                LEFT JOIN stock_groups sg ON si.stock_group_id = sg.id
                LEFT JOIN units_of_measure uom ON si.base_unit_id = uom.id
                WHERE si.id = @id";
            return await connection.QueryFirstOrDefaultAsync<StockItem>(sql, new { id });
        }

        public async Task<IEnumerable<StockItem>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>("SELECT * FROM stock_items ORDER BY name");
        }

        public async Task<IEnumerable<StockItem>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>(
                "SELECT * FROM stock_items WHERE company_id = @companyId ORDER BY name",
                new { companyId });
        }

        public async Task<(IEnumerable<StockItem> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "sku", "description", "stock_group_id",
                "base_unit_id", "hsn_sac_code", "gst_rate", "current_quantity", "current_value",
                "reorder_level", "is_batch_enabled", "valuation_method", "is_active",
                "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("stock_items", allowedColumns)
                .SearchAcross(new[] { "name", "sku", "description", "hsn_sac_code" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<StockItem>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<StockItem> AddAsync(StockItem entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_items (
                    id, company_id, name, sku, description, stock_group_id,
                    base_unit_id, hsn_sac_code, gst_rate, opening_quantity, opening_value,
                    current_quantity, current_value, reorder_level, reorder_quantity,
                    is_batch_enabled, valuation_method, tally_stock_item_guid, tally_stock_item_name,
                    is_active, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @Name, @Sku, @Description, @StockGroupId,
                    @BaseUnitId, @HsnSacCode, @GstRate, @OpeningQuantity, @OpeningValue,
                    @CurrentQuantity, @CurrentValue, @ReorderLevel, @ReorderQuantity,
                    @IsBatchEnabled, @ValuationMethod, @TallyStockItemGuid, @TallyStockItemName,
                    @IsActive, @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockItem>(sql, entity);
        }

        public async Task UpdateAsync(StockItem entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE stock_items SET
                    name = @Name,
                    sku = @Sku,
                    description = @Description,
                    stock_group_id = @StockGroupId,
                    base_unit_id = @BaseUnitId,
                    hsn_sac_code = @HsnSacCode,
                    gst_rate = @GstRate,
                    reorder_level = @ReorderLevel,
                    reorder_quantity = @ReorderQuantity,
                    is_batch_enabled = @IsBatchEnabled,
                    valuation_method = @ValuationMethod,
                    tally_stock_item_guid = @TallyStockItemGuid,
                    tally_stock_item_name = @TallyStockItemName,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_items WHERE id = @id", new { id });
        }

        public async Task<StockItem?> GetBySkuAsync(Guid companyId, string sku)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockItem>(
                "SELECT * FROM stock_items WHERE company_id = @companyId AND sku = @sku",
                new { companyId, sku });
        }

        public async Task<IEnumerable<StockItem>> GetByStockGroupIdAsync(Guid stockGroupId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>(
                "SELECT * FROM stock_items WHERE stock_group_id = @stockGroupId ORDER BY name",
                new { stockGroupId });
        }

        public async Task<IEnumerable<StockItem>> GetActiveAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>(
                "SELECT * FROM stock_items WHERE company_id = @companyId AND is_active = TRUE ORDER BY name",
                new { companyId });
        }

        public async Task<IEnumerable<StockItem>> GetLowStockItemsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>(
                @"SELECT * FROM stock_items
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND reorder_level IS NOT NULL
                    AND current_quantity <= reorder_level
                  ORDER BY name",
                new { companyId });
        }

        public async Task<IEnumerable<StockItem>> GetBatchEnabledItemsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockItem>(
                @"SELECT * FROM stock_items
                  WHERE company_id = @companyId AND is_batch_enabled = TRUE AND is_active = TRUE
                  ORDER BY name",
                new { companyId });
        }

        public async Task<bool> ExistsAsync(Guid companyId, string name, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM stock_items
                    WHERE company_id = @companyId
                      AND LOWER(name) = LOWER(@name)
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, name, excludeId });
        }

        public async Task<bool> SkuExistsAsync(Guid companyId, string sku, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM stock_items
                    WHERE company_id = @companyId
                      AND sku = @sku
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, sku, excludeId });
        }

        public async Task<bool> HasMovementsAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_movements WHERE stock_item_id = @stockItemId)",
                new { stockItemId });
        }

        public async Task UpdateCurrentStockAsync(Guid stockItemId, decimal quantity, decimal value)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_items SET
                    current_quantity = @quantity,
                    current_value = @value,
                    updated_at = NOW()
                WHERE id = @stockItemId",
                new { stockItemId, quantity, value });
        }

        public async Task RecalculateStockAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_items SET
                    current_quantity = COALESCE((
                        SELECT SUM(quantity) FROM stock_movements WHERE stock_item_id = @stockItemId
                    ), 0) + opening_quantity,
                    current_value = COALESCE((
                        SELECT SUM(value) FROM stock_movements WHERE stock_item_id = @stockItemId
                    ), 0) + opening_value,
                    updated_at = NOW()
                WHERE id = @stockItemId",
                new { stockItemId });
        }

        public async Task<StockItem?> GetByTallyGuidAsync(string tallyStockItemGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockItem>(
                "SELECT * FROM stock_items WHERE tally_stock_item_guid = @tallyStockItemGuid",
                new { tallyStockItemGuid });
        }
    }
}
