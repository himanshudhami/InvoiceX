using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class VendorsRepository : IVendorsRepository
    {
        private readonly string _connectionString;

        public VendorsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Vendors?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                "SELECT * FROM vendors WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Vendors>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                "SELECT * FROM vendors ORDER BY name");
        }

        public async Task<IEnumerable<Vendors>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                "SELECT * FROM vendors WHERE company_id = @companyId ORDER BY name",
                new { companyId });
        }

        public async Task<(IEnumerable<Vendors> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "name", "company_name", "email", "phone",
                "address_line_1", "address_line_2", "city", "state", "zip_code", "country",
                "tax_number", "notes", "credit_limit", "payment_terms", "is_active",
                "gstin", "gst_state_code", "vendor_type", "is_gst_registered",
                "pan_number", "tan_number", "default_tds_section", "default_tds_rate",
                "tds_applicable", "msme_registered", "msme_registration_number", "msme_category",
                "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("vendors", allowedColumns)
                .SearchAcross(new[] { "name", "company_name", "email", "phone", "gstin", "pan_number", "city", "state" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Vendors>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Vendors?> GetByGstinAsync(Guid companyId, string gstin)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                "SELECT * FROM vendors WHERE company_id = @companyId AND gstin = @gstin",
                new { companyId, gstin });
        }

        public async Task<Vendors?> GetByPanAsync(Guid companyId, string panNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                "SELECT * FROM vendors WHERE company_id = @companyId AND pan_number = @panNumber",
                new { companyId, panNumber });
        }

        public async Task<IEnumerable<Vendors>> GetMsmeVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                "SELECT * FROM vendors WHERE company_id = @companyId AND msme_registered = TRUE ORDER BY name",
                new { companyId });
        }

        public async Task<IEnumerable<Vendors>> GetTdsApplicableVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                "SELECT * FROM vendors WHERE company_id = @companyId AND tds_applicable = TRUE ORDER BY name",
                new { companyId });
        }

        public async Task<Vendors?> GetByTallyGuidAsync(string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                "SELECT * FROM vendors WHERE tally_ledger_guid = @tallyLedgerGuid",
                new { tallyLedgerGuid });
        }

        public async Task<Vendors> AddAsync(Vendors entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendors (
                company_id, name, company_name, email, phone,
                address_line_1, address_line_2, city, state, zip_code, country,
                tax_number, notes, credit_limit, payment_terms, is_active,
                gstin, gst_state_code, vendor_type, is_gst_registered,
                pan_number, tan_number, default_tds_section, default_tds_rate,
                tds_applicable, lower_tds_certificate, lower_tds_rate, lower_tds_certificate_valid_till,
                msme_registered, msme_registration_number, msme_category,
                bank_account_number, bank_ifsc_code, bank_name, bank_branch, bank_account_holder_name,
                default_expense_account_id, default_payable_account_id,
                tally_ledger_guid, tally_ledger_name,
                created_at, updated_at
            ) VALUES (
                @CompanyId, @Name, @CompanyName, @Email, @Phone,
                @AddressLine1, @AddressLine2, @City, @State, @ZipCode, @Country,
                @TaxNumber, @Notes, @CreditLimit, @PaymentTerms, @IsActive,
                @Gstin, @GstStateCode, @VendorType, @IsGstRegistered,
                @PanNumber, @TanNumber, @DefaultTdsSection, @DefaultTdsRate,
                @TdsApplicable, @LowerTdsCertificate, @LowerTdsRate, @LowerTdsCertificateValidTill,
                @MsmeRegistered, @MsmeRegistrationNumber, @MsmeCategory,
                @BankAccountNumber, @BankIfscCode, @BankName, @BankBranch, @BankAccountHolderName,
                @DefaultExpenseAccountId, @DefaultPayableAccountId,
                @TallyLedgerGuid, @TallyLedgerName,
                NOW(), NOW()
            ) RETURNING *";

            return await connection.QuerySingleAsync<Vendors>(sql, entity);
        }

        public async Task UpdateAsync(Vendors entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE vendors SET
                company_id = @CompanyId,
                name = @Name,
                company_name = @CompanyName,
                email = @Email,
                phone = @Phone,
                address_line_1 = @AddressLine1,
                address_line_2 = @AddressLine2,
                city = @City,
                state = @State,
                zip_code = @ZipCode,
                country = @Country,
                tax_number = @TaxNumber,
                notes = @Notes,
                credit_limit = @CreditLimit,
                payment_terms = @PaymentTerms,
                is_active = @IsActive,
                gstin = @Gstin,
                gst_state_code = @GstStateCode,
                vendor_type = @VendorType,
                is_gst_registered = @IsGstRegistered,
                pan_number = @PanNumber,
                tan_number = @TanNumber,
                default_tds_section = @DefaultTdsSection,
                default_tds_rate = @DefaultTdsRate,
                tds_applicable = @TdsApplicable,
                lower_tds_certificate = @LowerTdsCertificate,
                lower_tds_rate = @LowerTdsRate,
                lower_tds_certificate_valid_till = @LowerTdsCertificateValidTill,
                msme_registered = @MsmeRegistered,
                msme_registration_number = @MsmeRegistrationNumber,
                msme_category = @MsmeCategory,
                bank_account_number = @BankAccountNumber,
                bank_ifsc_code = @BankIfscCode,
                bank_name = @BankName,
                bank_branch = @BankBranch,
                bank_account_holder_name = @BankAccountHolderName,
                default_expense_account_id = @DefaultExpenseAccountId,
                default_payable_account_id = @DefaultPayableAccountId,
                tally_ledger_guid = @TallyLedgerGuid,
                tally_ledger_name = @TallyLedgerName,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM vendors WHERE id = @id",
                new { id });
        }

        public async Task<decimal> GetOutstandingBalanceAsync(Guid vendorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(total_amount - COALESCE(paid_amount, 0)), 0)
                FROM vendor_invoices
                WHERE vendor_id = @vendorId
                AND status NOT IN ('draft', 'cancelled')";
            return await connection.QuerySingleAsync<decimal>(sql, new { vendorId });
        }
    }
}
