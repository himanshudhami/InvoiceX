using Core.Entities.Forex;
using Core.Interfaces.Forex;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Forex
{
    public class FircTrackingRepository : IFircTrackingRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "firc_number", "firc_date", "bank_name", "bank_branch",
            "bank_swift_code", "purpose_code", "foreign_currency", "foreign_amount",
            "inr_amount", "exchange_rate", "remitter_name", "remitter_country",
            "remitter_bank", "beneficiary_name", "beneficiary_account", "payment_id",
            "edpms_reported", "edpms_report_date", "edpms_reference", "status",
            "created_at", "updated_at", "created_by", "notes"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "firc_number", "bank_name", "remitter_name", "beneficiary_name", "purpose_code"
        };

        public FircTrackingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<FircTracking?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<FircTracking>(
                "SELECT * FROM firc_tracking WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<FircTracking>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<FircTracking>(
                "SELECT * FROM firc_tracking ORDER BY firc_date DESC");
        }

        public async Task<(IEnumerable<FircTracking> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("firc_tracking", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "firc_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<FircTracking>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<FircTracking> AddAsync(FircTracking entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO firc_tracking (
                    company_id, firc_number, firc_date, bank_name, bank_branch,
                    bank_swift_code, purpose_code, foreign_currency, foreign_amount,
                    inr_amount, exchange_rate, remitter_name, remitter_country,
                    remitter_bank, beneficiary_name, beneficiary_account, payment_id,
                    edpms_reported, edpms_report_date, edpms_reference, status,
                    created_at, updated_at, created_by, notes
                ) VALUES (
                    @CompanyId, @FircNumber, @FircDate, @BankName, @BankBranch,
                    @BankSwiftCode, @PurposeCode, @ForeignCurrency, @ForeignAmount,
                    @InrAmount, @ExchangeRate, @RemitterName, @RemitterCountry,
                    @RemitterBank, @BeneficiaryName, @BeneficiaryAccount, @PaymentId,
                    @EdpmsReported, @EdpmsReportDate, @EdpmsReference, @Status,
                    NOW(), NOW(), @CreatedBy, @Notes
                ) RETURNING *";
            return await connection.QuerySingleAsync<FircTracking>(sql, entity);
        }

        public async Task UpdateAsync(FircTracking entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE firc_tracking SET
                    company_id = @CompanyId, firc_number = @FircNumber, firc_date = @FircDate,
                    bank_name = @BankName, bank_branch = @BankBranch, bank_swift_code = @BankSwiftCode,
                    purpose_code = @PurposeCode, foreign_currency = @ForeignCurrency,
                    foreign_amount = @ForeignAmount, inr_amount = @InrAmount,
                    exchange_rate = @ExchangeRate, remitter_name = @RemitterName,
                    remitter_country = @RemitterCountry, remitter_bank = @RemitterBank,
                    beneficiary_name = @BeneficiaryName, beneficiary_account = @BeneficiaryAccount,
                    payment_id = @PaymentId, edpms_reported = @EdpmsReported,
                    edpms_report_date = @EdpmsReportDate, edpms_reference = @EdpmsReference,
                    status = @Status, updated_at = NOW(), notes = @Notes
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM firc_tracking WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<FircTracking>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<FircTracking>(
                "SELECT * FROM firc_tracking WHERE company_id = @companyId ORDER BY firc_date DESC",
                new { companyId });
        }

        public async Task<FircTracking?> GetByFircNumberAsync(string fircNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<FircTracking>(
                "SELECT * FROM firc_tracking WHERE firc_number = @fircNumber",
                new { fircNumber });
        }

        public async Task<FircTracking?> GetByPaymentIdAsync(Guid paymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<FircTracking>(
                "SELECT * FROM firc_tracking WHERE payment_id = @paymentId",
                new { paymentId });
        }

        public async Task<IEnumerable<FircTracking>> GetUnlinkedAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<FircTracking>(
                @"SELECT * FROM firc_tracking
                  WHERE company_id = @companyId AND payment_id IS NULL
                  ORDER BY firc_date",
                new { companyId });
        }

        public async Task<IEnumerable<FircTracking>> GetPendingEdpmsReportingAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<FircTracking>(
                @"SELECT * FROM firc_tracking
                  WHERE company_id = @companyId AND edpms_reported = false
                  ORDER BY firc_date",
                new { companyId });
        }

        public async Task MarkEdpmsReportedAsync(Guid id, DateOnly reportDate, string? reference)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE firc_tracking SET
                    edpms_reported = true, edpms_report_date = @reportDate,
                    edpms_reference = @reference, updated_at = NOW()
                  WHERE id = @id",
                new { id, reportDate, reference });
        }

        public async Task AddInvoiceLinkAsync(FircInvoiceLink link)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO firc_invoice_links (firc_id, invoice_id, allocated_amount, allocated_amount_inr, created_at)
                  VALUES (@FircId, @InvoiceId, @AllocatedAmount, @AllocatedAmountInr, NOW())",
                link);
        }

        public async Task<IEnumerable<FircInvoiceLink>> GetInvoiceLinksAsync(Guid fircId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<FircInvoiceLink>(
                "SELECT * FROM firc_invoice_links WHERE firc_id = @fircId",
                new { fircId });
        }

        public async Task RemoveInvoiceLinkAsync(Guid fircId, Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM firc_invoice_links WHERE firc_id = @fircId AND invoice_id = @invoiceId",
                new { fircId, invoiceId });
        }
    }
}
