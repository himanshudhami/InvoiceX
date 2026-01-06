using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly string _connectionString;

        public StockMovementRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockMovement?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<StockMovement>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE company_id = @companyId ORDER BY movement_date DESC, created_at DESC",
                new { companyId });
        }

        public async Task<(IEnumerable<StockMovement> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "stock_item_id", "warehouse_id", "batch_id",
                "movement_date", "movement_type", "quantity", "rate", "value",
                "source_type", "source_id", "source_number", "running_quantity", "running_value",
                "created_at"
            };

            var builder = SqlQueryBuilder
                .From("stock_movements", allowedColumns)
                .SearchAcross(new[] { "source_number", "notes" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "movement_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<StockMovement>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<StockMovement> AddAsync(StockMovement entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_movements (
                    id, company_id, stock_item_id, warehouse_id, batch_id,
                    movement_date, movement_type, quantity, rate, value,
                    source_type, source_id, source_number, journal_entry_id,
                    tally_voucher_guid, running_quantity, running_value, notes, created_at
                ) VALUES (
                    @Id, @CompanyId, @StockItemId, @WarehouseId, @BatchId,
                    @MovementDate, @MovementType, @Quantity, @Rate, @Value,
                    @SourceType, @SourceId, @SourceNumber, @JournalEntryId,
                    @TallyVoucherGuid, @RunningQuantity, @RunningValue, @Notes, @CreatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockMovement>(sql, entity);
        }

        public async Task<IEnumerable<StockMovement>> AddRangeAsync(IEnumerable<StockMovement> entities)
        {
            var results = new List<StockMovement>();
            foreach (var entity in entities)
            {
                results.Add(await AddAsync(entity));
            }
            return results;
        }

        public async Task UpdateAsync(StockMovement entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                UPDATE stock_movements SET
                    movement_date = @MovementDate,
                    movement_type = @MovementType,
                    quantity = @Quantity,
                    rate = @Rate,
                    value = @Value,
                    source_type = @SourceType,
                    source_id = @SourceId,
                    source_number = @SourceNumber,
                    journal_entry_id = @JournalEntryId,
                    running_quantity = @RunningQuantity,
                    running_value = @RunningValue,
                    notes = @Notes
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_movements WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<StockMovement>> GetStockLedgerAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT * FROM stock_movements
                WHERE stock_item_id = @stockItemId
                  AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                  AND (@fromDate IS NULL OR movement_date >= @fromDate)
                  AND (@toDate IS NULL OR movement_date <= @toDate)
                ORDER BY movement_date, created_at";

            return await connection.QueryAsync<StockMovement>(sql,
                new { stockItemId, warehouseId, fromDate, toDate });
        }

        public async Task<IEnumerable<StockMovement>> GetByStockItemIdAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE stock_item_id = @stockItemId ORDER BY movement_date, created_at",
                new { stockItemId });
        }

        public async Task<IEnumerable<StockMovement>> GetByWarehouseIdAsync(Guid warehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE warehouse_id = @warehouseId ORDER BY movement_date, created_at",
                new { warehouseId });
        }

        public async Task<IEnumerable<StockMovement>> GetByBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE batch_id = @batchId ORDER BY movement_date, created_at",
                new { batchId });
        }

        public async Task<IEnumerable<StockMovement>> GetBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                "SELECT * FROM stock_movements WHERE source_type = @sourceType AND source_id = @sourceId ORDER BY movement_date",
                new { sourceType, sourceId });
        }

        public async Task<IEnumerable<StockMovement>> GetByDateRangeAsync(Guid companyId, DateOnly fromDate, DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockMovement>(
                @"SELECT * FROM stock_movements
                  WHERE company_id = @companyId
                    AND movement_date >= @fromDate
                    AND movement_date <= @toDate
                  ORDER BY movement_date, created_at",
                new { companyId, fromDate, toDate });
        }

        public async Task<decimal> GetTotalQuantityAsync(Guid stockItemId, Guid? warehouseId = null, Guid? batchId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(quantity), 0)
                FROM stock_movements
                WHERE stock_item_id = @stockItemId
                  AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                  AND (@batchId IS NULL OR batch_id = @batchId)";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { stockItemId, warehouseId, batchId });
        }

        public async Task<decimal> GetTotalValueAsync(Guid stockItemId, Guid? warehouseId = null, Guid? batchId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(value), 0)
                FROM stock_movements
                WHERE stock_item_id = @stockItemId
                  AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                  AND (@batchId IS NULL OR batch_id = @batchId)";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { stockItemId, warehouseId, batchId });
        }

        public async Task<(decimal Quantity, decimal Value)> GetStockPositionAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? asOfDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT
                    COALESCE(SUM(quantity), 0) as Quantity,
                    COALESCE(SUM(value), 0) as Value
                FROM stock_movements
                WHERE stock_item_id = @stockItemId
                  AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                  AND (@asOfDate IS NULL OR movement_date <= @asOfDate)";

            var result = await connection.QueryFirstAsync<(decimal Quantity, decimal Value)>(sql,
                new { stockItemId, warehouseId, asOfDate });
            return result;
        }

        public async Task RecalculateRunningTotalsAsync(Guid stockItemId, Guid? warehouseId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                WITH ordered_movements AS (
                    SELECT id, quantity, value,
                           SUM(quantity) OVER (ORDER BY movement_date, created_at) as running_qty,
                           SUM(value) OVER (ORDER BY movement_date, created_at) as running_val
                    FROM stock_movements
                    WHERE stock_item_id = @stockItemId
                      AND (@warehouseId IS NULL OR warehouse_id = @warehouseId)
                )
                UPDATE stock_movements sm
                SET running_quantity = om.running_qty,
                    running_value = om.running_val
                FROM ordered_movements om
                WHERE sm.id = om.id";

            await connection.ExecuteAsync(sql, new { stockItemId, warehouseId });
        }

        public async Task DeleteBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM stock_movements WHERE source_type = @sourceType AND source_id = @sourceId",
                new { sourceType, sourceId });
        }

        public async Task<bool> HasMovementsForSourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_movements WHERE source_type = @sourceType AND source_id = @sourceId)",
                new { sourceType, sourceId });
        }
    }
}
