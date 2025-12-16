using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Repository implementation for salary components using Dapper.
/// </summary>
public class SalaryComponentRepository : ISalaryComponentRepository
{
    private readonly string _connectionString;

    private static readonly string[] AllowedColumns = new[]
    {
        "id", "company_id", "component_code", "component_name", "component_type",
        "is_pf_wage", "is_esi_wage", "is_taxable", "is_pt_wage",
        "apply_proration", "proration_basis", "display_order", "show_on_payslip",
        "payslip_group", "is_active", "created_at", "updated_at", "created_by", "updated_by"
    };

    public SalaryComponentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SalaryComponent?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<SalaryComponent>(
            "SELECT * FROM salary_components WHERE id = @id",
            new { id });
    }

    public async Task<IEnumerable<SalaryComponent>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<SalaryComponent>(
            "SELECT * FROM salary_components WHERE is_active = TRUE ORDER BY display_order, component_code");
    }

    public async Task<IEnumerable<SalaryComponent>> GetByCompanyIdAsync(Guid? companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        // Get global components where no company-specific override exists, plus company-specific
        var sql = @"
            SELECT * FROM salary_components
            WHERE is_active = TRUE
              AND (
                  (company_id IS NULL AND component_code NOT IN (
                      SELECT component_code FROM salary_components WHERE company_id = @companyId
                  ))
                  OR company_id = @companyId
              )
            ORDER BY display_order, component_code";
        return await connection.QueryAsync<SalaryComponent>(sql, new { companyId });
    }

    public async Task<SalaryComponent?> GetByCodeAsync(string componentCode, Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        // Check company-specific first, then global
        return await connection.QueryFirstOrDefaultAsync<SalaryComponent>(
            @"SELECT * FROM salary_components
              WHERE component_code = @componentCode
                AND (company_id = @companyId OR (company_id IS NULL AND @companyId IS NULL)
                     OR (company_id IS NULL AND NOT EXISTS (
                         SELECT 1 FROM salary_components WHERE component_code = @componentCode AND company_id = @companyId
                     )))
                AND is_active = TRUE
              ORDER BY CASE WHEN company_id = @companyId THEN 0 ELSE 1 END
              LIMIT 1",
            new { componentCode, companyId });
    }

    public async Task<IEnumerable<SalaryComponent>> GetPfWageComponentsAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM salary_components
            WHERE is_pf_wage = TRUE AND is_active = TRUE
              AND (
                  (company_id IS NULL AND component_code NOT IN (
                      SELECT component_code FROM salary_components WHERE company_id = @companyId
                  ))
                  OR company_id = @companyId
              )
            ORDER BY display_order, component_code";
        return await connection.QueryAsync<SalaryComponent>(sql, new { companyId });
    }

    public async Task<IEnumerable<SalaryComponent>> GetEsiWageComponentsAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM salary_components
            WHERE is_esi_wage = TRUE AND is_active = TRUE
              AND (
                  (company_id IS NULL AND component_code NOT IN (
                      SELECT component_code FROM salary_components WHERE company_id = @companyId
                  ))
                  OR company_id = @companyId
              )
            ORDER BY display_order, component_code";
        return await connection.QueryAsync<SalaryComponent>(sql, new { companyId });
    }

    public async Task<IEnumerable<SalaryComponent>> GetTaxableComponentsAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM salary_components
            WHERE is_taxable = TRUE AND is_active = TRUE
              AND (
                  (company_id IS NULL AND component_code NOT IN (
                      SELECT component_code FROM salary_components WHERE company_id = @companyId
                  ))
                  OR company_id = @companyId
              )
            ORDER BY display_order, component_code";
        return await connection.QueryAsync<SalaryComponent>(sql, new { companyId });
    }

    public async Task<IEnumerable<SalaryComponent>> GetByTypeAsync(string componentType, Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM salary_components
            WHERE component_type = @componentType AND is_active = TRUE
              AND (
                  (company_id IS NULL AND component_code NOT IN (
                      SELECT component_code FROM salary_components WHERE company_id = @companyId
                  ))
                  OR company_id = @companyId
              )
            ORDER BY display_order, component_code";
        return await connection.QueryAsync<SalaryComponent>(sql, new { componentType, companyId });
    }

    public async Task<(IEnumerable<SalaryComponent> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var builder = SqlQueryBuilder
            .From("salary_components", AllowedColumns)
            .SearchAcross(new[] { "component_code", "component_name", "payslip_group" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "display_order";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<SalaryComponent>();
        var totalCount = await multi.ReadSingleAsync<int>();

        return (items, totalCount);
    }

    public async Task<SalaryComponent> AddAsync(SalaryComponent entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO salary_components
            (company_id, component_code, component_name, component_type,
             is_pf_wage, is_esi_wage, is_taxable, is_pt_wage,
             apply_proration, proration_basis, display_order, show_on_payslip,
             payslip_group, is_active, created_at, updated_at, created_by, updated_by)
            VALUES
            (@CompanyId, @ComponentCode, @ComponentName, @ComponentType,
             @IsPfWage, @IsEsiWage, @IsTaxable, @IsPtWage,
             @ApplyProration, @ProrationBasis, @DisplayOrder, @ShowOnPayslip,
             @PayslipGroup, @IsActive, NOW(), NOW(), @CreatedBy, @UpdatedBy)
            RETURNING *";
        return await connection.QuerySingleAsync<SalaryComponent>(sql, entity);
    }

    public async Task UpdateAsync(SalaryComponent entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"UPDATE salary_components SET
            company_id = @CompanyId,
            component_code = @ComponentCode,
            component_name = @ComponentName,
            component_type = @ComponentType,
            is_pf_wage = @IsPfWage,
            is_esi_wage = @IsEsiWage,
            is_taxable = @IsTaxable,
            is_pt_wage = @IsPtWage,
            apply_proration = @ApplyProration,
            proration_basis = @ProrationBasis,
            display_order = @DisplayOrder,
            show_on_payslip = @ShowOnPayslip,
            payslip_group = @PayslipGroup,
            is_active = @IsActive,
            updated_at = NOW(),
            updated_by = @UpdatedBy
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM salary_components WHERE id = @id", new { id });
    }

    public async Task<bool> ComponentCodeExistsAsync(string componentCode, Guid? companyId, Guid? excludeId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = excludeId.HasValue
            ? @"SELECT COUNT(*) FROM salary_components
                WHERE component_code = @componentCode
                  AND ((company_id = @companyId) OR (company_id IS NULL AND @companyId IS NULL))
                  AND id != @excludeId"
            : @"SELECT COUNT(*) FROM salary_components
                WHERE component_code = @componentCode
                  AND ((company_id = @companyId) OR (company_id IS NULL AND @companyId IS NULL))";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { componentCode, companyId, excludeId });
        return count > 0;
    }
}
