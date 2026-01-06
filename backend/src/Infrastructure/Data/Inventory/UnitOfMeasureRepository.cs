using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class UnitOfMeasureRepository : IUnitOfMeasureRepository
    {
        private readonly string _connectionString;

        public UnitOfMeasureRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UnitOfMeasure?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "SELECT * FROM units_of_measure WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<UnitOfMeasure>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UnitOfMeasure>(
                "SELECT * FROM units_of_measure ORDER BY is_system_unit DESC, name");
        }

        public async Task<IEnumerable<UnitOfMeasure>> GetByCompanyIdAsync(Guid? companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UnitOfMeasure>(
                @"SELECT * FROM units_of_measure
                  WHERE company_id = @companyId OR is_system_unit = TRUE
                  ORDER BY is_system_unit DESC, name",
                new { companyId });
        }

        public async Task<(IEnumerable<UnitOfMeasure> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "symbol", "decimal_places",
                "is_system_unit", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("units_of_measure", allowedColumns)
                .SearchAcross(new[] { "name", "symbol" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<UnitOfMeasure>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<UnitOfMeasure> AddAsync(UnitOfMeasure entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO units_of_measure (
                    id, company_id, name, symbol, decimal_places,
                    is_system_unit, tally_unit_guid, tally_unit_name,
                    created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @Name, @Symbol, @DecimalPlaces,
                    @IsSystemUnit, @TallyUnitGuid, @TallyUnitName,
                    @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<UnitOfMeasure>(sql, entity);
        }

        public async Task UpdateAsync(UnitOfMeasure entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE units_of_measure SET
                    name = @Name,
                    symbol = @Symbol,
                    decimal_places = @DecimalPlaces,
                    tally_unit_guid = @TallyUnitGuid,
                    tally_unit_name = @TallyUnitName,
                    updated_at = @UpdatedAt
                WHERE id = @Id AND is_system_unit = FALSE";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM units_of_measure WHERE id = @id AND is_system_unit = FALSE",
                new { id });
        }

        public async Task<IEnumerable<UnitOfMeasure>> GetSystemUnitsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UnitOfMeasure>(
                "SELECT * FROM units_of_measure WHERE is_system_unit = TRUE ORDER BY name");
        }

        public async Task<IEnumerable<UnitOfMeasure>> GetAllAvailableAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UnitOfMeasure>(
                @"SELECT * FROM units_of_measure
                  WHERE company_id = @companyId OR is_system_unit = TRUE
                  ORDER BY is_system_unit DESC, name",
                new { companyId });
        }

        public async Task<UnitOfMeasure?> GetBySymbolAsync(string symbol, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            if (companyId.HasValue)
            {
                return await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                    @"SELECT * FROM units_of_measure
                      WHERE LOWER(symbol) = LOWER(@symbol)
                        AND (company_id = @companyId OR is_system_unit = TRUE)",
                    new { symbol, companyId });
            }
            return await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "SELECT * FROM units_of_measure WHERE LOWER(symbol) = LOWER(@symbol)",
                new { symbol });
        }

        public async Task<UnitOfMeasure?> GetByNameAsync(Guid? companyId, string name)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                @"SELECT * FROM units_of_measure
                  WHERE LOWER(name) = LOWER(@name)
                    AND (company_id = @companyId OR is_system_unit = TRUE)",
                new { companyId, name });
        }

        public async Task<bool> ExistsAsync(string symbol, Guid? companyId = null, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM units_of_measure
                    WHERE LOWER(symbol) = LOWER(@symbol)
                      AND (company_id = @companyId OR is_system_unit = TRUE)
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, symbol, excludeId });
        }

        public async Task<bool> IsInUseAsync(Guid unitId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM stock_items WHERE base_unit_id = @unitId
                    UNION
                    SELECT 1 FROM unit_conversions WHERE from_unit_id = @unitId OR to_unit_id = @unitId
                )",
                new { unitId });
        }

        public async Task<UnitOfMeasure?> GetByTallyGuidAsync(string tallyUnitGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "SELECT * FROM units_of_measure WHERE tally_unit_guid = @tallyUnitGuid",
                new { tallyUnitGuid });
        }
    }
}
