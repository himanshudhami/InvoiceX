using Dapper;
using Npgsql;
using System.Text.Json;
using Core.Entities.Migration;
using Core.Interfaces.Migration;

namespace Infrastructure.Data.Migration
{
    public class TallyFieldMappingRepository : ITallyFieldMappingRepository
    {
        private readonly string _connectionString;

        public TallyFieldMappingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TallyFieldMapping?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity, target_account_id AS TargetAccountId,
                         target_account_type AS TargetAccountType, target_account_subtype AS TargetAccountSubtype,
                         default_account_code AS DefaultAccountCode, default_account_name AS DefaultAccountName,
                         target_tag_group AS TargetTagGroup, priority, is_active AS IsActive,
                         is_system_default AS IsSystemDefault,
                         created_at AS CreatedAt, updated_at AS UpdatedAt, created_by AS CreatedBy
                  FROM tally_field_mappings WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TallyFieldMapping>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity, target_account_id AS TargetAccountId,
                         target_account_type AS TargetAccountType, priority, is_active AS IsActive,
                         is_system_default AS IsSystemDefault
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                  ORDER BY priority, tally_group_name",
                new { companyId });
        }

        public async Task<IEnumerable<TallyFieldMapping>> GetActiveByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity, target_account_id AS TargetAccountId,
                         target_account_type AS TargetAccountType, target_tag_group AS TargetTagGroup,
                         priority, is_system_default AS IsSystemDefault
                  FROM tally_field_mappings
                  WHERE company_id = @companyId AND is_active = true
                  ORDER BY priority, tally_group_name",
                new { companyId });
        }

        public async Task<(IEnumerable<TallyFieldMapping> Items, int TotalCount)> GetPagedAsync(
            Guid companyId, int pageNumber, int pageSize, string? mappingType = null,
            string? sortBy = null, bool sortDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(mappingType))
                whereClause += " AND mapping_type = @mappingType";

            var orderBy = sortBy switch
            {
                "tallyGroupName" => "tally_group_name",
                "targetEntity" => "target_entity",
                "mappingType" => "mapping_type",
                _ => "priority"
            };
            orderBy += sortDescending ? " DESC" : " ASC";

            var countSql = $"SELECT COUNT(*) FROM tally_field_mappings {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { companyId, mappingType });

            var offset = (pageNumber - 1) * pageSize;
            var sql = $@"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                                tally_group_name AS TallyGroupName, tally_name AS TallyName,
                                target_entity AS TargetEntity, target_account_type AS TargetAccountType,
                                priority, is_active AS IsActive, is_system_default AS IsSystemDefault
                         FROM tally_field_mappings {whereClause}
                         ORDER BY {orderBy}
                         LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<TallyFieldMapping>(sql,
                new { companyId, mappingType, pageSize, offset });

            return (items, totalCount);
        }

        public async Task<TallyFieldMapping> AddAsync(TallyFieldMapping mapping)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            mapping.Id = Guid.NewGuid();
            mapping.CreatedAt = DateTime.UtcNow;
            mapping.UpdatedAt = DateTime.UtcNow;

            // Use upsert to handle case where mapping already exists (e.g., from seed)
            await connection.ExecuteAsync(
                @"INSERT INTO tally_field_mappings (
                    id, company_id, mapping_type, tally_group_name, tally_name,
                    target_entity, target_account_id, target_account_type, target_account_subtype,
                    default_account_code, default_account_name, target_tag_group,
                    priority, is_active, is_system_default, created_at, updated_at, created_by
                  ) VALUES (
                    @Id, @CompanyId, @MappingType, @TallyGroupName, @TallyName,
                    @TargetEntity, @TargetAccountId, @TargetAccountType, @TargetAccountSubtype,
                    @DefaultAccountCode, @DefaultAccountName, @TargetTagGroup,
                    @Priority, @IsActive, @IsSystemDefault, @CreatedAt, @UpdatedAt, @CreatedBy
                  )
                  ON CONFLICT ON CONSTRAINT uq_tally_mapping
                  DO UPDATE SET
                    target_entity = EXCLUDED.target_entity,
                    target_account_id = EXCLUDED.target_account_id,
                    target_account_type = EXCLUDED.target_account_type,
                    target_account_subtype = EXCLUDED.target_account_subtype,
                    default_account_code = EXCLUDED.default_account_code,
                    default_account_name = EXCLUDED.default_account_name,
                    target_tag_group = EXCLUDED.target_tag_group,
                    priority = EXCLUDED.priority,
                    is_active = EXCLUDED.is_active,
                    updated_at = CURRENT_TIMESTAMP", mapping);

            return mapping;
        }

        public async Task<IEnumerable<TallyFieldMapping>> BulkAddAsync(IEnumerable<TallyFieldMapping> mappings)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var mappingList = mappings.ToList();

            foreach (var mapping in mappingList)
            {
                mapping.Id = Guid.NewGuid();
                mapping.CreatedAt = DateTime.UtcNow;
                mapping.UpdatedAt = DateTime.UtcNow;
            }

            // Use upsert to handle case where mapping already exists (e.g., from seed)
            await connection.ExecuteAsync(
                @"INSERT INTO tally_field_mappings (
                    id, company_id, mapping_type, tally_group_name, tally_name,
                    target_entity, target_account_type, priority, is_active, is_system_default,
                    created_at, updated_at
                  ) VALUES (
                    @Id, @CompanyId, @MappingType, @TallyGroupName, @TallyName,
                    @TargetEntity, @TargetAccountType, @Priority, @IsActive, @IsSystemDefault,
                    @CreatedAt, @UpdatedAt
                  )
                  ON CONFLICT ON CONSTRAINT uq_tally_mapping
                  DO UPDATE SET
                    target_entity = EXCLUDED.target_entity,
                    target_account_type = EXCLUDED.target_account_type,
                    priority = EXCLUDED.priority,
                    is_active = EXCLUDED.is_active,
                    updated_at = CURRENT_TIMESTAMP", mappingList);

            return mappingList;
        }

        public async Task UpdateAsync(TallyFieldMapping mapping)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            mapping.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE tally_field_mappings SET
                    mapping_type = @MappingType, tally_group_name = @TallyGroupName,
                    tally_name = @TallyName, target_entity = @TargetEntity,
                    target_account_id = @TargetAccountId, target_account_type = @TargetAccountType,
                    target_account_subtype = @TargetAccountSubtype,
                    default_account_code = @DefaultAccountCode, default_account_name = @DefaultAccountName,
                    target_tag_group = @TargetTagGroup, priority = @Priority,
                    is_active = @IsActive, updated_at = @UpdatedAt
                  WHERE id = @Id", mapping);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tally_field_mappings WHERE id = @id", new { id });
        }

        public async Task<TallyFieldMapping?> GetMappingForGroupAsync(Guid companyId, string tallyGroupName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity, target_account_id AS TargetAccountId,
                         target_account_type AS TargetAccountType, target_tag_group AS TargetTagGroup
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                    AND mapping_type = 'ledger_group'
                    AND LOWER(tally_group_name) = LOWER(@tallyGroupName)
                    AND tally_name IS NULL
                    AND is_active = true
                  ORDER BY priority
                  LIMIT 1",
                new { companyId, tallyGroupName });
        }

        public async Task<TallyFieldMapping?> GetMappingForLedgerAsync(Guid companyId, string tallyGroupName, string tallyName)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First try to find a specific ledger mapping
            var specific = await connection.QueryFirstOrDefaultAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity, target_account_id AS TargetAccountId,
                         target_account_type AS TargetAccountType
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                    AND mapping_type = 'ledger'
                    AND LOWER(tally_group_name) = LOWER(@tallyGroupName)
                    AND LOWER(tally_name) = LOWER(@tallyName)
                    AND is_active = true
                  ORDER BY priority
                  LIMIT 1",
                new { companyId, tallyGroupName, tallyName });

            if (specific != null)
                return specific;

            // Fall back to group mapping
            return await GetMappingForGroupAsync(companyId, tallyGroupName);
        }

        public async Task<TallyFieldMapping?> GetMappingForStockGroupAsync(Guid companyId, string tallyStockGroupName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, tally_name AS TallyName,
                         target_entity AS TargetEntity
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                    AND mapping_type = 'stock_group'
                    AND LOWER(tally_group_name) = LOWER(@tallyStockGroupName)
                    AND is_active = true
                  ORDER BY priority
                  LIMIT 1",
                new { companyId, tallyStockGroupName });
        }

        public async Task<TallyFieldMapping?> GetMappingForCostCategoryAsync(Guid companyId, string tallyCostCategoryName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyFieldMapping>(
                @"SELECT id, company_id AS CompanyId, mapping_type AS MappingType,
                         tally_group_name AS TallyGroupName, target_entity AS TargetEntity,
                         target_tag_group AS TargetTagGroup
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                    AND mapping_type = 'cost_category'
                    AND LOWER(tally_group_name) = LOWER(@tallyCostCategoryName)
                    AND is_active = true
                  ORDER BY priority
                  LIMIT 1",
                new { companyId, tallyCostCategoryName });
        }

        public async Task<string> GetTargetEntityAsync(Guid companyId, string tallyGroupName, string? tallyName = null)
        {
            TallyFieldMapping? mapping;

            if (!string.IsNullOrEmpty(tallyName))
            {
                mapping = await GetMappingForLedgerAsync(companyId, tallyGroupName, tallyName);
            }
            else
            {
                mapping = await GetMappingForGroupAsync(companyId, tallyGroupName);
            }

            if (mapping != null)
                return mapping.TargetEntity;

            // Default mapping based on common Tally groups
            var groupLower = tallyGroupName.ToLower();
            return groupLower switch
            {
                "sundry creditors" => "vendors",
                "sundry debtors" => "customers",
                var g when g.Contains("bank") => "bank_accounts",
                "cash-in-hand" => "chart_of_accounts",
                var g when g.Contains("purchase") => "chart_of_accounts",
                var g when g.Contains("sales") => "chart_of_accounts",
                "duties & taxes" => "chart_of_accounts",
                var g when g.Contains("fixed asset") => "chart_of_accounts",
                // GST/Tax accounts should go to chart_of_accounts
                "input tax" or "output tax" => "chart_of_accounts",
                var g when g.Contains("gst") || g.Contains("tax") || g.Contains("cgst") || g.Contains("sgst") || g.Contains("igst") => "chart_of_accounts",
                // Consultants, contractors, freelancers are vendors (payables)
                "consultants" or "contractors" or "freelancers" => "vendors",
                var g when g.Contains("consultant") || g.Contains("contractor") => "vendors",
                // Other common account groups
                var g when g.Contains("expense") => "chart_of_accounts",
                var g when g.Contains("income") => "chart_of_accounts",
                var g when g.Contains("asset") => "chart_of_accounts",
                var g when g.Contains("liabilit") => "chart_of_accounts",
                var g when g.Contains("capital") || g.Contains("equity") => "chart_of_accounts",
                var g when g.Contains("reserve") || g.Contains("surplus") => "chart_of_accounts",
                var g when g.Contains("loan") || g.Contains("advance") => "chart_of_accounts",
                var g when g.Contains("provision") => "chart_of_accounts",
                var g when g.Contains("stock") || g.Contains("inventor") => "chart_of_accounts",
                var g when g.Contains("investment") => "chart_of_accounts",
                var g when g.Contains("current") => "chart_of_accounts",
                _ => "suspense"
            };
        }

        public async Task<List<string>> GetTagAssignmentsForGroupAsync(Guid companyId, string tallyGroupName)
        {
            if (string.IsNullOrEmpty(tallyGroupName))
                return new List<string>();

            using var connection = new NpgsqlConnection(_connectionString);

            // Get tag_assignments JSONB column for the matching ledger group
            var jsonString = await connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT tag_assignments::text
                  FROM tally_field_mappings
                  WHERE company_id = @companyId
                    AND mapping_type = 'ledger_group'
                    AND LOWER(tally_group_name) = LOWER(@tallyGroupName)
                    AND is_active = true
                    AND tag_assignments IS NOT NULL
                    AND tag_assignments != '[]'::jsonb
                  ORDER BY priority
                  LIMIT 1",
                new { companyId, tallyGroupName });

            if (string.IsNullOrEmpty(jsonString))
                return new List<string>();

            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(jsonString);
                return tags ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        public async Task SeedDefaultMappingsAsync(Guid companyId, Guid? createdBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "SELECT seed_tally_default_mappings(@companyId)",
                new { companyId });
        }

        public async Task<bool> HasDefaultMappingsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM tally_field_mappings
                  WHERE company_id = @companyId AND is_system_default = true",
                new { companyId });
            return count > 0;
        }
    }
}
