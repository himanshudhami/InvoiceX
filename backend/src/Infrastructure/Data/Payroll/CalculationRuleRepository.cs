using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll;

public class CalculationRuleRepository : ICalculationRuleRepository
{
    private readonly string _connectionString;
    private static readonly string[] AllowedColumns = new[]
    {
        "id", "company_id", "name", "description", "component_type", "component_code",
        "component_name", "rule_type", "formula_config", "priority", "effective_from",
        "effective_to", "is_active", "is_system", "is_taxable", "affects_pf_wage",
        "affects_esi_wage", "created_at", "updated_at", "created_by", "updated_by"
    };

    public CalculationRuleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CalculationRule?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<CalculationRule>(
            "SELECT * FROM calculation_rules WHERE id = @id",
            new { id });
    }

    public async Task<CalculationRule?> GetByIdWithConditionsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var rule = await connection.QueryFirstOrDefaultAsync<CalculationRule>(
            "SELECT * FROM calculation_rules WHERE id = @id",
            new { id });

        if (rule != null)
        {
            var conditions = await connection.QueryAsync<CalculationRuleCondition>(
                "SELECT * FROM calculation_rule_conditions WHERE rule_id = @id ORDER BY condition_group, id",
                new { id });
            rule.Conditions = conditions.ToList();
        }

        return rule;
    }

    public async Task<IEnumerable<CalculationRule>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<CalculationRule>(
            "SELECT * FROM calculation_rules ORDER BY priority, created_at DESC");
    }

    public async Task<IEnumerable<CalculationRule>> GetByCompanyIdAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<CalculationRule>(
            "SELECT * FROM calculation_rules WHERE company_id = @companyId ORDER BY priority, component_code",
            new { companyId });
    }

    public async Task<IEnumerable<CalculationRule>> GetActiveRulesForCompanyAsync(Guid companyId, DateTime? asOfDate = null)
    {
        var date = asOfDate ?? DateTime.UtcNow.Date;
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT * FROM calculation_rules
            WHERE company_id = @companyId
              AND is_active = true
              AND effective_from <= @date
              AND (effective_to IS NULL OR effective_to >= @date)
            ORDER BY priority, component_code";

        return await connection.QueryAsync<CalculationRule>(sql, new { companyId, date });
    }

    public async Task<IEnumerable<CalculationRule>> GetActiveRulesByComponentAsync(Guid companyId, string componentCode, DateTime? asOfDate = null)
    {
        var date = asOfDate ?? DateTime.UtcNow.Date;
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT * FROM calculation_rules
            WHERE company_id = @companyId
              AND component_code = @componentCode
              AND is_active = true
              AND effective_from <= @date
              AND (effective_to IS NULL OR effective_to >= @date)
            ORDER BY priority";

        return await connection.QueryAsync<CalculationRule>(sql, new { companyId, componentCode, date });
    }

    public async Task<(IEnumerable<CalculationRule> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var builder = SqlQueryBuilder
            .From("calculation_rules", AllowedColumns)
            .SearchAcross(new[] { "name", "description", "component_code", "component_name" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "priority";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<CalculationRule>();
        var totalCount = await multi.ReadSingleAsync<int>();
        return (items, totalCount);
    }

    public async Task<CalculationRule> AddAsync(CalculationRule entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO calculation_rules
            (company_id, name, description, component_type, component_code, component_name,
             rule_type, formula_config, priority, effective_from, effective_to,
             is_active, is_system, is_taxable, affects_pf_wage, affects_esi_wage,
             created_at, updated_at, created_by, updated_by)
            VALUES
            (@CompanyId, @Name, @Description, @ComponentType, @ComponentCode, @ComponentName,
             @RuleType, @FormulaConfig::jsonb, @Priority, @EffectiveFrom, @EffectiveTo,
             @IsActive, @IsSystem, @IsTaxable, @AffectsPfWage, @AffectsEsiWage,
             NOW(), NOW(), @CreatedBy, @UpdatedBy)
            RETURNING *";

        return await connection.QuerySingleAsync<CalculationRule>(sql, entity);
    }

    public async Task UpdateAsync(CalculationRule entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"UPDATE calculation_rules SET
            name = @Name,
            description = @Description,
            component_type = @ComponentType,
            component_code = @ComponentCode,
            component_name = @ComponentName,
            rule_type = @RuleType,
            formula_config = @FormulaConfig::jsonb,
            priority = @Priority,
            effective_from = @EffectiveFrom,
            effective_to = @EffectiveTo,
            is_active = @IsActive,
            is_taxable = @IsTaxable,
            affects_pf_wage = @AffectsPfWage,
            affects_esi_wage = @AffectsEsiWage,
            updated_at = NOW(),
            updated_by = @UpdatedBy
            WHERE id = @Id AND is_system = false";

        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        // Only delete non-system rules
        await connection.ExecuteAsync(
            "DELETE FROM calculation_rules WHERE id = @id AND is_system = false",
            new { id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM calculation_rules WHERE id = @id",
            new { id });
        return count > 0;
    }

    // Conditions
    public async Task<IEnumerable<CalculationRuleCondition>> GetConditionsByRuleIdAsync(Guid ruleId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<CalculationRuleCondition>(
            "SELECT * FROM calculation_rule_conditions WHERE rule_id = @ruleId ORDER BY condition_group, id",
            new { ruleId });
    }

    public async Task AddConditionAsync(CalculationRuleCondition condition)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO calculation_rule_conditions
            (rule_id, condition_group, field, operator, value, created_at)
            VALUES
            (@RuleId, @ConditionGroup, @Field, @Operator, @Value::jsonb, NOW())";

        await connection.ExecuteAsync(sql, condition);
    }

    public async Task DeleteConditionsByRuleIdAsync(Guid ruleId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM calculation_rule_conditions WHERE rule_id = @ruleId",
            new { ruleId });
    }
}

public class FormulaVariableRepository : IFormulaVariableRepository
{
    private readonly string _connectionString;

    public FormulaVariableRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<FormulaVariable>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<FormulaVariable>(
            "SELECT * FROM formula_variables ORDER BY source, display_name");
    }

    public async Task<IEnumerable<FormulaVariable>> GetActiveAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<FormulaVariable>(
            "SELECT * FROM formula_variables WHERE is_active = true ORDER BY source, display_name");
    }

    public async Task<FormulaVariable?> GetByCodeAsync(string code)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<FormulaVariable>(
            "SELECT * FROM formula_variables WHERE code = @code",
            new { code });
    }
}

public class CalculationRuleTemplateRepository : ICalculationRuleTemplateRepository
{
    private readonly string _connectionString;

    public CalculationRuleTemplateRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<CalculationRuleTemplate>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<CalculationRuleTemplate>(
            "SELECT * FROM calculation_rule_templates WHERE is_active = true ORDER BY display_order, name");
    }

    public async Task<IEnumerable<CalculationRuleTemplate>> GetByCategoryAsync(string category)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<CalculationRuleTemplate>(
            "SELECT * FROM calculation_rule_templates WHERE is_active = true AND category = @category ORDER BY display_order, name",
            new { category });
    }

    public async Task<CalculationRuleTemplate?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<CalculationRuleTemplate>(
            "SELECT * FROM calculation_rule_templates WHERE id = @id",
            new { id });
    }
}
