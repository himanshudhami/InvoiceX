using Core.Entities.Audit;
using Core.Interfaces.Audit;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Audit
{
    /// <summary>
    /// Dapper implementation of IAuditTrailRepository for MCA-compliant audit trail
    /// </summary>
    public class AuditTrailRepository : IAuditTrailRepository
    {
        private readonly string _connectionString;

        public AuditTrailRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<AuditTrail> AddAsync(AuditTrail entry)
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
                INSERT INTO audit_trail (
                    id, company_id, entity_type, entity_id, entity_display_name,
                    operation, old_values, new_values, changed_fields,
                    actor_id, actor_name, actor_email, actor_ip, user_agent,
                    correlation_id, request_path, request_method, created_at, checksum
                ) VALUES (
                    @Id, @CompanyId, @EntityType, @EntityId, @EntityDisplayName,
                    @Operation, @OldValues::jsonb, @NewValues::jsonb, @ChangedFields,
                    @ActorId, @ActorName, @ActorEmail, @ActorIp, @UserAgent,
                    @CorrelationId, @RequestPath, @RequestMethod, @CreatedAt, @Checksum
                )
                RETURNING *";

            return await connection.QueryFirstAsync<AuditTrail>(sql, entry);
        }

        public async Task AddBatchAsync(IEnumerable<AuditTrail> entries)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                const string sql = @"
                    INSERT INTO audit_trail (
                        id, company_id, entity_type, entity_id, entity_display_name,
                        operation, old_values, new_values, changed_fields,
                        actor_id, actor_name, actor_email, actor_ip, user_agent,
                        correlation_id, request_path, request_method, created_at, checksum
                    ) VALUES (
                        @Id, @CompanyId, @EntityType, @EntityId, @EntityDisplayName,
                        @Operation, @OldValues::jsonb, @NewValues::jsonb, @ChangedFields,
                        @ActorId, @ActorName, @ActorEmail, @ActorIp, @UserAgent,
                        @CorrelationId, @RequestPath, @RequestMethod, @CreatedAt, @Checksum
                    )";

                foreach (var entry in entries)
                {
                    if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
                    if (entry.CreatedAt == default) entry.CreatedAt = DateTime.UtcNow;
                    await connection.ExecuteAsync(sql, entry, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuditTrail?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT * FROM audit_trail
                WHERE id = @id";

            return await connection.QueryFirstOrDefaultAsync<AuditTrail>(sql, new { id });
        }

        public async Task<IEnumerable<AuditTrail>> GetByEntityAsync(string entityType, Guid entityId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT * FROM audit_trail
                WHERE entity_type = @entityType AND entity_id = @entityId
                ORDER BY created_at DESC";

            return await connection.QueryAsync<AuditTrail>(sql, new { entityType, entityId });
        }

        public async Task<IEnumerable<AuditTrail>> GetByActorAsync(Guid actorId, int limit = 100)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT * FROM audit_trail
                WHERE actor_id = @actorId
                ORDER BY created_at DESC
                LIMIT @limit";

            return await connection.QueryAsync<AuditTrail>(sql, new { actorId, limit });
        }

        public async Task<(IEnumerable<AuditTrail> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? entityType = null,
            Guid? entityId = null,
            string? operation = null,
            Guid? actorId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrEmpty(entityType))
            {
                whereClauses.Add("entity_type = @entityType");
                parameters.Add("entityType", entityType);
            }

            if (entityId.HasValue)
            {
                whereClauses.Add("entity_id = @entityId");
                parameters.Add("entityId", entityId.Value);
            }

            if (!string.IsNullOrEmpty(operation))
            {
                whereClauses.Add("operation = @operation");
                parameters.Add("operation", operation);
            }

            if (actorId.HasValue)
            {
                whereClauses.Add("actor_id = @actorId");
                parameters.Add("actorId", actorId.Value);
            }

            if (fromDate.HasValue)
            {
                whereClauses.Add("created_at >= @fromDate");
                parameters.Add("fromDate", fromDate.Value);
            }

            if (toDate.HasValue)
            {
                whereClauses.Add("created_at <= @toDate");
                parameters.Add("toDate", toDate.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClauses.Add("(entity_display_name ILIKE @search OR actor_name ILIKE @search OR actor_email ILIKE @search)");
                parameters.Add("search", $"%{searchTerm}%");
            }

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (pageNumber - 1) * pageSize;

            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var sql = $@"
                SELECT * FROM audit_trail
                WHERE {whereClause}
                ORDER BY created_at DESC
                OFFSET @offset LIMIT @limit";

            var countSql = $@"
                SELECT COUNT(*) FROM audit_trail
                WHERE {whereClause}";

            var items = await connection.QueryAsync<AuditTrail>(sql, parameters);
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<IEnumerable<AuditTrail>> GetRecentAsync(Guid companyId, int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT * FROM audit_trail
                WHERE company_id = @companyId
                ORDER BY created_at DESC
                LIMIT @limit";

            return await connection.QueryAsync<AuditTrail>(sql, new { companyId, limit });
        }

        public async Task<IEnumerable<AuditTrail>> GetByDateRangeAsync(
            Guid companyId,
            DateTime fromDate,
            DateTime toDate,
            string? entityType = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM audit_trail
                WHERE company_id = @companyId
                  AND created_at >= @fromDate
                  AND created_at <= @toDate";

            if (!string.IsNullOrEmpty(entityType))
            {
                sql += " AND entity_type = @entityType";
            }

            sql += " ORDER BY created_at DESC";

            return await connection.QueryAsync<AuditTrail>(sql, new { companyId, fromDate, toDate, entityType });
        }

        public async Task<Dictionary<string, int>> GetOperationCountsAsync(
            Guid companyId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (fromDate.HasValue)
            {
                whereClauses.Add("created_at >= @fromDate");
                parameters.Add("fromDate", fromDate.Value);
            }

            if (toDate.HasValue)
            {
                whereClauses.Add("created_at <= @toDate");
                parameters.Add("toDate", toDate.Value);
            }

            var whereClause = string.Join(" AND ", whereClauses);

            var sql = $@"
                SELECT operation, COUNT(*) as count
                FROM audit_trail
                WHERE {whereClause}
                GROUP BY operation";

            var results = await connection.QueryAsync<(string operation, int count)>(sql, parameters);
            return results.ToDictionary(r => r.operation, r => r.count);
        }
    }
}
