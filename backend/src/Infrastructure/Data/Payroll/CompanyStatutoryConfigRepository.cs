using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class CompanyStatutoryConfigRepository : ICompanyStatutoryConfigRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "pf_enabled", "pf_registration_number", "pf_employee_rate",
            "pf_employer_rate", "pf_admin_charges_rate", "pf_edli_rate", "pf_wage_ceiling",
            "pf_include_special_allowance", "pf_calculation_mode", "pf_trust_type",
            "pf_trust_name", "pf_trust_registration_number", "restricted_pf_max_wage",
            "esi_enabled", "esi_registration_number",
            "esi_employee_rate", "esi_employer_rate", "esi_gross_ceiling", "pt_enabled",
            "pt_state", "pt_registration_number", "tan_number", "default_tax_regime",
            "gratuity_enabled", "gratuity_rate", "effective_from", "effective_to",
            "is_active", "created_at", "updated_at", "created_by", "updated_by"
        };

        public CompanyStatutoryConfigRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CompanyStatutoryConfig?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CompanyStatutoryConfig>(
                "SELECT * FROM company_statutory_configs WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<CompanyStatutoryConfig>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CompanyStatutoryConfig>(
                "SELECT * FROM company_statutory_configs ORDER BY created_at DESC");
        }

        public async Task<CompanyStatutoryConfig?> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM company_statutory_configs 
                      WHERE company_id = @companyId 
                        AND is_active = true
                        AND effective_from <= CURRENT_DATE
                        AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                      ORDER BY effective_from DESC
                      LIMIT 1";
            
            System.Diagnostics.Debug.WriteLine($"[GetByCompanyIdAsync] SQL: {sql}");
            System.Diagnostics.Debug.WriteLine($"[GetByCompanyIdAsync] CompanyId: {companyId}");
            
            return await connection.QueryFirstOrDefaultAsync<CompanyStatutoryConfig>(sql, new { companyId });
        }

        public async Task<IEnumerable<CompanyStatutoryConfig>> GetActiveConfigsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CompanyStatutoryConfig>(
                @"SELECT * FROM company_statutory_configs 
                  WHERE is_active = true
                    AND effective_from <= CURRENT_DATE
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY effective_from DESC");
        }

        public async Task<(IEnumerable<CompanyStatutoryConfig> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("company_statutory_configs", AllowedColumns)
                .SearchAcross(new[] { "pf_registration_number", "esi_registration_number", "pt_state", "pt_registration_number" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            // Log the queries for debugging
            var fullSql = dataSql + ";" + countSql;
            System.Diagnostics.Debug.WriteLine($"[GetPagedAsync] Data SQL: {dataSql}");
            System.Diagnostics.Debug.WriteLine($"[GetPagedAsync] Count SQL: {countSql}");
            System.Diagnostics.Debug.WriteLine($"[GetPagedAsync] Full SQL: {fullSql}");

            using var multi = await connection.QueryMultipleAsync(fullSql, parameters);
            var items = await multi.ReadAsync<CompanyStatutoryConfig>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<CompanyStatutoryConfig> AddAsync(CompanyStatutoryConfig entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO company_statutory_configs
                (company_id, pf_enabled, pf_registration_number, pf_employee_rate, pf_employer_rate,
                 pf_wage_ceiling, pf_include_special_allowance, pf_calculation_mode, pf_trust_type,
                 pf_trust_name, pf_trust_registration_number, restricted_pf_max_wage,
                 esi_enabled, esi_registration_number, esi_employee_rate, esi_employer_rate,
                 esi_gross_ceiling, pt_enabled, pt_state, pt_registration_number,
                 gratuity_enabled, gratuity_rate, effective_from,
                 is_active, created_at, updated_at)
                VALUES
                (@CompanyId, @PfEnabled, @PfRegistrationNumber, @PfEmployeeRate, @PfEmployerRate,
                 @PfWageCeiling, @PfIncludeSpecialAllowance, @PfCalculationMode, @PfTrustType,
                 @PfTrustName, @PfTrustRegistrationNumber, @RestrictedPfMaxWage,
                 @EsiEnabled, @EsiRegistrationNumber, @EsiEmployeeRate, @EsiEmployerRate,
                 @EsiGrossCeiling, @PtEnabled, @PtState, @PtRegistrationNumber,
                 @GratuityEnabled, @GratuityRate, @EffectiveFrom,
                 @IsActive, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<CompanyStatutoryConfig>(sql, entity);
        }

        public async Task UpdateAsync(CompanyStatutoryConfig entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE company_statutory_configs SET
                company_id = @CompanyId,
                pf_enabled = @PfEnabled,
                pf_registration_number = @PfRegistrationNumber,
                pf_employee_rate = @PfEmployeeRate,
                pf_employer_rate = @PfEmployerRate,
                pf_wage_ceiling = @PfWageCeiling,
                pf_include_special_allowance = @PfIncludeSpecialAllowance,
                pf_calculation_mode = @PfCalculationMode,
                pf_trust_type = @PfTrustType,
                pf_trust_name = @PfTrustName,
                pf_trust_registration_number = @PfTrustRegistrationNumber,
                restricted_pf_max_wage = @RestrictedPfMaxWage,
                esi_enabled = @EsiEnabled,
                esi_registration_number = @EsiRegistrationNumber,
                esi_employee_rate = @EsiEmployeeRate,
                esi_employer_rate = @EsiEmployerRate,
                esi_gross_ceiling = @EsiGrossCeiling,
                pt_enabled = @PtEnabled,
                pt_state = @PtState,
                pt_registration_number = @PtRegistrationNumber,
                gratuity_enabled = @GratuityEnabled,
                gratuity_rate = @GratuityRate,
                is_active = @IsActive,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM company_statutory_configs WHERE id = @id", new { id });
        }

        public async Task<bool> ExistsForCompanyAsync(Guid companyId, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? @"SELECT COUNT(*) FROM company_statutory_configs 
                   WHERE company_id = @companyId 
                     AND id != @excludeId 
                     AND is_active = true
                     AND effective_from <= CURRENT_DATE
                     AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)"
                : @"SELECT COUNT(*) FROM company_statutory_configs 
                   WHERE company_id = @companyId 
                     AND is_active = true
                     AND effective_from <= CURRENT_DATE
                     AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { companyId, excludeId });
            return count > 0;
        }
    }
}
