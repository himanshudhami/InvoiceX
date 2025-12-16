using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class ProfessionalTaxSlabRepository : IProfessionalTaxSlabRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "state", "min_monthly_income", "max_monthly_income", "monthly_tax",
            "february_tax", "effective_from", "effective_to", "is_active", "created_at", "updated_at"
        };

        public ProfessionalTaxSlabRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ProfessionalTaxSlab?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ProfessionalTaxSlab>(
                "SELECT * FROM professional_tax_slabs WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<ProfessionalTaxSlab>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ProfessionalTaxSlab>(
                "SELECT * FROM professional_tax_slabs ORDER BY state, min_monthly_income");
        }

        public async Task<IEnumerable<ProfessionalTaxSlab>> GetByStateAsync(string state)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ProfessionalTaxSlab>(
                @"SELECT * FROM professional_tax_slabs
                  WHERE state = @state AND is_active = true
                  ORDER BY min_monthly_income",
                new { state });
        }

        public async Task<ProfessionalTaxSlab?> GetSlabForIncomeAsync(decimal monthlyIncome, string state)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ProfessionalTaxSlab>(
                @"SELECT * FROM professional_tax_slabs
                  WHERE state = @state
                    AND is_active = true
                    AND min_monthly_income <= @monthlyIncome
                    AND (max_monthly_income IS NULL OR max_monthly_income >= @monthlyIncome)
                  ORDER BY min_monthly_income DESC
                  LIMIT 1",
                new { monthlyIncome, state });
        }

        public async Task<(IEnumerable<ProfessionalTaxSlab> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("professional_tax_slabs", AllowedColumns)
                .SearchAcross(new[] { "state" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "state";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<ProfessionalTaxSlab>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<ProfessionalTaxSlab> AddAsync(ProfessionalTaxSlab entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO professional_tax_slabs
                (state, min_monthly_income, max_monthly_income, monthly_tax,
                 february_tax, effective_from, effective_to, is_active, created_at, updated_at)
                VALUES
                (@State, @MinMonthlyIncome, @MaxMonthlyIncome, @MonthlyTax,
                 @FebruaryTax, @EffectiveFrom, @EffectiveTo, @IsActive, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<ProfessionalTaxSlab>(sql, entity);
        }

        public async Task UpdateAsync(ProfessionalTaxSlab entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE professional_tax_slabs SET
                state = @State,
                min_monthly_income = @MinMonthlyIncome,
                max_monthly_income = @MaxMonthlyIncome,
                monthly_tax = @MonthlyTax,
                february_tax = @FebruaryTax,
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
            await connection.ExecuteAsync("DELETE FROM professional_tax_slabs WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<ProfessionalTaxSlab>> BulkAddAsync(IEnumerable<ProfessionalTaxSlab> entities)
        {
            var results = new List<ProfessionalTaxSlab>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entity in entities)
            {
                var sql = @"INSERT INTO professional_tax_slabs
                    (state, min_monthly_income, max_monthly_income, monthly_tax,
                     february_tax, effective_from, effective_to, is_active, created_at, updated_at)
                    VALUES
                    (@State, @MinMonthlyIncome, @MaxMonthlyIncome, @MonthlyTax,
                     @FebruaryTax, @EffectiveFrom, @EffectiveTo, @IsActive, NOW(), NOW())
                    RETURNING *";
                var created = await connection.QuerySingleAsync<ProfessionalTaxSlab>(sql, entity);
                results.Add(created);
            }

            return results;
        }

        /// <summary>
        /// Get distinct states that have PT slabs configured
        /// </summary>
        public async Task<IEnumerable<string>> GetDistinctStatesAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<string>(
                "SELECT DISTINCT state FROM professional_tax_slabs WHERE is_active = true ORDER BY state");
        }

        /// <summary>
        /// Check if a slab exists for a state within a given income range (for overlap validation)
        /// </summary>
        public async Task<bool> ExistsForStateAndRangeAsync(string state, decimal minIncome, decimal? maxIncome, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT EXISTS(
                SELECT 1 FROM professional_tax_slabs
                WHERE state = @state
                  AND is_active = true
                  AND (@excludeId IS NULL OR id != @excludeId)
                  AND (
                    (min_monthly_income <= @minIncome AND (max_monthly_income IS NULL OR max_monthly_income >= @minIncome))
                    OR
                    (@maxIncome IS NOT NULL AND min_monthly_income <= @maxIncome AND (max_monthly_income IS NULL OR max_monthly_income >= @maxIncome))
                    OR
                    (min_monthly_income >= @minIncome AND (@maxIncome IS NULL OR min_monthly_income <= @maxIncome))
                  )
            )";
            return await connection.ExecuteScalarAsync<bool>(sql, new { state, minIncome, maxIncome, excludeId });
        }
    }
}
