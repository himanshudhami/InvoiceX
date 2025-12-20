using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities.FileStorage;
using Core.Interfaces.FileStorage;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.FileStorage
{
    /// <summary>
    /// Dapper implementation of IDocumentAuditLogRepository
    /// </summary>
    public class DocumentAuditLogRepository : IDocumentAuditLogRepository
    {
        private readonly string _connectionString;

        public DocumentAuditLogRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<DocumentAuditLog> AddAsync(DocumentAuditLog entry)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }

            if (entry.CreatedAt == default)
            {
                entry.CreatedAt = DateTime.UtcNow;
            }

            const string sql = @"
                INSERT INTO document_audit_log (
                    id, company_id, document_id, file_storage_id, action,
                    actor_id, actor_ip, user_agent, metadata, created_at
                ) VALUES (
                    @Id, @CompanyId, @DocumentId, @FileStorageId, @Action,
                    @ActorId, @ActorIp, @UserAgent, @Metadata::jsonb, @CreatedAt
                )
                RETURNING *";

            return await connection.QueryFirstAsync<DocumentAuditLog>(sql, entry);
        }

        public async Task<IEnumerable<DocumentAuditLog>> GetByDocumentIdAsync(Guid documentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT dal.*, u.display_name as actor_name
                FROM document_audit_log dal
                LEFT JOIN users u ON dal.actor_id = u.id
                WHERE dal.document_id = @documentId
                ORDER BY dal.created_at DESC";

            return await connection.QueryAsync<DocumentAuditLog>(sql, new { documentId });
        }

        public async Task<IEnumerable<DocumentAuditLog>> GetByFileStorageIdAsync(Guid fileStorageId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT dal.*, u.display_name as actor_name, fs.original_filename as file_name
                FROM document_audit_log dal
                LEFT JOIN users u ON dal.actor_id = u.id
                LEFT JOIN file_storage fs ON dal.file_storage_id = fs.id
                WHERE dal.file_storage_id = @fileStorageId
                ORDER BY dal.created_at DESC";

            return await connection.QueryAsync<DocumentAuditLog>(sql, new { fileStorageId });
        }

        public async Task<IEnumerable<DocumentAuditLog>> GetByActorAsync(Guid actorId, int limit = 100)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT dal.*, fs.original_filename as file_name
                FROM document_audit_log dal
                LEFT JOIN file_storage fs ON dal.file_storage_id = fs.id
                WHERE dal.actor_id = @actorId
                ORDER BY dal.created_at DESC
                LIMIT @limit";

            return await connection.QueryAsync<DocumentAuditLog>(sql, new { actorId, limit });
        }

        public async Task<(IEnumerable<DocumentAuditLog> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? action = null,
            Guid? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "dal.company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrEmpty(action))
            {
                whereClauses.Add("dal.action = @action");
                parameters.Add("action", action);
            }

            if (actorId.HasValue)
            {
                whereClauses.Add("dal.actor_id = @actorId");
                parameters.Add("actorId", actorId.Value);
            }

            if (fromDate.HasValue)
            {
                whereClauses.Add("dal.created_at >= @fromDate");
                parameters.Add("fromDate", fromDate.Value);
            }

            if (toDate.HasValue)
            {
                whereClauses.Add("dal.created_at <= @toDate");
                parameters.Add("toDate", toDate.Value);
            }

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (pageNumber - 1) * pageSize;

            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var sql = $@"
                SELECT dal.*, u.display_name as actor_name, fs.original_filename as file_name
                FROM document_audit_log dal
                LEFT JOIN users u ON dal.actor_id = u.id
                LEFT JOIN file_storage fs ON dal.file_storage_id = fs.id
                WHERE {whereClause}
                ORDER BY dal.created_at DESC
                OFFSET @offset LIMIT @limit";

            var countSql = $@"
                SELECT COUNT(*) FROM document_audit_log dal
                WHERE {whereClause}";

            var items = await connection.QueryAsync<DocumentAuditLog>(sql, parameters);
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<IEnumerable<DocumentAuditLog>> GetRecentAsync(Guid companyId, int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT dal.*, u.display_name as actor_name, fs.original_filename as file_name
                FROM document_audit_log dal
                LEFT JOIN users u ON dal.actor_id = u.id
                LEFT JOIN file_storage fs ON dal.file_storage_id = fs.id
                WHERE dal.company_id = @companyId
                ORDER BY dal.created_at DESC
                LIMIT @limit";

            return await connection.QueryAsync<DocumentAuditLog>(sql, new { companyId, limit });
        }
    }
}
