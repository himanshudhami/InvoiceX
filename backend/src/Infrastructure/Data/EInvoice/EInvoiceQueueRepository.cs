using System.Text.Json;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.EInvoice
{
    public class EInvoiceQueueRepository : IEInvoiceQueueRepository
    {
        private readonly string _connectionString;

        public EInvoiceQueueRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<EInvoiceQueue?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type, priority,
                         status, retry_count, max_retries, next_retry_at,
                         started_at, completed_at, processor_id, error_code,
                         error_message, request_payload, created_at, updated_at
                  FROM einvoice_queue WHERE id = @Id",
                new { Id = id });

            return result == null ? null : MapToEntity(result);
        }

        public async Task<EInvoiceQueue?> GetByInvoiceIdAsync(Guid invoiceId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id, invoice_id, action_type, priority,
                         status, retry_count, max_retries, next_retry_at,
                         started_at, completed_at, processor_id, error_code,
                         error_message, request_payload, created_at, updated_at
                  FROM einvoice_queue WHERE invoice_id = @InvoiceId";

            if (!string.IsNullOrEmpty(status))
                sql += " AND status = @Status";

            sql += " ORDER BY created_at DESC LIMIT 1";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql,
                new { InvoiceId = invoiceId, Status = status });

            return result == null ? null : MapToEntity(result);
        }

        public async Task<IEnumerable<EInvoiceQueue>> GetPendingAsync(int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type, priority,
                         status, retry_count, max_retries, next_retry_at,
                         started_at, completed_at, processor_id, error_code,
                         error_message, request_payload, created_at, updated_at
                  FROM einvoice_queue
                  WHERE status = 'pending'
                    AND (next_retry_at IS NULL OR next_retry_at <= NOW())
                  ORDER BY priority ASC, created_at ASC
                  LIMIT @Limit",
                new { Limit = limit });

            return results.Select(r => (EInvoiceQueue)MapToEntity(r)).ToList();
        }

        public async Task<IEnumerable<EInvoiceQueue>> GetRetryableAsync(DateTime currentTime, int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type, priority,
                         status, retry_count, max_retries, next_retry_at,
                         started_at, completed_at, processor_id, error_code,
                         error_message, request_payload, created_at, updated_at
                  FROM einvoice_queue
                  WHERE status = 'pending'
                    AND next_retry_at IS NOT NULL
                    AND next_retry_at <= @CurrentTime
                    AND retry_count < max_retries
                  ORDER BY priority ASC, next_retry_at ASC
                  LIMIT @Limit",
                new { CurrentTime = currentTime, Limit = limit });

            return results.Select(r => (EInvoiceQueue)MapToEntity(r)).ToList();
        }

        public async Task<IEnumerable<EInvoiceQueue>> GetByCompanyIdAsync(Guid companyId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id, invoice_id, action_type, priority,
                         status, retry_count, max_retries, next_retry_at,
                         started_at, completed_at, processor_id, error_code,
                         error_message, request_payload, created_at, updated_at
                  FROM einvoice_queue
                  WHERE company_id = @CompanyId";

            if (!string.IsNullOrEmpty(status))
                sql += " AND status = @Status";

            sql += " ORDER BY created_at DESC";

            var results = await connection.QueryAsync<dynamic>(sql,
                new { CompanyId = companyId, Status = status });

            return results.Select(r => (EInvoiceQueue)MapToEntity(r)).ToList();
        }

        public async Task<EInvoiceQueue> AddAsync(EInvoiceQueue queueItem)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var requestPayloadJson = queueItem.RequestPayload != null
                ? queueItem.RequestPayload.RootElement.GetRawText()
                : null;

            var id = await connection.ExecuteScalarAsync<Guid>(
                @"INSERT INTO einvoice_queue
                  (company_id, invoice_id, action_type, priority, status,
                   retry_count, max_retries, next_retry_at, request_payload)
                  VALUES
                  (@CompanyId, @InvoiceId, @ActionType, @Priority, @Status,
                   @RetryCount, @MaxRetries, @NextRetryAt, @RequestPayload::jsonb)
                  RETURNING id",
                new
                {
                    queueItem.CompanyId,
                    queueItem.InvoiceId,
                    queueItem.ActionType,
                    queueItem.Priority,
                    queueItem.Status,
                    queueItem.RetryCount,
                    queueItem.MaxRetries,
                    queueItem.NextRetryAt,
                    RequestPayload = requestPayloadJson
                });

            queueItem.Id = id;
            return queueItem;
        }

        public async Task UpdateAsync(EInvoiceQueue queueItem)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var requestPayloadJson = queueItem.RequestPayload != null
                ? queueItem.RequestPayload.RootElement.GetRawText()
                : null;

            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    action_type = @ActionType,
                    priority = @Priority,
                    status = @Status,
                    retry_count = @RetryCount,
                    max_retries = @MaxRetries,
                    next_retry_at = @NextRetryAt,
                    started_at = @StartedAt,
                    completed_at = @CompletedAt,
                    processor_id = @ProcessorId,
                    error_code = @ErrorCode,
                    error_message = @ErrorMessage,
                    request_payload = @RequestPayload::jsonb,
                    updated_at = NOW()
                  WHERE id = @Id",
                new
                {
                    queueItem.Id,
                    queueItem.ActionType,
                    queueItem.Priority,
                    queueItem.Status,
                    queueItem.RetryCount,
                    queueItem.MaxRetries,
                    queueItem.NextRetryAt,
                    queueItem.StartedAt,
                    queueItem.CompletedAt,
                    queueItem.ProcessorId,
                    queueItem.ErrorCode,
                    queueItem.ErrorMessage,
                    RequestPayload = requestPayloadJson
                });
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? errorCode = null, string? errorMessage = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    status = @Status,
                    error_code = @ErrorCode,
                    error_message = @ErrorMessage,
                    updated_at = NOW()
                  WHERE id = @Id",
                new { Id = id, Status = status, ErrorCode = errorCode, ErrorMessage = errorMessage });
        }

        public async Task MarkAsProcessingAsync(Guid id, string processorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    status = 'processing',
                    started_at = NOW(),
                    processor_id = @ProcessorId,
                    updated_at = NOW()
                  WHERE id = @Id AND status = 'pending'",
                new { Id = id, ProcessorId = processorId });
        }

        public async Task MarkAsCompletedAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    status = 'completed',
                    completed_at = NOW(),
                    updated_at = NOW()
                  WHERE id = @Id",
                new { Id = id });
        }

        public async Task MarkAsFailedAsync(Guid id, string errorCode, string errorMessage, DateTime? nextRetryAt = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    status = CASE WHEN retry_count >= max_retries THEN 'failed' ELSE 'pending' END,
                    retry_count = retry_count + 1,
                    error_code = @ErrorCode,
                    error_message = @ErrorMessage,
                    next_retry_at = @NextRetryAt,
                    updated_at = NOW()
                  WHERE id = @Id",
                new { Id = id, ErrorCode = errorCode, ErrorMessage = errorMessage, NextRetryAt = nextRetryAt });
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM einvoice_queue WHERE id = @Id",
                new { Id = id });
        }

        public async Task CancelByInvoiceIdAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_queue SET
                    status = 'cancelled',
                    updated_at = NOW()
                  WHERE invoice_id = @InvoiceId AND status IN ('pending', 'processing')",
                new { InvoiceId = invoiceId });
        }

        private static EInvoiceQueue MapToEntity(dynamic row)
        {
            return new EInvoiceQueue
            {
                Id = row.id,
                CompanyId = row.company_id,
                InvoiceId = row.invoice_id,
                ActionType = row.action_type,
                Priority = row.priority,
                Status = row.status,
                RetryCount = row.retry_count,
                MaxRetries = row.max_retries,
                NextRetryAt = row.next_retry_at,
                StartedAt = row.started_at,
                CompletedAt = row.completed_at,
                ProcessorId = row.processor_id,
                ErrorCode = row.error_code,
                ErrorMessage = row.error_message,
                RequestPayload = row.request_payload != null
                    ? JsonDocument.Parse(row.request_payload.ToString())
                    : null,
                CreatedAt = row.created_at,
                UpdatedAt = row.updated_at
            };
        }
    }
}
