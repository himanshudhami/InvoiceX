using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class TaxSlabRepository : ITaxSlabRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "regime", "financial_year", "min_income", "max_income", "rate",
            "cess_rate", "surcharge_rate", "surcharge_threshold", "description",
            "applicable_to_category", "is_active", "created_at", "updated_at"
        };

        public TaxSlabRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TaxSlab?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TaxSlab>(
                "SELECT * FROM tax_slabs WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TaxSlab>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TaxSlab>(
                "SELECT * FROM tax_slabs ORDER BY regime, min_income");
        }

        public async Task<IEnumerable<TaxSlab>> GetByFinancialYearAsync(string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TaxSlab>(
                "SELECT * FROM tax_slabs WHERE financial_year = @financialYear AND is_active = true ORDER BY regime, min_income",
                new { financialYear });
        }

        public async Task<IEnumerable<TaxSlab>> GetByRegimeAndYearAsync(string regime, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TaxSlab>(
                "SELECT * FROM tax_slabs WHERE regime = @regime AND financial_year = @financialYear AND is_active = true ORDER BY min_income",
                new { regime, financialYear });
        }

        /// <summary>
        /// Get tax slabs by regime, financial year, and taxpayer category.
        /// For senior citizens (60+) in old regime, returns senior-specific slabs.
        /// For new regime, returns 'all' slabs regardless of age (new regime has no age-based differentiation).
        /// </summary>
        public async Task<IEnumerable<TaxSlab>> GetByRegimeYearAndCategoryAsync(string regime, string financialYear, string category)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // New regime has no age-based differentiation, always use 'all'
            var effectiveCategory = regime == "new" ? "all" : category;

            return await connection.QueryAsync<TaxSlab>(
                @"SELECT * FROM tax_slabs
                  WHERE regime = @regime
                    AND financial_year = @financialYear
                    AND applicable_to_category = @effectiveCategory
                    AND is_active = true
                  ORDER BY min_income",
                new { regime, financialYear, effectiveCategory });
        }

        public async Task<TaxSlab?> GetSlabForIncomeAsync(decimal income, string regime, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TaxSlab>(
                @"SELECT * FROM tax_slabs
                  WHERE regime = @regime
                    AND financial_year = @financialYear
                    AND is_active = true
                    AND min_income <= @income
                    AND (max_income IS NULL OR max_income >= @income)
                  ORDER BY min_income DESC
                  LIMIT 1",
                new { income, regime, financialYear });
        }

        public async Task<(IEnumerable<TaxSlab> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("tax_slabs", AllowedColumns)
                .SearchAcross(new[] { "regime", "financial_year", "description" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "min_income";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<TaxSlab>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<TaxSlab> AddAsync(TaxSlab entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO tax_slabs
                (regime, financial_year, min_income, max_income, rate, cess_rate,
                 surcharge_rate, surcharge_threshold, description, is_active, created_at, updated_at)
                VALUES
                (@Regime, @FinancialYear, @MinIncome, @MaxIncome, @Rate, @CessRate,
                 @SurchargeRate, @SurchargeThreshold, @Description, @IsActive, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<TaxSlab>(sql, entity);
        }

        public async Task UpdateAsync(TaxSlab entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE tax_slabs SET
                regime = @Regime,
                financial_year = @FinancialYear,
                min_income = @MinIncome,
                max_income = @MaxIncome,
                rate = @Rate,
                cess_rate = @CessRate,
                surcharge_rate = @SurchargeRate,
                surcharge_threshold = @SurchargeThreshold,
                description = @Description,
                is_active = @IsActive,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM tax_slabs WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<TaxSlab>> BulkAddAsync(IEnumerable<TaxSlab> entities)
        {
            var results = new List<TaxSlab>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entity in entities)
            {
                var sql = @"INSERT INTO tax_slabs
                    (regime, financial_year, min_income, max_income, rate, cess_rate,
                     surcharge_rate, surcharge_threshold, description, is_active, created_at, updated_at)
                    VALUES
                    (@Regime, @FinancialYear, @MinIncome, @MaxIncome, @Rate, @CessRate,
                     @SurchargeRate, @SurchargeThreshold, @Description, @IsActive, NOW(), NOW())
                    RETURNING *";
                var created = await connection.QuerySingleAsync<TaxSlab>(sql, entity);
                results.Add(created);
            }

            return results;
        }
    }
}
