using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    /// <summary>
    /// Vendor repository that queries from the unified parties table
    /// Maps party data with is_vendor=true to Vendors entity for backward compatibility
    /// </summary>
    public class VendorsRepository : IVendorsRepository
    {
        private readonly string _connectionString;

        // SQL to select from parties table and map to Vendors entity
        private const string SelectSql = @"
            SELECT
                p.id as Id,
                p.company_id as CompanyId,
                p.name as Name,
                p.display_name as CompanyName,
                p.email as Email,
                p.phone as Phone,
                p.address_line1 as AddressLine1,
                p.address_line2 as AddressLine2,
                p.city as City,
                p.state as State,
                p.pincode as ZipCode,
                p.country as Country,
                p.pan_number as TaxNumber,
                COALESCE(vp.credit_limit, 0) as CreditLimit,
                COALESCE(vp.payment_terms_days, 30) as PaymentTerms,
                p.is_active as IsActive,
                p.gstin as Gstin,
                p.gst_state_code as GstStateCode,
                COALESCE(vp.vendor_type, p.party_type) as VendorType,
                p.is_gst_registered as IsGstRegistered,
                p.pan_number as PanNumber,
                vp.tan_number as TanNumber,
                vp.default_tds_section as DefaultTdsSection,
                vp.default_tds_rate as DefaultTdsRate,
                COALESCE(vp.tds_applicable, false) as TdsApplicable,
                vp.lower_tds_certificate as LowerTdsCertificate,
                vp.lower_tds_rate as LowerTdsRate,
                vp.lower_tds_valid_till as LowerTdsCertificateValidTill,
                COALESCE(vp.msme_registered, false) as MsmeRegistered,
                vp.msme_registration_number as MsmeRegistrationNumber,
                vp.msme_category as MsmeCategory,
                vp.bank_account_number as BankAccountNumber,
                vp.bank_ifsc_code as BankIfscCode,
                vp.bank_name as BankName,
                vp.bank_branch as BankBranch,
                vp.bank_account_holder as BankAccountHolderName,
                vp.default_expense_account_id as DefaultExpenseAccountId,
                vp.default_payable_account_id as DefaultPayableAccountId,
                p.tally_ledger_guid as TallyLedgerGuid,
                p.tally_ledger_name as TallyLedgerName,
                p.created_at as CreatedAt,
                p.updated_at as UpdatedAt
            FROM parties p
            LEFT JOIN party_vendor_profiles vp ON vp.party_id = p.id";

        public VendorsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Vendors?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.id = @id AND p.is_vendor = true",
                new { id });
        }

        public async Task<IEnumerable<Vendors>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                $"{SelectSql} WHERE p.is_vendor = true ORDER BY p.name");
        }

        public async Task<IEnumerable<Vendors>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.is_vendor = true ORDER BY p.name",
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

            var whereClauses = new List<string> { "p.is_vendor = true" };
            var parameters = new DynamicParameters();

            // Search across multiple fields
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add(@"(
                    p.name ILIKE @searchTerm OR
                    p.display_name ILIKE @searchTerm OR
                    p.email ILIKE @searchTerm OR
                    p.phone ILIKE @searchTerm OR
                    p.city ILIKE @searchTerm OR
                    p.gstin ILIKE @searchTerm OR
                    p.pan_number ILIKE @searchTerm
                )");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            // Apply filters
            if (filters != null)
            {
                if (filters.TryGetValue("company_id", out var companyId))
                {
                    whereClauses.Add("p.company_id = @companyId");
                    parameters.Add("companyId", companyId);
                }
                if (filters.TryGetValue("is_active", out var isActive))
                {
                    whereClauses.Add("p.is_active = @isActive");
                    parameters.Add("isActive", isActive);
                }
                if (filters.TryGetValue("is_gst_registered", out var isGstRegistered))
                {
                    whereClauses.Add("p.is_gst_registered = @isGstRegistered");
                    parameters.Add("isGstRegistered", isGstRegistered);
                }
                if (filters.TryGetValue("tds_applicable", out var tdsApplicable))
                {
                    whereClauses.Add("vp.tds_applicable = @tdsApplicable");
                    parameters.Add("tdsApplicable", tdsApplicable);
                }
                if (filters.TryGetValue("msme_registered", out var msmeRegistered))
                {
                    whereClauses.Add("vp.msme_registered = @msmeRegistered");
                    parameters.Add("msmeRegistered", msmeRegistered);
                }
            }

            var whereClause = string.Join(" AND ", whereClauses);

            // Map sort columns
            var sortColumn = sortBy?.ToLowerInvariant() switch
            {
                "name" => "p.name",
                "email" => "p.email",
                "city" => "p.city",
                "companyname" => "p.display_name",
                "company_name" => "p.display_name",
                "createdat" => "p.created_at",
                "created_at" => "p.created_at",
                "gstin" => "p.gstin",
                "pan_number" => "p.pan_number",
                _ => "p.name"
            };
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                {SelectSql}
                WHERE {whereClause}
                ORDER BY {sortColumn} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*)
                FROM parties p
                LEFT JOIN party_vendor_profiles vp ON vp.party_id = p.id
                WHERE {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Vendors>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Vendors?> GetByGstinAsync(Guid companyId, string gstin)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.gstin = @gstin AND p.is_vendor = true",
                new { companyId, gstin });
        }

        public async Task<Vendors?> GetByPanAsync(Guid companyId, string panNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.pan_number = @panNumber AND p.is_vendor = true",
                new { companyId, panNumber });
        }

        public async Task<IEnumerable<Vendors>> GetMsmeVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.is_vendor = true AND vp.msme_registered = true ORDER BY p.name",
                new { companyId });
        }

        public async Task<IEnumerable<Vendors>> GetTdsApplicableVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.is_vendor = true AND vp.tds_applicable = true ORDER BY p.name",
                new { companyId });
        }

        public async Task<Vendors?> GetByTallyGuidAsync(string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.tally_ledger_guid = @tallyLedgerGuid AND p.is_vendor = true",
                new { tallyLedgerGuid });
        }

        public async Task<Vendors> AddAsync(Vendors entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Insert into parties table
                var partySql = @"
                    INSERT INTO parties
                    (company_id, name, display_name, email, phone, address_line1, address_line2,
                     city, state, pincode, country, pan_number, is_active, gstin, gst_state_code,
                     is_gst_registered, party_type, is_customer, is_vendor, is_employee,
                     tally_ledger_guid, tally_ledger_name, created_at, updated_at)
                    VALUES
                    (@CompanyId, @Name, @CompanyName, @Email, @Phone, @AddressLine1, @AddressLine2,
                     @City, @State, @ZipCode, @Country, @PanNumber, @IsActive, @Gstin, @GstStateCode,
                     @IsGstRegistered, @VendorType, false, true, false,
                     @TallyLedgerGuid, @TallyLedgerName, NOW(), NOW())
                    RETURNING id";

                var partyId = await connection.ExecuteScalarAsync<Guid>(partySql, entity, transaction);
                entity.Id = partyId;

                // Insert vendor profile
                var profileSql = @"
                    INSERT INTO party_vendor_profiles
                    (id, party_id, company_id, vendor_type, tds_applicable, default_tds_section,
                     default_tds_rate, tan_number, lower_tds_certificate, lower_tds_rate,
                     lower_tds_valid_till, msme_registered, msme_registration_number, msme_category,
                     bank_account_number, bank_ifsc_code, bank_name, bank_branch, bank_account_holder,
                     default_expense_account_id, default_payable_account_id, payment_terms_days,
                     credit_limit, created_at, updated_at)
                    VALUES
                    (gen_random_uuid(), @PartyId, @CompanyId, @VendorType, @TdsApplicable, @DefaultTdsSection,
                     @DefaultTdsRate, @TanNumber, @LowerTdsCertificate, @LowerTdsRate,
                     @LowerTdsCertificateValidTill, @MsmeRegistered, @MsmeRegistrationNumber, @MsmeCategory,
                     @BankAccountNumber, @BankIfscCode, @BankName, @BankBranch, @BankAccountHolderName,
                     @DefaultExpenseAccountId, @DefaultPayableAccountId, @PaymentTerms,
                     @CreditLimit, NOW(), NOW())";

                await connection.ExecuteAsync(profileSql, new
                {
                    PartyId = partyId,
                    entity.CompanyId,
                    entity.VendorType,
                    entity.TdsApplicable,
                    entity.DefaultTdsSection,
                    entity.DefaultTdsRate,
                    entity.TanNumber,
                    entity.LowerTdsCertificate,
                    entity.LowerTdsRate,
                    entity.LowerTdsCertificateValidTill,
                    entity.MsmeRegistered,
                    entity.MsmeRegistrationNumber,
                    entity.MsmeCategory,
                    entity.BankAccountNumber,
                    entity.BankIfscCode,
                    entity.BankName,
                    entity.BankBranch,
                    entity.BankAccountHolderName,
                    entity.DefaultExpenseAccountId,
                    entity.DefaultPayableAccountId,
                    entity.PaymentTerms,
                    entity.CreditLimit
                }, transaction);

                await transaction.CommitAsync();

                // Fetch and return the complete entity
                return (await GetByIdAsync(partyId))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Vendors entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Update parties table
                var partySql = @"
                    UPDATE parties SET
                        company_id = @CompanyId,
                        name = @Name,
                        display_name = @CompanyName,
                        email = @Email,
                        phone = @Phone,
                        address_line1 = @AddressLine1,
                        address_line2 = @AddressLine2,
                        city = @City,
                        state = @State,
                        pincode = @ZipCode,
                        country = @Country,
                        pan_number = @PanNumber,
                        is_active = @IsActive,
                        gstin = @Gstin,
                        gst_state_code = @GstStateCode,
                        is_gst_registered = @IsGstRegistered,
                        party_type = @VendorType,
                        tally_ledger_guid = @TallyLedgerGuid,
                        tally_ledger_name = @TallyLedgerName,
                        updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(partySql, entity, transaction);

                // Upsert vendor profile
                var profileSql = @"
                    INSERT INTO party_vendor_profiles
                    (id, party_id, company_id, vendor_type, tds_applicable, default_tds_section,
                     default_tds_rate, tan_number, lower_tds_certificate, lower_tds_rate,
                     lower_tds_valid_till, msme_registered, msme_registration_number, msme_category,
                     bank_account_number, bank_ifsc_code, bank_name, bank_branch, bank_account_holder,
                     default_expense_account_id, default_payable_account_id, payment_terms_days,
                     credit_limit, created_at, updated_at)
                    VALUES
                    (gen_random_uuid(), @Id, @CompanyId, @VendorType, @TdsApplicable, @DefaultTdsSection,
                     @DefaultTdsRate, @TanNumber, @LowerTdsCertificate, @LowerTdsRate,
                     @LowerTdsCertificateValidTill, @MsmeRegistered, @MsmeRegistrationNumber, @MsmeCategory,
                     @BankAccountNumber, @BankIfscCode, @BankName, @BankBranch, @BankAccountHolderName,
                     @DefaultExpenseAccountId, @DefaultPayableAccountId, @PaymentTerms,
                     @CreditLimit, NOW(), NOW())
                    ON CONFLICT (party_id) DO UPDATE SET
                        vendor_type = EXCLUDED.vendor_type,
                        tds_applicable = EXCLUDED.tds_applicable,
                        default_tds_section = EXCLUDED.default_tds_section,
                        default_tds_rate = EXCLUDED.default_tds_rate,
                        tan_number = EXCLUDED.tan_number,
                        lower_tds_certificate = EXCLUDED.lower_tds_certificate,
                        lower_tds_rate = EXCLUDED.lower_tds_rate,
                        lower_tds_valid_till = EXCLUDED.lower_tds_valid_till,
                        msme_registered = EXCLUDED.msme_registered,
                        msme_registration_number = EXCLUDED.msme_registration_number,
                        msme_category = EXCLUDED.msme_category,
                        bank_account_number = EXCLUDED.bank_account_number,
                        bank_ifsc_code = EXCLUDED.bank_ifsc_code,
                        bank_name = EXCLUDED.bank_name,
                        bank_branch = EXCLUDED.bank_branch,
                        bank_account_holder = EXCLUDED.bank_account_holder,
                        default_expense_account_id = EXCLUDED.default_expense_account_id,
                        default_payable_account_id = EXCLUDED.default_payable_account_id,
                        payment_terms_days = EXCLUDED.payment_terms_days,
                        credit_limit = EXCLUDED.credit_limit,
                        updated_at = NOW()";

                await connection.ExecuteAsync(profileSql, entity, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Deleting from parties will cascade to profiles due to FK constraint
            await connection.ExecuteAsync(
                "DELETE FROM parties WHERE id = @id AND is_vendor = true",
                new { id });
        }

        public async Task<decimal> GetOutstandingBalanceAsync(Guid vendorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(total_amount - COALESCE(paid_amount, 0)), 0)
                FROM vendor_invoices
                WHERE party_id = @vendorId
                AND status NOT IN ('draft', 'cancelled')";
            return await connection.QuerySingleAsync<decimal>(sql, new { vendorId });
        }

        public async Task<Vendors?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.tally_ledger_guid = @tallyLedgerGuid AND p.is_vendor = true",
                new { companyId, tallyLedgerGuid });
        }

        public async Task<Vendors?> GetByNameAsync(Guid companyId, string name)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Vendors>(
                $"{SelectSql} WHERE p.company_id = @companyId AND LOWER(p.name) = LOWER(@name) AND p.is_vendor = true",
                new { companyId, name });
        }
    }
}
