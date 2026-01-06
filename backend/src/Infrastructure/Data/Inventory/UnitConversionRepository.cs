using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Inventory
{
    public class UnitConversionRepository : IUnitConversionRepository
    {
        private readonly string _connectionString;

        public UnitConversionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UnitConversion?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UnitConversion>(
                "SELECT * FROM unit_conversions WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<UnitConversion>> GetByStockItemIdAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UnitConversion>(
                @"SELECT uc.*,
                         fu.name as FromUnitName, fu.symbol as FromUnitSymbol,
                         tu.name as ToUnitName, tu.symbol as ToUnitSymbol
                  FROM unit_conversions uc
                  LEFT JOIN units_of_measure fu ON uc.from_unit_id = fu.id
                  LEFT JOIN units_of_measure tu ON uc.to_unit_id = tu.id
                  WHERE uc.stock_item_id = @stockItemId",
                new { stockItemId });
        }

        public async Task<UnitConversion> AddAsync(UnitConversion entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO unit_conversions (
                    id, stock_item_id, from_unit_id, to_unit_id, conversion_factor, created_at
                ) VALUES (
                    @Id, @StockItemId, @FromUnitId, @ToUnitId, @ConversionFactor, @CreatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<UnitConversion>(sql, entity);
        }

        public async Task UpdateAsync(UnitConversion entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                UPDATE unit_conversions SET
                    from_unit_id = @FromUnitId,
                    to_unit_id = @ToUnitId,
                    conversion_factor = @ConversionFactor
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM unit_conversions WHERE id = @id", new { id });
        }

        public async Task DeleteByStockItemIdAsync(Guid stockItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM unit_conversions WHERE stock_item_id = @stockItemId",
                new { stockItemId });
        }

        public async Task<UnitConversion?> GetConversionAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UnitConversion>(
                @"SELECT * FROM unit_conversions
                  WHERE stock_item_id = @stockItemId
                    AND from_unit_id = @fromUnitId
                    AND to_unit_id = @toUnitId",
                new { stockItemId, fromUnitId, toUnitId });
        }

        public async Task<decimal?> GetConversionFactorAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Direct conversion
            var factor = await connection.ExecuteScalarAsync<decimal?>(
                @"SELECT conversion_factor FROM unit_conversions
                  WHERE stock_item_id = @stockItemId
                    AND from_unit_id = @fromUnitId
                    AND to_unit_id = @toUnitId",
                new { stockItemId, fromUnitId, toUnitId });

            if (factor.HasValue) return factor;

            // Reverse conversion
            var reverseFactor = await connection.ExecuteScalarAsync<decimal?>(
                @"SELECT conversion_factor FROM unit_conversions
                  WHERE stock_item_id = @stockItemId
                    AND from_unit_id = @toUnitId
                    AND to_unit_id = @fromUnitId",
                new { stockItemId, fromUnitId, toUnitId });

            if (reverseFactor.HasValue && reverseFactor.Value != 0)
                return 1 / reverseFactor.Value;

            return null;
        }

        public async Task<bool> ExistsAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM unit_conversions
                    WHERE stock_item_id = @stockItemId
                      AND from_unit_id = @fromUnitId
                      AND to_unit_id = @toUnitId
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql,
                new { stockItemId, fromUnitId, toUnitId, excludeId });
        }

        public async Task<IEnumerable<UnitConversion>> AddRangeAsync(IEnumerable<UnitConversion> entities)
        {
            var results = new List<UnitConversion>();
            foreach (var entity in entities)
            {
                results.Add(await AddAsync(entity));
            }
            return results;
        }

        public async Task ReplaceConversionsAsync(Guid stockItemId, IEnumerable<UnitConversion> conversions)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Delete existing conversions
                await connection.ExecuteAsync(
                    "DELETE FROM unit_conversions WHERE stock_item_id = @stockItemId",
                    new { stockItemId },
                    transaction);

                // Insert new conversions
                foreach (var conversion in conversions)
                {
                    conversion.Id = Guid.NewGuid();
                    conversion.StockItemId = stockItemId;
                    conversion.CreatedAt = DateTime.UtcNow;

                    await connection.ExecuteAsync(@"
                        INSERT INTO unit_conversions (
                            id, stock_item_id, from_unit_id, to_unit_id, conversion_factor, created_at
                        ) VALUES (
                            @Id, @StockItemId, @FromUnitId, @ToUnitId, @ConversionFactor, @CreatedAt
                        )",
                        conversion,
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
