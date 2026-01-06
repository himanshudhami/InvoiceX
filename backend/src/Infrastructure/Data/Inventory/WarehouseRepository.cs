using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Inventory
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly string _connectionString;

        public WarehouseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Warehouse?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "SELECT * FROM warehouses WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Warehouse>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Warehouse>(
                @"SELECT * FROM warehouses
                  WHERE company_id = @companyId
                  ORDER BY is_default DESC, name",
                new { companyId });
        }

        public async Task<(IEnumerable<Warehouse> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "code", "address", "city", "state", "pin_code",
                "is_default", "parent_warehouse_id", "is_active", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("warehouses", allowedColumns)
                .SearchAcross(new[] { "name", "code", "address", "city" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Warehouse>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Warehouse> AddAsync(Warehouse entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO warehouses (
                    id, company_id, name, code, address, city, state, pin_code,
                    is_default, parent_warehouse_id, tally_godown_guid, tally_godown_name,
                    is_active, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @Name, @Code, @Address, @City, @State, @PinCode,
                    @IsDefault, @ParentWarehouseId, @TallyGodownGuid, @TallyGodownName,
                    @IsActive, @CreatedAt, @UpdatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<Warehouse>(sql, entity);
        }

        public async Task UpdateAsync(Warehouse entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE warehouses SET
                    name = @Name,
                    code = @Code,
                    address = @Address,
                    city = @City,
                    state = @State,
                    pin_code = @PinCode,
                    is_default = @IsDefault,
                    parent_warehouse_id = @ParentWarehouseId,
                    tally_godown_guid = @TallyGodownGuid,
                    tally_godown_name = @TallyGodownName,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM warehouses WHERE id = @id", new { id });
        }

        public async Task<Warehouse?> GetDefaultAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "SELECT * FROM warehouses WHERE company_id = @companyId AND is_default = TRUE",
                new { companyId });
        }

        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Warehouse>("SELECT * FROM warehouses ORDER BY name");
        }

        public async Task<IEnumerable<Warehouse>> GetChildWarehousesAsync(Guid parentWarehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Warehouse>(
                "SELECT * FROM warehouses WHERE parent_warehouse_id = @parentWarehouseId ORDER BY name",
                new { parentWarehouseId });
        }

        public async Task<IEnumerable<Warehouse>> GetActiveAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Warehouse>(
                @"SELECT * FROM warehouses
                  WHERE company_id = @companyId AND is_active = TRUE
                  ORDER BY is_default DESC, name",
                new { companyId });
        }

        public async Task<bool> ExistsAsync(Guid companyId, string name, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM warehouses
                    WHERE company_id = @companyId
                      AND LOWER(name) = LOWER(@name)
                      AND (@excludeId IS NULL OR id != @excludeId)
                )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, name, excludeId });
        }

        public async Task<bool> HasMovementsAsync(Guid warehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM stock_movements WHERE warehouse_id = @warehouseId)",
                new { warehouseId });
        }

        public async Task SetDefaultAsync(Guid companyId, Guid warehouseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE warehouses SET is_default = FALSE WHERE company_id = @companyId;
                UPDATE warehouses SET is_default = TRUE WHERE id = @warehouseId;",
                new { companyId, warehouseId });
        }

        public async Task<Warehouse?> GetByTallyGuidAsync(string tallyGodownGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "SELECT * FROM warehouses WHERE tally_godown_guid = @tallyGodownGuid",
                new { tallyGodownGuid });
        }
    }
}
