using Dapper;
using Npgsql;
using Core.Entities.Migration;
using Core.Interfaces.Migration;

namespace Infrastructure.Data.Migration
{
    /// <summary>
    /// Repository for Tally ledger to control account + party mappings.
    /// Part of COA Modernization (Task 16).
    /// </summary>
    public class TallyLedgerMappingRepository : ITallyLedgerMappingRepository
    {
        private readonly string _connectionString;

        private const string SelectColumns = @"
            id, company_id AS CompanyId,
            tally_ledger_name AS TallyLedgerName, tally_ledger_guid AS TallyLedgerGuid,
            tally_parent_group AS TallyParentGroup,
            control_account_id AS ControlAccountId, party_type AS PartyType, party_id AS PartyId,
            legacy_coa_id AS LegacyCOAId, opening_balance AS OpeningBalance,
            opening_balance_date AS OpeningBalanceDate,
            is_active AS IsActive, last_sync_at AS LastSyncAt,
            created_at AS CreatedAt, updated_at AS UpdatedAt";

        public TallyLedgerMappingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== CRUD ====================

        public async Task<TallyLedgerMapping?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyLedgerMapping>(
                $"SELECT {SelectColumns} FROM tally_ledger_mapping WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TallyLedgerMapping>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId
                   ORDER BY party_type, tally_ledger_name",
                new { companyId });
        }

        public async Task<IEnumerable<TallyLedgerMapping>> GetActiveByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId AND is_active = true
                   ORDER BY party_type, tally_ledger_name",
                new { companyId });
        }

        public async Task<(IEnumerable<TallyLedgerMapping> Items, int TotalCount)> GetPagedAsync(
            Guid companyId, int pageNumber, int pageSize, string? partyType = null,
            string? searchTerm = null, string? sortBy = null, bool sortDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(partyType))
                whereClause += " AND party_type = @partyType";
            if (!string.IsNullOrEmpty(searchTerm))
                whereClause += " AND tally_ledger_name ILIKE @searchTerm";

            var orderBy = sortBy switch
            {
                "tallyLedgerName" => "tally_ledger_name",
                "partyType" => "party_type",
                "createdAt" => "created_at",
                _ => "tally_ledger_name"
            };
            var direction = sortDescending ? "DESC" : "ASC";

            var countSql = $"SELECT COUNT(*) FROM tally_ledger_mapping {whereClause}";
            var dataSql = $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                             {whereClause}
                             ORDER BY {orderBy} {direction}
                             LIMIT @pageSize OFFSET @offset";

            var offset = (pageNumber - 1) * pageSize;
            var parameters = new
            {
                companyId,
                partyType,
                searchTerm = $"%{searchTerm}%",
                pageSize,
                offset
            };

            var count = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<TallyLedgerMapping>(dataSql, parameters);

            return (items, count);
        }

        public async Task<TallyLedgerMapping> AddAsync(TallyLedgerMapping mapping)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            mapping.Id = Guid.NewGuid();
            mapping.CreatedAt = DateTime.UtcNow;
            mapping.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(@"
                INSERT INTO tally_ledger_mapping (
                    id, company_id, tally_ledger_name, tally_ledger_guid, tally_parent_group,
                    control_account_id, party_type, party_id, legacy_coa_id,
                    opening_balance, opening_balance_date, is_active, last_sync_at,
                    created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @TallyLedgerName, @TallyLedgerGuid, @TallyParentGroup,
                    @ControlAccountId, @PartyType, @PartyId, @LegacyCOAId,
                    @OpeningBalance, @OpeningBalanceDate, @IsActive, @LastSyncAt,
                    @CreatedAt, @UpdatedAt
                )", mapping);

            return mapping;
        }

        public async Task<IEnumerable<TallyLedgerMapping>> BulkAddAsync(IEnumerable<TallyLedgerMapping> mappings)
        {
            var mappingsList = mappings.ToList();
            if (!mappingsList.Any()) return mappingsList;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var mapping in mappingsList)
                {
                    mapping.Id = Guid.NewGuid();
                    mapping.CreatedAt = DateTime.UtcNow;
                    mapping.UpdatedAt = DateTime.UtcNow;

                    await connection.ExecuteAsync(@"
                        INSERT INTO tally_ledger_mapping (
                            id, company_id, tally_ledger_name, tally_ledger_guid, tally_parent_group,
                            control_account_id, party_type, party_id, legacy_coa_id,
                            opening_balance, opening_balance_date, is_active, last_sync_at,
                            created_at, updated_at
                        ) VALUES (
                            @Id, @CompanyId, @TallyLedgerName, @TallyLedgerGuid, @TallyParentGroup,
                            @ControlAccountId, @PartyType, @PartyId, @LegacyCOAId,
                            @OpeningBalance, @OpeningBalanceDate, @IsActive, @LastSyncAt,
                            @CreatedAt, @UpdatedAt
                        )", mapping, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return mappingsList;
        }

        public async Task UpdateAsync(TallyLedgerMapping mapping)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            mapping.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(@"
                UPDATE tally_ledger_mapping SET
                    tally_ledger_name = @TallyLedgerName,
                    tally_ledger_guid = @TallyLedgerGuid,
                    tally_parent_group = @TallyParentGroup,
                    control_account_id = @ControlAccountId,
                    party_type = @PartyType,
                    party_id = @PartyId,
                    legacy_coa_id = @LegacyCOAId,
                    opening_balance = @OpeningBalance,
                    opening_balance_date = @OpeningBalanceDate,
                    is_active = @IsActive,
                    last_sync_at = @LastSyncAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id", mapping);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tally_ledger_mapping WHERE id = @id",
                new { id });
        }

        // ==================== Lookup Methods ====================

        public async Task<TallyLedgerMapping?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId AND tally_ledger_name = @tallyLedgerName AND is_active = true",
                new { companyId, tallyLedgerName });
        }

        public async Task<TallyLedgerMapping?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId AND tally_ledger_guid = @tallyLedgerGuid AND is_active = true",
                new { companyId, tallyLedgerGuid });
        }

        public async Task<TallyLedgerMapping?> GetByPartyIdAsync(Guid companyId, Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId AND party_id = @partyId AND is_active = true",
                new { companyId, partyId });
        }

        public async Task<IEnumerable<TallyLedgerMapping>> GetByPartyTypeAsync(Guid companyId, string partyType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyLedgerMapping>(
                $@"SELECT {SelectColumns} FROM tally_ledger_mapping
                   WHERE company_id = @companyId AND party_type = @partyType AND is_active = true
                   ORDER BY tally_ledger_name",
                new { companyId, partyType });
        }

        // ==================== Seed Methods ====================

        public async Task<int> SeedFromPartiesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get control account IDs
            var payablesAccountId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM chart_of_accounts WHERE company_id = @companyId AND account_code = '2100'",
                new { companyId });

            var receivablesAccountId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM chart_of_accounts WHERE company_id = @companyId AND account_code = '1120'",
                new { companyId });

            // Seed from parties that don't already have mappings
            // Uses is_vendor/is_customer booleans to determine party role
            var sql = @"
                INSERT INTO tally_ledger_mapping (
                    id, company_id, tally_ledger_name, tally_ledger_guid, tally_parent_group,
                    party_type, party_id, control_account_id, is_active, created_at, updated_at
                )
                SELECT
                    gen_random_uuid(),
                    p.company_id,
                    COALESCE(p.tally_ledger_name, p.display_name),
                    p.tally_ledger_guid,
                    p.tally_group_name,
                    CASE
                        WHEN p.is_vendor THEN 'vendor'
                        WHEN p.is_customer THEN 'customer'
                    END,
                    p.id,
                    CASE
                        WHEN p.is_vendor THEN @payablesAccountId
                        WHEN p.is_customer THEN @receivablesAccountId
                    END,
                    true,
                    NOW(),
                    NOW()
                FROM parties p
                WHERE p.company_id = @companyId
                  AND (p.is_vendor = true OR p.is_customer = true)
                  AND NOT EXISTS (
                      SELECT 1 FROM tally_ledger_mapping tlm
                      WHERE tlm.company_id = p.company_id AND tlm.party_id = p.id
                  )";

            return await connection.ExecuteAsync(sql, new { companyId, payablesAccountId, receivablesAccountId });
        }

        public async Task<bool> HasMappingsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM tally_ledger_mapping WHERE company_id = @companyId)",
                new { companyId });
        }

        public async Task<Dictionary<string, int>> GetMappingCountsByTypeAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<(string PartyType, int Count)>(
                @"SELECT party_type AS PartyType, COUNT(*) AS Count
                  FROM tally_ledger_mapping
                  WHERE company_id = @companyId AND is_active = true
                  GROUP BY party_type",
                new { companyId });

            return results.ToDictionary(r => r.PartyType ?? "unknown", r => r.Count);
        }
    }
}
