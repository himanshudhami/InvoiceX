using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class ContractorPaymentRepository : IContractorPaymentRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "party_id", "company_id", "payment_month", "payment_year", "invoice_number",
            "contract_reference", "gross_amount", "tds_rate", "tds_amount", "other_deductions",
            "net_payable", "gst_applicable", "gst_rate", "gst_amount", "total_invoice_amount",
            "status", "payment_date", "payment_method", "payment_reference", "description",
            "remarks", "bank_transaction_id", "reconciled_at", "reconciled_by",
            "created_at", "updated_at", "created_by", "updated_by"
        };

        // Base SELECT with party join for name and reconciliation status from contractor_payments
        private const string SelectWithParty = @"
            SELECT cp.id, cp.party_id, cp.company_id, cp.payment_month, cp.payment_year,
                   cp.invoice_number, cp.contract_reference, cp.gross_amount, cp.tds_section,
                   cp.tds_rate, cp.tds_amount, cp.contractor_pan, cp.pan_verified,
                   cp.other_deductions, cp.net_payable, cp.gst_applicable, cp.gst_rate,
                   cp.gst_amount, cp.total_invoice_amount, cp.status, cp.payment_date,
                   cp.payment_method, cp.payment_reference, cp.bank_account_id,
                   cp.accrual_journal_entry_id, cp.disbursement_journal_entry_id,
                   cp.description, cp.remarks, cp.tally_voucher_guid, cp.tally_voucher_number,
                   cp.tally_migration_batch_id, cp.created_at, cp.updated_at,
                   cp.created_by, cp.updated_by,
                   p.name as PartyName,
                   cp.bank_transaction_id as BankTransactionId,
                   cp.reconciled_at as ReconciledAt,
                   cp.reconciled_by as ReconciledBy,
                   (cp.bank_transaction_id IS NOT NULL) as IsReconciled
            FROM contractor_payments cp
            LEFT JOIN parties p ON p.id = cp.party_id";

        public ContractorPaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ContractorPayment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ContractorPayment>(
                $"{SelectWithParty} WHERE cp.id = @id",
                new { id });
        }

        public async Task<IEnumerable<ContractorPayment>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                $"{SelectWithParty} ORDER BY cp.payment_year DESC, cp.payment_month DESC");
        }

        public async Task<IEnumerable<ContractorPayment>> GetByPartyIdAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                $"{SelectWithParty} WHERE cp.party_id = @partyId ORDER BY cp.payment_year DESC, cp.payment_month DESC",
                new { partyId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                $"{SelectWithParty} WHERE cp.company_id = @companyId ORDER BY cp.payment_year DESC, cp.payment_month DESC",
                new { companyId });
        }

        public async Task<ContractorPayment?> GetByPartyAndMonthAsync(Guid partyId, int paymentMonth, int paymentYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ContractorPayment>(
                $"{SelectWithParty} WHERE cp.party_id = @partyId AND cp.payment_month = @paymentMonth AND cp.payment_year = @paymentYear",
                new { partyId, paymentMonth, paymentYear });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByMonthYearAsync(int paymentMonth, int paymentYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = $"{SelectWithParty} WHERE cp.payment_month = @paymentMonth AND cp.payment_year = @paymentYear";

            if (companyId.HasValue)
            {
                sql += " AND cp.company_id = @companyId";
            }

            sql += " ORDER BY cp.created_at";

            return await connection.QueryAsync<ContractorPayment>(sql, new { paymentMonth, paymentYear, companyId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByStatusAsync(string status, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = $"{SelectWithParty} WHERE cp.status = @status";

            if (companyId.HasValue)
            {
                sql += " AND cp.company_id = @companyId";
            }

            sql += " ORDER BY cp.payment_year DESC, cp.payment_month DESC";

            return await connection.QueryAsync<ContractorPayment>(sql, new { status, companyId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByFinancialYearAsync(Guid partyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            return await connection.QueryAsync<ContractorPayment>(
                $@"{SelectWithParty}
                  WHERE cp.party_id = @partyId
                    AND ((cp.payment_year = @startYear AND cp.payment_month >= 4)
                         OR (cp.payment_year = @endYear AND cp.payment_month <= 3))
                  ORDER BY cp.payment_year, cp.payment_month",
                new { partyId, startYear, endYear });
        }

        public async Task<(IEnumerable<ContractorPayment> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Build WHERE clause
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(cp.invoice_number ILIKE @searchTerm OR cp.contract_reference ILIKE @searchTerm OR cp.description ILIKE @searchTerm OR p.name ILIKE @searchTerm)");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            if (filters != null)
            {
                if (filters.TryGetValue("company_id", out var companyId))
                {
                    whereClauses.Add("cp.company_id = @companyId");
                    parameters.Add("companyId", companyId);
                }
                if (filters.TryGetValue("party_id", out var partyId))
                {
                    whereClauses.Add("cp.party_id = @partyId");
                    parameters.Add("partyId", partyId);
                }
                if (filters.TryGetValue("payment_month", out var month))
                {
                    whereClauses.Add("cp.payment_month = @paymentMonth");
                    parameters.Add("paymentMonth", month);
                }
                if (filters.TryGetValue("payment_year", out var year))
                {
                    whereClauses.Add("cp.payment_year = @paymentYear");
                    parameters.Add("paymentYear", year);
                }
                if (filters.TryGetValue("status", out var status))
                {
                    whereClauses.Add("cp.status = @status");
                    parameters.Add("status", status);
                }
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            // Determine sort column
            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? $"cp.{sortBy}" : "cp.created_at";
            var orderDir = sortDescending ? "DESC" : "ASC";

            // Pagination
            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("limit", pageSize);
            parameters.Add("offset", offset);

            var dataSql = $@"
                {SelectWithParty}
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                LIMIT @limit OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(DISTINCT cp.id)
                FROM contractor_payments cp
                LEFT JOIN parties p ON p.id = cp.party_id
                {whereClause}";

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<ContractorPayment>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<ContractorPayment> AddAsync(ContractorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO contractor_payments
                (party_id, company_id, payment_month, payment_year, invoice_number,
                 contract_reference, gross_amount, tds_section, tds_rate, tds_amount, contractor_pan, pan_verified,
                 other_deductions, net_payable, gst_applicable, gst_rate, gst_amount, total_invoice_amount,
                 status, payment_date, payment_method, payment_reference, bank_account_id,
                 accrual_journal_entry_id, disbursement_journal_entry_id,
                 description, remarks,
                 tally_voucher_guid, tally_voucher_number, tally_migration_batch_id,
                 created_at, updated_at, created_by, updated_by)
                VALUES
                (@PartyId, @CompanyId, @PaymentMonth, @PaymentYear, @InvoiceNumber,
                 @ContractReference, @GrossAmount, @TdsSection, @TdsRate, @TdsAmount, @ContractorPan, @PanVerified,
                 @OtherDeductions, @NetPayable, @GstApplicable, @GstRate, @GstAmount, @TotalInvoiceAmount,
                 @Status, @PaymentDate, @PaymentMethod, @PaymentReference, @BankAccountId,
                 @AccrualJournalEntryId, @DisbursementJournalEntryId,
                 @Description, @Remarks,
                 @TallyVoucherGuid, @TallyVoucherNumber, @TallyMigrationBatchId,
                 NOW(), NOW(), @CreatedBy, @UpdatedBy)
                RETURNING *";

            return await connection.QuerySingleAsync<ContractorPayment>(sql, entity);
        }

        public async Task UpdateAsync(ContractorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE contractor_payments SET
                party_id = @PartyId,
                company_id = @CompanyId,
                payment_month = @PaymentMonth,
                payment_year = @PaymentYear,
                invoice_number = @InvoiceNumber,
                contract_reference = @ContractReference,
                gross_amount = @GrossAmount,
                tds_rate = @TdsRate,
                tds_amount = @TdsAmount,
                other_deductions = @OtherDeductions,
                net_payable = @NetPayable,
                gst_applicable = @GstApplicable,
                gst_rate = @GstRate,
                gst_amount = @GstAmount,
                total_invoice_amount = @TotalInvoiceAmount,
                status = @Status,
                payment_date = @PaymentDate,
                payment_method = @PaymentMethod,
                payment_reference = @PaymentReference,
                bank_account_id = @BankAccountId,
                accrual_journal_entry_id = @AccrualJournalEntryId,
                disbursement_journal_entry_id = @DisbursementJournalEntryId,
                description = @Description,
                remarks = @Remarks,
                bank_transaction_id = @BankTransactionId,
                reconciled_at = @ReconciledAt,
                reconciled_by = @ReconciledBy,
                updated_at = NOW(),
                updated_by = @UpdatedBy
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM contractor_payments WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<ContractorPayment>> BulkAddAsync(IEnumerable<ContractorPayment> entities)
        {
            var results = new List<ContractorPayment>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entity in entities)
            {
                var sql = @"INSERT INTO contractor_payments
                    (party_id, company_id, payment_month, payment_year, invoice_number,
                     contract_reference, gross_amount, tds_rate, tds_amount, other_deductions,
                     net_payable, gst_applicable, gst_rate, gst_amount, total_invoice_amount,
                     status, payment_date, payment_method, payment_reference, description,
                     remarks, created_at, updated_at, created_by, updated_by)
                    VALUES
                    (@PartyId, @CompanyId, @PaymentMonth, @PaymentYear, @InvoiceNumber,
                     @ContractReference, @GrossAmount, @TdsRate, @TdsAmount, @OtherDeductions,
                     @NetPayable, @GstApplicable, @GstRate, @GstAmount, @TotalInvoiceAmount,
                     @Status, @PaymentDate, @PaymentMethod, @PaymentReference, @Description,
                     @Remarks, NOW(), NOW(), @CreatedBy, @UpdatedBy)
                    RETURNING *";
                var created = await connection.QuerySingleAsync<ContractorPayment>(sql, entity);
                results.Add(created);
            }

            return results;
        }

        public async Task<bool> ExistsForPartyAndMonthAsync(Guid partyId, int paymentMonth, int paymentYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM contractor_payments WHERE party_id = @partyId AND payment_month = @paymentMonth AND payment_year = @paymentYear AND id != @excludeId"
                : "SELECT COUNT(*) FROM contractor_payments WHERE party_id = @partyId AND payment_month = @paymentMonth AND payment_year = @paymentYear";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { partyId, paymentMonth, paymentYear, excludeId });
            return count > 0;
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE contractor_payments SET
                status = @status,
                payment_date = CASE WHEN @status = 'paid' THEN NOW() ELSE payment_date END,
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, status });
        }

        public async Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE contractor_payments SET
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
                @"UPDATE contractor_payments SET
                    bank_transaction_id = NULL,
                    reconciled_at = NULL,
                    reconciled_by = NULL,
                    updated_at = NOW()
                WHERE id = @id",
                new { id });
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(int paymentMonth, int paymentYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT
                COALESCE(SUM(gross_amount), 0) AS TotalGross,
                COALESCE(SUM(tds_amount), 0) AS TotalTds,
                COALESCE(SUM(gst_amount), 0) AS TotalGst,
                COALESCE(SUM(net_payable), 0) AS TotalNet,
                COALESCE(SUM(total_invoice_amount), 0) AS TotalInvoice,
                COUNT(*) AS ContractorCount
                FROM contractor_payments
                WHERE payment_month = @paymentMonth AND payment_year = @paymentYear";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            var result = await connection.QueryFirstAsync(sql, new { paymentMonth, paymentYear, companyId });
            return new Dictionary<string, decimal>
            {
                ["TotalGross"] = result.totalgross,
                ["TotalTds"] = result.totaltds,
                ["TotalGst"] = result.totalgst,
                ["TotalNet"] = result.totalnet,
                ["TotalInvoice"] = result.totalinvoice,
                ["ContractorCount"] = result.contractorcount
            };
        }

        public async Task<Dictionary<string, decimal>> GetYtdSummaryAsync(Guid partyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            var sql = @"SELECT
                COALESCE(SUM(gross_amount), 0) AS YtdGross,
                COALESCE(SUM(tds_amount), 0) AS YtdTds,
                COALESCE(SUM(gst_amount), 0) AS YtdGst,
                COALESCE(SUM(net_payable), 0) AS YtdNet,
                COUNT(*) AS PaymentCount
                FROM contractor_payments
                WHERE party_id = @partyId
                  AND ((payment_year = @startYear AND payment_month >= 4)
                       OR (payment_year = @endYear AND payment_month <= 3))";

            var result = await connection.QueryFirstAsync(sql, new { partyId, startYear, endYear });
            return new Dictionary<string, decimal>
            {
                ["YtdGross"] = result.ytdgross,
                ["YtdTds"] = result.ytdtds,
                ["YtdGst"] = result.ytdgst,
                ["YtdNet"] = result.ytdnet,
                ["PaymentCount"] = result.paymentcount
            };
        }

        public async Task<ContractorPayment?> GetByTallyGuidAsync(Guid companyId, string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ContractorPayment>(
                $"{SelectWithParty} WHERE cp.company_id = @companyId AND cp.tally_voucher_guid = @tallyVoucherGuid",
                new { companyId, tallyVoucherGuid });
        }
    }
}
