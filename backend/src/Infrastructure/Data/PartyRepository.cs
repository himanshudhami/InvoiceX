using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class PartyRepository : IPartyRepository
    {
        private readonly string _connectionString;

        public PartyRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<Party?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         legal_name AS LegalName, party_code AS PartyCode,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         email, phone, mobile, website, contact_person AS ContactPerson,
                         address_line1 AS AddressLine1, address_line2 AS AddressLine2,
                         city, state, state_code AS StateCode, pincode, country,
                         pan_number AS PanNumber, gstin, is_gst_registered AS IsGstRegistered,
                         gst_state_code AS GstStateCode, party_type AS PartyType,
                         is_active AS IsActive, notes,
                         tally_ledger_guid AS TallyLedgerGuid, tally_ledger_name AS TallyLedgerName,
                         tally_group_name AS TallyGroupName, tally_migration_batch_id AS TallyMigrationBatchId,
                         created_at AS CreatedAt, updated_at AS UpdatedAt,
                         created_by AS CreatedBy, updated_by AS UpdatedBy
                  FROM parties WHERE id = @id",
                new { id });
        }

        public async Task<Party?> GetByIdWithProfilesAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                       legal_name AS LegalName, party_code AS PartyCode,
                       is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                       email, phone, mobile, website, contact_person AS ContactPerson,
                       address_line1 AS AddressLine1, address_line2 AS AddressLine2,
                       city, state, state_code AS StateCode, pincode, country,
                       pan_number AS PanNumber, gstin, is_gst_registered AS IsGstRegistered,
                       gst_state_code AS GstStateCode, party_type AS PartyType,
                       is_active AS IsActive, notes,
                       tally_ledger_guid AS TallyLedgerGuid, tally_ledger_name AS TallyLedgerName,
                       tally_group_name AS TallyGroupName, tally_migration_batch_id AS TallyMigrationBatchId,
                       created_at AS CreatedAt, updated_at AS UpdatedAt,
                       created_by AS CreatedBy, updated_by AS UpdatedBy
                FROM parties WHERE id = @id;

                SELECT id, party_id AS PartyId, company_id AS CompanyId,
                       vendor_type AS VendorType, tds_applicable AS TdsApplicable,
                       default_tds_section AS DefaultTdsSection, default_tds_rate AS DefaultTdsRate,
                       tan_number AS TanNumber, lower_tds_certificate AS LowerTdsCertificate,
                       lower_tds_rate AS LowerTdsRate, lower_tds_valid_from AS LowerTdsValidFrom,
                       lower_tds_valid_till AS LowerTdsValidTill,
                       msme_registered AS MsmeRegistered, msme_registration_number AS MsmeRegistrationNumber,
                       msme_category AS MsmeCategory,
                       bank_account_number AS BankAccountNumber, bank_ifsc_code AS BankIfscCode,
                       bank_name AS BankName, bank_branch AS BankBranch,
                       bank_account_holder AS BankAccountHolder, bank_account_type AS BankAccountType,
                       default_expense_account_id AS DefaultExpenseAccountId,
                       default_payable_account_id AS DefaultPayableAccountId,
                       payment_terms_days AS PaymentTermsDays, credit_limit AS CreditLimit,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM party_vendor_profiles WHERE party_id = @id;

                SELECT id, party_id AS PartyId, company_id AS CompanyId,
                       customer_type AS CustomerType, credit_limit AS CreditLimit,
                       payment_terms_days AS PaymentTermsDays,
                       default_revenue_account_id AS DefaultRevenueAccountId,
                       default_receivable_account_id AS DefaultReceivableAccountId,
                       e_invoice_applicable AS EInvoiceApplicable,
                       e_way_bill_applicable AS EWayBillApplicable,
                       default_discount_percent AS DefaultDiscountPercent,
                       price_list_id AS PriceListId,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM party_customer_profiles WHERE party_id = @id;";

            using var multi = await connection.QueryMultipleAsync(sql, new { id });

            var party = await multi.ReadFirstOrDefaultAsync<Party>();
            if (party != null)
            {
                party.VendorProfile = await multi.ReadFirstOrDefaultAsync<PartyVendorProfile>();
                party.CustomerProfile = await multi.ReadFirstOrDefaultAsync<PartyCustomerProfile>();
            }

            return party;
        }

        public async Task<IEnumerable<Party>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         email, phone, city, state, gstin, pan_number AS PanNumber,
                         is_active AS IsActive, party_type AS PartyType,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM parties ORDER BY name");
        }

        public async Task<Party> AddAsync(Party entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO parties (
                company_id, name, display_name, legal_name, party_code,
                is_customer, is_vendor, is_employee,
                email, phone, mobile, website, contact_person,
                address_line1, address_line2, city, state, state_code, pincode, country,
                pan_number, gstin, is_gst_registered, gst_state_code, party_type,
                is_active, notes,
                tally_ledger_guid, tally_ledger_name, tally_group_name, tally_migration_batch_id,
                created_at, updated_at, created_by, updated_by
            ) VALUES (
                @CompanyId, @Name, @DisplayName, @LegalName, @PartyCode,
                @IsCustomer, @IsVendor, @IsEmployee,
                @Email, @Phone, @Mobile, @Website, @ContactPerson,
                @AddressLine1, @AddressLine2, @City, @State, @StateCode, @Pincode, @Country,
                @PanNumber, @Gstin, @IsGstRegistered, @GstStateCode, @PartyType,
                @IsActive, @Notes,
                @TallyLedgerGuid, @TallyLedgerName, @TallyGroupName, @TallyMigrationBatchId,
                NOW(), NOW(), @CreatedBy, @UpdatedBy
            ) RETURNING id, company_id AS CompanyId, name, display_name AS DisplayName,
                        is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                        created_at AS CreatedAt, updated_at AS UpdatedAt";

            return await connection.QuerySingleAsync<Party>(sql, entity);
        }

        public async Task UpdateAsync(Party entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE parties SET
                name = @Name, display_name = @DisplayName, legal_name = @LegalName, party_code = @PartyCode,
                is_customer = @IsCustomer, is_vendor = @IsVendor, is_employee = @IsEmployee,
                email = @Email, phone = @Phone, mobile = @Mobile, website = @Website, contact_person = @ContactPerson,
                address_line1 = @AddressLine1, address_line2 = @AddressLine2, city = @City, state = @State,
                state_code = @StateCode, pincode = @Pincode, country = @Country,
                pan_number = @PanNumber, gstin = @Gstin, is_gst_registered = @IsGstRegistered,
                gst_state_code = @GstStateCode, party_type = @PartyType,
                is_active = @IsActive, notes = @Notes,
                tally_ledger_guid = @TallyLedgerGuid, tally_ledger_name = @TallyLedgerName,
                tally_group_name = @TallyGroupName, tally_migration_batch_id = @TallyMigrationBatchId,
                updated_at = NOW(), updated_by = @UpdatedBy
            WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM parties WHERE id = @id", new { id });
        }

        // ==================== Company-scoped Queries ====================

        public async Task<IEnumerable<Party>> GetByCompanyIdAsync(Guid companyId, bool? isVendor = null, bool? isCustomer = null, bool? isEmployee = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "company_id = @companyId";
            if (isVendor.HasValue)
                whereClause += " AND is_vendor = @isVendor";
            if (isCustomer.HasValue)
                whereClause += " AND is_customer = @isCustomer";
            if (isEmployee.HasValue)
                whereClause += " AND is_employee = @isEmployee";

            var sql = $@"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                                is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                                email, phone, city, state, gstin, pan_number AS PanNumber,
                                is_active AS IsActive, party_type AS PartyType,
                                tally_group_name AS TallyGroupName,
                                created_at AS CreatedAt, updated_at AS UpdatedAt
                         FROM parties WHERE {whereClause} ORDER BY name";

            return await connection.QueryAsync<Party>(sql, new { companyId, isVendor, isCustomer, isEmployee });
        }

        public async Task<(IEnumerable<Party> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            bool? isVendor = null,
            bool? isCustomer = null,
            bool? isEmployee = null,
            bool? isActive = null,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var allowedColumns = new[] {
                "id", "company_id", "name", "display_name", "legal_name", "party_code",
                "is_customer", "is_vendor", "is_employee",
                "email", "phone", "mobile", "city", "state", "pincode", "country",
                "pan_number", "gstin", "is_gst_registered", "party_type", "is_active",
                "tally_group_name", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("parties", allowedColumns)
                .WhereEquals("company_id", companyId)
                .SearchAcross(new[] { "name", "display_name", "email", "phone", "gstin", "pan_number", "city" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            if (isVendor.HasValue)
                builder.WhereEquals("is_vendor", isVendor.Value);
            if (isCustomer.HasValue)
                builder.WhereEquals("is_customer", isCustomer.Value);
            if (isEmployee.HasValue)
                builder.WhereEquals("is_employee", isEmployee.Value);
            if (isActive.HasValue)
                builder.WhereEquals("is_active", isActive.Value);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "name";
            builder.OrderBy(orderBy, sortDescending);

            builder.Select(@"id, company_id AS CompanyId, name, display_name AS DisplayName,
                legal_name AS LegalName, party_code AS PartyCode,
                is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                email, phone, mobile, city, state, gstin, pan_number AS PanNumber,
                is_gst_registered AS IsGstRegistered, party_type AS PartyType,
                is_active AS IsActive, tally_group_name AS TallyGroupName,
                created_at AS CreatedAt, updated_at AS UpdatedAt");

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Party>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        // ==================== Lookup Methods ====================

        public async Task<Party?> GetByNameAsync(Guid companyId, string name)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         email, phone, gstin, pan_number AS PanNumber, is_active AS IsActive
                  FROM parties WHERE company_id = @companyId AND LOWER(name) = LOWER(@name)",
                new { companyId, name });
        }

        public async Task<Party?> GetByPartyCodeAsync(Guid companyId, string partyCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee
                  FROM parties WHERE company_id = @companyId AND party_code = @partyCode",
                new { companyId, partyCode });
        }

        public async Task<Party?> GetByGstinAsync(Guid companyId, string gstin)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor
                  FROM parties WHERE company_id = @companyId AND gstin = @gstin",
                new { companyId, gstin });
        }

        public async Task<Party?> GetByPanAsync(Guid companyId, string panNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor
                  FROM parties WHERE company_id = @companyId AND pan_number = @panNumber",
                new { companyId, panNumber });
        }

        // ==================== Role-specific Queries ====================

        public async Task<IEnumerable<Party>> GetVendorsAsync(Guid companyId)
        {
            return await GetByCompanyIdAsync(companyId, isVendor: true);
        }

        public async Task<IEnumerable<Party>> GetCustomersAsync(Guid companyId)
        {
            return await GetByCompanyIdAsync(companyId, isCustomer: true);
        }

        public async Task<IEnumerable<Party>> GetMsmeVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT p.id, p.company_id AS CompanyId, p.name, p.display_name AS DisplayName,
                         p.is_customer AS IsCustomer, p.is_vendor AS IsVendor,
                         p.email, p.phone, p.gstin, p.pan_number AS PanNumber
                  FROM parties p
                  INNER JOIN party_vendor_profiles vp ON p.id = vp.party_id
                  WHERE p.company_id = @companyId AND p.is_vendor = true AND vp.msme_registered = true
                  ORDER BY p.name",
                new { companyId });
        }

        public async Task<IEnumerable<Party>> GetTdsApplicableVendorsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT p.id, p.company_id AS CompanyId, p.name, p.display_name AS DisplayName,
                         p.is_customer AS IsCustomer, p.is_vendor AS IsVendor,
                         p.email, p.phone, p.gstin, p.pan_number AS PanNumber
                  FROM parties p
                  INNER JOIN party_vendor_profiles vp ON p.id = vp.party_id
                  WHERE p.company_id = @companyId AND p.is_vendor = true AND vp.tds_applicable = true
                  ORDER BY p.name",
                new { companyId });
        }

        // ==================== Profile Management ====================

        public async Task<PartyVendorProfile?> GetVendorProfileAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PartyVendorProfile>(
                @"SELECT id, party_id AS PartyId, company_id AS CompanyId,
                         vendor_type AS VendorType, tds_applicable AS TdsApplicable,
                         default_tds_section AS DefaultTdsSection, default_tds_rate AS DefaultTdsRate,
                         tan_number AS TanNumber, lower_tds_certificate AS LowerTdsCertificate,
                         lower_tds_rate AS LowerTdsRate, lower_tds_valid_from AS LowerTdsValidFrom,
                         lower_tds_valid_till AS LowerTdsValidTill,
                         msme_registered AS MsmeRegistered, msme_registration_number AS MsmeRegistrationNumber,
                         msme_category AS MsmeCategory,
                         bank_account_number AS BankAccountNumber, bank_ifsc_code AS BankIfscCode,
                         bank_name AS BankName, bank_branch AS BankBranch,
                         bank_account_holder AS BankAccountHolder, bank_account_type AS BankAccountType,
                         default_expense_account_id AS DefaultExpenseAccountId,
                         default_payable_account_id AS DefaultPayableAccountId,
                         payment_terms_days AS PaymentTermsDays, credit_limit AS CreditLimit,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM party_vendor_profiles WHERE party_id = @partyId",
                new { partyId });
        }

        public async Task<PartyCustomerProfile?> GetCustomerProfileAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<PartyCustomerProfile>(
                @"SELECT id, party_id AS PartyId, company_id AS CompanyId,
                         customer_type AS CustomerType, credit_limit AS CreditLimit,
                         payment_terms_days AS PaymentTermsDays,
                         default_revenue_account_id AS DefaultRevenueAccountId,
                         default_receivable_account_id AS DefaultReceivableAccountId,
                         e_invoice_applicable AS EInvoiceApplicable,
                         e_way_bill_applicable AS EWayBillApplicable,
                         default_discount_percent AS DefaultDiscountPercent,
                         price_list_id AS PriceListId,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM party_customer_profiles WHERE party_id = @partyId",
                new { partyId });
        }

        public async Task AddVendorProfileAsync(PartyVendorProfile profile)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO party_vendor_profiles (
                    party_id, company_id, vendor_type, tds_applicable,
                    default_tds_section, default_tds_rate, tan_number,
                    lower_tds_certificate, lower_tds_rate, lower_tds_valid_from, lower_tds_valid_till,
                    msme_registered, msme_registration_number, msme_category,
                    bank_account_number, bank_ifsc_code, bank_name, bank_branch,
                    bank_account_holder, bank_account_type,
                    default_expense_account_id, default_payable_account_id,
                    payment_terms_days, credit_limit,
                    created_at, updated_at
                ) VALUES (
                    @PartyId, @CompanyId, @VendorType, @TdsApplicable,
                    @DefaultTdsSection, @DefaultTdsRate, @TanNumber,
                    @LowerTdsCertificate, @LowerTdsRate, @LowerTdsValidFrom, @LowerTdsValidTill,
                    @MsmeRegistered, @MsmeRegistrationNumber, @MsmeCategory,
                    @BankAccountNumber, @BankIfscCode, @BankName, @BankBranch,
                    @BankAccountHolder, @BankAccountType,
                    @DefaultExpenseAccountId, @DefaultPayableAccountId,
                    @PaymentTermsDays, @CreditLimit,
                    NOW(), NOW()
                )", profile);
        }

        public async Task AddCustomerProfileAsync(PartyCustomerProfile profile)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO party_customer_profiles (
                    party_id, company_id, customer_type, credit_limit, payment_terms_days,
                    default_revenue_account_id, default_receivable_account_id,
                    e_invoice_applicable, e_way_bill_applicable, default_discount_percent, price_list_id,
                    created_at, updated_at
                ) VALUES (
                    @PartyId, @CompanyId, @CustomerType, @CreditLimit, @PaymentTermsDays,
                    @DefaultRevenueAccountId, @DefaultReceivableAccountId,
                    @EInvoiceApplicable, @EWayBillApplicable, @DefaultDiscountPercent, @PriceListId,
                    NOW(), NOW()
                )", profile);
        }

        public async Task UpdateVendorProfileAsync(PartyVendorProfile profile)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE party_vendor_profiles SET
                    vendor_type = @VendorType, tds_applicable = @TdsApplicable,
                    default_tds_section = @DefaultTdsSection, default_tds_rate = @DefaultTdsRate,
                    tan_number = @TanNumber, lower_tds_certificate = @LowerTdsCertificate,
                    lower_tds_rate = @LowerTdsRate, lower_tds_valid_from = @LowerTdsValidFrom,
                    lower_tds_valid_till = @LowerTdsValidTill,
                    msme_registered = @MsmeRegistered, msme_registration_number = @MsmeRegistrationNumber,
                    msme_category = @MsmeCategory,
                    bank_account_number = @BankAccountNumber, bank_ifsc_code = @BankIfscCode,
                    bank_name = @BankName, bank_branch = @BankBranch,
                    bank_account_holder = @BankAccountHolder, bank_account_type = @BankAccountType,
                    default_expense_account_id = @DefaultExpenseAccountId,
                    default_payable_account_id = @DefaultPayableAccountId,
                    payment_terms_days = @PaymentTermsDays, credit_limit = @CreditLimit,
                    updated_at = NOW()
                WHERE party_id = @PartyId", profile);
        }

        public async Task UpdateCustomerProfileAsync(PartyCustomerProfile profile)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE party_customer_profiles SET
                    customer_type = @CustomerType, credit_limit = @CreditLimit,
                    payment_terms_days = @PaymentTermsDays,
                    default_revenue_account_id = @DefaultRevenueAccountId,
                    default_receivable_account_id = @DefaultReceivableAccountId,
                    e_invoice_applicable = @EInvoiceApplicable,
                    e_way_bill_applicable = @EWayBillApplicable,
                    default_discount_percent = @DefaultDiscountPercent,
                    price_list_id = @PriceListId,
                    updated_at = NOW()
                WHERE party_id = @PartyId", profile);
        }

        public async Task DeleteVendorProfileAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM party_vendor_profiles WHERE party_id = @partyId",
                new { partyId });
        }

        public async Task DeleteCustomerProfileAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM party_customer_profiles WHERE party_id = @partyId",
                new { partyId });
        }

        // ==================== Tag Management ====================

        public async Task<IEnumerable<PartyTag>> GetTagsAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<PartyTag>(
                @"SELECT id, party_id AS PartyId, tag_id AS TagId, source,
                         created_at AS CreatedAt, created_by AS CreatedBy
                  FROM party_tags WHERE party_id = @partyId",
                new { partyId });
        }

        public async Task AddTagAsync(Guid partyId, Guid tagId, string source = "manual", Guid? createdBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO party_tags (party_id, tag_id, source, created_at, created_by)
                  VALUES (@partyId, @tagId, @source, NOW(), @createdBy)
                  ON CONFLICT (party_id, tag_id) DO NOTHING",
                new { partyId, tagId, source, createdBy });
        }

        public async Task RemoveTagAsync(Guid partyId, Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM party_tags WHERE party_id = @partyId AND tag_id = @tagId",
                new { partyId, tagId });
        }

        public async Task<IEnumerable<Party>> GetPartiesByTagAsync(Guid companyId, Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT p.id, p.company_id AS CompanyId, p.name, p.display_name AS DisplayName,
                         p.is_customer AS IsCustomer, p.is_vendor AS IsVendor,
                         p.email, p.phone, p.gstin, p.pan_number AS PanNumber
                  FROM parties p
                  INNER JOIN party_tags pt ON p.id = pt.party_id
                  WHERE p.company_id = @companyId AND pt.tag_id = @tagId
                  ORDER BY p.name",
                new { companyId, tagId });
        }

        public async Task<IEnumerable<Party>> GetPartiesByTagGroupAsync(Guid companyId, string tagGroup)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT DISTINCT p.id, p.company_id AS CompanyId, p.name, p.display_name AS DisplayName,
                         p.is_customer AS IsCustomer, p.is_vendor AS IsVendor,
                         p.email, p.phone, p.gstin, p.pan_number AS PanNumber
                  FROM parties p
                  INNER JOIN party_tags pt ON p.id = pt.party_id
                  INNER JOIN tags t ON pt.tag_id = t.id
                  WHERE p.company_id = @companyId AND t.tag_group = @tagGroup
                  ORDER BY p.name",
                new { companyId, tagGroup });
        }

        // ==================== Tally Migration ====================

        public async Task<Party?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         tally_ledger_guid AS TallyLedgerGuid, tally_ledger_name AS TallyLedgerName,
                         tally_group_name AS TallyGroupName
                  FROM parties WHERE company_id = @companyId AND tally_ledger_guid = @tallyLedgerGuid",
                new { companyId, tallyLedgerGuid });
        }

        public async Task<IEnumerable<Party>> GetByTallyGroupAsync(Guid companyId, string tallyGroupName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor,
                         tally_ledger_guid AS TallyLedgerGuid, tally_group_name AS TallyGroupName
                  FROM parties WHERE company_id = @companyId AND LOWER(tally_group_name) = LOWER(@tallyGroupName)
                  ORDER BY name",
                new { companyId, tallyGroupName });
        }

        public async Task<Party?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         tally_ledger_guid AS TallyLedgerGuid, tally_ledger_name AS TallyLedgerName,
                         tally_group_name AS TallyGroupName, pan_number AS PanNumber
                  FROM parties
                  WHERE company_id = @companyId
                    AND (tally_ledger_name = @tallyLedgerName OR name = @tallyLedgerName)",
                new { companyId, tallyLedgerName });
        }

        // ==================== Balance/Outstanding ====================

        public async Task<decimal> GetVendorOutstandingBalanceAsync(Guid partyId)
        {
            // This will be updated once vendor_invoices table is recreated with party_id
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(total_amount - COALESCE(paid_amount, 0)), 0)
                FROM vendor_invoices
                WHERE party_id = @partyId
                AND status NOT IN ('draft', 'cancelled')";
            try
            {
                return await connection.QuerySingleAsync<decimal>(sql, new { partyId });
            }
            catch
            {
                return 0; // Table may not exist yet during migration
            }
        }

        public async Task<decimal> GetCustomerOutstandingBalanceAsync(Guid partyId)
        {
            // This will be updated once invoices table is recreated with party_id
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(total_amount - COALESCE(paid_amount, 0)), 0)
                FROM invoices
                WHERE party_id = @partyId
                AND status NOT IN ('draft', 'cancelled')";
            try
            {
                return await connection.QuerySingleAsync<decimal>(sql, new { partyId });
            }
            catch
            {
                return 0; // Table may not exist yet during migration
            }
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<Party>> BulkAddAsync(IEnumerable<Party> parties)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var partyList = parties.ToList();

            foreach (var party in partyList)
            {
                var sql = @"INSERT INTO parties (
                    company_id, name, display_name, legal_name, party_code,
                    is_customer, is_vendor, is_employee,
                    email, phone, mobile, website, contact_person,
                    address_line1, address_line2, city, state, state_code, pincode, country,
                    pan_number, gstin, is_gst_registered, gst_state_code, party_type,
                    is_active, notes,
                    tally_ledger_guid, tally_ledger_name, tally_group_name, tally_migration_batch_id,
                    created_at, updated_at, created_by
                ) VALUES (
                    @CompanyId, @Name, @DisplayName, @LegalName, @PartyCode,
                    @IsCustomer, @IsVendor, @IsEmployee,
                    @Email, @Phone, @Mobile, @Website, @ContactPerson,
                    @AddressLine1, @AddressLine2, @City, @State, @StateCode, @Pincode, @Country,
                    @PanNumber, @Gstin, @IsGstRegistered, @GstStateCode, @PartyType,
                    @IsActive, @Notes,
                    @TallyLedgerGuid, @TallyLedgerName, @TallyGroupName, @TallyMigrationBatchId,
                    NOW(), NOW(), @CreatedBy
                ) RETURNING id";

                party.Id = await connection.QuerySingleAsync<Guid>(sql, party);
            }

            return partyList;
        }

        public async Task BulkUpdateRolesAsync(IEnumerable<Guid> partyIds, bool? isVendor = null, bool? isCustomer = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var updates = new List<string>();
            if (isVendor.HasValue)
                updates.Add("is_vendor = @isVendor");
            if (isCustomer.HasValue)
                updates.Add("is_customer = @isCustomer");

            if (updates.Any())
            {
                var sql = $"UPDATE parties SET {string.Join(", ", updates)}, updated_at = NOW() WHERE id = ANY(@partyIds)";
                await connection.ExecuteAsync(sql, new { partyIds = partyIds.ToArray(), isVendor, isCustomer });
            }
        }

        public async Task<IEnumerable<Party>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return Enumerable.Empty<Party>();

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Party>(
                @"SELECT id, company_id AS CompanyId, name, display_name AS DisplayName,
                         is_customer AS IsCustomer, is_vendor AS IsVendor, is_employee AS IsEmployee,
                         email, phone, gstin, pan_number AS PanNumber, is_active AS IsActive
                  FROM parties WHERE id = ANY(@ids)",
                new { ids = idList.ToArray() });
        }
    }
}
