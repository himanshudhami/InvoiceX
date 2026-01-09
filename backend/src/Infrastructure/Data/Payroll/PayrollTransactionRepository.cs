using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class PayrollTransactionRepository : IPayrollTransactionRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "payroll_run_id", "employee_id", "salary_structure_id", "payroll_month",
            "payroll_year", "payroll_type", "working_days", "present_days", "lop_days",
            "basic_earned", "hra_earned", "da_earned", "conveyance_earned", "medical_earned",
            "special_allowance_earned", "other_allowances_earned", "lta_paid", "bonus_paid",
            "arrears", "reimbursements", "incentives", "other_earnings", "gross_earnings",
            "pf_employee", "esi_employee", "professional_tax", "tds_deducted", "loan_recovery",
            "advance_recovery", "other_deductions", "total_deductions", "net_payable",
            "pf_employer", "pf_admin_charges", "pf_edli", "esi_employer", "gratuity_provision",
            "total_employer_cost", "tds_calculation", "tds_hr_override", "tds_override_reason",
            "status", "payment_date", "payment_method", "payment_reference", "bank_account",
            "remarks", "bank_transaction_id", "reconciled_at", "reconciled_by",
            "created_at", "updated_at"
        };

        public PayrollTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PayrollTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<PayrollTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions ORDER BY payroll_year DESC, payroll_month DESC");
        }

        public async Task<IEnumerable<PayrollTransaction>> GetByPayrollRunIdAsync(Guid payrollRunId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions WHERE payroll_run_id = @payrollRunId ORDER BY created_at",
                new { payrollRunId });
        }

        public async Task<PayrollTransaction?> GetByEmployeeAndMonthAsync(Guid employeeId, int payrollMonth, int payrollYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions WHERE employee_id = @employeeId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear",
                new { employeeId, payrollMonth, payrollYear });
        }

        public async Task<IEnumerable<PayrollTransaction>> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions WHERE employee_id = @employeeId ORDER BY payroll_year DESC, payroll_month DESC",
                new { employeeId });
        }

        public async Task<IEnumerable<PayrollTransaction>> GetByFinancialYearAsync(Guid employeeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Parse financial year (e.g., "2024-25") to get date range
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            return await connection.QueryAsync<PayrollTransaction>(
                @"SELECT * FROM payroll_transactions
                  WHERE employee_id = @employeeId
                    AND ((payroll_year = @startYear AND payroll_month >= 4)
                         OR (payroll_year = @endYear AND payroll_month <= 3))
                  ORDER BY payroll_year, payroll_month",
                new { employeeId, startYear, endYear });
        }

        public async Task<IEnumerable<PayrollTransaction>> GetByStatusAsync(string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PayrollTransaction>(
                "SELECT * FROM payroll_transactions WHERE status = @status ORDER BY payroll_year DESC, payroll_month DESC",
                new { status });
        }

        public async Task<(IEnumerable<PayrollTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Extract company_id filter for special handling (requires join with payroll_runs)
            Guid? companyId = null;
            if (filters != null && filters.TryGetValue("company_id", out var companyIdObj))
            {
                companyId = (Guid)companyIdObj;
                filters.Remove("company_id");
            }

            var builder = SqlQueryBuilder
                .From("payroll_transactions", AllowedColumns)
                .SearchAcross(new[] { "status", "payment_reference", "remarks" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            // If company filter is specified, modify SQL to join with payroll_runs
            if (companyId.HasValue)
            {
                // Modify data query to join with payroll_runs for company filtering
                dataSql = dataSql.Replace(
                    "FROM payroll_transactions",
                    "FROM payroll_transactions pt INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id");
                dataSql = dataSql.Replace(
                    "SELECT *",
                    "SELECT pt.*");

                // Prefix ambiguous columns with pt. (columns that exist in both payroll_transactions and payroll_runs)
                var ambiguousColumns = new[] { "payroll_month", "payroll_year", "status", "created_at", "updated_at" };
                foreach (var col in ambiguousColumns)
                {
                    // Replace in WHERE clause (e.g., "payroll_year = " -> "pt.payroll_year = ")
                    dataSql = dataSql.Replace($"{col} =", $"pt.{col} =");
                    dataSql = dataSql.Replace($"{col} IN", $"pt.{col} IN");
                    dataSql = dataSql.Replace($"{col} LIKE", $"pt.{col} LIKE");
                    // Replace in ORDER BY clause
                    dataSql = dataSql.Replace($"ORDER BY {col}", $"ORDER BY pt.{col}");
                }

                // Add company filter to WHERE clause
                if (dataSql.Contains("WHERE"))
                {
                    dataSql = dataSql.Replace("WHERE", "WHERE pr.company_id = @companyId AND");
                }
                else
                {
                    // Find ORDER BY and insert WHERE before it
                    var orderByIndex = dataSql.IndexOf("ORDER BY");
                    if (orderByIndex > 0)
                    {
                        dataSql = dataSql.Insert(orderByIndex, "WHERE pr.company_id = @companyId ");
                    }
                }

                // Modify count query similarly
                countSql = countSql.Replace(
                    "FROM payroll_transactions",
                    "FROM payroll_transactions pt INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id");
                // Prefix ambiguous columns in count query too
                foreach (var col in ambiguousColumns)
                {
                    countSql = countSql.Replace($"{col} =", $"pt.{col} =");
                    countSql = countSql.Replace($"{col} IN", $"pt.{col} IN");
                    countSql = countSql.Replace($"{col} LIKE", $"pt.{col} LIKE");
                }
                if (countSql.Contains("WHERE"))
                {
                    countSql = countSql.Replace("WHERE", "WHERE pr.company_id = @companyId AND");
                }
                else
                {
                    countSql += " WHERE pr.company_id = @companyId";
                }

                // Add parameter using DynamicParameters.Add method
                parameters.Add("companyId", companyId.Value);
            }

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<PayrollTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<PayrollTransaction> AddAsync(PayrollTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO payroll_transactions
                (payroll_run_id, employee_id, salary_structure_id, payroll_month, payroll_year,
                 payroll_type, working_days, present_days, lop_days, basic_earned, hra_earned,
                 da_earned, conveyance_earned, medical_earned, special_allowance_earned,
                 other_allowances_earned, lta_paid, bonus_paid, arrears, reimbursements,
                 incentives, other_earnings, gross_earnings, pf_employee, esi_employee,
                 professional_tax, tds_deducted, loan_recovery, advance_recovery, other_deductions,
                 total_deductions, net_payable, pf_employer, pf_admin_charges, pf_edli,
                 esi_employer, gratuity_provision, total_employer_cost, tds_calculation,
                 tds_hr_override, tds_override_reason, status, payment_date, payment_method,
                 payment_reference, bank_account, remarks, created_at, updated_at)
                VALUES
                (@PayrollRunId, @EmployeeId, @SalaryStructureId, @PayrollMonth, @PayrollYear,
                 @PayrollType, @WorkingDays, @PresentDays, @LopDays, @BasicEarned, @HraEarned,
                 @DaEarned, @ConveyanceEarned, @MedicalEarned, @SpecialAllowanceEarned,
                 @OtherAllowancesEarned, @LtaPaid, @BonusPaid, @Arrears, @Reimbursements,
                 @Incentives, @OtherEarnings, @GrossEarnings, @PfEmployee, @EsiEmployee,
                 @ProfessionalTax, @TdsDeducted, @LoanRecovery, @AdvanceRecovery, @OtherDeductions,
                 @TotalDeductions, @NetPayable, @PfEmployer, @PfAdminCharges, @PfEdli,
                 @EsiEmployer, @GratuityProvision, @TotalEmployerCost, 
                 CASE WHEN @TdsCalculation IS NULL OR @TdsCalculation = '' THEN NULL::jsonb ELSE CAST(@TdsCalculation AS jsonb) END,
                 @TdsHrOverride, @TdsOverrideReason, @Status, @PaymentDate, @PaymentMethod,
                 @PaymentReference, @BankAccount, @Remarks, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<PayrollTransaction>(sql, entity);
        }

        public async Task UpdateAsync(PayrollTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_transactions SET
                payroll_run_id = @PayrollRunId,
                employee_id = @EmployeeId,
                salary_structure_id = @SalaryStructureId,
                payroll_month = @PayrollMonth,
                payroll_year = @PayrollYear,
                payroll_type = @PayrollType,
                working_days = @WorkingDays,
                present_days = @PresentDays,
                lop_days = @LopDays,
                basic_earned = @BasicEarned,
                hra_earned = @HraEarned,
                da_earned = @DaEarned,
                conveyance_earned = @ConveyanceEarned,
                medical_earned = @MedicalEarned,
                special_allowance_earned = @SpecialAllowanceEarned,
                other_allowances_earned = @OtherAllowancesEarned,
                lta_paid = @LtaPaid,
                bonus_paid = @BonusPaid,
                arrears = @Arrears,
                reimbursements = @Reimbursements,
                incentives = @Incentives,
                other_earnings = @OtherEarnings,
                gross_earnings = @GrossEarnings,
                pf_employee = @PfEmployee,
                esi_employee = @EsiEmployee,
                professional_tax = @ProfessionalTax,
                tds_deducted = @TdsDeducted,
                loan_recovery = @LoanRecovery,
                advance_recovery = @AdvanceRecovery,
                other_deductions = @OtherDeductions,
                total_deductions = @TotalDeductions,
                net_payable = @NetPayable,
                pf_employer = @PfEmployer,
                pf_admin_charges = @PfAdminCharges,
                pf_edli = @PfEdli,
                esi_employer = @EsiEmployer,
                gratuity_provision = @GratuityProvision,
                total_employer_cost = @TotalEmployerCost,
                tds_calculation = CASE WHEN @TdsCalculation IS NULL OR @TdsCalculation = '' THEN NULL::jsonb ELSE CAST(@TdsCalculation AS jsonb) END,
                tds_hr_override = @TdsHrOverride,
                tds_override_reason = @TdsOverrideReason,
                status = @Status,
                payment_date = @PaymentDate,
                payment_method = @PaymentMethod,
                payment_reference = @PaymentReference,
                bank_account = @BankAccount,
                remarks = @Remarks,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM payroll_transactions WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<PayrollTransaction>> BulkAddAsync(IEnumerable<PayrollTransaction> entities)
        {
            var results = new List<PayrollTransaction>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entity in entities)
            {
                var created = await AddAsync(entity);
                results.Add(created);
            }

            return results;
        }

        public async Task<bool> ExistsForEmployeeAndMonthAsync(Guid employeeId, int payrollMonth, int payrollYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM payroll_transactions WHERE employee_id = @employeeId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear AND id != @excludeId"
                : "SELECT COUNT(*) FROM payroll_transactions WHERE employee_id = @employeeId AND payroll_month = @payrollMonth AND payroll_year = @payrollYear";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { employeeId, payrollMonth, payrollYear, excludeId });
            return count > 0;
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_transactions SET
                status = @status,
                payment_date = CASE WHEN @status = 'paid' THEN NOW() ELSE payment_date END,
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, status });
        }

        public async Task UpdateTdsOverrideAsync(Guid id, decimal tdsOverride, string reason)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE payroll_transactions SET
                tds_hr_override = @tdsOverride,
                tds_override_reason = @reason,
                tds_deducted = @tdsOverride,
                total_deductions = pf_employee + esi_employee + professional_tax + @tdsOverride + loan_recovery + advance_recovery + other_deductions,
                net_payable = gross_earnings - (pf_employee + esi_employee + professional_tax + @tdsOverride + loan_recovery + advance_recovery + other_deductions),
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, tdsOverride, reason });
        }

        public async Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE payroll_transactions SET
                    bank_transaction_id = @bankTransactionId,
                    reconciled_at = NOW(),
                    reconciled_by = @reconciledBy,
                    updated_at = NOW()
                WHERE id = @id",
                new { id, bankTransactionId, reconciledBy });
        }

        public async Task ClearReconciliationAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE payroll_transactions SET
                    bank_transaction_id = NULL,
                    reconciled_at = NULL,
                    reconciled_by = NULL,
                    updated_at = NOW()
                WHERE id = @id",
                new { id });
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(Guid payrollRunId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT
                COALESCE(SUM(gross_earnings), 0) AS TotalGross,
                COALESCE(SUM(basic_earned), 0) AS TotalBasic,
                COALESCE(SUM(hra_earned), 0) AS TotalHra,
                COALESCE(SUM(pf_employee), 0) AS TotalPfEmployee,
                COALESCE(SUM(esi_employee), 0) AS TotalEsiEmployee,
                COALESCE(SUM(professional_tax), 0) AS TotalPt,
                COALESCE(SUM(tds_deducted), 0) AS TotalTds,
                COALESCE(SUM(total_deductions), 0) AS TotalDeductions,
                COALESCE(SUM(net_payable), 0) AS TotalNet,
                COALESCE(SUM(pf_employer), 0) AS TotalPfEmployer,
                COALESCE(SUM(esi_employer), 0) AS TotalEsiEmployer,
                COALESCE(SUM(total_employer_cost), 0) AS TotalEmployerCost,
                COUNT(*) AS EmployeeCount
                FROM payroll_transactions WHERE payroll_run_id = @payrollRunId";

            var result = await connection.QueryFirstOrDefaultAsync(sql, new { payrollRunId });
            if (result == null)
            {
                return new Dictionary<string, decimal>
                {
                    ["TotalGross"] = 0,
                    ["TotalBasic"] = 0,
                    ["TotalHra"] = 0,
                    ["TotalPfEmployee"] = 0,
                    ["TotalEsiEmployee"] = 0,
                    ["TotalPt"] = 0,
                    ["TotalTds"] = 0,
                    ["TotalDeductions"] = 0,
                    ["TotalNet"] = 0,
                    ["TotalPfEmployer"] = 0,
                    ["TotalEsiEmployer"] = 0,
                    ["TotalEmployerCost"] = 0,
                    ["EmployeeCount"] = 0
                };
            }
            return new Dictionary<string, decimal>
            {
                ["TotalGross"] = result.totalgross ?? 0,
                ["TotalBasic"] = result.totalbasic ?? 0,
                ["TotalHra"] = result.totalhra ?? 0,
                ["TotalPfEmployee"] = result.totalpfemployee ?? 0,
                ["TotalEsiEmployee"] = result.totalesiemployee ?? 0,
                ["TotalPt"] = result.totalpt ?? 0,
                ["TotalTds"] = result.totaltds ?? 0,
                ["TotalDeductions"] = result.totaldeductions ?? 0,
                ["TotalNet"] = result.totalnet ?? 0,
                ["TotalPfEmployer"] = result.totalpfemployer ?? 0,
                ["TotalEsiEmployer"] = result.totalesiemployer ?? 0,
                ["TotalEmployerCost"] = result.totalemployercost ?? 0,
                ["EmployeeCount"] = result.employeecount ?? 0
            };
        }

        public async Task<Dictionary<string, decimal>> GetYtdSummaryAsync(Guid employeeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            var sql = @"SELECT
                COALESCE(SUM(gross_earnings), 0) AS YtdGross,
                COALESCE(SUM(basic_earned), 0) AS YtdBasic,
                COALESCE(SUM(pf_employee), 0) AS YtdPfEmployee,
                COALESCE(SUM(professional_tax), 0) AS YtdPt,
                COALESCE(SUM(tds_deducted), 0) AS YtdTds,
                COALESCE(SUM(total_deductions), 0) AS YtdDeductions,
                COALESCE(SUM(net_payable), 0) AS YtdNet,
                COUNT(*) AS MonthCount
                FROM payroll_transactions
                WHERE employee_id = @employeeId
                  AND ((payroll_year = @startYear AND payroll_month >= 4)
                       OR (payroll_year = @endYear AND payroll_month <= 3))";

            var result = await connection.QueryFirstOrDefaultAsync(sql, new { employeeId, startYear, endYear });
            if (result == null)
            {
                return new Dictionary<string, decimal>
                {
                    ["YtdGross"] = 0,
                    ["YtdBasic"] = 0,
                    ["YtdPfEmployee"] = 0,
                    ["YtdPt"] = 0,
                    ["YtdTds"] = 0,
                    ["YtdDeductions"] = 0,
                    ["YtdNet"] = 0,
                    ["MonthCount"] = 0
                };
            }
            return new Dictionary<string, decimal>
            {
                ["YtdGross"] = result.ytdgross ?? 0,
                ["YtdBasic"] = result.ytdbasic ?? 0,
                ["YtdPfEmployee"] = result.ytdpfemployee ?? 0,
                ["YtdPt"] = result.ytdpt ?? 0,
                ["YtdTds"] = result.ytdtds ?? 0,
                ["YtdDeductions"] = result.ytddeductions ?? 0,
                ["YtdNet"] = result.ytdnet ?? 0,
                ["MonthCount"] = result.monthcount ?? 0
            };
        }

        public async Task<IEnumerable<PayrollTransaction>> GetByCompanyAndPeriodAsync(
            Guid companyId,
            int payrollMonth,
            int payrollYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT pt.*, e.employee_name, e.email, e.pan_number
                FROM payroll_transactions pt
                INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
                LEFT JOIN employees e ON pt.employee_id = e.id
                WHERE pr.company_id = @companyId
                  AND pt.payroll_month = @payrollMonth
                  AND pt.payroll_year = @payrollYear
                ORDER BY e.employee_name";

            var transactions = await connection.QueryAsync<PayrollTransaction, Core.Entities.Employees, PayrollTransaction>(
                sql,
                (transaction, employee) =>
                {
                    transaction.Employee = employee;
                    return transaction;
                },
                new { companyId, payrollMonth, payrollYear },
                splitOn: "employee_name");

            return transactions;
        }
    }
}
