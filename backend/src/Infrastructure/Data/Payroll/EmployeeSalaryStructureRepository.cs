using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class EmployeeSalaryStructureRepository : IEmployeeSalaryStructureRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "employee_id", "company_id", "effective_from", "effective_to", "annual_ctc",
            "basic_salary", "hra", "dearness_allowance", "conveyance_allowance", "medical_allowance",
            "special_allowance", "other_allowances", "lta_annual", "bonus_annual", "pf_employer_monthly",
            "esi_employer_monthly", "gratuity_monthly", "monthly_gross", "is_active", "revision_reason",
            "approved_by", "approved_at", "created_at", "updated_at", "created_by", "updated_by"
        };

        public EmployeeSalaryStructureRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EmployeeSalaryStructure?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeSalaryStructure>(
                "SELECT * FROM employee_salary_structures WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<EmployeeSalaryStructure>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeSalaryStructure>(
                "SELECT * FROM employee_salary_structures ORDER BY effective_from DESC");
        }

        public async Task<EmployeeSalaryStructure?> GetCurrentByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeSalaryStructure>(
                @"SELECT * FROM employee_salary_structures
                  WHERE employee_id = @employeeId
                    AND is_active = true
                    AND effective_from <= CURRENT_DATE
                    AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)
                  ORDER BY effective_from DESC
                  LIMIT 1",
                new { employeeId });
        }

        public async Task<IEnumerable<EmployeeSalaryStructure>> GetHistoryByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeSalaryStructure>(
                "SELECT * FROM employee_salary_structures WHERE employee_id = @employeeId ORDER BY effective_from DESC",
                new { employeeId });
        }

        public async Task<EmployeeSalaryStructure?> GetEffectiveAsOfDateAsync(Guid employeeId, DateTime asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var asOfDateOnly = asOfDate.Date;
            return await connection.QueryFirstOrDefaultAsync<EmployeeSalaryStructure>(
                @"SELECT * FROM employee_salary_structures
                  WHERE employee_id = @employeeId
                    AND effective_from <= @asOfDateOnly
                    AND (effective_to IS NULL OR effective_to >= @asOfDateOnly)
                  ORDER BY effective_from DESC
                  LIMIT 1",
                new { employeeId, asOfDateOnly });
        }

        public async Task<IEnumerable<EmployeeSalaryStructure>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeSalaryStructure>(
                "SELECT * FROM employee_salary_structures WHERE company_id = @companyId ORDER BY effective_from DESC",
                new { companyId });
        }

        public async Task<IEnumerable<EmployeeSalaryStructure>> GetActiveStructuresAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM employee_salary_structures
                        WHERE is_active = true
                          AND effective_from <= CURRENT_DATE
                          AND (effective_to IS NULL OR effective_to >= CURRENT_DATE)";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            sql += " ORDER BY effective_from DESC";

            return await connection.QueryAsync<EmployeeSalaryStructure>(sql, new { companyId });
        }

        public async Task<(IEnumerable<EmployeeSalaryStructure> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("employee_salary_structures", AllowedColumns)
                .SearchAcross(new[] { "revision_reason", "approved_by" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "effective_from";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<EmployeeSalaryStructure>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<EmployeeSalaryStructure> AddAsync(EmployeeSalaryStructure entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO employee_salary_structures
                (employee_id, company_id, effective_from, effective_to, annual_ctc, basic_salary,
                 hra, dearness_allowance, conveyance_allowance, medical_allowance, special_allowance,
                 other_allowances, lta_annual, bonus_annual, pf_employer_monthly, esi_employer_monthly,
                 gratuity_monthly, monthly_gross, is_active, revision_reason, approved_by, approved_at,
                 created_at, updated_at, created_by, updated_by)
                VALUES
                (@EmployeeId, @CompanyId, @EffectiveFrom, @EffectiveTo, @AnnualCtc, @BasicSalary,
                 @Hra, @DearnessAllowance, @ConveyanceAllowance, @MedicalAllowance, @SpecialAllowance,
                 @OtherAllowances, @LtaAnnual, @BonusAnnual, @PfEmployerMonthly, @EsiEmployerMonthly,
                 @GratuityMonthly, @MonthlyGross, @IsActive, @RevisionReason, @ApprovedBy, @ApprovedAt,
                 NOW(), NOW(), @CreatedBy, @UpdatedBy)
                RETURNING *";

            return await connection.QuerySingleAsync<EmployeeSalaryStructure>(sql, entity);
        }

        public async Task UpdateAsync(EmployeeSalaryStructure entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE employee_salary_structures SET
                employee_id = @EmployeeId,
                company_id = @CompanyId,
                effective_from = @EffectiveFrom,
                effective_to = @EffectiveTo,
                annual_ctc = @AnnualCtc,
                basic_salary = @BasicSalary,
                hra = @Hra,
                dearness_allowance = @DearnessAllowance,
                conveyance_allowance = @ConveyanceAllowance,
                medical_allowance = @MedicalAllowance,
                special_allowance = @SpecialAllowance,
                other_allowances = @OtherAllowances,
                lta_annual = @LtaAnnual,
                bonus_annual = @BonusAnnual,
                pf_employer_monthly = @PfEmployerMonthly,
                esi_employer_monthly = @EsiEmployerMonthly,
                gratuity_monthly = @GratuityMonthly,
                monthly_gross = @MonthlyGross,
                is_active = @IsActive,
                revision_reason = @RevisionReason,
                approved_by = @ApprovedBy,
                approved_at = @ApprovedAt,
                updated_at = NOW(),
                updated_by = @UpdatedBy
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employee_salary_structures WHERE id = @id", new { id });
        }

        public async Task DeactivatePreviousStructuresAsync(Guid employeeId, DateTime effectiveFrom, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var effectiveFromDate = effectiveFrom.Date;
            var effectiveToDate = effectiveFromDate.AddDays(-1);
            var sql = @"UPDATE employee_salary_structures SET
                is_active = false,
                effective_to = @effectiveToDate,
                updated_at = NOW()
                WHERE employee_id = @employeeId
                  AND effective_from < @effectiveFromDate
                  AND (effective_to IS NULL OR effective_to >= @effectiveFromDate)";

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
            }

            await connection.ExecuteAsync(sql, new { employeeId, effectiveFromDate, effectiveToDate, excludeId });
        }

        public async Task<bool> HasOverlappingStructureAsync(Guid employeeId, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var effectiveFromDate = effectiveFrom.Date;
            var sql = @"SELECT COUNT(*) FROM employee_salary_structures
                        WHERE employee_id = @employeeId
                          AND is_active = true
                          AND effective_from <= COALESCE(@effectiveToDate::date, DATE '9999-12-31')
                          AND (effective_to IS NULL OR effective_to >= @effectiveFromDate)";

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
            }

            var effectiveToDate = effectiveTo?.Date;
            var count = await connection.ExecuteScalarAsync<int>(sql, new { employeeId, effectiveFromDate, effectiveToDate, excludeId });
            return count > 0;
        }
    }
}
