using Core.Entities.Manufacturing;
using Core.Interfaces.Manufacturing;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Manufacturing;

public class ProductionOrderItemRepository : IProductionOrderItemRepository
{
    private readonly string _connectionString;

    public ProductionOrderItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProductionOrderItem?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT poi.*,
                   si.name as component_name, si.sku as component_sku,
                   u.name as unit_name, u.symbol as unit_symbol,
                   sb.batch_number,
                   w.name as warehouse_name
            FROM production_order_items poi
            LEFT JOIN stock_items si ON poi.component_id = si.id
            LEFT JOIN units_of_measure u ON poi.unit_id = u.id
            LEFT JOIN stock_batches sb ON poi.batch_id = sb.id
            LEFT JOIN warehouses w ON poi.warehouse_id = w.id
            WHERE poi.id = @id";
        return await connection.QueryFirstOrDefaultAsync<ProductionOrderItem>(sql, new { id });
    }

    public async Task<IEnumerable<ProductionOrderItem>> GetByOrderIdAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT poi.*,
                   si.name as component_name, si.sku as component_sku,
                   u.name as unit_name, u.symbol as unit_symbol,
                   sb.batch_number,
                   w.name as warehouse_name
            FROM production_order_items poi
            LEFT JOIN stock_items si ON poi.component_id = si.id
            LEFT JOIN units_of_measure u ON poi.unit_id = u.id
            LEFT JOIN stock_batches sb ON poi.batch_id = sb.id
            LEFT JOIN warehouses w ON poi.warehouse_id = w.id
            WHERE poi.production_order_id = @productionOrderId
            ORDER BY poi.created_at";
        return await connection.QueryAsync<ProductionOrderItem>(sql, new { productionOrderId });
    }

    public async Task<ProductionOrderItem> AddAsync(ProductionOrderItem entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            INSERT INTO production_order_items (id, production_order_id, component_id,
                planned_quantity, consumed_quantity, unit_id, batch_id, warehouse_id, notes, created_at)
            VALUES (@Id, @ProductionOrderId, @ComponentId,
                @PlannedQuantity, @ConsumedQuantity, @UnitId, @BatchId, @WarehouseId, @Notes, @CreatedAt)
            RETURNING *";
        return await connection.QuerySingleAsync<ProductionOrderItem>(sql, entity);
    }

    public async Task<IEnumerable<ProductionOrderItem>> AddRangeAsync(IEnumerable<ProductionOrderItem> entities)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var results = new List<ProductionOrderItem>();
        foreach (var entity in entities)
        {
            var sql = @"
                INSERT INTO production_order_items (id, production_order_id, component_id,
                    planned_quantity, consumed_quantity, unit_id, batch_id, warehouse_id, notes, created_at)
                VALUES (@Id, @ProductionOrderId, @ComponentId,
                    @PlannedQuantity, @ConsumedQuantity, @UnitId, @BatchId, @WarehouseId, @Notes, @CreatedAt)
                RETURNING *";
            var result = await connection.QuerySingleAsync<ProductionOrderItem>(sql, entity, transaction);
            results.Add(result);
        }

        await transaction.CommitAsync();
        return results;
    }

    public async Task UpdateAsync(ProductionOrderItem entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE production_order_items SET
                component_id = @ComponentId,
                planned_quantity = @PlannedQuantity,
                consumed_quantity = @ConsumedQuantity,
                unit_id = @UnitId,
                batch_id = @BatchId,
                warehouse_id = @WarehouseId,
                notes = @Notes
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task UpdateConsumedQuantityAsync(Guid id, decimal consumedQuantity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE production_order_items SET consumed_quantity = @consumedQuantity WHERE id = @id",
            new { id, consumedQuantity });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM production_order_items WHERE id = @id", new { id });
    }

    public async Task DeleteByOrderIdAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM production_order_items WHERE production_order_id = @productionOrderId",
            new { productionOrderId });
    }

    public async Task ReplaceItemsAsync(Guid productionOrderId, IEnumerable<ProductionOrderItem> items)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(
            "DELETE FROM production_order_items WHERE production_order_id = @productionOrderId",
            new { productionOrderId }, transaction);

        foreach (var item in items)
        {
            var sql = @"
                INSERT INTO production_order_items (id, production_order_id, component_id,
                    planned_quantity, consumed_quantity, unit_id, batch_id, warehouse_id, notes, created_at)
                VALUES (@Id, @ProductionOrderId, @ComponentId,
                    @PlannedQuantity, @ConsumedQuantity, @UnitId, @BatchId, @WarehouseId, @Notes, @CreatedAt)";
            await connection.ExecuteAsync(sql, item, transaction);
        }

        await transaction.CommitAsync();
    }

    public async Task<decimal> GetTotalPlannedQuantityAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(planned_quantity), 0) FROM production_order_items WHERE production_order_id = @productionOrderId",
            new { productionOrderId });
    }

    public async Task<decimal> GetTotalConsumedQuantityAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(consumed_quantity), 0) FROM production_order_items WHERE production_order_id = @productionOrderId",
            new { productionOrderId });
    }

    public async Task<bool> AllItemsConsumedAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        // Check if all items have consumed_quantity >= planned_quantity * 0.9 (allow 10% variance)
        var sql = @"
            SELECT NOT EXISTS(
                SELECT 1 FROM production_order_items
                WHERE production_order_id = @productionOrderId
                  AND consumed_quantity < planned_quantity * 0.9
            )";
        return await connection.ExecuteScalarAsync<bool>(sql, new { productionOrderId });
    }
}
