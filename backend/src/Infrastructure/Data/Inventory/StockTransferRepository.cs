using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class StockTransferRepository : IStockTransferRepository
    {
        private readonly string _connectionString;

        public StockTransferRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockTransfer?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockTransfer>(
                "SELECT * FROM stock_transfers WHERE id = @id",
                new { id });
        }

        public async Task<StockTransfer?> GetByIdWithItemsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT st.*,
                       fw.name as FromWarehouseName,
                       tw.name as ToWarehouseName
                FROM stock_transfers st
                LEFT JOIN warehouses fw ON st.from_warehouse_id = fw.id
                LEFT JOIN warehouses tw ON st.to_warehouse_id = tw.id
                WHERE st.id = @id;

                SELECT sti.*, si.name as StockItemName, si.sku as StockItemSku
                FROM stock_transfer_items sti
                LEFT JOIN stock_items si ON sti.stock_item_id = si.id
                WHERE sti.stock_transfer_id = @id";

            using var multi = await connection.QueryMultipleAsync(sql, new { id });
            var transfer = await multi.ReadFirstOrDefaultAsync<StockTransfer>();
            if (transfer != null)
            {
                transfer.Items = (await multi.ReadAsync<StockTransferItem>()).ToList();
            }
            return transfer;
        }

        public async Task<IEnumerable<StockTransfer>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockTransfer>(
                "SELECT * FROM stock_transfers WHERE company_id = @companyId ORDER BY transfer_date DESC, transfer_number DESC",
                new { companyId });
        }

        public async Task<(IEnumerable<StockTransfer> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "transfer_number", "transfer_date",
                "from_warehouse_id", "to_warehouse_id", "status",
                "created_at", "updated_at", "completed_at"
            };

            var builder = SqlQueryBuilder
                .From("stock_transfers", allowedColumns)
                .SearchAcross(new[] { "transfer_number", "notes" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "transfer_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<StockTransfer>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<StockTransfer> AddAsync(StockTransfer entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_transfers (
                    id, company_id, transfer_number, transfer_date,
                    from_warehouse_id, to_warehouse_id, status, notes,
                    tally_voucher_guid, created_by, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @TransferNumber, @TransferDate,
                    @FromWarehouseId, @ToWarehouseId, @Status, @Notes,
                    @TallyVoucherGuid, @CreatedBy, @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockTransfer>(sql, entity);
        }

        public async Task UpdateAsync(StockTransfer entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE stock_transfers SET
                    transfer_number = @TransferNumber,
                    transfer_date = @TransferDate,
                    from_warehouse_id = @FromWarehouseId,
                    to_warehouse_id = @ToWarehouseId,
                    status = @Status,
                    notes = @Notes,
                    tally_voucher_guid = @TallyVoucherGuid,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_transfers WHERE id = @id", new { id });
        }

        public async Task<StockTransfer?> GetByTransferNumberAsync(Guid companyId, string transferNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockTransfer>(
                "SELECT * FROM stock_transfers WHERE company_id = @companyId AND transfer_number = @transferNumber",
                new { companyId, transferNumber });
        }

        public async Task<IEnumerable<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool asSource = true)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = asSource
                ? "SELECT * FROM stock_transfers WHERE from_warehouse_id = @warehouseId ORDER BY transfer_date DESC"
                : "SELECT * FROM stock_transfers WHERE to_warehouse_id = @warehouseId ORDER BY transfer_date DESC";
            return await connection.QueryAsync<StockTransfer>(sql, new { warehouseId });
        }

        public async Task<IEnumerable<StockTransfer>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockTransfer>(
                "SELECT * FROM stock_transfers WHERE company_id = @companyId AND status = @status ORDER BY transfer_date DESC",
                new { companyId, status });
        }

        public async Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockTransfer>(
                "SELECT * FROM stock_transfers WHERE company_id = @companyId AND status IN ('draft', 'in_transit') ORDER BY transfer_date",
                new { companyId });
        }

        public async Task<bool> ExistsAsync(Guid companyId, string transferNumber, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM stock_transfers
                    WHERE company_id = @companyId
                      AND transfer_number = @transferNumber
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, transferNumber, excludeId });
        }

        public async Task UpdateStatusAsync(Guid id, string status, Guid? userId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_transfers SET
                    status = @status,
                    updated_at = NOW()
                WHERE id = @id",
                new { id, status });
        }

        public async Task CompleteTransferAsync(Guid id, Guid completedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_transfers SET
                    status = 'completed',
                    completed_at = NOW(),
                    updated_at = NOW()
                WHERE id = @id",
                new { id, completedBy });
        }

        public async Task CancelTransferAsync(Guid id, Guid cancelledBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_transfers SET
                    status = 'cancelled',
                    updated_at = NOW()
                WHERE id = @id",
                new { id, cancelledBy });
        }

        public async Task<string> GenerateTransferNumberAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var year = DateTime.UtcNow.Year.ToString();
            var sql = @"
                SELECT COALESCE(MAX(CAST(SUBSTRING(transfer_number FROM 'ST-\d{4}-(\d+)') AS INTEGER)), 0) + 1
                FROM stock_transfers
                WHERE company_id = @companyId
                  AND transfer_number LIKE @pattern";

            var nextNumber = await connection.ExecuteScalarAsync<int>(sql,
                new { companyId, pattern = $"ST-{year}-%" });

            return $"ST-{year}-{nextNumber:D5}";
        }
    }
}
