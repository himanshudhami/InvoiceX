using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    /// <summary>
    /// Customer repository that queries from the unified parties table
    /// Maps party data with is_customer=true to Customers entity for backward compatibility
    /// </summary>
    public class CustomersRepository : ICustomersRepository
    {
        private readonly string _connectionString;

        // SQL to select from parties table and map to Customers entity
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
                COALESCE(cp.credit_limit, 0) as CreditLimit,
                COALESCE(cp.payment_terms_days, 30) as PaymentTerms,
                p.is_active as IsActive,
                p.gstin as Gstin,
                p.gst_state_code as GstStateCode,
                COALESCE(cp.customer_type, p.party_type) as CustomerType,
                p.is_gst_registered as IsGstRegistered,
                p.pan_number as PanNumber,
                p.tally_ledger_guid as TallyLedgerGuid,
                p.tally_ledger_name as TallyLedgerName,
                p.tally_migration_batch_id as TallyMigrationBatchId,
                p.created_at as CreatedAt,
                p.updated_at as UpdatedAt
            FROM parties p
            LEFT JOIN party_customer_profiles cp ON cp.party_id = p.id";

        public CustomersRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Customers?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Customers>(
                $"{SelectSql} WHERE p.id = @id AND p.is_customer = true",
                new { id });
        }

        public async Task<IEnumerable<Customers>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Customers>(
                $"{SelectSql} WHERE p.is_customer = true ORDER BY p.name");
        }

        public async Task<(IEnumerable<Customers> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "p.is_customer = true" };
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
                    p.gstin ILIKE @searchTerm
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
                LEFT JOIN party_customer_profiles cp ON cp.party_id = p.id
                WHERE {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Customers>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Customers> AddAsync(Customers entity)
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
                     tally_ledger_guid, tally_ledger_name, tally_migration_batch_id, created_at, updated_at)
                    VALUES
                    (@CompanyId, @Name, @CompanyName, @Email, @Phone, @AddressLine1, @AddressLine2,
                     @City, @State, @ZipCode, @Country, @PanNumber, @IsActive, @Gstin, @GstStateCode,
                     @IsGstRegistered, @CustomerType, true, false, false,
                     @TallyLedgerGuid, @TallyLedgerName, @TallyMigrationBatchId, NOW(), NOW())
                    RETURNING id";

                var partyId = await connection.ExecuteScalarAsync<Guid>(partySql, entity, transaction);
                entity.Id = partyId;

                // Insert customer profile if needed
                var profileSql = @"
                    INSERT INTO party_customer_profiles
                    (id, party_id, company_id, customer_type, credit_limit, payment_terms_days, created_at, updated_at)
                    VALUES
                    (gen_random_uuid(), @PartyId, @CompanyId, @CustomerType, @CreditLimit, @PaymentTerms, NOW(), NOW())";

                await connection.ExecuteAsync(profileSql, new
                {
                    PartyId = partyId,
                    entity.CompanyId,
                    entity.CustomerType,
                    entity.CreditLimit,
                    entity.PaymentTerms
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

        public async Task UpdateAsync(Customers entity)
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
                        party_type = @CustomerType,
                        updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(partySql, entity, transaction);

                // Upsert customer profile
                var profileSql = @"
                    INSERT INTO party_customer_profiles
                    (id, party_id, company_id, customer_type, credit_limit, payment_terms_days, created_at, updated_at)
                    VALUES
                    (gen_random_uuid(), @Id, @CompanyId, @CustomerType, @CreditLimit, @PaymentTerms, NOW(), NOW())
                    ON CONFLICT (party_id) DO UPDATE SET
                        customer_type = EXCLUDED.customer_type,
                        credit_limit = EXCLUDED.credit_limit,
                        payment_terms_days = EXCLUDED.payment_terms_days,
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
                "DELETE FROM parties WHERE id = @id AND is_customer = true",
                new { id });
        }

        public async Task<Customers?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Customers>(
                $"{SelectSql} WHERE p.company_id = @companyId AND p.tally_ledger_guid = @tallyLedgerGuid AND p.is_customer = true",
                new { companyId, tallyLedgerGuid });
        }

        public async Task<Customers?> GetByNameAsync(Guid companyId, string name)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Customers>(
                $"{SelectSql} WHERE p.company_id = @companyId AND LOWER(p.name) = LOWER(@name) AND p.is_customer = true",
                new { companyId, name });
        }
    }
}
