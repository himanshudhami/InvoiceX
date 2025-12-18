using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class EmployeePayrollInfoRepository : IEmployeePayrollInfoRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "employee_id", "company_id", "uan", "pf_account_number", "esi_number",
            "bank_account_number", "bank_name", "bank_ifsc", "tax_regime", "pan_number",
            "payroll_type", "is_pf_applicable", "is_esi_applicable", "is_pt_applicable",
            "opted_for_restricted_pf",
            "date_of_joining", "date_of_leaving", "is_active", "created_at", "updated_at",
            "residential_status", "date_of_birth", "tax_regime_effective_from", "work_state"
        };

        public EmployeePayrollInfoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EmployeePayrollInfo?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeePayrollInfo>(
                "SELECT * FROM employee_payroll_info WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<EmployeePayrollInfo>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeePayrollInfo>(
                "SELECT * FROM employee_payroll_info ORDER BY created_at DESC");
        }

        public async Task<EmployeePayrollInfo?> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeePayrollInfo>(
                "SELECT * FROM employee_payroll_info WHERE employee_id = @employeeId",
                new { employeeId });
        }

        public async Task<IEnumerable<EmployeePayrollInfo>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeePayrollInfo>(
                "SELECT * FROM employee_payroll_info WHERE company_id = @companyId AND is_active = true ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<EmployeePayrollInfo>> GetByPayrollTypeAsync(string payrollType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeePayrollInfo>(
                "SELECT * FROM employee_payroll_info WHERE payroll_type = @payrollType AND is_active = true ORDER BY created_at DESC",
                new { payrollType });
        }

        public async Task<IEnumerable<EmployeePayrollInfo>> GetActiveEmployeesForPayrollAsync(Guid companyId, int payrollMonth, int payrollYear, string? payrollType = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Calculate the last day of the payroll month
            var payrollMonthEnd = new DateTime(payrollYear, payrollMonth, DateTime.DaysInMonth(payrollYear, payrollMonth));
            var payrollMonthStart = new DateTime(payrollYear, payrollMonth, 1);

            var sql = @"SELECT * FROM employee_payroll_info
                        WHERE company_id = @companyId
                          AND (date_of_joining IS NULL OR date_of_joining <= @payrollMonthEnd)
                          AND (date_of_leaving IS NULL OR date_of_leaving >= @payrollMonthStart)";

            if (!string.IsNullOrEmpty(payrollType))
            {
                sql += " AND payroll_type = @payrollType";
            }

            sql += " ORDER BY created_at";

            return await connection.QueryAsync<EmployeePayrollInfo>(sql, new { companyId, payrollType, payrollMonthEnd, payrollMonthStart });
        }

        public async Task<(IEnumerable<EmployeePayrollInfo> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("employee_payroll_info", AllowedColumns)
                .SearchAcross(new[] { "uan", "pf_account_number", "esi_number", "pan_number", "bank_account_number" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<EmployeePayrollInfo>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<EmployeePayrollInfo> AddAsync(EmployeePayrollInfo entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO employee_payroll_info
                (employee_id, company_id, uan, pf_account_number, esi_number, bank_account_number,
                 bank_name, bank_ifsc, tax_regime, pan_number, payroll_type, is_pf_applicable,
                 is_esi_applicable, is_pt_applicable, opted_for_restricted_pf,
                 date_of_joining, date_of_leaving, is_active,
                 residential_status, date_of_birth, tax_regime_effective_from, work_state,
                 created_at, updated_at)
                VALUES
                (@EmployeeId, @CompanyId, @Uan, @PfAccountNumber, @EsiNumber, @BankAccountNumber,
                 @BankName, @BankIfsc, @TaxRegime, @PanNumber, @PayrollType, @IsPfApplicable,
                 @IsEsiApplicable, @IsPtApplicable, @OptedForRestrictedPf,
                 @DateOfJoining, @DateOfLeaving, @IsActive,
                 @ResidentialStatus, @DateOfBirth, @TaxRegimeEffectiveFrom, @WorkState,
                 NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<EmployeePayrollInfo>(sql, entity);
        }

        public async Task UpdateAsync(EmployeePayrollInfo entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE employee_payroll_info SET
                employee_id = @EmployeeId,
                company_id = @CompanyId,
                uan = @Uan,
                pf_account_number = @PfAccountNumber,
                esi_number = @EsiNumber,
                bank_account_number = @BankAccountNumber,
                bank_name = @BankName,
                bank_ifsc = @BankIfsc,
                tax_regime = @TaxRegime,
                pan_number = @PanNumber,
                payroll_type = @PayrollType,
                is_pf_applicable = @IsPfApplicable,
                is_esi_applicable = @IsEsiApplicable,
                is_pt_applicable = @IsPtApplicable,
                opted_for_restricted_pf = @OptedForRestrictedPf,
                date_of_joining = @DateOfJoining,
                date_of_leaving = @DateOfLeaving,
                is_active = @IsActive,
                residential_status = @ResidentialStatus,
                date_of_birth = @DateOfBirth,
                tax_regime_effective_from = @TaxRegimeEffectiveFrom,
                work_state = @WorkState,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employee_payroll_info WHERE id = @id", new { id });
        }

        public async Task<bool> ExistsForEmployeeAsync(Guid employeeId, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM employee_payroll_info WHERE employee_id = @employeeId AND id != @excludeId"
                : "SELECT COUNT(*) FROM employee_payroll_info WHERE employee_id = @employeeId";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { employeeId, excludeId });
            return count > 0;
        }

        public async Task<bool> UanExistsAsync(string uan, Guid? excludeEmployeeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeEmployeeId.HasValue
                ? "SELECT COUNT(*) FROM employee_payroll_info WHERE uan = @uan AND employee_id != @excludeEmployeeId"
                : "SELECT COUNT(*) FROM employee_payroll_info WHERE uan = @uan";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { uan, excludeEmployeeId });
            return count > 0;
        }

        public async Task<bool> EsiNumberExistsAsync(string esiNumber, Guid? excludeEmployeeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeEmployeeId.HasValue
                ? "SELECT COUNT(*) FROM employee_payroll_info WHERE esi_number = @esiNumber AND employee_id != @excludeEmployeeId"
                : "SELECT COUNT(*) FROM employee_payroll_info WHERE esi_number = @esiNumber";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { esiNumber, excludeEmployeeId });
            return count > 0;
        }

        public async Task ResignEmployeeAsync(Guid employeeId, DateTime lastWorkingDay)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE employee_payroll_info
                SET date_of_leaving = @lastWorkingDay,
                    is_active = false,
                    updated_at = NOW()
                WHERE employee_id = @employeeId",
                new { employeeId, lastWorkingDay });
        }

        public async Task RejoinEmployeeAsync(Guid employeeId, DateTime? rejoiningDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE employee_payroll_info
                SET date_of_leaving = NULL,
                    is_active = true,
                    date_of_joining = COALESCE(@rejoiningDate, date_of_joining),
                    updated_at = NOW()
                WHERE employee_id = @employeeId",
                new { employeeId, rejoiningDate });
        }
    }
}
