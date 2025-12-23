using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Tax
{
    /// <summary>
    /// Repository implementation for Lower Deduction Certificates (Form 13)
    /// </summary>
    public class LowerDeductionCertificateRepository : ILowerDeductionCertificateRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id",
            "certificate_number", "certificate_date", "valid_from", "valid_to", "financial_year",
            "certificate_type",
            "deductee_type", "deductee_id", "deductee_name", "deductee_pan", "deductee_address",
            "tds_section", "normal_rate", "certificate_rate",
            "threshold_amount", "utilized_amount",
            "assessing_officer", "ao_designation", "ao_office_address",
            "certificate_document_id",
            "status", "revoked_at", "revocation_reason",
            "notes", "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "certificate_number", "deductee_name", "deductee_pan", "tds_section"
        };

        public LowerDeductionCertificateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<LowerDeductionCertificate?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LowerDeductionCertificate>(
                "SELECT * FROM lower_deduction_certificates WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<LowerDeductionCertificate>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                "SELECT * FROM lower_deduction_certificates ORDER BY valid_from DESC");
        }

        public async Task<(IEnumerable<LowerDeductionCertificate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("lower_deduction_certificates", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "valid_from";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<LowerDeductionCertificate>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<LowerDeductionCertificate> AddAsync(LowerDeductionCertificate entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO lower_deduction_certificates (
                    id, company_id,
                    certificate_number, certificate_date, valid_from, valid_to, financial_year,
                    certificate_type,
                    deductee_type, deductee_id, deductee_name, deductee_pan, deductee_address,
                    tds_section, normal_rate, certificate_rate,
                    threshold_amount, utilized_amount,
                    assessing_officer, ao_designation, ao_office_address,
                    certificate_document_id,
                    status, revoked_at, revocation_reason,
                    notes, created_at, updated_at, created_by
                )
                VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CompanyId,
                    @CertificateNumber, @CertificateDate, @ValidFrom, @ValidTo, @FinancialYear,
                    @CertificateType,
                    @DeducteeType, @DeducteeId, @DeducteeName, @DeducteePan, @DeducteeAddress,
                    @TdsSection, @NormalRate, @CertificateRate,
                    @ThresholdAmount, @UtilizedAmount,
                    @AssessingOfficer, @AoDesignation, @AoOfficeAddress,
                    @CertificateDocumentId,
                    @Status, @RevokedAt, @RevocationReason,
                    @Notes, NOW(), NOW(), @CreatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<LowerDeductionCertificate>(sql, entity);
        }

        public async Task UpdateAsync(LowerDeductionCertificate entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE lower_deduction_certificates SET
                    company_id = @CompanyId,
                    certificate_number = @CertificateNumber,
                    certificate_date = @CertificateDate,
                    valid_from = @ValidFrom,
                    valid_to = @ValidTo,
                    financial_year = @FinancialYear,
                    certificate_type = @CertificateType,
                    deductee_type = @DeducteeType,
                    deductee_id = @DeducteeId,
                    deductee_name = @DeducteeName,
                    deductee_pan = @DeducteePan,
                    deductee_address = @DeducteeAddress,
                    tds_section = @TdsSection,
                    normal_rate = @NormalRate,
                    certificate_rate = @CertificateRate,
                    threshold_amount = @ThresholdAmount,
                    utilized_amount = @UtilizedAmount,
                    assessing_officer = @AssessingOfficer,
                    ao_designation = @AoDesignation,
                    ao_office_address = @AoOfficeAddress,
                    certificate_document_id = @CertificateDocumentId,
                    status = @Status,
                    revoked_at = @RevokedAt,
                    revocation_reason = @RevocationReason,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM lower_deduction_certificates WHERE id = @id",
                new { id });
        }

        // ==================== Company Queries ====================

        public async Task<IEnumerable<LowerDeductionCertificate>> GetByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId
                  ORDER BY valid_from DESC",
                new { companyId });
        }

        public async Task<IEnumerable<LowerDeductionCertificate>> GetActiveByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId
                    AND status = 'active'
                    AND valid_from <= CURRENT_DATE
                    AND valid_to >= CURRENT_DATE
                  ORDER BY valid_from DESC",
                new { companyId });
        }

        // ==================== Certificate Validation ====================

        public async Task<LowerDeductionCertificate?> GetValidCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId
                    AND deductee_pan = @deducteePan
                    AND tds_section = @tdsSection
                    AND status = 'active'
                    AND valid_from <= @transactionDate
                    AND valid_to >= @transactionDate
                    AND (threshold_amount IS NULL OR utilized_amount < threshold_amount)
                  ORDER BY certificate_rate ASC
                  LIMIT 1",
                new { companyId, deducteePan, tdsSection, transactionDate });
        }

        public async Task<bool> HasValidCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM lower_deduction_certificates
                    WHERE company_id = @companyId
                      AND deductee_pan = @deducteePan
                      AND tds_section = @tdsSection
                      AND status = 'active'
                      AND valid_from <= @transactionDate
                      AND valid_to >= @transactionDate
                      AND (threshold_amount IS NULL OR utilized_amount < threshold_amount)
                )",
                new { companyId, deducteePan, tdsSection, transactionDate });
        }

        public async Task<LdcValidationResult> ValidateCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate,
            decimal amount)
        {
            var certificate = await GetValidCertificateAsync(companyId, deducteePan, tdsSection, transactionDate);

            if (certificate == null)
            {
                return new LdcValidationResult
                {
                    IsValid = false,
                    ValidationMessage = "No valid certificate found for this deductee and section"
                };
            }

            var remainingThreshold = certificate.ThresholdAmount.HasValue
                ? certificate.ThresholdAmount.Value - certificate.UtilizedAmount
                : (decimal?)null;

            // Check if amount exceeds remaining threshold
            if (remainingThreshold.HasValue && amount > remainingThreshold.Value)
            {
                return new LdcValidationResult
                {
                    IsValid = true,
                    CertificateId = certificate.Id,
                    CertificateNumber = certificate.CertificateNumber,
                    CertificateType = certificate.CertificateType,
                    NormalRate = certificate.NormalRate,
                    CertificateRate = certificate.CertificateRate,
                    RemainingThreshold = remainingThreshold,
                    ValidationMessage = $"Certificate valid but only covers Rs.{remainingThreshold:N2}. Normal rate applies to excess."
                };
            }

            return new LdcValidationResult
            {
                IsValid = true,
                CertificateId = certificate.Id,
                CertificateNumber = certificate.CertificateNumber,
                CertificateType = certificate.CertificateType,
                NormalRate = certificate.NormalRate,
                CertificateRate = certificate.CertificateRate,
                RemainingThreshold = remainingThreshold,
                ValidationMessage = $"Valid certificate: {certificate.CertificateNumber}. Rate: {certificate.CertificateRate}%"
            };
        }

        // ==================== Deductee Queries ====================

        public async Task<IEnumerable<LowerDeductionCertificate>> GetByDeducteePanAsync(Guid companyId, string deducteePan)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId AND deductee_pan = @deducteePan
                  ORDER BY valid_from DESC",
                new { companyId, deducteePan });
        }

        public async Task<IEnumerable<LowerDeductionCertificate>> GetByDeducteeIdAsync(Guid deducteeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE deductee_id = @deducteeId
                  ORDER BY valid_from DESC",
                new { deducteeId });
        }

        // ==================== Section Queries ====================

        public async Task<IEnumerable<LowerDeductionCertificate>> GetBySectionAsync(Guid companyId, string tdsSection)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId AND tds_section = @tdsSection
                  ORDER BY valid_from DESC",
                new { companyId, tdsSection });
        }

        // ==================== Status Queries ====================

        public async Task<IEnumerable<LowerDeductionCertificate>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId AND status = @status
                  ORDER BY valid_from DESC",
                new { companyId, status });
        }

        public async Task<IEnumerable<LowerDeductionCertificate>> GetExpiringAsync(Guid companyId, int daysAhead)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId
                    AND status = 'active'
                    AND valid_to BETWEEN CURRENT_DATE AND (CURRENT_DATE + @daysAhead)
                  ORDER BY valid_to ASC",
                new { companyId, daysAhead });
        }

        public async Task<IEnumerable<LowerDeductionCertificate>> GetExhaustedAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LowerDeductionCertificate>(
                @"SELECT * FROM lower_deduction_certificates
                  WHERE company_id = @companyId
                    AND status = 'active'
                    AND threshold_amount IS NOT NULL
                    AND utilized_amount >= threshold_amount * 0.9
                  ORDER BY (utilized_amount / threshold_amount) DESC",
                new { companyId });
        }

        // ==================== Utilization ====================

        public async Task UpdateUtilizedAmountAsync(Guid id, decimal additionalAmount)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE lower_deduction_certificates SET
                    utilized_amount = utilized_amount + @additionalAmount,
                    status = CASE
                        WHEN threshold_amount IS NOT NULL AND (utilized_amount + @additionalAmount) >= threshold_amount
                        THEN 'exhausted'
                        ELSE status
                    END,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, additionalAmount });
        }

        public async Task<Guid> RecordUsageAsync(LdcUsageRecord usage)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO ldc_usage_log (
                    id, certificate_id, company_id,
                    transaction_date, transaction_type, transaction_id, transaction_number,
                    gross_amount, normal_tds_amount, actual_tds_amount, tds_savings,
                    cumulative_utilized, remaining_threshold,
                    notes, created_at, created_by
                )
                VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CertificateId, @CompanyId,
                    @TransactionDate, @TransactionType, @TransactionId, @TransactionNumber,
                    @GrossAmount, @NormalTdsAmount, @ActualTdsAmount, @TdsSavings,
                    @CumulativeUtilized, @RemainingThreshold,
                    @Notes, NOW(), @CreatedBy
                )
                RETURNING id";

            return await connection.ExecuteScalarAsync<Guid>(sql, usage);
        }

        public async Task<IEnumerable<LdcUsageRecord>> GetUsageHistoryAsync(Guid certificateId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LdcUsageRecord>(
                @"SELECT * FROM ldc_usage_log
                  WHERE certificate_id = @certificateId
                  ORDER BY transaction_date DESC, created_at DESC",
                new { certificateId });
        }

        // ==================== Status Updates ====================

        public async Task UpdateStatusAsync(Guid id, string status, string? reason = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE lower_deduction_certificates SET
                    status = @status,
                    notes = CASE WHEN @reason IS NOT NULL THEN COALESCE(notes, '') || E'\n' || @reason ELSE notes END,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, status, reason });
        }

        public async Task RevokeCertificateAsync(Guid id, string reason)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE lower_deduction_certificates SET
                    status = 'revoked',
                    revoked_at = NOW(),
                    revocation_reason = @reason,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, reason });
        }
    }
}
