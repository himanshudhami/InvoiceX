using Core.Entities.Manufacturing;
using Core.Interfaces.Manufacturing;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Manufacturing;

public class BomRepository : IBomRepository
{
    private readonly string _connectionString;

    public BomRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<BillOfMaterials?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.id = @id";
        return await connection.QueryFirstOrDefaultAsync<BillOfMaterials>(sql, new { id });
    }

    public async Task<BillOfMaterials?> GetByIdWithItemsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.id = @id;

            SELECT bi.*,
                   si.name as component_name, si.sku as component_sku,
                   u.name as unit_name, u.symbol as unit_symbol
            FROM bom_items bi
            LEFT JOIN stock_items si ON bi.component_id = si.id
            LEFT JOIN units_of_measure u ON bi.unit_id = u.id
            WHERE bi.bom_id = @id
            ORDER BY bi.sequence, bi.created_at";

        using var multi = await connection.QueryMultipleAsync(sql, new { id });
        var bom = await multi.ReadFirstOrDefaultAsync<BillOfMaterials>();
        if (bom != null)
        {
            bom.Items = (await multi.ReadAsync<BomItem>()).ToList();
        }
        return bom;
    }

    public async Task<IEnumerable<BillOfMaterials>> GetAllAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.company_id = @companyId
            ORDER BY b.name";
        return await connection.QueryAsync<BillOfMaterials>(sql, new { companyId });
    }

    public async Task<IEnumerable<BillOfMaterials>> GetActiveAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.company_id = @companyId AND b.is_active = true
            ORDER BY b.name";
        return await connection.QueryAsync<BillOfMaterials>(sql, new { companyId });
    }

    public async Task<IEnumerable<BillOfMaterials>> GetByFinishedGoodAsync(Guid finishedGoodId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.finished_good_id = @finishedGoodId
            ORDER BY b.version DESC, b.created_at DESC";
        return await connection.QueryAsync<BillOfMaterials>(sql, new { finishedGoodId });
    }

    public async Task<BillOfMaterials?> GetActiveBomForProductAsync(Guid finishedGoodId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            WHERE b.finished_good_id = @finishedGoodId
              AND b.is_active = true
              AND (b.effective_from IS NULL OR b.effective_from <= CURRENT_DATE)
              AND (b.effective_to IS NULL OR b.effective_to >= CURRENT_DATE)
            ORDER BY b.created_at DESC
            LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<BillOfMaterials>(sql, new { finishedGoodId });
    }

    public async Task<(IEnumerable<BillOfMaterials> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        Guid? finishedGoodId = null, bool? isActive = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (companyId.HasValue)
        {
            whereConditions.Add("b.company_id = @companyId");
            parameters.Add("companyId", companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereConditions.Add("(b.name ILIKE @searchTerm OR b.code ILIKE @searchTerm OR si.name ILIKE @searchTerm)");
            parameters.Add("searchTerm", $"%{searchTerm}%");
        }

        if (finishedGoodId.HasValue)
        {
            whereConditions.Add("b.finished_good_id = @finishedGoodId");
            parameters.Add("finishedGoodId", finishedGoodId.Value);
        }

        if (isActive.HasValue)
        {
            whereConditions.Add("b.is_active = @isActive");
            parameters.Add("isActive", isActive.Value);
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var countSql = $@"
            SELECT COUNT(*) FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            {whereClause}";

        var dataSql = $@"
            SELECT b.*,
                   si.name as finished_good_name, si.sku as finished_good_sku,
                   u.name as output_unit_name, u.symbol as output_unit_symbol
            FROM bill_of_materials b
            LEFT JOIN stock_items si ON b.finished_good_id = si.id
            LEFT JOIN units_of_measure u ON b.output_unit_id = u.id
            {whereClause}
            ORDER BY b.name
            OFFSET @offset LIMIT @limit";

        parameters.Add("offset", (pageNumber - 1) * pageSize);
        parameters.Add("limit", pageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<BillOfMaterials>(dataSql, parameters);

        return (items, totalCount);
    }

    public async Task<BillOfMaterials> AddAsync(BillOfMaterials entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            INSERT INTO bill_of_materials (id, company_id, finished_good_id, name, code, version,
                effective_from, effective_to, output_quantity, output_unit_id, is_active, notes, created_at)
            VALUES (@Id, @CompanyId, @FinishedGoodId, @Name, @Code, @Version,
                @EffectiveFrom, @EffectiveTo, @OutputQuantity, @OutputUnitId, @IsActive, @Notes, @CreatedAt)
            RETURNING *";
        return await connection.QuerySingleAsync<BillOfMaterials>(sql, entity);
    }

    public async Task UpdateAsync(BillOfMaterials entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            UPDATE bill_of_materials SET
                finished_good_id = @FinishedGoodId,
                name = @Name,
                code = @Code,
                version = @Version,
                effective_from = @EffectiveFrom,
                effective_to = @EffectiveTo,
                output_quantity = @OutputQuantity,
                output_unit_id = @OutputUnitId,
                is_active = @IsActive,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM bill_of_materials WHERE id = @id", new { id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM bill_of_materials WHERE id = @id)", new { id });
    }

    public async Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = excludeId.HasValue
            ? "SELECT EXISTS(SELECT 1 FROM bill_of_materials WHERE company_id = @companyId AND code = @code AND id != @excludeId)"
            : "SELECT EXISTS(SELECT 1 FROM bill_of_materials WHERE company_id = @companyId AND code = @code)";
        return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, code, excludeId });
    }
}
