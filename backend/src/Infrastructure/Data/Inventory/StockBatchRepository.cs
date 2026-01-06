using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class StockBatchRepository : IStockBatchRepository
    {
        private readonly string _connectionString;

        public StockBatchRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockBatch?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockBatch>(
                "SELECT * FROM stock_batches WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<StockBatch>> GetByStockItemIdAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockBatch>(
                "SELECT * FROM stock_batches WHERE stock_item_id = @stockItemId ORDER BY expiry_date, batch_number",
                new { stockItemId });
        }

        public async Task<IEnumerable<StockBatch>> GetByWarehouseIdAsync(Guid warehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockBatch>(
                "SELECT * FROM stock_batches WHERE warehouse_id = @warehouseId ORDER BY expiry_date, batch_number",
                new { warehouseId });
        }

        public async Task<IEnumerable<StockBatch>> GetByStockItemAndWarehouseAsync(Guid stockItemId, Guid warehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockBatch>(
                @"SELECT * FROM stock_batches
                  WHERE stock_item_id = @stockItemId AND warehouse_id = @warehouseId
                  ORDER BY expiry_date, batch_number",
                new { stockItemId, warehouseId });
        }

        public async Task<(IEnumerable<StockBatch> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "stock_item_id", "warehouse_id", "batch_number",
                "manufacturing_date", "expiry_date", "quantity", "value",
                "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("stock_batches", allowedColumns)
                .SearchAcross(new[] { "batch_number" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "expiry_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<StockBatch>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<StockBatch> AddAsync(StockBatch entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_batches (
                    id, stock_item_id, warehouse_id, batch_number,
                    manufacturing_date, expiry_date, quantity, value,
                    tally_batch_guid, created_at, updated_at
                ) VALUES (
                    @Id, @StockItemId, @WarehouseId, @BatchNumber,
                    @ManufacturingDate, @ExpiryDate, @Quantity, @Value,
                    @TallyBatchGuid, @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockBatch>(sql, entity);
        }

        public async Task UpdateAsync(StockBatch entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE stock_batches SET
                    batch_number = @BatchNumber,
                    manufacturing_date = @ManufacturingDate,
                    expiry_date = @ExpiryDate,
                    quantity = @Quantity,
                    value = @Value,
                    tally_batch_guid = @TallyBatchGuid,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_batches WHERE id = @id", new { id });
        }

        public async Task<StockBatch?> GetByBatchNumberAsync(Guid stockItemId, Guid warehouseId, string batchNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockBatch>(
                @"SELECT * FROM stock_batches
                  WHERE stock_item_id = @stockItemId
                    AND warehouse_id = @warehouseId
                    AND batch_number = @batchNumber",
                new { stockItemId, warehouseId, batchNumber });
        }

        public async Task<IEnumerable<StockBatch>> GetExpiringBatchesAsync(Guid companyId, int daysFromNow)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockBatch>(
                @"SELECT sb.* FROM stock_batches sb
                  INNER JOIN stock_items si ON sb.stock_item_id = si.id
                  WHERE si.company_id = @companyId
                    AND sb.expiry_date IS NOT NULL
                    AND sb.expiry_date <= CURRENT_DATE + @daysFromNow
                    AND sb.expiry_date > CURRENT_DATE
                    AND sb.quantity > 0
                  ORDER BY sb.expiry_date",
                new { companyId, daysFromNow });
        }

        public async Task<IEnumerable<StockBatch>> GetExpiredBatchesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockBatch>(
                @"SELECT sb.* FROM stock_batches sb
                  INNER JOIN stock_items si ON sb.stock_item_id = si.id
                  WHERE si.company_id = @companyId
                    AND sb.expiry_date IS NOT NULL
                    AND sb.expiry_date < CURRENT_DATE
                    AND sb.quantity > 0
                  ORDER BY sb.expiry_date",
                new { companyId });
        }

        public async Task<IEnumerable<StockBatch>> GetBatchesWithStockAsync(Guid stockItemId, Guid? warehouseId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT * FROM stock_batches
                WHERE stock_item_id = @stockItemId
                  AND quantity > 0
                  AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                ORDER BY expiry_date NULLS LAST, manufacturing_date, batch_number";
            return await connection.QueryAsync<StockBatch>(sql, new { stockItemId, warehouseId });
        }

        public async Task<bool> ExistsAsync(Guid stockItemId, Guid warehouseId, string batchNumber, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM stock_batches
                    WHERE stock_item_id = @stockItemId
                      AND warehouse_id = @warehouseId
                      AND batch_number = @batchNumber
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql,
                new { stockItemId, warehouseId, batchNumber, excludeId });
        }

        public async Task<bool> HasMovementsAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_movements WHERE batch_id = @batchId)",
                new { batchId });
        }

        public async Task UpdateQuantityAsync(Guid batchId, decimal quantityChange, decimal valueChange)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_batches SET
                    quantity = quantity + @quantityChange,
                    value = value + @valueChange,
                    updated_at = NOW()
                WHERE id = @batchId",
                new { batchId, quantityChange, valueChange });
        }
    }
}
