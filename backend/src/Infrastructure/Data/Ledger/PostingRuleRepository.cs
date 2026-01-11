using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Ledger
{
    public class PostingRuleRepository : IPostingRuleRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "rule_code", "rule_name", "description",
            "source_type", "trigger_event", "conditions_json", "posting_template",
            "financial_year", "effective_from", "effective_to",
            "priority", "is_active", "created_by", "created_at", "updated_at"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "rule_code", "rule_name", "description", "source_type"
        };

        public PostingRuleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<PostingRule?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PostingRule>(
                "SELECT * FROM posting_rules WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<PostingRule>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PostingRule>(
                "SELECT * FROM posting_rules ORDER BY source_type, priority");
        }

        public async Task<(IEnumerable<PostingRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    var paramName = filter.Key.Replace(".", "_");
                    conditions.Add($"{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchConditions = SearchableColumns.Select(col => $"{col} ILIKE @searchTerm");
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "source_type";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM posting_rules
                {whereClause}
                ORDER BY {orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM posting_rules
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<PostingRule>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<PostingRule> AddAsync(PostingRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO posting_rules (
                    company_id, rule_code, rule_name, description,
                    source_type, trigger_event, conditions_json, posting_template,
                    financial_year, effective_from, effective_to,
                    priority, is_active, created_by, created_at, updated_at
                )
                VALUES (
                    @CompanyId, @RuleCode, @RuleName, @Description,
                    @SourceType, @TriggerEvent, @ConditionsJson::jsonb, @PostingTemplate::jsonb,
                    @FinancialYear, @EffectiveFrom, @EffectiveTo,
                    @Priority, @IsActive, @CreatedBy, NOW(), NOW()
                )
                RETURNING *";

            return await connection.QuerySingleAsync<PostingRule>(sql, entity);
        }

        public async Task UpdateAsync(PostingRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE posting_rules SET
                    rule_name = @RuleName,
                    description = @Description,
                    conditions_json = @ConditionsJson::jsonb,
                    posting_template = @PostingTemplate::jsonb,
                    financial_year = @FinancialYear,
                    effective_from = @EffectiveFrom,
                    effective_to = @EffectiveTo,
                    priority = @Priority,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM posting_rules WHERE id = @id",
                new { id });
        }

        // ==================== Company-Specific Queries ====================

        public async Task<IEnumerable<PostingRule>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PostingRule>(
                @"SELECT * FROM posting_rules
                  WHERE company_id = @companyId OR company_id IS NULL
                  ORDER BY source_type, priority",
                new { companyId });
        }

        public async Task<PostingRule?> GetByCodeAsync(Guid? companyId, string ruleCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"SELECT * FROM posting_rules
                        WHERE rule_code = @ruleCode";

            if (companyId.HasValue)
            {
                sql += " AND (company_id = @companyId OR company_id IS NULL)";
            }

            sql += " ORDER BY company_id NULLS LAST LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<PostingRule>(sql, new { companyId, ruleCode });
        }

        public async Task<IEnumerable<PostingRule>> GetBySourceTypeAsync(
            Guid companyId,
            string sourceType,
            string? triggerEvent = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"SELECT * FROM posting_rules
                        WHERE (company_id = @companyId OR company_id IS NULL)
                        AND source_type = @sourceType
                        AND is_active = TRUE";

            if (!string.IsNullOrEmpty(triggerEvent))
            {
                sql += " AND trigger_event = @triggerEvent";
            }

            sql += " ORDER BY priority";

            return await connection.QueryAsync<PostingRule>(sql, new { companyId, sourceType, triggerEvent });
        }

        // ==================== Rule Matching ====================

        public async Task<IEnumerable<PostingRule>> FindMatchingRulesAsync(
            Guid companyId,
            string sourceType,
            string triggerEvent,
            DateOnly transactionDate,
            string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Explicitly cast jsonb columns to text to avoid Npgsql JsonDocument mapping issues
            var sql = @"SELECT id, company_id, rule_code, rule_name,
                        source_type, trigger_event,
                        conditions_json::text as conditions_json,
                        posting_template::text as posting_template,
                        financial_year, effective_from, effective_to,
                        priority, is_active, is_system_rule, created_by, created_at, updated_at
                        FROM posting_rules
                        WHERE (company_id = @companyId OR company_id IS NULL)
                        AND source_type = @sourceType
                        AND trigger_event = @triggerEvent
                        AND is_active = TRUE
                        AND effective_from <= @transactionDate
                        AND (effective_to IS NULL OR effective_to >= @transactionDate)";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND (financial_year = @financialYear OR financial_year IS NULL)";
            }

            sql += " ORDER BY company_id NULLS LAST, priority";

            return await connection.QueryAsync<PostingRule>(
                sql, new { companyId, sourceType, triggerEvent, transactionDate, financialYear });
        }

        public async Task<PostingRule?> GetBestMatchingRuleAsync(
            Guid companyId,
            string sourceType,
            string triggerEvent,
            Dictionary<string, object> conditions,
            DateOnly transactionDate)
        {
            var rules = await FindMatchingRulesAsync(companyId, sourceType, triggerEvent, transactionDate);
            var ruleList = rules.ToList();

            // Log for debugging
            Console.WriteLine($"[GetBestMatchingRuleAsync] Found {ruleList.Count} rules for {sourceType}/{triggerEvent}");

            foreach (var rule in ruleList)
            {
                Console.WriteLine($"[GetBestMatchingRuleAsync] Checking rule {rule.RuleCode}, ConditionsJson='{rule.ConditionsJson ?? "NULL"}'");

                if (string.IsNullOrEmpty(rule.ConditionsJson))
                {
                    Console.WriteLine($"[GetBestMatchingRuleAsync] Rule {rule.RuleCode} has no conditions - MATCH");
                    return rule; // No conditions means it matches all
                }

                // Parse and match conditions
                var matched = MatchesConditions(rule.ConditionsJson, conditions);
                Console.WriteLine($"[GetBestMatchingRuleAsync] Rule {rule.RuleCode} MatchesConditions={matched}");

                if (matched)
                {
                    return rule;
                }
            }

            Console.WriteLine($"[GetBestMatchingRuleAsync] No rules matched");
            return null;
        }

        private bool MatchesConditions(string conditionsJson, Dictionary<string, object> actualConditions)
        {
            try
            {
                // Debug: Log what we're trying to match
                System.Diagnostics.Debug.WriteLine($"MatchesConditions: JSON='{conditionsJson}', ActualKeys=[{string.Join(",", actualConditions.Keys)}]");

                var ruleConditions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(conditionsJson);
                if (ruleConditions == null) return true;

                foreach (var kvp in ruleConditions)
                {
                    if (!actualConditions.TryGetValue(kvp.Key, out var actualValue))
                    {
                        return false; // Required condition not present
                    }

                    // Handle boolean comparison properly
                    if (kvp.Value.ValueKind == System.Text.Json.JsonValueKind.True ||
                        kvp.Value.ValueKind == System.Text.Json.JsonValueKind.False)
                    {
                        bool ruleBoolean = kvp.Value.GetBoolean();
                        bool actualBoolean = actualValue switch
                        {
                            bool b => b,
                            string s => bool.TryParse(s, out var parsed) && parsed,
                            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.True => true,
                            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.False => false,
                            _ => false
                        };

                        if (ruleBoolean != actualBoolean)
                        {
                            return false;
                        }
                    }
                    // Handle number comparison
                    else if (kvp.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        var ruleNumber = kvp.Value.GetDecimal();
                        var actualNumber = actualValue switch
                        {
                            decimal d => d,
                            double db => (decimal)db,
                            int i => (decimal)i,
                            long l => (decimal)l,
                            string s when decimal.TryParse(s, out var parsed) => parsed,
                            _ => decimal.MinValue
                        };

                        if (ruleNumber != actualNumber)
                        {
                            return false;
                        }
                    }
                    // Handle string comparison (default)
                    else
                    {
                        var ruleValue = kvp.Value.GetString();
                        var actual = actualValue?.ToString();

                        if (!string.Equals(ruleValue, actual, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==================== Initialization ====================

        public async Task<bool> HasRulesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM posting_rules WHERE company_id = @companyId",
                new { companyId });
            return count > 0;
        }

        public async Task InitializeDefaultRulesAsync(Guid companyId, Guid? createdBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "SELECT create_default_posting_rules(@companyId, @createdBy)",
                new { companyId, createdBy });
        }

        // ==================== Usage Logging ====================

        public async Task LogUsageAsync(PostingRuleUsageLog usageLog)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO posting_rule_usage_log (
                    posting_rule_id, journal_entry_id, source_type, source_id,
                    rule_snapshot, computed_at, computed_by, success, error_message
                )
                VALUES (
                    @PostingRuleId, @JournalEntryId, @SourceType, @SourceId,
                    @RuleSnapshot::jsonb, NOW(), @ComputedBy, @Success, @ErrorMessage
                )";
            await connection.ExecuteAsync(sql, usageLog);
        }

        public async Task<IEnumerable<PostingRuleUsageLog>> GetUsageHistoryAsync(Guid ruleId, int limit = 100)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PostingRuleUsageLog>(
                @"SELECT * FROM posting_rule_usage_log
                  WHERE posting_rule_id = @ruleId
                  ORDER BY applied_at DESC
                  LIMIT @limit",
                new { ruleId, limit });
        }
    }
}
