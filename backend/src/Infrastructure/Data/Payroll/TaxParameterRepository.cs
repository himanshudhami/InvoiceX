using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Repository implementation for tax parameters using Dapper.
/// </summary>
public class TaxParameterRepository : ITaxParameterRepository
{
    private readonly string _connectionString;

    private static readonly string[] AllowedColumns = new[]
    {
        "id", "financial_year", "regime", "parameter_code", "parameter_name",
        "parameter_value", "parameter_type", "description", "legal_reference",
        "effective_from", "effective_to", "is_active", "created_at", "updated_at",
        "created_by", "updated_by"
    };

    public TaxParameterRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TaxParameter?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<TaxParameter>(
            "SELECT * FROM tax_parameters WHERE id = @id",
            new { id });
    }

    public async Task<IEnumerable<TaxParameter>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<TaxParameter>(
            "SELECT * FROM tax_parameters WHERE is_active = TRUE ORDER BY financial_year DESC, parameter_code");
    }

    public async Task<IEnumerable<TaxParameter>> GetByFinancialYearAsync(string financialYear)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<TaxParameter>(
            @"SELECT * FROM tax_parameters
              WHERE financial_year = @financialYear AND is_active = TRUE
              ORDER BY parameter_code",
            new { financialYear });
    }

    public async Task<IEnumerable<TaxParameter>> GetByRegimeAndYearAsync(string regime, string financialYear)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<TaxParameter>(
            @"SELECT * FROM tax_parameters
              WHERE (regime = @regime OR regime = 'both')
                AND financial_year = @financialYear
                AND is_active = TRUE
              ORDER BY parameter_code",
            new { regime, financialYear });
    }

    public async Task<TaxParameter?> GetParameterAsync(string parameterCode, string regime, string financialYear)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<TaxParameter>(
            @"SELECT * FROM tax_parameters
              WHERE parameter_code = @parameterCode
                AND (regime = @regime OR regime = 'both')
                AND financial_year = @financialYear
                AND is_active = TRUE
              ORDER BY CASE WHEN regime = @regime THEN 0 ELSE 1 END
              LIMIT 1",
            new { parameterCode, regime, financialYear });
    }

    public async Task<decimal> GetParameterValueAsync(string parameterCode, string regime, string financialYear, decimal defaultValue = 0)
    {
        var param = await GetParameterAsync(parameterCode, regime, financialYear);
        return param?.ParameterValue ?? defaultValue;
    }

    public async Task<Dictionary<string, decimal>> GetAllParametersForRegimeAsync(string regime, string financialYear)
    {
        var parameters = await GetByRegimeAndYearAsync(regime, financialYear);
        var result = new Dictionary<string, decimal>();

        foreach (var p in parameters)
        {
            // If key already exists (from 'both'), prefer the regime-specific value
            if (!result.ContainsKey(p.ParameterCode) || p.Regime == regime)
            {
                result[p.ParameterCode] = p.ParameterValue;
            }
        }

        return result;
    }

    public async Task<(IEnumerable<TaxParameter> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var builder = SqlQueryBuilder
            .From("tax_parameters", AllowedColumns)
            .SearchAcross(new[] { "parameter_code", "parameter_name", "description" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "parameter_code";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<TaxParameter>();
        var totalCount = await multi.ReadSingleAsync<int>();

        return (items, totalCount);
    }

    public async Task<TaxParameter> AddAsync(TaxParameter entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO tax_parameters
            (financial_year, regime, parameter_code, parameter_name, parameter_value,
             parameter_type, description, legal_reference, effective_from, effective_to,
             is_active, created_at, updated_at, created_by, updated_by)
            VALUES
            (@FinancialYear, @Regime, @ParameterCode, @ParameterName, @ParameterValue,
             @ParameterType, @Description, @LegalReference, @EffectiveFrom, @EffectiveTo,
             @IsActive, NOW(), NOW(), @CreatedBy, @UpdatedBy)
            RETURNING *";
        return await connection.QuerySingleAsync<TaxParameter>(sql, entity);
    }

    public async Task UpdateAsync(TaxParameter entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"UPDATE tax_parameters SET
            financial_year = @FinancialYear,
            regime = @Regime,
            parameter_code = @ParameterCode,
            parameter_name = @ParameterName,
            parameter_value = @ParameterValue,
            parameter_type = @ParameterType,
            description = @Description,
            legal_reference = @LegalReference,
            effective_from = @EffectiveFrom,
            effective_to = @EffectiveTo,
            is_active = @IsActive,
            updated_at = NOW(),
            updated_by = @UpdatedBy
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM tax_parameters WHERE id = @id", new { id });
    }
}
