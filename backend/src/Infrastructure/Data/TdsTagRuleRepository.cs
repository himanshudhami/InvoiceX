using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    /// <summary>
    /// Repository implementation for Tag-driven TDS Rules
    /// </summary>
    public class TdsTagRuleRepository : ITdsTagRuleRepository
    {
        private readonly string _connectionString;

        public TdsTagRuleRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // ==================== Basic CRUD ====================

        public async Task<TdsTagRule?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.id = @id";

            return await connection.QueryFirstOrDefaultAsync<TdsTagRule>(sql, new { id });
        }

        public async Task<IEnumerable<TdsTagRule>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.company_id = @companyId
                ORDER BY r.tds_section, t.name";

            return await connection.QueryAsync<TdsTagRule>(sql, new { companyId });
        }

        public async Task<IEnumerable<TdsTagRule>> GetActiveByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.company_id = @companyId
                  AND r.is_active = true
                  AND r.effective_from <= CURRENT_DATE
                  AND (r.effective_to IS NULL OR r.effective_to >= CURRENT_DATE)
                ORDER BY r.tds_section, t.name";

            return await connection.QueryAsync<TdsTagRule>(sql, new { companyId });
        }

        public async Task<TdsTagRule> AddAsync(TdsTagRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO tds_tag_rules (
                    id, company_id, tag_id,
                    tds_section, tds_section_clause,
                    tds_rate_with_pan, tds_rate_without_pan, tds_rate_individual, tds_rate_company,
                    threshold_single_payment, threshold_annual,
                    applies_to_individual, applies_to_huf, applies_to_company, applies_to_firm,
                    applies_to_llp, applies_to_trust, applies_to_aop_boi, applies_to_government,
                    lower_certificate_allowed, nil_certificate_allowed, exemption_notes,
                    effective_from, effective_to, is_active, created_by
                ) VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CompanyId, @TagId,
                    @TdsSection, @TdsSectionClause,
                    @TdsRateWithPan, @TdsRateWithoutPan, @TdsRateIndividual, @TdsRateCompany,
                    @ThresholdSinglePayment, @ThresholdAnnual,
                    @AppliesToIndividual, @AppliesToHuf, @AppliesToCompany, @AppliesToFirm,
                    @AppliesToLlp, @AppliesToTrust, @AppliesToAopBoi, @AppliesToGovernment,
                    @LowerCertificateAllowed, @NilCertificateAllowed, @ExemptionNotes,
                    @EffectiveFrom, @EffectiveTo, @IsActive, @CreatedBy
                )
                RETURNING *";

            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            return await connection.QuerySingleAsync<TdsTagRule>(sql, entity);
        }

        public async Task UpdateAsync(TdsTagRule entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE tds_tag_rules SET
                    tag_id = @TagId,
                    tds_section = @TdsSection,
                    tds_section_clause = @TdsSectionClause,
                    tds_rate_with_pan = @TdsRateWithPan,
                    tds_rate_without_pan = @TdsRateWithoutPan,
                    tds_rate_individual = @TdsRateIndividual,
                    tds_rate_company = @TdsRateCompany,
                    threshold_single_payment = @ThresholdSinglePayment,
                    threshold_annual = @ThresholdAnnual,
                    applies_to_individual = @AppliesToIndividual,
                    applies_to_huf = @AppliesToHuf,
                    applies_to_company = @AppliesToCompany,
                    applies_to_firm = @AppliesToFirm,
                    applies_to_llp = @AppliesToLlp,
                    applies_to_trust = @AppliesToTrust,
                    applies_to_aop_boi = @AppliesToAopBoi,
                    applies_to_government = @AppliesToGovernment,
                    lower_certificate_allowed = @LowerCertificateAllowed,
                    nil_certificate_allowed = @NilCertificateAllowed,
                    exemption_notes = @ExemptionNotes,
                    effective_from = @EffectiveFrom,
                    effective_to = @EffectiveTo,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "DELETE FROM tds_tag_rules WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id });
        }

        // ==================== Tag-based Queries ====================

        public async Task<TdsTagRule?> GetByTagIdAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.tag_id = @tagId
                  AND r.is_active = true
                  AND r.effective_from <= CURRENT_DATE
                  AND (r.effective_to IS NULL OR r.effective_to >= CURRENT_DATE)
                ORDER BY r.effective_from DESC
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<TdsTagRule>(sql, new { tagId });
        }

        public async Task<IEnumerable<TdsTagRule>> GetAllByTagIdAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.tag_id = @tagId
                ORDER BY r.effective_from DESC";

            return await connection.QueryAsync<TdsTagRule>(sql, new { tagId });
        }

        public async Task<IEnumerable<TdsTagRule>> GetBySectionAsync(Guid companyId, string tdsSection)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.company_id = @companyId
                  AND r.tds_section = @tdsSection
                  AND r.is_active = true
                ORDER BY t.name";

            return await connection.QueryAsync<TdsTagRule>(sql, new { companyId, tdsSection });
        }

        public async Task<IEnumerable<TdsTagRule>> GetEffectiveRulesAsync(Guid companyId, DateOnly? asOfDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE r.company_id = @companyId
                  AND r.is_active = true
                  AND r.effective_from <= @effectiveDate
                  AND (r.effective_to IS NULL OR r.effective_to >= @effectiveDate)
                ORDER BY r.tds_section, t.name";

            return await connection.QueryAsync<TdsTagRule>(sql, new { companyId, effectiveDate });
        }

        // ==================== Party TDS Detection ====================

        public async Task<TdsTagRule?> GetRuleForPartyAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM party_tags pt
                JOIN tags t ON t.id = pt.tag_id
                JOIN tds_tag_rules r ON r.tag_id = t.id
                WHERE pt.party_id = @partyId
                  AND t.tag_group = 'tds_section'
                  AND t.is_active = true
                  AND r.is_active = true
                  AND r.effective_from <= CURRENT_DATE
                  AND (r.effective_to IS NULL OR r.effective_to >= CURRENT_DATE)
                ORDER BY pt.created_at ASC
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<TdsTagRule>(sql, new { partyId });
        }

        public async Task<IEnumerable<TdsTagRule>> GetRulesForPartyTagsAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM party_tags pt
                JOIN tags t ON t.id = pt.tag_id
                JOIN tds_tag_rules r ON r.tag_id = t.id
                WHERE pt.party_id = @partyId
                  AND t.tag_group = 'tds_section'
                  AND t.is_active = true
                  AND r.is_active = true
                  AND r.effective_from <= CURRENT_DATE
                  AND (r.effective_to IS NULL OR r.effective_to >= CURRENT_DATE)
                ORDER BY pt.created_at ASC";

            return await connection.QueryAsync<TdsTagRule>(sql, new { partyId });
        }

        // ==================== Paged Query ====================

        public async Task<(IEnumerable<TdsTagRule> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? tdsSection = null,
            bool? isActive = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereConditions = new List<string> { "r.company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrEmpty(tdsSection))
            {
                whereConditions.Add("r.tds_section = @tdsSection");
                parameters.Add("tdsSection", tdsSection);
            }

            if (isActive.HasValue)
            {
                whereConditions.Add("r.is_active = @isActive");
                parameters.Add("isActive", isActive.Value);
            }

            var whereClause = string.Join(" AND ", whereConditions);

            // Validate sort column
            var validSortColumns = new[] { "tds_section", "tds_rate_with_pan", "threshold_annual", "created_at", "tag_name" };
            var orderColumn = validSortColumns.Contains(sortBy?.ToLower()) ? sortBy!.ToLower() : "tds_section";
            var orderDirection = sortDescending ? "DESC" : "ASC";

            // Count query
            var countSql = $"SELECT COUNT(*) FROM tds_tag_rules r WHERE {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Data query
            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var dataSql = $@"
                SELECT r.*, t.name as tag_name, t.code as tag_code, t.color as tag_color
                FROM tds_tag_rules r
                LEFT JOIN tags t ON t.id = r.tag_id
                WHERE {whereClause}
                ORDER BY {orderColumn} {orderDirection}
                OFFSET @offset LIMIT @limit";

            var items = await connection.QueryAsync<TdsTagRule>(dataSql, parameters);

            return (items, totalCount);
        }

        // ==================== Seeding ====================

        public async Task SeedDefaultsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT seed_tds_system(@companyId)";
            await connection.ExecuteAsync(sql, new { companyId });
        }

        public async Task<bool> HasTdsTagsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM tags
                    WHERE company_id = @companyId
                      AND tag_group = 'tds_section'
                      AND is_system = true
                )";

            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId });
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<TdsTagRule>> BulkAddAsync(IEnumerable<TdsTagRule> rules)
        {
            var results = new List<TdsTagRule>();
            foreach (var rule in rules)
            {
                results.Add(await AddAsync(rule));
            }
            return results;
        }
    }
}
