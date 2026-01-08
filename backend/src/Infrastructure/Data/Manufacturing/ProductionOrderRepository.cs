using Core.Entities.Manufacturing;
using Core.Interfaces.Manufacturing;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Manufacturing;

public class ProductionOrderRepository : IProductionOrderRepository
{
    private readonly string _connectionString;

    public ProductionOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProductionOrder?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name,
                   ur.name as released_by_name,
                   us.name as started_by_name,
                   uc.name as completed_by_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            LEFT JOIN users ur ON po.released_by = ur.id
            LEFT JOIN users us ON po.started_by = us.id
            LEFT JOIN users uc ON po.completed_by = uc.id
            WHERE po.id = @id";
        return await connection.QueryFirstOrDefaultAsync<ProductionOrder>(sql, new { id });
    }

    public async Task<ProductionOrder?> GetByIdWithItemsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name,
                   ur.name as released_by_name,
                   us.name as started_by_name,
                   uc.name as completed_by_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            LEFT JOIN users ur ON po.released_by = ur.id
            LEFT JOIN users us ON po.started_by = us.id
            LEFT JOIN users uc ON po.completed_by = uc.id
            WHERE po.id = @id;

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
            WHERE poi.production_order_id = @id
            ORDER BY poi.created_at";

        using var multi = await connection.QueryMultipleAsync(sql, new { id });
        var order = await multi.ReadFirstOrDefaultAsync<ProductionOrder>();
        if (order != null)
        {
            order.Items = (await multi.ReadAsync<ProductionOrderItem>()).ToList();
        }
        return order;
    }

    public async Task<IEnumerable<ProductionOrder>> GetAllAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            WHERE po.company_id = @companyId
            ORDER BY po.created_at DESC";
        return await connection.QueryAsync<ProductionOrder>(sql, new { companyId });
    }

    public async Task<IEnumerable<ProductionOrder>> GetByStatusAsync(Guid companyId, string status)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            WHERE po.company_id = @companyId AND po.status = @status
            ORDER BY po.created_at DESC";
        return await connection.QueryAsync<ProductionOrder>(sql, new { companyId, status });
    }

    public async Task<IEnumerable<ProductionOrder>> GetByBomAsync(Guid bomId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            WHERE po.bom_id = @bomId
            ORDER BY po.created_at DESC";
        return await connection.QueryAsync<ProductionOrder>(sql, new { bomId });
    }

    public async Task<IEnumerable<ProductionOrder>> GetByFinishedGoodAsync(Guid finishedGoodId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            WHERE po.finished_good_id = @finishedGoodId
            ORDER BY po.created_at DESC";
        return await connection.QueryAsync<ProductionOrder>(sql, new { finishedGoodId });
    }

    public async Task<(IEnumerable<ProductionOrder> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        string? status = null, Guid? bomId = null, Guid? finishedGoodId = null,
        Guid? warehouseId = null, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (companyId.HasValue)
        {
            whereConditions.Add("po.company_id = @companyId");
            parameters.Add("companyId", companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereConditions.Add("(po.order_number ILIKE @searchTerm OR si.name ILIKE @searchTerm)");
            parameters.Add("searchTerm", $"%{searchTerm}%");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereConditions.Add("po.status = @status");
            parameters.Add("status", status);
        }

        if (bomId.HasValue)
        {
            whereConditions.Add("po.bom_id = @bomId");
            parameters.Add("bomId", bomId.Value);
        }

        if (finishedGoodId.HasValue)
        {
            whereConditions.Add("po.finished_good_id = @finishedGoodId");
            parameters.Add("finishedGoodId", finishedGoodId.Value);
        }

        if (warehouseId.HasValue)
        {
            whereConditions.Add("po.warehouse_id = @warehouseId");
            parameters.Add("warehouseId", warehouseId.Value);
        }

        if (fromDate.HasValue)
        {
            whereConditions.Add("po.planned_start_date >= @fromDate");
            parameters.Add("fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereConditions.Add("po.planned_start_date <= @toDate");
            parameters.Add("toDate", toDate.Value);
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var countSql = $@"
            SELECT COUNT(*) FROM production_orders po
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            {whereClause}";

        var dataSql = $@"
            SELECT po.*,
                   b.name as bom_name,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   w.name as warehouse_name
            FROM production_orders po
            LEFT JOIN bill_of_materials b ON po.bom_id = b.id
            LEFT JOIN stock_items si ON po.finished_good_id = si.id
            LEFT JOIN warehouses w ON po.warehouse_id = w.id
            {whereClause}
            ORDER BY po.created_at DESC
            OFFSET @offset LIMIT @limit";

        parameters.Add("offset", (pageNumber - 1) * pageSize);
        parameters.Add("limit", pageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<ProductionOrder>(dataSql, parameters);

        return (items, totalCount);
    }

    public async Task<ProductionOrder> AddAsync(ProductionOrder entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            INSERT INTO production_orders (id, company_id, order_number, bom_id, finished_good_id,
                warehouse_id, planned_quantity, actual_quantity, planned_start_date, planned_end_date,
                actual_start_date, actual_end_date, status, notes, released_by, released_at,
                started_by, started_at, completed_by, completed_at, cancelled_by, cancelled_at, created_at)
            VALUES (@Id, @CompanyId, @OrderNumber, @BomId, @FinishedGoodId,
                @WarehouseId, @PlannedQuantity, @ActualQuantity, @PlannedStartDate, @PlannedEndDate,
                @ActualStartDate, @ActualEndDate, @Status, @Notes, @ReleasedBy, @ReleasedAt,
                @StartedBy, @StartedAt, @CompletedBy, @CompletedAt, @CancelledBy, @CancelledAt, @CreatedAt)
            RETURNING *";
        return await connection.QuerySingleAsync<ProductionOrder>(sql, entity);
    }

    public async Task UpdateAsync(ProductionOrder entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE production_orders SET
                bom_id = @BomId,
                finished_good_id = @FinishedGoodId,
                warehouse_id = @WarehouseId,
                planned_quantity = @PlannedQuantity,
                actual_quantity = @ActualQuantity,
                planned_start_date = @PlannedStartDate,
                planned_end_date = @PlannedEndDate,
                actual_start_date = @ActualStartDate,
                actual_end_date = @ActualEndDate,
                status = @Status,
                notes = @Notes,
                released_by = @ReleasedBy,
                released_at = @ReleasedAt,
                started_by = @StartedBy,
                started_at = @StartedAt,
                completed_by = @CompletedBy,
                completed_at = @CompletedAt,
                cancelled_by = @CancelledBy,
                cancelled_at = @CancelledAt,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid? userId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE production_orders SET
                status = @status,
                updated_at = @updatedAt
            WHERE id = @id";
        await connection.ExecuteAsync(sql, new { id, status, updatedAt = DateTime.UtcNow });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM production_orders WHERE id = @id", new { id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM production_orders WHERE id = @id)", new { id });
    }

    public async Task<string> GenerateOrderNumberAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var year = DateTime.UtcNow.Year;
        var sql = @"
            SELECT COALESCE(MAX(CAST(SUBSTRING(order_number FROM 'PO-\d{4}-(\d+)') AS INTEGER)), 0) + 1
            FROM production_orders
            WHERE company_id = @companyId AND order_number LIKE @pattern";
        var nextNumber = await connection.ExecuteScalarAsync<int>(sql, new { companyId, pattern = $"PO-{year}-%" });
        return $"PO-{year}-{nextNumber:D5}";
    }
}
