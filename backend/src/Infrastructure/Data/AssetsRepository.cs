using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;
using System.Text;
using System.Linq;

namespace Infrastructure.Data;

public class AssetsRepository : IAssetsRepository
{
    private readonly string _connectionString;

    public AssetsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Assets?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Assets>("SELECT * FROM assets WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<Assets>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Assets>("SELECT * FROM assets");
    }

    public async Task<(IEnumerable<Assets> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] {
            "id", "company_id", "category_id", "model_id", "asset_type", "status",
            "asset_tag", "name", "serial_number", "location", "vendor",
            "purchase_type", "invoice_reference", "purchase_date", "in_service_date", "depreciation_start_date",
            "warranty_expiration", "purchase_cost", "currency", "depreciation_method", "useful_life_months",
            "salvage_value", "residual_book_value", "linked_loan_id", "down_payment_amount", "gst_amount", "gst_rate",
            "created_at", "updated_at"
        };

        var builder = SqlQueryBuilder
            .From("assets", allowedColumns)
            .SearchAcross(new[] { "asset_tag", "name", "serial_number", "location", "vendor" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<Assets>();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<Assets> AddAsync(Assets entity)
    {
        const string sql = @"INSERT INTO assets
        (company_id, category_id, model_id, asset_type, status, asset_tag, serial_number, name, description, location, vendor, purchase_type, invoice_reference, purchase_date, in_service_date, depreciation_start_date, warranty_expiration, purchase_cost, currency, depreciation_method, useful_life_months, salvage_value, residual_book_value, custom_properties, notes, linked_loan_id, down_payment_amount, gst_amount, gst_rate, itc_eligible, tds_on_interest, created_at, updated_at)
        VALUES (@CompanyId, @CategoryId, @ModelId, @AssetType, @Status, @AssetTag, @SerialNumber, @Name, @Description, @Location, @Vendor, @PurchaseType, @InvoiceReference, @PurchaseDate, @InServiceDate, @DepreciationStartDate, @WarrantyExpiration, @PurchaseCost, @Currency, @DepreciationMethod, @UsefulLifeMonths, @SalvageValue, @ResidualBookValue, CAST(@CustomProperties AS jsonb), @Notes, @LinkedLoanId, @DownPaymentAmount, @GstAmount, @GstRate, @ItcEligible, @TdsOnInterest, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<Assets>(sql, entity);
    }

    public async Task UpdateAsync(Assets entity)
    {
        const string sql = @"UPDATE assets SET
            category_id=@CategoryId,
            model_id=@ModelId,
            asset_type=@AssetType,
            status=@Status,
            asset_tag=@AssetTag,
            serial_number=@SerialNumber,
            name=@Name,
            description=@Description,
            location=@Location,
            vendor=@Vendor,
            purchase_type=@PurchaseType,
            invoice_reference=@InvoiceReference,
            purchase_date=@PurchaseDate,
            in_service_date=@InServiceDate,
            depreciation_start_date=@DepreciationStartDate,
            warranty_expiration=@WarrantyExpiration,
            purchase_cost=@PurchaseCost,
            currency=@Currency,
            depreciation_method=@DepreciationMethod,
            useful_life_months=@UsefulLifeMonths,
            salvage_value=@SalvageValue,
            residual_book_value=@ResidualBookValue,
            custom_properties=CAST(@CustomProperties AS jsonb),
            notes=@Notes,
            linked_loan_id=@LinkedLoanId,
            down_payment_amount=@DownPaymentAmount,
            gst_amount=@GstAmount,
            gst_rate=@GstRate,
            itc_eligible=@ItcEligible,
            tds_on_interest=@TdsOnInterest,
            updated_at=NOW()
        WHERE id=@Id";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM assets WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<AssetAssignments>> GetAssignmentsAsync(Guid assetId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<AssetAssignments>("SELECT * FROM asset_assignments WHERE asset_id=@assetId ORDER BY assigned_on DESC", new { assetId });
    }

    public async Task<IEnumerable<AssetAssignments>> GetAllAssignmentsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<AssetAssignments>("SELECT * FROM asset_assignments ORDER BY assigned_on DESC");
    }

    public async Task<IEnumerable<AssetAssignments>> GetAssignmentsByEmployeeAsync(Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<AssetAssignments>(
            "SELECT * FROM asset_assignments WHERE employee_id = @employeeId ORDER BY assigned_on DESC",
            new { employeeId });
    }

    public async Task<AssetAssignments> AddAssignmentAsync(AssetAssignments assignment)
    {
        const string sql = @"INSERT INTO asset_assignments
        (asset_id, target_type, employee_id, company_id, assigned_on, returned_on, condition_out, condition_in, notes, created_at, updated_at)
        VALUES (@AssetId, @TargetType, @EmployeeId, @CompanyId, @AssignedOn, @ReturnedOn, @ConditionOut, @ConditionIn, @Notes, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<AssetAssignments>(sql, assignment);
    }

    public async Task<AssetAssignments?> GetAssignmentByIdAsync(Guid assignmentId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<AssetAssignments>("SELECT * FROM asset_assignments WHERE id=@assignmentId", new { assignmentId });
    }

    public async Task ReturnAssignmentAsync(Guid assignmentId, DateTime? returnedOn, string? conditionIn)
    {
        const string sql = @"UPDATE asset_assignments SET returned_on=@returnedOn, condition_in=@conditionIn, updated_at=NOW() WHERE id=@assignmentId";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { assignmentId, returnedOn, conditionIn });
    }

    public async Task<IEnumerable<AssetDocuments>> GetDocumentsAsync(Guid assetId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<AssetDocuments>("SELECT * FROM asset_documents WHERE asset_id=@assetId ORDER BY uploaded_at DESC", new { assetId });
    }

    public async Task<AssetDocuments> AddDocumentAsync(AssetDocuments document)
    {
        const string sql = @"INSERT INTO asset_documents (asset_id, name, url, content_type, uploaded_at, notes)
        VALUES (@AssetId, @Name, @Url, @ContentType, COALESCE(@UploadedAt, NOW()), @Notes)
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<AssetDocuments>(sql, document);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM asset_documents WHERE id=@documentId", new { documentId });
    }

    public async Task<(IEnumerable<AssetMaintenance> Items, int TotalCount)> GetMaintenancePagedAsync(int pageNumber, int pageSize, Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("offset", (pageNumber - 1) * pageSize);
        parameters.Add("pageSize", pageSize);

        if (filters != null)
        {
            foreach (var kv in filters)
            {
                var column = kv.Key.ToLowerInvariant();
                switch (column)
                {
                    case "company_id":
                        conditions.Add("a.company_id = @companyId");
                        parameters.Add("companyId", kv.Value);
                        break;
                    case "asset_id":
                        conditions.Add("m.asset_id = @assetId");
                        parameters.Add("assetId", kv.Value);
                        break;
                    case "status":
                        conditions.Add("m.status = @status");
                        parameters.Add("status", kv.Value);
                        break;
                }
            }
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var dataSql = $@"SELECT m.* FROM asset_maintenance m
JOIN assets a ON m.asset_id = a.id
{where}
ORDER BY m.opened_at DESC
LIMIT @pageSize OFFSET @offset;";
        var countSql = $@"SELECT COUNT(*) FROM asset_maintenance m
JOIN assets a ON m.asset_id = a.id
{where};";

        using var multi = await connection.QueryMultipleAsync(dataSql + countSql, parameters);
        var items = await multi.ReadAsync<AssetMaintenance>();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<IEnumerable<AssetMaintenance>> GetMaintenanceByAssetAsync(Guid assetId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<AssetMaintenance>("SELECT * FROM asset_maintenance WHERE asset_id=@assetId ORDER BY opened_at DESC", new { assetId });
    }

    public async Task<AssetMaintenance> AddMaintenanceAsync(AssetMaintenance maintenance)
    {
        const string sql = @"INSERT INTO asset_maintenance
            (asset_id, title, description, status, opened_at, closed_at, vendor, cost, currency, due_date, notes, created_at, updated_at)
            VALUES (@AssetId, @Title, @Description, @Status, COALESCE(@OpenedAt, NOW()), @ClosedAt, @Vendor, @Cost, @Currency, @DueDate, @Notes, NOW(), NOW())
            RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<AssetMaintenance>(sql, maintenance);
    }

    public async Task<AssetMaintenance?> GetMaintenanceByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<AssetMaintenance>("SELECT * FROM asset_maintenance WHERE id=@id", new { id });
    }

    public async Task UpdateMaintenanceAsync(AssetMaintenance maintenance)
    {
        const string sql = @"UPDATE asset_maintenance SET
            status=@Status,
            closed_at=@ClosedAt,
            due_date=@DueDate,
            vendor=@Vendor,
            cost=@Cost,
            currency=@Currency,
            notes=@Notes,
            updated_at=NOW()
        WHERE id=@Id";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, maintenance);
    }

    public async Task<AssetDisposals?> GetDisposalByAssetAsync(Guid assetId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<AssetDisposals>("SELECT * FROM asset_disposals WHERE asset_id=@assetId ORDER BY disposed_on DESC LIMIT 1", new { assetId });
    }

    public async Task<AssetDisposals> AddDisposalAsync(AssetDisposals disposal)
    {
        const string sql = @"INSERT INTO asset_disposals
            (asset_id, disposed_on, method, proceeds, disposal_cost, currency, buyer, notes, created_at, updated_at)
            VALUES (@AssetId, @DisposedOn, @Method, @Proceeds, @DisposalCost, @Currency, @Buyer, @Notes, NOW(), NOW())
            RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<AssetDisposals>(sql, disposal);
    }

    public async Task<IDictionary<Guid, decimal>> GetMaintenanceTotalsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<(Guid AssetId, decimal? Total)>("SELECT asset_id as AssetId, COALESCE(SUM(cost),0) as Total FROM asset_maintenance GROUP BY asset_id");
        return rows.ToDictionary(r => r.AssetId, r => r.Total ?? 0);
    }

    public async Task<bool> AssetTagExistsAsync(Guid companyId, string assetTag)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var exists = await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM assets WHERE company_id = @companyId AND asset_tag = @assetTag)",
            new { companyId, assetTag });
        return exists;
    }

    public async Task<IEnumerable<Assets>> GetAvailableAssetsAsync(Guid companyId, string? searchTerm = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("companyId", companyId);

        var sql = new StringBuilder(@"
            SELECT * FROM assets
            WHERE company_id = @companyId
            AND LOWER(status) = 'available'");

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql.Append(" AND (LOWER(name) LIKE @search OR LOWER(asset_tag) LIKE @search OR LOWER(serial_number) LIKE @search)");
            parameters.Add("search", $"%{searchTerm.ToLower()}%");
        }

        sql.Append(" ORDER BY name ASC LIMIT 50");

        return await connection.QueryAsync<Assets>(sql.ToString(), parameters);
    }
}




