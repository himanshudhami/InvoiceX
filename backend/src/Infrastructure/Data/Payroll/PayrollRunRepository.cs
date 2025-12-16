using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class PayrollRunRepository : IPayrollRunRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "payroll_month", "payroll_year", "financial_year", "status",
            "total_employees", "total_contractors", "total_gross_salary", "total_deductions",
            "total_net_salary", "total_employer_pf", "total_employer_esi", "total_employer_cost",
            "computed_by", "computed_at", "approved_by", "approved_at", "paid_by", "paid_at",
            "payment_reference", "payment_mode", "remarks", "created_at", "updated_at",
            "created_by", "updated_by"
        };

        public PayrollRunRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PayrollRun?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<PayrollRun>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollRun>(
                "SELECT * FROM payroll_runs ORDER BY payroll_year DESC, payroll_month DESC");
        }

        public async Task<PayrollRun?> GetByCompanyAndMonthAsync(Guid companyId, int payrollMonth, int payrollYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE company_id = @companyId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear",
                new { companyId, payrollMonth, payrollYear });
        }

        public async Task<IEnumerable<PayrollRun>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE company_id = @companyId ORDER BY payroll_year DESC, payroll_month DESC",
                new { companyId });
        }

        public async Task<IEnumerable<PayrollRun>> GetByFinancialYearAsync(string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE financial_year = @financialYear ORDER BY payroll_year DESC, payroll_month DESC",
                new { financialYear });
        }

        public async Task<IEnumerable<PayrollRun>> GetByStatusAsync(string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE status = @status ORDER BY payroll_year DESC, payroll_month DESC",
                new { status });
        }

        public async Task<PayrollRun?> GetLatestByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PayrollRun>(
                "SELECT * FROM payroll_runs WHERE company_id = @companyId ORDER BY payroll_year DESC, payroll_month DESC LIMIT 1",
                new { companyId });
        }

        public async Task<(IEnumerable<PayrollRun> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("payroll_runs", AllowedColumns)
                .SearchAcross(new[] { "financial_year", "status", "payment_reference", "remarks" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<PayrollRun>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<PayrollRun> AddAsync(PayrollRun entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO payroll_runs
                (company_id, payroll_month, payroll_year, financial_year, status, total_employees,
                 total_contractors, total_gross_salary, total_deductions, total_net_salary,
                 total_employer_pf, total_employer_esi, total_employer_cost, computed_by, computed_at,
                 approved_by, approved_at, paid_by, paid_at, payment_reference, payment_mode,
                 remarks, created_at, updated_at, created_by, updated_by)
                VALUES
                (@CompanyId, @PayrollMonth, @PayrollYear, @FinancialYear, @Status, @TotalEmployees,
                 @TotalContractors, @TotalGrossSalary, @TotalDeductions, @TotalNetSalary,
                 @TotalEmployerPf, @TotalEmployerEsi, @TotalEmployerCost, @ComputedBy, @ComputedAt,
                 @ApprovedBy, @ApprovedAt, @PaidBy, @PaidAt, @PaymentReference, @PaymentMode,
                 @Remarks, NOW(), NOW(), @CreatedBy, @UpdatedBy)
                RETURNING *";

            return await connection.QuerySingleAsync<PayrollRun>(sql, entity);
        }

        public async Task UpdateAsync(PayrollRun entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_runs SET
                company_id = @CompanyId,
                payroll_month = @PayrollMonth,
                payroll_year = @PayrollYear,
                financial_year = @FinancialYear,
                status = @Status,
                total_employees = @TotalEmployees,
                total_contractors = @TotalContractors,
                total_gross_salary = @TotalGrossSalary,
                total_deductions = @TotalDeductions,
                total_net_salary = @TotalNetSalary,
                total_employer_pf = @TotalEmployerPf,
                total_employer_esi = @TotalEmployerEsi,
                total_employer_cost = @TotalEmployerCost,
                computed_by = @ComputedBy,
                computed_at = @ComputedAt,
                approved_by = @ApprovedBy,
                approved_at = @ApprovedAt,
                paid_by = @PaidBy,
                paid_at = @PaidAt,
                payment_reference = @PaymentReference,
                payment_mode = @PaymentMode,
                remarks = @Remarks,
                updated_at = NOW(),
                updated_by = @UpdatedBy
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM payroll_runs WHERE id = @id", new { id });
        }

        public async Task<bool> ExistsForCompanyAndMonthAsync(Guid companyId, int payrollMonth, int payrollYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM payroll_runs WHERE company_id = @companyId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear AND id != @excludeId"
                : "SELECT COUNT(*) FROM payroll_runs WHERE company_id = @companyId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { companyId, payrollMonth, payrollYear, excludeId });
            return count > 0;
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? updatedBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_runs SET
                status = @status,
                computed_at = CASE WHEN @status = 'computed' THEN NOW() ELSE computed_at END,
                computed_by = CASE WHEN @status = 'computed' THEN @updatedBy ELSE computed_by END,
                approved_at = CASE WHEN @status = 'approved' THEN NOW() ELSE approved_at END,
                approved_by = CASE WHEN @status = 'approved' THEN @updatedBy ELSE approved_by END,
                paid_at = CASE WHEN @status = 'paid' THEN NOW() ELSE paid_at END,
                paid_by = CASE WHEN @status = 'paid' THEN @updatedBy ELSE paid_by END,
                updated_at = NOW(),
                updated_by = @updatedBy
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, status, updatedBy });
        }

        public async Task UpdateTotalsAsync(Guid id, int totalEmployees, int totalContractors,
            decimal totalGross, decimal totalDeductions, decimal totalNet,
            decimal totalEmployerPf, decimal totalEmployerEsi, decimal totalEmployerCost)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_runs SET
                total_employees = @totalEmployees,
                total_contractors = @totalContractors,
                total_gross_salary = @totalGross,
                total_deductions = @totalDeductions,
                total_net_salary = @totalNet,
                total_employer_pf = @totalEmployerPf,
                total_employer_esi = @totalEmployerEsi,
                total_employer_cost = @totalEmployerCost,
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, totalEmployees, totalContractors, totalGross, totalDeductions, totalNet, totalEmployerPf, totalEmployerEsi, totalEmployerCost });
        }
    }
}
