using Core.Entities.Tags;
using Core.Interfaces.Tags;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Tags
{
    public class AttributionRuleRepository : IAttributionRuleRepository
    {
        private readonly string _connectionString;

        public AttributionRuleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<AttributionRule?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AttributionRule>(
                "SELECT * FROM attribution_rules WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<AttributionRule>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                "SELECT * FROM attribution_rules ORDER BY priority, name");
        }

        public async Task<IEnumerable<AttributionRule>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                "SELECT * FROM attribution_rules WHERE company_id = @companyId ORDER BY priority, name",
                new { companyId });
        }

        public async Task<AttributionRule> AddAsync(AttributionRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO attribution_rules (
                    id, company_id, name, description,
                    rule_type, applies_to, conditions,
                    tag_assignments, allocation_method, split_metric,
                    priority, stop_on_match, overwrite_existing,
                    effective_from, effective_to,
                    times_applied, last_applied_at, total_amount_tagged,
                    is_active, created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @CompanyId, @Name, @Description,
                    @RuleType, @AppliesTo::jsonb, @Conditions::jsonb,
                    @TagAssignments::jsonb, @AllocationMethod, @SplitMetric,
                    @Priority, @StopOnMatch, @OverwriteExisting,
                    @EffectiveFrom, @EffectiveTo,
                    @TimesApplied, @LastAppliedAt, @TotalAmountTagged,
                    @IsActive, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<AttributionRule>(sql, entity);
        }

        public async Task UpdateAsync(AttributionRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE attribution_rules SET
                    name = @Name,
                    description = @Description,
                    rule_type = @RuleType,
                    applies_to = @AppliesTo::jsonb,
                    conditions = @Conditions::jsonb,
                    tag_assignments = @TagAssignments::jsonb,
                    allocation_method = @AllocationMethod,
                    split_metric = @SplitMetric,
                    priority = @Priority,
                    stop_on_match = @StopOnMatch,
                    overwrite_existing = @OverwriteExisting,
                    effective_from = @EffectiveFrom,
                    effective_to = @EffectiveTo,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM attribution_rules WHERE id = @id", new { id });
        }

        // ==================== Paged/Filtered ====================

        public async Task<(IEnumerable<AttributionRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "description", "rule_type",
                "allocation_method", "priority", "is_active",
                "times_applied", "total_amount_tagged", "last_applied_at",
                "effective_from", "effective_to", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("attribution_rules", allowedColumns)
                .SearchAcross(new[] { "name", "description" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "priority";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<AttributionRule>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        // ==================== Active Rules ====================

        public async Task<IEnumerable<AttributionRule>> GetActiveRulesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND (effective_from IS NULL OR effective_from <= CURRENT_DATE)
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY priority",
                new { companyId });
        }

        public async Task<IEnumerable<AttributionRule>> GetRulesForTransactionTypeAsync(
            Guid companyId,
            string transactionType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND (effective_from IS NULL OR effective_from <= CURRENT_DATE)
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                    AND (applies_to @> @transactionTypeJson::jsonb OR applies_to @> '[""*""]'::jsonb)
                  ORDER BY priority",
                new { companyId, transactionTypeJson = $"[\"{transactionType}\"]" });
        }

        public async Task<IEnumerable<AttributionRule>> GetByRuleTypeAsync(Guid companyId, string ruleType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId AND rule_type = @ruleType
                  ORDER BY priority",
                new { companyId, ruleType });
        }

        // ==================== Matching ====================

        public async Task<IEnumerable<AttributionRule>> GetVendorRulesAsync(Guid companyId, Guid vendorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND rule_type = 'vendor'
                    AND (effective_from IS NULL OR effective_from <= CURRENT_DATE)
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                    AND (conditions->'vendor_ids' @> @vendorIdJson::jsonb
                         OR conditions->>'vendor_name_contains' IS NOT NULL)
                  ORDER BY priority",
                new { companyId, vendorIdJson = $"\"{vendorId}\"" });
        }

        public async Task<IEnumerable<AttributionRule>> GetCustomerRulesAsync(Guid companyId, Guid customerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND rule_type = 'customer'
                    AND (effective_from IS NULL OR effective_from <= CURRENT_DATE)
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                    AND (conditions->'customer_ids' @> @customerIdJson::jsonb
                         OR conditions->>'customer_name_contains' IS NOT NULL)
                  ORDER BY priority",
                new { companyId, customerIdJson = $"\"{customerId}\"" });
        }

        public async Task<IEnumerable<AttributionRule>> GetAccountRulesAsync(Guid companyId, Guid accountId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AttributionRule>(
                @"SELECT * FROM attribution_rules
                  WHERE company_id = @companyId
                    AND is_active = TRUE
                    AND rule_type = 'account'
                    AND (effective_from IS NULL OR effective_from <= CURRENT_DATE)
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                    AND conditions->'account_ids' @> @accountIdJson::jsonb
                  ORDER BY priority",
                new { companyId, accountIdJson = $"\"{accountId}\"" });
        }

        // ==================== Statistics ====================

        public async Task UpdateRuleStatisticsAsync(Guid ruleId, decimal amountTagged)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE attribution_rules SET
                    times_applied = times_applied + 1,
                    total_amount_tagged = total_amount_tagged + @amountTagged,
                    last_applied_at = CURRENT_TIMESTAMP,
                    updated_at = CURRENT_TIMESTAMP
                  WHERE id = @ruleId",
                new { ruleId, amountTagged });
        }

        public async Task<IEnumerable<RulePerformanceSummary>> GetRulePerformanceSummaryAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RulePerformanceSummary>(
                @"SELECT
                    ar.id as RuleId,
                    ar.name as RuleName,
                    ar.rule_type as RuleType,
                    ar.priority as Priority,
                    ar.is_active as IsActive,
                    ar.times_applied as TimesApplied,
                    ar.total_amount_tagged as TotalAmountTagged,
                    ar.last_applied_at as LastAppliedAt,
                    COUNT(DISTINCT tt.id) as CurrentTagsCount
                  FROM attribution_rules ar
                  LEFT JOIN transaction_tags tt ON ar.id = tt.attribution_rule_id
                  WHERE ar.company_id = @companyId
                  GROUP BY ar.id, ar.name, ar.rule_type, ar.priority, ar.is_active,
                           ar.times_applied, ar.total_amount_tagged, ar.last_applied_at
                  ORDER BY ar.priority",
                new { companyId });
        }

        // ==================== Validation ====================

        public async Task<bool> ExistsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM attribution_rules WHERE id = @id)",
                new { id });
        }

        public async Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM attribution_rules
                    WHERE company_id = @companyId
                      AND name = @name
                      AND (@excludeId IS NULL OR id != @excludeId)
                )",
                new { companyId, name, excludeId });
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<AttributionRule>> AddManyAsync(IEnumerable<AttributionRule> rules)
        {
            var results = new List<AttributionRule>();
            foreach (var rule in rules)
            {
                results.Add(await AddAsync(rule));
            }
            return results;
        }

        public async Task ReorderPrioritiesAsync(Guid companyId, IEnumerable<(Guid RuleId, int NewPriority)> priorities)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            foreach (var (ruleId, newPriority) in priorities)
            {
                await connection.ExecuteAsync(
                    "UPDATE attribution_rules SET priority = @newPriority, updated_at = CURRENT_TIMESTAMP WHERE id = @ruleId AND company_id = @companyId",
                    new { ruleId, newPriority, companyId });
            }
        }
    }
}
