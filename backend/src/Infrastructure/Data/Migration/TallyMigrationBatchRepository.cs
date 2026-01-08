using Dapper;
using Npgsql;
using Core.Entities.Migration;
using Core.Interfaces.Migration;

namespace Infrastructure.Data.Migration
{
    public class TallyMigrationBatchRepository : ITallyMigrationBatchRepository
    {
        private readonly string _connectionString;

        public TallyMigrationBatchRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TallyMigrationBatch?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationBatch>(
                @"SELECT id, company_id AS CompanyId, batch_number AS BatchNumber, import_type AS ImportType,
                         source_file_name AS SourceFileName, source_file_size AS SourceFileSize,
                         source_format AS SourceFormat, source_checksum AS SourceChecksum,
                         tally_company_name AS TallyCompanyName, tally_company_guid AS TallyCompanyGuid,
                         tally_from_date AS TallyFromDate, tally_to_date AS TallyToDate,
                         tally_financial_year AS TallyFinancialYear, status,
                         total_ledgers AS TotalLedgers, imported_ledgers AS ImportedLedgers,
                         skipped_ledgers AS SkippedLedgers, failed_ledgers AS FailedLedgers,
                         total_stock_items AS TotalStockItems, imported_stock_items AS ImportedStockItems,
                         skipped_stock_items AS SkippedStockItems, failed_stock_items AS FailedStockItems,
                         total_cost_centers AS TotalCostCenters, imported_cost_centers AS ImportedCostCenters,
                         skipped_cost_centers AS SkippedCostCenters, failed_cost_centers AS FailedCostCenters,
                         total_godowns AS TotalGodowns, imported_godowns AS ImportedGodowns,
                         total_units AS TotalUnits, imported_units AS ImportedUnits,
                         total_stock_groups AS TotalStockGroups, imported_stock_groups AS ImportedStockGroups,
                         total_vouchers AS TotalVouchers, imported_vouchers AS ImportedVouchers,
                         skipped_vouchers AS SkippedVouchers, failed_vouchers AS FailedVouchers,
                         voucher_counts AS VoucherCounts,
                         suspense_entries_created AS SuspenseEntriesCreated,
                         suspense_total_amount AS SuspenseTotalAmount,
                         upload_started_at AS UploadStartedAt, parsing_started_at AS ParsingStartedAt,
                         parsing_completed_at AS ParsingCompletedAt, validation_started_at AS ValidationStartedAt,
                         validation_completed_at AS ValidationCompletedAt, import_started_at AS ImportStartedAt,
                         import_completed_at AS ImportCompletedAt,
                         error_message AS ErrorMessage, error_details AS ErrorDetails,
                         mapping_config AS MappingConfig,
                         created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt
                  FROM tally_migration_batches WHERE id = @id",
                new { id });
        }

        public async Task<TallyMigrationBatch?> GetByBatchNumberAsync(Guid companyId, string batchNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationBatch>(
                @"SELECT * FROM tally_migration_batches
                  WHERE company_id = @companyId AND batch_number = @batchNumber",
                new { companyId, batchNumber });
        }

        public async Task<IEnumerable<TallyMigrationBatch>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyMigrationBatch>(
                @"SELECT id, company_id AS CompanyId, batch_number AS BatchNumber, import_type AS ImportType,
                         source_file_name AS SourceFileName, source_format AS SourceFormat, status,
                         total_ledgers AS TotalLedgers, imported_ledgers AS ImportedLedgers,
                         total_vouchers AS TotalVouchers, imported_vouchers AS ImportedVouchers,
                         tally_company_name AS TallyCompanyName, tally_from_date AS TallyFromDate,
                         tally_to_date AS TallyToDate, import_completed_at AS ImportCompletedAt,
                         created_at AS CreatedAt, error_message AS ErrorMessage
                  FROM tally_migration_batches
                  WHERE company_id = @companyId
                  ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<(IEnumerable<TallyMigrationBatch> Items, int TotalCount)> GetPagedAsync(
            Guid companyId, int pageNumber, int pageSize, string? status = null,
            string? sortBy = null, bool sortDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(status))
                whereClause += " AND status = @status";

            var orderBy = sortBy switch
            {
                "batchNumber" => "batch_number",
                "status" => "status",
                "importCompletedAt" => "import_completed_at",
                _ => "created_at"
            };
            orderBy += sortDescending ? " DESC" : " ASC";

            var countSql = $"SELECT COUNT(*) FROM tally_migration_batches {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { companyId, status });

            var offset = (pageNumber - 1) * pageSize;
            var sql = $@"SELECT id, company_id AS CompanyId, batch_number AS BatchNumber, import_type AS ImportType,
                                source_file_name AS SourceFileName, source_format AS SourceFormat, status,
                                total_ledgers AS TotalLedgers, imported_ledgers AS ImportedLedgers,
                                total_vouchers AS TotalVouchers, imported_vouchers AS ImportedVouchers,
                                tally_company_name AS TallyCompanyName, import_completed_at AS ImportCompletedAt,
                                created_at AS CreatedAt, error_message AS ErrorMessage
                         FROM tally_migration_batches {whereClause}
                         ORDER BY {orderBy}
                         LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<TallyMigrationBatch>(sql,
                new { companyId, status, pageSize, offset });

            return (items, totalCount);
        }

        public async Task<TallyMigrationBatch> AddAsync(TallyMigrationBatch batch)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            batch.Id = Guid.NewGuid();
            batch.CreatedAt = DateTime.UtcNow;
            batch.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO tally_migration_batches (
                    id, company_id, batch_number, import_type, source_file_name, source_file_size,
                    source_format, source_checksum, tally_company_name, tally_company_guid,
                    tally_from_date, tally_to_date, tally_financial_year, status,
                    voucher_counts, mapping_config, created_at, created_by, updated_at
                  ) VALUES (
                    @Id, @CompanyId, @BatchNumber, @ImportType, @SourceFileName, @SourceFileSize,
                    @SourceFormat, @SourceChecksum, @TallyCompanyName, @TallyCompanyGuid,
                    @TallyFromDate, @TallyToDate, @TallyFinancialYear, @Status,
                    @VoucherCounts::jsonb, @MappingConfig::jsonb, @CreatedAt, @CreatedBy, @UpdatedAt
                  )", batch);

            return batch;
        }

        public async Task UpdateAsync(TallyMigrationBatch batch)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            batch.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE tally_migration_batches SET
                    status = @Status,
                    total_ledgers = @TotalLedgers, imported_ledgers = @ImportedLedgers,
                    skipped_ledgers = @SkippedLedgers, failed_ledgers = @FailedLedgers,
                    total_stock_items = @TotalStockItems, imported_stock_items = @ImportedStockItems,
                    skipped_stock_items = @SkippedStockItems, failed_stock_items = @FailedStockItems,
                    total_cost_centers = @TotalCostCenters, imported_cost_centers = @ImportedCostCenters,
                    skipped_cost_centers = @SkippedCostCenters, failed_cost_centers = @FailedCostCenters,
                    total_godowns = @TotalGodowns, imported_godowns = @ImportedGodowns,
                    total_units = @TotalUnits, imported_units = @ImportedUnits,
                    total_stock_groups = @TotalStockGroups, imported_stock_groups = @ImportedStockGroups,
                    total_vouchers = @TotalVouchers, imported_vouchers = @ImportedVouchers,
                    skipped_vouchers = @SkippedVouchers, failed_vouchers = @FailedVouchers,
                    voucher_counts = @VoucherCounts::jsonb,
                    suspense_entries_created = @SuspenseEntriesCreated,
                    suspense_total_amount = @SuspenseTotalAmount,
                    upload_started_at = @UploadStartedAt, parsing_started_at = @ParsingStartedAt,
                    parsing_completed_at = @ParsingCompletedAt, validation_started_at = @ValidationStartedAt,
                    validation_completed_at = @ValidationCompletedAt, import_started_at = @ImportStartedAt,
                    import_completed_at = @ImportCompletedAt,
                    error_message = @ErrorMessage, error_details = @ErrorDetails::jsonb,
                    mapping_config = @MappingConfig::jsonb, updated_at = @UpdatedAt
                  WHERE id = @Id", batch);
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tally_migration_batches SET
                    status = @status,
                    error_message = @errorMessage,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, status, errorMessage });
        }

        public async Task UpdateCountsAsync(Guid id, TallyMigrationBatch batch)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tally_migration_batches SET
                    total_ledgers = @TotalLedgers, imported_ledgers = @ImportedLedgers,
                    total_vouchers = @TotalVouchers, imported_vouchers = @ImportedVouchers,
                    total_stock_items = @TotalStockItems, imported_stock_items = @ImportedStockItems,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, batch.TotalLedgers, batch.ImportedLedgers, batch.TotalVouchers,
                      batch.ImportedVouchers, batch.TotalStockItems, batch.ImportedStockItems });
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tally_migration_batches WHERE id = @id", new { id });
        }

        public async Task<TallyMigrationBatch?> GetLatestByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationBatch>(
                @"SELECT * FROM tally_migration_batches
                  WHERE company_id = @companyId
                  ORDER BY created_at DESC LIMIT 1",
                new { companyId });
        }

        public async Task<TallyMigrationBatch?> GetLatestCompletedAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationBatch>(
                @"SELECT * FROM tally_migration_batches
                  WHERE company_id = @companyId AND status = 'completed'
                  ORDER BY import_completed_at DESC LIMIT 1",
                new { companyId });
        }

        public async Task<DateOnly?> GetLastImportDateAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<DateOnly?>(
                @"SELECT tally_to_date FROM tally_migration_batches
                  WHERE company_id = @companyId AND status = 'completed'
                  ORDER BY tally_to_date DESC LIMIT 1",
                new { companyId });
        }

        public async Task<string> GenerateBatchNumberAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) + 1 FROM tally_migration_batches WHERE company_id = @companyId",
                new { companyId });

            var year = DateTime.UtcNow.Year;
            return $"TALLY-{year}-{count:D3}";
        }
    }
}
