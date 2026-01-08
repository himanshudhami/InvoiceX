using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Inventory;

public class SerialNumberRepository : ISerialNumberRepository
{
    private readonly string _connectionString;

    public SerialNumberRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SerialNumber?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name,
                   sb.batch_number,
                   po.order_number as production_order_number
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            LEFT JOIN stock_batches sb ON sn.batch_id = sb.id
            LEFT JOIN production_orders po ON sn.production_order_id = po.id
            WHERE sn.id = @id";
        return await connection.QueryFirstOrDefaultAsync<SerialNumber>(sql, new { id });
    }

    public async Task<SerialNumber?> GetBySerialNoAsync(Guid companyId, Guid stockItemId, string serialNo)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.company_id = @companyId
              AND sn.stock_item_id = @stockItemId
              AND sn.serial_no = @serialNo";
        return await connection.QueryFirstOrDefaultAsync<SerialNumber>(sql, new { companyId, stockItemId, serialNo });
    }

    public async Task<IEnumerable<SerialNumber>> GetAllAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.company_id = @companyId
            ORDER BY sn.created_at DESC";
        return await connection.QueryAsync<SerialNumber>(sql, new { companyId });
    }

    public async Task<IEnumerable<SerialNumber>> GetByStockItemAsync(Guid stockItemId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.stock_item_id = @stockItemId
            ORDER BY sn.serial_no";
        return await connection.QueryAsync<SerialNumber>(sql, new { stockItemId });
    }

    public async Task<IEnumerable<SerialNumber>> GetByWarehouseAsync(Guid warehouseId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.warehouse_id = @warehouseId
            ORDER BY sn.serial_no";
        return await connection.QueryAsync<SerialNumber>(sql, new { warehouseId });
    }

    public async Task<IEnumerable<SerialNumber>> GetByStatusAsync(Guid companyId, string status)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.company_id = @companyId AND sn.status = @status
            ORDER BY sn.serial_no";
        return await connection.QueryAsync<SerialNumber>(sql, new { companyId, status });
    }

    public async Task<IEnumerable<SerialNumber>> GetAvailableAsync(Guid stockItemId, Guid? warehouseId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.stock_item_id = @stockItemId
              AND sn.status = 'available'
              AND (@warehouseId IS NULL OR sn.warehouse_id = @warehouseId)
            ORDER BY sn.serial_no";
        return await connection.QueryAsync<SerialNumber>(sql, new { stockItemId, warehouseId });
    }

    public async Task<IEnumerable<SerialNumber>> GetByProductionOrderAsync(Guid productionOrderId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            WHERE sn.production_order_id = @productionOrderId
            ORDER BY sn.serial_no";
        return await connection.QueryAsync<SerialNumber>(sql, new { productionOrderId });
    }

    public async Task<(IEnumerable<SerialNumber> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, Guid? stockItemId = null,
        Guid? warehouseId = null, string? status = null, string? searchTerm = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (companyId.HasValue)
        {
            whereConditions.Add("sn.company_id = @companyId");
            parameters.Add("companyId", companyId.Value);
        }

        if (stockItemId.HasValue)
        {
            whereConditions.Add("sn.stock_item_id = @stockItemId");
            parameters.Add("stockItemId", stockItemId.Value);
        }

        if (warehouseId.HasValue)
        {
            whereConditions.Add("sn.warehouse_id = @warehouseId");
            parameters.Add("warehouseId", warehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereConditions.Add("sn.status = @status");
            parameters.Add("status", status);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereConditions.Add("(sn.serial_no ILIKE @searchTerm OR si.name ILIKE @searchTerm)");
            parameters.Add("searchTerm", $"%{searchTerm}%");
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var countSql = $@"
            SELECT COUNT(*) FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            {whereClause}";

        var dataSql = $@"
            SELECT sn.*,
                   si.name as stock_item_name, si.sku as stock_item_sku,
                   w.name as warehouse_name
            FROM serial_numbers sn
            LEFT JOIN stock_items si ON sn.stock_item_id = si.id
            LEFT JOIN warehouses w ON sn.warehouse_id = w.id
            {whereClause}
            ORDER BY sn.serial_no
            OFFSET @offset LIMIT @limit";

        parameters.Add("offset", (pageNumber - 1) * pageSize);
        parameters.Add("limit", pageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<SerialNumber>(dataSql, parameters);

        return (items, totalCount);
    }

    public async Task<SerialNumber> AddAsync(SerialNumber entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            INSERT INTO serial_numbers (id, company_id, stock_item_id, serial_no, warehouse_id,
                batch_id, status, manufacturing_date, warranty_expiry, production_order_id,
                sold_at, sold_invoice_id, notes, created_at)
            VALUES (@Id, @CompanyId, @StockItemId, @SerialNo, @WarehouseId,
                @BatchId, @Status, @ManufacturingDate, @WarrantyExpiry, @ProductionOrderId,
                @SoldAt, @SoldInvoiceId, @Notes, @CreatedAt)
            RETURNING *";
        return await connection.QuerySingleAsync<SerialNumber>(sql, entity);
    }

    public async Task<IEnumerable<SerialNumber>> AddRangeAsync(IEnumerable<SerialNumber> entities)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var results = new List<SerialNumber>();
        foreach (var entity in entities)
        {
            var sql = @"
                INSERT INTO serial_numbers (id, company_id, stock_item_id, serial_no, warehouse_id,
                    batch_id, status, manufacturing_date, warranty_expiry, production_order_id,
                    sold_at, sold_invoice_id, notes, created_at)
                VALUES (@Id, @CompanyId, @StockItemId, @SerialNo, @WarehouseId,
                    @BatchId, @Status, @ManufacturingDate, @WarrantyExpiry, @ProductionOrderId,
                    @SoldAt, @SoldInvoiceId, @Notes, @CreatedAt)
                RETURNING *";
            var result = await connection.QuerySingleAsync<SerialNumber>(sql, entity, transaction);
            results.Add(result);
        }

        await transaction.CommitAsync();
        return results;
    }

    public async Task UpdateAsync(SerialNumber entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE serial_numbers SET
                warehouse_id = @WarehouseId,
                batch_id = @BatchId,
                status = @Status,
                manufacturing_date = @ManufacturingDate,
                warranty_expiry = @WarrantyExpiry,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE serial_numbers SET status = @status, updated_at = @updatedAt WHERE id = @id",
            new { id, status, updatedAt = DateTime.UtcNow });
    }

    public async Task MarkAsSoldAsync(Guid id, Guid invoiceId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            @"UPDATE serial_numbers SET
                status = 'sold',
                sold_at = @soldAt,
                sold_invoice_id = @invoiceId,
                updated_at = @updatedAt
              WHERE id = @id",
            new { id, invoiceId, soldAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM serial_numbers WHERE id = @id", new { id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM serial_numbers WHERE id = @id)", new { id });
    }

    public async Task<bool> SerialNoExistsAsync(Guid companyId, Guid stockItemId, string serialNo, Guid? excludeId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = excludeId.HasValue
            ? "SELECT EXISTS(SELECT 1 FROM serial_numbers WHERE company_id = @companyId AND stock_item_id = @stockItemId AND serial_no = @serialNo AND id != @excludeId)"
            : "SELECT EXISTS(SELECT 1 FROM serial_numbers WHERE company_id = @companyId AND stock_item_id = @stockItemId AND serial_no = @serialNo)";
        return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, stockItemId, serialNo, excludeId });
    }

    public async Task<int> GetCountByItemAsync(Guid stockItemId, string? status = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = status != null
            ? "SELECT COUNT(*) FROM serial_numbers WHERE stock_item_id = @stockItemId AND status = @status"
            : "SELECT COUNT(*) FROM serial_numbers WHERE stock_item_id = @stockItemId";
        return await connection.ExecuteScalarAsync<int>(sql, new { stockItemId, status });
    }
}
