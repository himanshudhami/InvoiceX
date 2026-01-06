using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Inventory
{
    public class StockTransferItemRepository : IStockTransferItemRepository
    {
        private readonly string _connectionString;

        public StockTransferItemRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StockTransferItem?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockTransferItem>(
                "SELECT * FROM stock_transfer_items WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<StockTransferItem>> GetByTransferIdAsync(Guid stockTransferId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockTransferItem>(
                @"SELECT sti.*, si.name as StockItemName, si.sku as StockItemSku, sb.batch_number as BatchNumber
                  FROM stock_transfer_items sti
                  LEFT JOIN stock_items si ON sti.stock_item_id = si.id
                  LEFT JOIN stock_batches sb ON sti.batch_id = sb.id
                  WHERE sti.stock_transfer_id = @stockTransferId",
                new { stockTransferId });
        }

        public async Task<StockTransferItem> AddAsync(StockTransferItem entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO stock_transfer_items (
                    id, stock_transfer_id, stock_item_id, batch_id,
                    quantity, rate, value, received_quantity, created_at
                ) VALUES (
                    @Id, @StockTransferId, @StockItemId, @BatchId,
                    @Quantity, @Rate, @Value, @ReceivedQuantity, @CreatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<StockTransferItem>(sql, entity);
        }

        public async Task<IEnumerable<StockTransferItem>> AddRangeAsync(IEnumerable<StockTransferItem> entities)
        {
            var results = new List<StockTransferItem>();
            foreach (var entity in entities)
            {
                results.Add(await AddAsync(entity));
            }
            return results;
        }

        public async Task UpdateAsync(StockTransferItem entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                UPDATE stock_transfer_items SET
                    stock_item_id = @StockItemId,
                    batch_id = @BatchId,
                    quantity = @Quantity,
                    rate = @Rate,
                    value = @Value,
                    received_quantity = @ReceivedQuantity
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM stock_transfer_items WHERE id = @id", new { id });
        }

        public async Task DeleteByTransferIdAsync(Guid stockTransferId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM stock_transfer_items WHERE stock_transfer_id = @stockTransferId",
                new { stockTransferId });
        }

        public async Task<IEnumerable<StockTransferItem>> GetByStockItemIdAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StockTransferItem>(
                "SELECT * FROM stock_transfer_items WHERE stock_item_id = @stockItemId",
                new { stockItemId });
        }

        public async Task<decimal> GetTotalQuantityByTransferAsync(Guid stockTransferId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(quantity), 0) FROM stock_transfer_items WHERE stock_transfer_id = @stockTransferId",
                new { stockTransferId });
        }

        public async Task<decimal> GetTotalValueByTransferAsync(Guid stockTransferId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(value), 0) FROM stock_transfer_items WHERE stock_transfer_id = @stockTransferId",
                new { stockTransferId });
        }

        public async Task UpdateReceivedQuantityAsync(Guid id, decimal receivedQuantity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE stock_transfer_items SET
                    received_quantity = @receivedQuantity
                WHERE id = @id",
                new { id, receivedQuantity });
        }

        public async Task<bool> AllItemsReceivedAsync(Guid stockTransferId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT NOT EXISTS(
                    SELECT 1 FROM stock_transfer_items
                    WHERE stock_transfer_id = @stockTransferId
                      AND (received_quantity IS NULL OR received_quantity < quantity)
                )",
                new { stockTransferId });
        }

        public async Task ReplaceItemsAsync(Guid stockTransferId, IEnumerable<StockTransferItem> items)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Delete existing items
                await connection.ExecuteAsync(
                    "DELETE FROM stock_transfer_items WHERE stock_transfer_id = @stockTransferId",
                    new { stockTransferId },
                    transaction);

                // Insert new items
                foreach (var item in items)
                {
                    item.Id = Guid.NewGuid();
                    item.StockTransferId = stockTransferId;
                    item.CreatedAt = DateTime.UtcNow;

                    await connection.ExecuteAsync(@"
                        INSERT INTO stock_transfer_items (
                            id, stock_transfer_id, stock_item_id, batch_id,
                            quantity, rate, value, received_quantity, created_at
                        ) VALUES (
                            @Id, @StockTransferId, @StockItemId, @BatchId,
                            @Quantity, @Rate, @Value, @ReceivedQuantity, @CreatedAt
                        )",
                        item,
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
