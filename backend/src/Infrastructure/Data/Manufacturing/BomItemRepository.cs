using Core.Entities.Manufacturing;
using Core.Interfaces.Manufacturing;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Manufacturing;

public class BomItemRepository : IBomItemRepository
{
    private readonly string _connectionString;

    public BomItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<BomItem?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT bi.*,
                   si.name as component_name, si.sku as component_sku,
                   u.name as unit_name, u.symbol as unit_symbol
            FROM bom_items bi
            LEFT JOIN stock_items si ON bi.component_id = si.id
            LEFT JOIN units_of_measure u ON bi.unit_id = u.id
            WHERE bi.id = @id";
        return await connection.QueryFirstOrDefaultAsync<BomItem>(sql, new { id });
    }

    public async Task<IEnumerable<BomItem>> GetByBomIdAsync(Guid bomId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT bi.*,
                   si.name as component_name, si.sku as component_sku,
                   u.name as unit_name, u.symbol as unit_symbol
            FROM bom_items bi
            LEFT JOIN stock_items si ON bi.component_id = si.id
            LEFT JOIN units_of_measure u ON bi.unit_id = u.id
            WHERE bi.bom_id = @bomId
            ORDER BY bi.sequence, bi.created_at";
        return await connection.QueryAsync<BomItem>(sql, new { bomId });
    }

    public async Task<BomItem> AddAsync(BomItem entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            INSERT INTO bom_items (id, bom_id, component_id, quantity, unit_id,
                scrap_percentage, is_optional, sequence, notes, created_at)
            VALUES (@Id, @BomId, @ComponentId, @Quantity, @UnitId,
                @ScrapPercentage, @IsOptional, @Sequence, @Notes, @CreatedAt)
            RETURNING *";
        return await connection.QuerySingleAsync<BomItem>(sql, entity);
    }

    public async Task<IEnumerable<BomItem>> AddRangeAsync(IEnumerable<BomItem> entities)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var results = new List<BomItem>();
        foreach (var entity in entities)
        {
            var sql = @"
                INSERT INTO bom_items (id, bom_id, component_id, quantity, unit_id,
                    scrap_percentage, is_optional, sequence, notes, created_at)
                VALUES (@Id, @BomId, @ComponentId, @Quantity, @UnitId,
                    @ScrapPercentage, @IsOptional, @Sequence, @Notes, @CreatedAt)
                RETURNING *";
            var result = await connection.QuerySingleAsync<BomItem>(sql, entity, transaction);
            results.Add(result);
        }

        await transaction.CommitAsync();
        return results;
    }

    public async Task UpdateAsync(BomItem entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE bom_items SET
                component_id = @ComponentId,
                quantity = @Quantity,
                unit_id = @UnitId,
                scrap_percentage = @ScrapPercentage,
                is_optional = @IsOptional,
                sequence = @Sequence,
                notes = @Notes
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM bom_items WHERE id = @id", new { id });
    }

    public async Task DeleteByBomIdAsync(Guid bomId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM bom_items WHERE bom_id = @bomId", new { bomId });
    }

    public async Task ReplaceItemsAsync(Guid bomId, IEnumerable<BomItem> items)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync("DELETE FROM bom_items WHERE bom_id = @bomId", new { bomId }, transaction);

        foreach (var item in items)
        {
            var sql = @"
                INSERT INTO bom_items (id, bom_id, component_id, quantity, unit_id,
                    scrap_percentage, is_optional, sequence, notes, created_at)
                VALUES (@Id, @BomId, @ComponentId, @Quantity, @UnitId,
                    @ScrapPercentage, @IsOptional, @Sequence, @Notes, @CreatedAt)";
            await connection.ExecuteAsync(sql, item, transaction);
        }

        await transaction.CommitAsync();
    }

    public async Task<int> GetItemCountAsync(Guid bomId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM bom_items WHERE bom_id = @bomId", new { bomId });
    }
}
