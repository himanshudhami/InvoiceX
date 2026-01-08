using Dapper;
using Npgsql;
using Core.Entities.Migration;
using Core.Interfaces.Migration;

namespace Infrastructure.Data.Migration
{
    public class TallyMigrationLogRepository : ITallyMigrationLogRepository
    {
        private readonly string _connectionString;

        public TallyMigrationLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TallyMigrationLog?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationLog>(
                @"SELECT id, batch_id AS BatchId, record_type AS RecordType,
                         tally_guid AS TallyGuid, tally_name AS TallyName,
                         tally_parent_name AS TallyParentName, tally_date AS TallyDate,
                         target_entity AS TargetEntity, target_id AS TargetId, status,
                         skip_reason AS SkipReason, error_message AS ErrorMessage,
                         error_code AS ErrorCode, validation_warnings AS ValidationWarnings,
                         raw_data AS RawData, tally_amount AS TallyAmount,
                         imported_amount AS ImportedAmount, amount_difference AS AmountDifference,
                         processing_order AS ProcessingOrder, processing_duration_ms AS ProcessingDurationMs,
                         created_at AS CreatedAt
                  FROM tally_migration_logs WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<TallyMigrationLog>> GetByBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyMigrationLog>(
                @"SELECT id, batch_id AS BatchId, record_type AS RecordType,
                         tally_guid AS TallyGuid, tally_name AS TallyName, status,
                         target_entity AS TargetEntity, target_id AS TargetId,
                         error_message AS ErrorMessage, tally_amount AS TallyAmount,
                         created_at AS CreatedAt
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId
                  ORDER BY processing_order",
                new { batchId });
        }

        public async Task<(IEnumerable<TallyMigrationLog> Items, int TotalCount)> GetPagedByBatchIdAsync(
            Guid batchId, int pageNumber, int pageSize, string? recordType = null,
            string? status = null, string? sortBy = null, bool sortDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE batch_id = @batchId";
            if (!string.IsNullOrEmpty(recordType))
                whereClause += " AND record_type = @recordType";
            if (!string.IsNullOrEmpty(status))
                whereClause += " AND status = @status";

            var orderBy = sortBy switch
            {
                "tallyName" => "tally_name",
                "status" => "status",
                "recordType" => "record_type",
                _ => "processing_order"
            };
            orderBy += sortDescending ? " DESC NULLS LAST" : " ASC NULLS LAST";

            var countSql = $"SELECT COUNT(*) FROM tally_migration_logs {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { batchId, recordType, status });

            var offset = (pageNumber - 1) * pageSize;
            var sql = $@"SELECT id, batch_id AS BatchId, record_type AS RecordType,
                                tally_guid AS TallyGuid, tally_name AS TallyName, status,
                                target_entity AS TargetEntity, target_id AS TargetId,
                                error_message AS ErrorMessage, skip_reason AS SkipReason,
                                tally_amount AS TallyAmount, created_at AS CreatedAt
                         FROM tally_migration_logs {whereClause}
                         ORDER BY {orderBy}
                         LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<TallyMigrationLog>(sql,
                new { batchId, recordType, status, pageSize, offset });

            return (items, totalCount);
        }

        public async Task<TallyMigrationLog> AddAsync(TallyMigrationLog log)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            log.Id = Guid.NewGuid();
            log.CreatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO tally_migration_logs (
                    id, batch_id, record_type, tally_guid, tally_name, tally_parent_name,
                    tally_date, target_entity, target_id, status, skip_reason, error_message,
                    error_code, validation_warnings, raw_data, tally_amount, imported_amount,
                    amount_difference, processing_order, processing_duration_ms, created_at
                  ) VALUES (
                    @Id, @BatchId, @RecordType, @TallyGuid, @TallyName, @TallyParentName,
                    @TallyDate, @TargetEntity, @TargetId, @Status, @SkipReason, @ErrorMessage,
                    @ErrorCode, @ValidationWarnings::jsonb, @RawData::jsonb, @TallyAmount, @ImportedAmount,
                    @AmountDifference, @ProcessingOrder, @ProcessingDurationMs, @CreatedAt
                  )", log);

            return log;
        }

        public async Task<IEnumerable<TallyMigrationLog>> BulkAddAsync(IEnumerable<TallyMigrationLog> logs)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var logList = logs.ToList();
            foreach (var log in logList)
            {
                log.Id = Guid.NewGuid();
                log.CreatedAt = DateTime.UtcNow;
            }

            // Use bulk insert for performance
            using var writer = await connection.BeginBinaryImportAsync(
                @"COPY tally_migration_logs (id, batch_id, record_type, tally_guid, tally_name,
                  tally_parent_name, tally_date, target_entity, target_id, status, skip_reason,
                  error_message, error_code, tally_amount, processing_order, created_at)
                  FROM STDIN (FORMAT BINARY)");

            foreach (var log in logList)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(log.Id, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(log.BatchId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(log.RecordType, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(log.TallyGuid, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(log.TallyName, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(log.TallyParentName, NpgsqlTypes.NpgsqlDbType.Varchar);
                if (log.TallyDate.HasValue)
                    await writer.WriteAsync(log.TallyDate.Value, NpgsqlTypes.NpgsqlDbType.Date);
                else
                    await writer.WriteNullAsync();
                await writer.WriteAsync(log.TargetEntity, NpgsqlTypes.NpgsqlDbType.Varchar);
                if (log.TargetId.HasValue)
                    await writer.WriteAsync(log.TargetId.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
                else
                    await writer.WriteNullAsync();
                await writer.WriteAsync(log.Status, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(log.SkipReason, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(log.ErrorMessage, NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(log.ErrorCode, NpgsqlTypes.NpgsqlDbType.Varchar);
                if (log.TallyAmount.HasValue)
                    await writer.WriteAsync(log.TallyAmount.Value, NpgsqlTypes.NpgsqlDbType.Numeric);
                else
                    await writer.WriteNullAsync();
                if (log.ProcessingOrder.HasValue)
                    await writer.WriteAsync(log.ProcessingOrder.Value, NpgsqlTypes.NpgsqlDbType.Integer);
                else
                    await writer.WriteNullAsync();
                await writer.WriteAsync(log.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
            }

            await writer.CompleteAsync();
            return logList;
        }

        public async Task UpdateAsync(TallyMigrationLog log)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tally_migration_logs SET
                    status = @Status, target_entity = @TargetEntity, target_id = @TargetId,
                    skip_reason = @SkipReason, error_message = @ErrorMessage, error_code = @ErrorCode,
                    imported_amount = @ImportedAmount, amount_difference = @AmountDifference,
                    processing_duration_ms = @ProcessingDurationMs
                  WHERE id = @Id", log);
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, Guid? targetId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE tally_migration_logs SET
                    status = @status, error_message = @errorMessage, target_id = @targetId
                  WHERE id = @id",
                new { id, status, errorMessage, targetId });
        }

        public async Task DeleteByBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM tally_migration_logs WHERE batch_id = @batchId",
                new { batchId });
        }

        public async Task<TallyMigrationLog?> GetByTallyGuidAsync(Guid batchId, string tallyGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TallyMigrationLog>(
                @"SELECT * FROM tally_migration_logs
                  WHERE batch_id = @batchId AND tally_guid = @tallyGuid",
                new { batchId, tallyGuid });
        }

        public async Task<IEnumerable<TallyMigrationLog>> GetFailedByBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyMigrationLog>(
                @"SELECT id, batch_id AS BatchId, record_type AS RecordType,
                         tally_guid AS TallyGuid, tally_name AS TallyName, status,
                         error_message AS ErrorMessage, error_code AS ErrorCode,
                         raw_data AS RawData
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId AND status = 'failed'
                  ORDER BY processing_order",
                new { batchId });
        }

        public async Task<IEnumerable<TallyMigrationLog>> GetSuspenseByBatchIdAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TallyMigrationLog>(
                @"SELECT id, batch_id AS BatchId, record_type AS RecordType,
                         tally_guid AS TallyGuid, tally_name AS TallyName, status,
                         target_entity AS TargetEntity, target_id AS TargetId,
                         tally_amount AS TallyAmount
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId AND status = 'mapped_to_suspense'
                  ORDER BY processing_order",
                new { batchId });
        }

        public async Task<Dictionary<string, int>> GetCountsByStatusAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<(string Status, int Count)>(
                @"SELECT status, COUNT(*)::int AS Count
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId
                  GROUP BY status",
                new { batchId });

            return results.ToDictionary(r => r.Status, r => r.Count);
        }

        public async Task<Dictionary<string, int>> GetCountsByRecordTypeAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<(string RecordType, int Count)>(
                @"SELECT record_type, COUNT(*)::int AS Count
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId
                  GROUP BY record_type",
                new { batchId });

            return results.ToDictionary(r => r.RecordType, r => r.Count);
        }

        public async Task<decimal> GetTotalAmountDifferenceAsync(Guid batchId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(ABS(amount_difference)), 0)
                  FROM tally_migration_logs
                  WHERE batch_id = @batchId AND amount_difference IS NOT NULL",
                new { batchId });
        }
    }
}
