using Core.Entities.Intercompany;
using Core.Interfaces.Intercompany;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Intercompany
{
    public class CompanyRelationshipRepository : ICompanyRelationshipRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "parent_company_id", "child_company_id", "relationship_type",
            "ownership_percentage", "effective_from", "effective_to",
            "consolidation_method", "functional_currency", "is_active",
            "notes", "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "relationship_type", "consolidation_method", "notes"
        };

        public CompanyRelationshipRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CompanyRelationship?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CompanyRelationship>(
                "SELECT * FROM company_relationships WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<CompanyRelationship>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CompanyRelationship>(
                "SELECT * FROM company_relationships WHERE is_active = true ORDER BY parent_company_id, ownership_percentage DESC");
        }

        public async Task<(IEnumerable<CompanyRelationship> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("company_relationships", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<CompanyRelationship>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<CompanyRelationship> AddAsync(CompanyRelationship entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO company_relationships (
                    parent_company_id, child_company_id, relationship_type,
                    ownership_percentage, effective_from, effective_to,
                    consolidation_method, functional_currency, is_active,
                    notes, created_at, updated_at, created_by
                ) VALUES (
                    @ParentCompanyId, @ChildCompanyId, @RelationshipType,
                    @OwnershipPercentage, @EffectiveFrom, @EffectiveTo,
                    @ConsolidationMethod, @FunctionalCurrency, @IsActive,
                    @Notes, NOW(), NOW(), @CreatedBy
                ) RETURNING *";
            return await connection.QuerySingleAsync<CompanyRelationship>(sql, entity);
        }

        public async Task UpdateAsync(CompanyRelationship entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE company_relationships SET
                    parent_company_id = @ParentCompanyId,
                    child_company_id = @ChildCompanyId,
                    relationship_type = @RelationshipType,
                    ownership_percentage = @OwnershipPercentage,
                    effective_from = @EffectiveFrom,
                    effective_to = @EffectiveTo,
                    consolidation_method = @ConsolidationMethod,
                    functional_currency = @FunctionalCurrency,
                    is_active = @IsActive,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM company_relationships WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<CompanyRelationship>> GetSubsidiariesAsync(Guid parentCompanyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CompanyRelationship>(
                @"SELECT * FROM company_relationships
                  WHERE parent_company_id = @parentCompanyId
                    AND is_active = true
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY ownership_percentage DESC",
                new { parentCompanyId });
        }

        public async Task<CompanyRelationship?> GetParentAsync(Guid childCompanyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CompanyRelationship>(
                @"SELECT * FROM company_relationships
                  WHERE child_company_id = @childCompanyId
                    AND is_active = true
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY ownership_percentage DESC
                  LIMIT 1",
                new { childCompanyId });
        }

        public async Task<IEnumerable<CompanyRelationship>> GetRelationshipsForCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CompanyRelationship>(
                @"SELECT * FROM company_relationships
                  WHERE (parent_company_id = @companyId OR child_company_id = @companyId)
                    AND is_active = true
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY ownership_percentage DESC",
                new { companyId });
        }

        public async Task<bool> AreCompaniesRelatedAsync(Guid companyId1, Guid companyId2)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM company_relationships
                  WHERE is_active = true
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                    AND (
                        (parent_company_id = @companyId1 AND child_company_id = @companyId2) OR
                        (parent_company_id = @companyId2 AND child_company_id = @companyId1)
                    )",
                new { companyId1, companyId2 });
            return count > 0;
        }

        public async Task<IEnumerable<Guid>> GetGroupCompanyIdsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Get all companies in the same group using recursive CTE
            var sql = @"
                WITH RECURSIVE company_group AS (
                    -- Start with the given company
                    SELECT parent_company_id, child_company_id
                    FROM company_relationships
                    WHERE is_active = true
                      AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                      AND (parent_company_id = @companyId OR child_company_id = @companyId)

                    UNION

                    -- Recursively find related companies
                    SELECT r.parent_company_id, r.child_company_id
                    FROM company_relationships r
                    INNER JOIN company_group g ON
                        r.parent_company_id = g.child_company_id OR
                        r.parent_company_id = g.parent_company_id OR
                        r.child_company_id = g.child_company_id OR
                        r.child_company_id = g.parent_company_id
                    WHERE r.is_active = true
                      AND (r.effective_to IS NULL OR r.effective_to >= CURRENT_DATE)
                )
                SELECT DISTINCT id FROM (
                    SELECT parent_company_id as id FROM company_group
                    UNION
                    SELECT child_company_id as id FROM company_group
                    UNION
                    SELECT @companyId as id
                ) all_companies";

            return await connection.QueryAsync<Guid>(sql, new { companyId });
        }
    }
}
