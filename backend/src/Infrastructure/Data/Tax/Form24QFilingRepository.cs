using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Tax
{
    /// <summary>
    /// Repository implementation for Form 24Q Filing operations.
    /// Uses Dapper for efficient database access with PostgreSQL.
    /// </summary>
    public class Form24QFilingRepository : IForm24QFilingRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "financial_year", "quarter", "tan", "form_type",
            "revision_number", "total_employees", "total_salary_paid",
            "total_tds_deducted", "total_tds_deposited", "variance", "status",
            "filing_date", "acknowledgement_number", "created_at", "updated_at"
        };

        public Form24QFilingRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // ==================== Basic CRUD Operations ====================

        public async Task<Form24QFiling?> GetByIdAsync(Guid id)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE id = @id";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Form24QFiling>(sql, new { id });
        }

        public async Task<IEnumerable<Form24QFiling>> GetByCompanyAsync(Guid companyId, string? financialYear = null)
        {
            var sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId";

            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND financial_year = @financialYear";
                parameters.Add("financialYear", financialYear);
            }

            sql += " ORDER BY financial_year DESC, quarter";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, parameters);
        }

        public async Task<(IEnumerable<Form24QFiling> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? quarter = null,
            string? status = null,
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

            if (!string.IsNullOrEmpty(quarter))
            {
                whereClauses.Add("quarter = @quarter");
                parameters.Add("quarter", quarter);
            }

            if (!string.IsNullOrEmpty(status))
            {
                whereClauses.Add("status = @status");
                parameters.Add("status", status);
            }

            var whereClause = string.Join(" AND ", whereClauses);

            // Validate sort column
            var orderColumn = AllowedColumns.Contains(sortBy?.ToLower() ?? "") ? sortBy : "financial_year";
            var orderDirection = sortDescending ? "DESC" : "ASC";
            var secondarySort = orderColumn == "financial_year" ? ", quarter" : "";

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var countSql = $"SELECT COUNT(*) FROM form_24q_filings WHERE {whereClause}";
            var dataSql = $@"
                SELECT * FROM form_24q_filings
                WHERE {whereClause}
                ORDER BY {orderColumn} {orderDirection}{secondarySort}
                OFFSET @offset LIMIT @limit";

            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<Form24QFiling>(dataSql, parameters);

            return (items, count);
        }

        public async Task<Form24QFiling> AddAsync(Form24QFiling filing)
        {
            filing.Id = filing.Id == Guid.Empty ? Guid.NewGuid() : filing.Id;
            filing.CreatedAt = DateTime.UtcNow;
            filing.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO form_24q_filings (
                    id, company_id, financial_year, quarter, tan,
                    form_type, original_filing_id, revision_number,
                    total_employees, total_salary_paid, total_tds_deducted,
                    total_tds_deposited, variance,
                    annexure1_data, annexure2_data, employee_records, challan_records,
                    status, validation_errors, validation_warnings,
                    validated_at, validated_by,
                    fvu_file_path, fvu_generated_at, fvu_version,
                    filing_date, acknowledgement_number, token_number,
                    provisional_receipt_number,
                    submitted_at, submitted_by,
                    rejection_reason, rejected_at,
                    created_at, created_by, updated_at, updated_by
                ) VALUES (
                    @Id, @CompanyId, @FinancialYear, @Quarter, @Tan,
                    @FormType, @OriginalFilingId, @RevisionNumber,
                    @TotalEmployees, @TotalSalaryPaid, @TotalTdsDeducted,
                    @TotalTdsDeposited, @Variance,
                    @Annexure1Data::jsonb, @Annexure2Data::jsonb, @EmployeeRecords::jsonb, @ChallanRecords::jsonb,
                    @Status, @ValidationErrors::jsonb, @ValidationWarnings::jsonb,
                    @ValidatedAt, @ValidatedBy,
                    @FvuFilePath, @FvuGeneratedAt, @FvuVersion,
                    @FilingDate, @AcknowledgementNumber, @TokenNumber,
                    @ProvisionalReceiptNumber,
                    @SubmittedAt, @SubmittedBy,
                    @RejectionReason, @RejectedAt,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
                )";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, filing);

            return filing;
        }

        public async Task UpdateAsync(Form24QFiling filing)
        {
            filing.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE form_24q_filings SET
                    total_employees = @TotalEmployees,
                    total_salary_paid = @TotalSalaryPaid,
                    total_tds_deducted = @TotalTdsDeducted,
                    total_tds_deposited = @TotalTdsDeposited,
                    variance = @Variance,
                    annexure1_data = @Annexure1Data::jsonb,
                    annexure2_data = @Annexure2Data::jsonb,
                    employee_records = @EmployeeRecords::jsonb,
                    challan_records = @ChallanRecords::jsonb,
                    status = @Status,
                    validation_errors = @ValidationErrors::jsonb,
                    validation_warnings = @ValidationWarnings::jsonb,
                    validated_at = @ValidatedAt,
                    validated_by = @ValidatedBy,
                    fvu_file_path = @FvuFilePath,
                    fvu_generated_at = @FvuGeneratedAt,
                    fvu_version = @FvuVersion,
                    filing_date = @FilingDate,
                    acknowledgement_number = @AcknowledgementNumber,
                    token_number = @TokenNumber,
                    provisional_receipt_number = @ProvisionalReceiptNumber,
                    submitted_at = @SubmittedAt,
                    submitted_by = @SubmittedBy,
                    rejection_reason = @RejectionReason,
                    rejected_at = @RejectedAt,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, filing);
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = "DELETE FROM form_24q_filings WHERE id = @id";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { id });
        }

        // ==================== Specialized Queries ====================

        public async Task<Form24QFiling?> GetByCompanyQuarterAsync(Guid companyId, string financialYear, string quarter)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND quarter = @quarter
                  AND form_type = 'regular'
                  AND status NOT IN ('revised', 'rejected')
                ORDER BY created_at DESC
                LIMIT 1";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Form24QFiling>(sql, new { companyId, financialYear, quarter });
        }

        public async Task<IEnumerable<Form24QFiling>> GetByFinancialYearAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                ORDER BY quarter, form_type, revision_number DESC";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, new { companyId, financialYear });
        }

        public async Task<bool> ExistsAsync(Guid companyId, string financialYear, string quarter, string formType = "regular")
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM form_24q_filings
                    WHERE company_id = @companyId
                      AND financial_year = @financialYear
                      AND quarter = @quarter
                      AND form_type = @formType
                      AND status NOT IN ('revised', 'rejected')
                )";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(sql, new { companyId, financialYear, quarter, formType });
        }

        public async Task<IEnumerable<Form24QFiling>> GetCorrectionsAsync(Guid originalFilingId)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE original_filing_id = @originalFilingId
                ORDER BY revision_number";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, new { originalFilingId });
        }

        public async Task<int> GetNextRevisionNumberAsync(Guid companyId, string financialYear, string quarter)
        {
            const string sql = @"
                SELECT COALESCE(MAX(revision_number), 0) + 1
                FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND quarter = @quarter";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { companyId, financialYear, quarter });
        }

        public async Task<IEnumerable<Form24QFiling>> GetByStatusAsync(Guid companyId, string status)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId
                  AND status = @status
                ORDER BY financial_year DESC, quarter";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, new { companyId, status });
        }

        public async Task<IEnumerable<Form24QFiling>> GetPendingFilingsAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND form_type = 'regular'
                  AND status NOT IN ('acknowledged', 'revised')
                ORDER BY quarter";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<Form24QFiling>> GetOverdueFilingsAsync(Guid companyId)
        {
            const string sql = @"
                SELECT * FROM form_24q_filings
                WHERE company_id = @companyId
                  AND form_type = 'regular'
                  AND status NOT IN ('acknowledged', 'revised')
                  AND is_form24q_overdue(financial_year, quarter, status) = true
                ORDER BY financial_year DESC, quarter";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Form24QFiling>(sql, new { companyId });
        }

        public async Task<Form24QFilingStatistics> GetStatisticsAsync(Guid companyId, string financialYear)
        {
            const string sql = @"
                SELECT
                    @financialYear as FinancialYear,
                    COUNT(*) FILTER (WHERE form_type = 'regular') as TotalFilings,
                    COUNT(*) FILTER (WHERE status = 'draft') as DraftCount,
                    COUNT(*) FILTER (WHERE status = 'validated') as ValidatedCount,
                    COUNT(*) FILTER (WHERE status = 'fvu_generated') as FvuGeneratedCount,
                    COUNT(*) FILTER (WHERE status = 'submitted') as SubmittedCount,
                    COUNT(*) FILTER (WHERE status = 'acknowledged') as AcknowledgedCount,
                    COUNT(*) FILTER (WHERE status = 'rejected') as RejectedCount,
                    COUNT(*) FILTER (WHERE form_type = 'regular' AND status NOT IN ('acknowledged', 'revised')) as PendingCount,
                    COUNT(*) FILTER (WHERE form_type = 'regular' AND status NOT IN ('acknowledged', 'revised') AND is_form24q_overdue(financial_year, quarter, status)) as OverdueCount,
                    COALESCE(SUM(total_tds_deducted) FILTER (WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected')), 0) as TotalTdsDeducted,
                    COALESCE(SUM(total_tds_deposited) FILTER (WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected')), 0) as TotalTdsDeposited,
                    COALESCE(SUM(variance) FILTER (WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected')), 0) as TotalVariance
                FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear";

            using var connection = new NpgsqlConnection(_connectionString);
            var stats = await connection.QueryFirstAsync<Form24QFilingStatistics>(sql, new { companyId, financialYear });

            // Get quarterly breakdown
            const string quarterlySql = @"
                SELECT
                    quarter as Quarter,
                    status as Status,
                    true as HasFiling,
                    is_form24q_overdue(financial_year, quarter, status) as IsOverdue,
                    get_form24q_due_date(@financialYear, quarter) as DueDate,
                    total_employees as TotalEmployees,
                    total_tds_deducted as TdsDeducted,
                    total_tds_deposited as TdsDeposited,
                    acknowledgement_number as AcknowledgementNumber
                FROM form_24q_filings
                WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  AND form_type = 'regular'
                  AND status NOT IN ('revised', 'rejected')
                ORDER BY quarter";

            var quarterlyData = (await connection.QueryAsync<QuarterStatus>(quarterlySql, new { companyId, financialYear })).ToList();

            stats.Q1Status = quarterlyData.FirstOrDefault(q => q.Quarter == "Q1");
            stats.Q2Status = quarterlyData.FirstOrDefault(q => q.Quarter == "Q2");
            stats.Q3Status = quarterlyData.FirstOrDefault(q => q.Quarter == "Q3");
            stats.Q4Status = quarterlyData.FirstOrDefault(q => q.Quarter == "Q4");

            return stats;
        }

        public async Task UpdateStatusBulkAsync(IEnumerable<Guid> ids, string status, Guid? updatedBy)
        {
            const string sql = @"
                UPDATE form_24q_filings
                SET status = @status,
                    updated_at = CURRENT_TIMESTAMP,
                    updated_by = @updatedBy
                WHERE id = ANY(@ids)";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { ids = ids.ToArray(), status, updatedBy });
        }
    }
}
