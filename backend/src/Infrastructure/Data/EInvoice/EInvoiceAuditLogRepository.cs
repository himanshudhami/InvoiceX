using System.Text.Json;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.EInvoice
{
    public class EInvoiceAuditLogRepository : IEInvoiceAuditLogRepository
    {
        private readonly string _connectionString;

        public EInvoiceAuditLogRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<EInvoiceAuditLog?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log WHERE id = @Id",
                new { Id = id });

            return result == null ? null : MapToEntity(result);
        }

        public async Task<IEnumerable<EInvoiceAuditLog>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log
                  WHERE invoice_id = @InvoiceId
                  ORDER BY created_at DESC",
                new { InvoiceId = invoiceId });

            return results.Select(r => (EInvoiceAuditLog)MapToEntity(r)).ToList();
        }

        public async Task<IEnumerable<EInvoiceAuditLog>> GetByCompanyIdAsync(Guid companyId, int limit = 100)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log
                  WHERE company_id = @CompanyId
                  ORDER BY created_at DESC
                  LIMIT @Limit",
                new { CompanyId = companyId, Limit = limit });

            return results.Select(r => (EInvoiceAuditLog)MapToEntity(r)).ToList();
        }

        public async Task<EInvoiceAuditLog?> GetByIrnAsync(string irn)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log
                  WHERE irn = @Irn
                  ORDER BY created_at DESC
                  LIMIT 1",
                new { Irn = irn });

            return result == null ? null : MapToEntity(result);
        }

        public async Task<(IEnumerable<EInvoiceAuditLog> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            string? actionType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE company_id = @CompanyId";
            if (!string.IsNullOrEmpty(actionType))
                whereClause += " AND action_type = @ActionType";
            if (fromDate.HasValue)
                whereClause += " AND created_at >= @FromDate";
            if (toDate.HasValue)
                whereClause += " AND created_at <= @ToDate";

            var countSql = $"SELECT COUNT(*) FROM einvoice_audit_log {whereClause}";
            var dataSql = $@"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log
                  {whereClause}
                  ORDER BY created_at DESC
                  LIMIT @PageSize OFFSET @Offset";

            var parameters = new
            {
                CompanyId = companyId,
                ActionType = actionType,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize
            };

            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var results = await connection.QueryAsync<dynamic>(dataSql, parameters);

            return (results.Select(r => (EInvoiceAuditLog)MapToEntity(r)).ToList(), totalCount);
        }

        public async Task<EInvoiceAuditLog> AddAsync(EInvoiceAuditLog auditLog)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var requestPayloadJson = auditLog.RequestPayload != null
                ? auditLog.RequestPayload.RootElement.GetRawText()
                : null;
            var responsePayloadJson = auditLog.ResponsePayload != null
                ? auditLog.ResponsePayload.RootElement.GetRawText()
                : null;

            var id = await connection.ExecuteScalarAsync<Guid>(
                @"INSERT INTO einvoice_audit_log
                  (company_id, invoice_id, action_type, request_timestamp,
                   request_payload, request_hash, response_status, response_payload,
                   response_time_ms, irn, ack_number, ack_date, error_code,
                   error_message, gsp_provider, environment, api_version,
                   user_id, ip_address)
                  VALUES
                  (@CompanyId, @InvoiceId, @ActionType, @RequestTimestamp,
                   @RequestPayload::jsonb, @RequestHash, @ResponseStatus, @ResponsePayload::jsonb,
                   @ResponseTimeMs, @Irn, @AckNumber, @AckDate, @ErrorCode,
                   @ErrorMessage, @GspProvider, @Environment, @ApiVersion,
                   @UserId, @IpAddress)
                  RETURNING id",
                new
                {
                    auditLog.CompanyId,
                    auditLog.InvoiceId,
                    auditLog.ActionType,
                    auditLog.RequestTimestamp,
                    RequestPayload = requestPayloadJson,
                    auditLog.RequestHash,
                    auditLog.ResponseStatus,
                    ResponsePayload = responsePayloadJson,
                    auditLog.ResponseTimeMs,
                    auditLog.Irn,
                    auditLog.AckNumber,
                    auditLog.AckDate,
                    auditLog.ErrorCode,
                    auditLog.ErrorMessage,
                    auditLog.GspProvider,
                    auditLog.Environment,
                    auditLog.ApiVersion,
                    auditLog.UserId,
                    auditLog.IpAddress
                });

            auditLog.Id = id;
            return auditLog;
        }

        public async Task<IEnumerable<EInvoiceAuditLog>> GetErrorsAsync(Guid companyId, int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var results = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_id, invoice_id, action_type,
                         request_timestamp, request_payload, request_hash,
                         response_status, response_payload, response_time_ms,
                         irn, ack_number, ack_date, error_code, error_message,
                         gsp_provider, environment, api_version, user_id,
                         ip_address, created_at
                  FROM einvoice_audit_log
                  WHERE company_id = @CompanyId AND error_code IS NOT NULL
                  ORDER BY created_at DESC
                  LIMIT @Limit",
                new { CompanyId = companyId, Limit = limit });

            return results.Select(r => (EInvoiceAuditLog)MapToEntity(r)).ToList();
        }

        private static EInvoiceAuditLog MapToEntity(dynamic row)
        {
            return new EInvoiceAuditLog
            {
                Id = row.id,
                CompanyId = row.company_id,
                InvoiceId = row.invoice_id,
                ActionType = row.action_type,
                RequestTimestamp = row.request_timestamp,
                RequestPayload = row.request_payload != null
                    ? JsonDocument.Parse(row.request_payload.ToString())
                    : null,
                RequestHash = row.request_hash,
                ResponseStatus = row.response_status,
                ResponsePayload = row.response_payload != null
                    ? JsonDocument.Parse(row.response_payload.ToString())
                    : null,
                ResponseTimeMs = row.response_time_ms,
                Irn = row.irn,
                AckNumber = row.ack_number,
                AckDate = row.ack_date,
                ErrorCode = row.error_code,
                ErrorMessage = row.error_message,
                GspProvider = row.gsp_provider,
                Environment = row.environment,
                ApiVersion = row.api_version,
                UserId = row.user_id,
                IpAddress = row.ip_address,
                CreatedAt = row.created_at
            };
        }
    }
}
