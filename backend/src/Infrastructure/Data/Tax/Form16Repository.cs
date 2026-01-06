using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Tax
{
    /// <summary>
    /// Repository implementation for Form 16 (TDS Certificate) operations.
    /// Uses Dapper for efficient database access with PostgreSQL.
    /// </summary>
    public class Form16Repository : IForm16Repository
    {
        private readonly string _connectionString;

        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "employee_id", "financial_year", "certificate_number",
            "tan", "employee_pan", "employee_name", "gross_salary", "taxable_income",
            "total_tds_deducted", "total_tds_deposited", "tax_regime", "status",
            "generated_at", "verified_at", "issued_at", "created_at", "updated_at"
        };

        public Form16Repository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // ==================== Basic CRUD Operations ====================

        public async Task<Form16?> GetByIdAsync(Guid id)
        {
            const string sql = @"
                SELECT * FROM form_16
                WHERE id = @id";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Form16>(sql, new { id });
        }

        public async Task<IEnumerable<Form16>> GetByCompanyIdAsync(Guid companyId)
        {
            const string sql = @"
                SELECT * FROM form_16
                WHERE company_id = @companyId
                ORDER BY financial_year DESC, employee_name";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form16>(sql, new { companyId });
        }

        public async Task<(IEnumerable<Form16> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            var whereClauses = new List<string> { "company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrEmpty(financialYear))
            {
                whereClauses.Add("financial_year = @financialYear");
                parameters.Add("financialYear", financialYear);
            }

            if (!string.IsNullOrEmpty(status))
            {
                whereClauses.Add("status = @status");
                parameters.Add("status", status);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClauses.Add("(employee_name ILIKE @searchTerm OR employee_pan ILIKE @searchTerm OR certificate_number ILIKE @searchTerm)");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = string.Join(" AND ", whereClauses);

            // Validate sort column
            var orderColumn = AllowedColumns.Contains(sortBy?.ToLower() ?? "") ? sortBy : "created_at";
            var orderDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var countSql = $"SELECT COUNT(*) FROM form_16 WHERE {whereClause}";
            var dataSql = $@"
                SELECT * FROM form_16
                WHERE {whereClause}
                ORDER BY {orderColumn} {orderDirection}
                OFFSET @offset LIMIT @limit";

            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<Form16>(dataSql, parameters);

            return (items, count);
        }

        public async Task<Form16> AddAsync(Form16 form16)
        {
            const string sql = @"
                INSERT INTO form_16 (
                    id, company_id, employee_id, financial_year, certificate_number,
                    tan, deductor_pan, deductor_name, deductor_address, deductor_city,
                    deductor_state, deductor_pincode, deductor_email, deductor_phone,
                    employee_pan, employee_name, employee_address, employee_city,
                    employee_state, employee_pincode, employee_email,
                    period_from, period_to,
                    q1_tds_deducted, q1_tds_deposited, q1_challan_details,
                    q2_tds_deducted, q2_tds_deposited, q2_challan_details,
                    q3_tds_deducted, q3_tds_deposited, q3_challan_details,
                    q4_tds_deducted, q4_tds_deposited, q4_challan_details,
                    total_tds_deducted, total_tds_deposited,
                    gross_salary, perquisites, profits_in_lieu, total_salary,
                    hra_exemption, lta_exemption, other_exemptions, total_exemptions,
                    standard_deduction, entertainment_allowance, professional_tax,
                    section_80c, section_80ccc, section_80ccd1, section_80ccd1b,
                    section_80ccd2, section_80d, section_80e, section_80g,
                    section_80tta, section_24, other_deductions, total_deductions,
                    tax_regime, taxable_income, tax_on_income, rebate_87a,
                    tax_after_rebate, surcharge, cess, total_tax_liability,
                    relief_89, net_tax_payable,
                    previous_employer_income, previous_employer_tds, other_income,
                    verified_by_name, verified_by_designation, verified_by_pan, place, signature_date,
                    status, generated_at, generated_by, verified_at, verified_by,
                    issued_at, issued_by, pdf_path,
                    salary_breakdown_json, tax_computation_json,
                    created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @CompanyId, @EmployeeId, @FinancialYear, @CertificateNumber,
                    @Tan, @DeductorPan, @DeductorName, @DeductorAddress, @DeductorCity,
                    @DeductorState, @DeductorPincode, @DeductorEmail, @DeductorPhone,
                    @EmployeePan, @EmployeeName, @EmployeeAddress, @EmployeeCity,
                    @EmployeeState, @EmployeePincode, @EmployeeEmail,
                    @PeriodFrom, @PeriodTo,
                    @Q1TdsDeducted, @Q1TdsDeposited, @Q1ChallanDetails::jsonb,
                    @Q2TdsDeducted, @Q2TdsDeposited, @Q2ChallanDetails::jsonb,
                    @Q3TdsDeducted, @Q3TdsDeposited, @Q3ChallanDetails::jsonb,
                    @Q4TdsDeducted, @Q4TdsDeposited, @Q4ChallanDetails::jsonb,
                    @TotalTdsDeducted, @TotalTdsDeposited,
                    @GrossSalary, @Perquisites, @ProfitsInLieu, @TotalSalary,
                    @HraExemption, @LtaExemption, @OtherExemptions, @TotalExemptions,
                    @StandardDeduction, @EntertainmentAllowance, @ProfessionalTax,
                    @Section80C, @Section80CCC, @Section80CCD1, @Section80CCD1B,
                    @Section80CCD2, @Section80D, @Section80E, @Section80G,
                    @Section80TTA, @Section24, @OtherDeductions, @TotalDeductions,
                    @TaxRegime, @TaxableIncome, @TaxOnIncome, @Rebate87A,
                    @TaxAfterRebate, @Surcharge, @Cess, @TotalTaxLiability,
                    @Relief89, @NetTaxPayable,
                    @PreviousEmployerIncome, @PreviousEmployerTds, @OtherIncome,
                    @VerifiedByName, @VerifiedByDesignation, @VerifiedByPan, @Place, @SignatureDate,
                    @Status, @GeneratedAt, @GeneratedBy, @VerifiedAt, @VerifiedBy,
                    @IssuedAt, @IssuedBy, @PdfPath,
                    @SalaryBreakdownJson::jsonb, @TaxComputationJson::jsonb,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )
                RETURNING *";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleAsync<Form16>(sql, form16);
        }

        public async Task UpdateAsync(Form16 form16)
        {
            const string sql = @"
                UPDATE form_16 SET
                    tan = @Tan,
                    deductor_pan = @DeductorPan,
                    deductor_name = @DeductorName,
                    deductor_address = @DeductorAddress,
                    deductor_city = @DeductorCity,
                    deductor_state = @DeductorState,
                    deductor_pincode = @DeductorPincode,
                    employee_pan = @EmployeePan,
                    employee_name = @EmployeeName,
                    employee_address = @EmployeeAddress,
                    period_from = @PeriodFrom,
                    period_to = @PeriodTo,
                    q1_tds_deducted = @Q1TdsDeducted,
                    q1_tds_deposited = @Q1TdsDeposited,
                    q1_challan_details = @Q1ChallanDetails::jsonb,
                    q2_tds_deducted = @Q2TdsDeducted,
                    q2_tds_deposited = @Q2TdsDeposited,
                    q2_challan_details = @Q2ChallanDetails::jsonb,
                    q3_tds_deducted = @Q3TdsDeducted,
                    q3_tds_deposited = @Q3TdsDeposited,
                    q3_challan_details = @Q3ChallanDetails::jsonb,
                    q4_tds_deducted = @Q4TdsDeducted,
                    q4_tds_deposited = @Q4TdsDeposited,
                    q4_challan_details = @Q4ChallanDetails::jsonb,
                    total_tds_deducted = @TotalTdsDeducted,
                    total_tds_deposited = @TotalTdsDeposited,
                    gross_salary = @GrossSalary,
                    perquisites = @Perquisites,
                    profits_in_lieu = @ProfitsInLieu,
                    total_salary = @TotalSalary,
                    hra_exemption = @HraExemption,
                    lta_exemption = @LtaExemption,
                    other_exemptions = @OtherExemptions,
                    total_exemptions = @TotalExemptions,
                    standard_deduction = @StandardDeduction,
                    professional_tax = @ProfessionalTax,
                    section_80c = @Section80C,
                    section_80ccd1b = @Section80CCD1B,
                    section_80d = @Section80D,
                    section_80e = @Section80E,
                    section_80g = @Section80G,
                    section_80tta = @Section80TTA,
                    section_24 = @Section24,
                    other_deductions = @OtherDeductions,
                    total_deductions = @TotalDeductions,
                    tax_regime = @TaxRegime,
                    taxable_income = @TaxableIncome,
                    tax_on_income = @TaxOnIncome,
                    rebate_87a = @Rebate87A,
                    tax_after_rebate = @TaxAfterRebate,
                    surcharge = @Surcharge,
                    cess = @Cess,
                    total_tax_liability = @TotalTaxLiability,
                    relief_89 = @Relief89,
                    net_tax_payable = @NetTaxPayable,
                    previous_employer_income = @PreviousEmployerIncome,
                    previous_employer_tds = @PreviousEmployerTds,
                    other_income = @OtherIncome,
                    verified_by_name = @VerifiedByName,
                    verified_by_designation = @VerifiedByDesignation,
                    verified_by_pan = @VerifiedByPan,
                    place = @Place,
                    signature_date = @SignatureDate,
                    status = @Status,
                    generated_at = @GeneratedAt,
                    generated_by = @GeneratedBy,
                    verified_at = @VerifiedAt,
                    verified_by = @VerifiedBy,
                    issued_at = @IssuedAt,
                    issued_by = @IssuedBy,
                    pdf_path = @PdfPath,
                    salary_breakdown_json = @SalaryBreakdownJson::jsonb,
                    tax_computation_json = @TaxComputationJson::jsonb,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, form16);
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = "DELETE FROM form_16 WHERE id = @id";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { id });
        }

        // ==================== Specialized Queries ====================

        public async Task<Form16?> GetByEmployeeAndFyAsync(Guid companyId, Guid employeeId, string financialYear)
        {
            const string sql = @"
                SELECT * FROM form_16
                WHERE company_id = @companyId
                AND employee_id = @employeeId
                AND financial_year = @financialYear";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Form16>(sql, new { companyId, employeeId, financialYear });
        }

        public async Task<IEnumerable<Form16>> GetByFinancialYearAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT * FROM form_16
                WHERE company_id = @companyId
                AND financial_year = @financialYear
                ORDER BY employee_name";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form16>(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<Guid>> GetEmployeesPendingForm16Async(Guid companyId, string financialYear)
        {
            // Get employees who had payroll in the FY but don't have Form 16
            const string sql = @"
                SELECT DISTINCT pt.employee_id
                FROM payroll_transactions pt
                INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
                WHERE pr.company_id = @companyId
                AND pr.financial_year = @financialYear
                AND pr.status IN ('computed', 'approved', 'paid')
                AND NOT EXISTS (
                    SELECT 1 FROM form_16 f16
                    WHERE f16.company_id = @companyId
                    AND f16.employee_id = pt.employee_id
                    AND f16.financial_year = @financialYear
                )";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Guid>(sql, new { companyId, financialYear });
        }

        public async Task<bool> ExistsAsync(Guid companyId, Guid employeeId, string financialYear)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM form_16
                    WHERE company_id = @companyId
                    AND employee_id = @employeeId
                    AND financial_year = @financialYear
                )";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, employeeId, financialYear });
        }

        public async Task<int> GetNextCertificateSerialAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT COALESCE(MAX(
                    CAST(SPLIT_PART(certificate_number, '/', 3) AS INTEGER)
                ), 0) + 1
                FROM form_16
                WHERE company_id = @companyId
                AND financial_year = @financialYear";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { companyId, financialYear });
        }

        public async Task BulkInsertAsync(IEnumerable<Form16> form16s)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var form16 in form16s)
                {
                    await AddAsync(form16);
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateStatusBulkAsync(IEnumerable<Guid> ids, string status, Guid? updatedBy)
        {
            const string sql = @"
                UPDATE form_16
                SET status = @status,
                    updated_at = @updatedAt,
                    updated_by = @updatedBy
                WHERE id = ANY(@ids)";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                ids = ids.ToArray(),
                status,
                updatedAt = DateTime.UtcNow,
                updatedBy
            });
        }

        public async Task<Form16Statistics> GetStatisticsAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT
                    @financialYear AS financial_year,
                    (SELECT COUNT(DISTINCT pt.employee_id)
                     FROM payroll_transactions pt
                     INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
                     WHERE pr.company_id = @companyId AND pr.financial_year = @financialYear
                     AND pr.status IN ('computed', 'approved', 'paid')) AS total_employees,
                    COUNT(*) FILTER (WHERE status != 'cancelled') AS form16_generated,
                    COUNT(*) FILTER (WHERE status IN ('verified', 'issued')) AS form16_verified,
                    COUNT(*) FILTER (WHERE status = 'issued') AS form16_issued,
                    COUNT(*) FILTER (WHERE status IN ('draft', 'generated')) AS form16_pending,
                    COALESCE(SUM(total_tds_deducted) FILTER (WHERE status != 'cancelled'), 0) AS total_tds_deducted,
                    COALESCE(SUM(total_tds_deposited) FILTER (WHERE status != 'cancelled'), 0) AS total_tds_deposited
                FROM form_16
                WHERE company_id = @companyId
                AND financial_year = @financialYear";

            using var connection = new NpgsqlConnection(_connectionString);
            var stats = await connection.QueryFirstOrDefaultAsync<Form16Statistics>(
                sql, new { companyId, financialYear });

            return stats ?? new Form16Statistics { FinancialYear = financialYear };
        }
    }
}
