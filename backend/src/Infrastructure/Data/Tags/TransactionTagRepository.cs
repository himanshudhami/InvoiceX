using Core.Entities.Tags;
using Core.Interfaces.Tags;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Tags
{
    public class TransactionTagRepository : ITransactionTagRepository
    {
        private readonly string _connectionString;

        public TransactionTagRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Basic CRUD ====================

        public async Task<TransactionTag?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TransactionTag>(
                "SELECT * FROM transaction_tags WHERE id = @id",
                new { id });
        }

        public async Task<TransactionTag> AddAsync(TransactionTag entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            entity.CreatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO transaction_tags (
                    id, transaction_id, transaction_type, tag_id,
                    allocated_amount, allocation_percentage, allocation_method,
                    source, attribution_rule_id, confidence_score,
                    created_at, created_by
                ) VALUES (
                    @Id, @TransactionId, @TransactionType, @TagId,
                    @AllocatedAmount, @AllocationPercentage, @AllocationMethod,
                    @Source, @AttributionRuleId, @ConfidenceScore,
                    @CreatedAt, @CreatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<TransactionTag>(sql, entity);
        }

        public async Task UpdateAsync(TransactionTag entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                UPDATE transaction_tags SET
                    allocated_amount = @AllocatedAmount,
                    allocation_percentage = @AllocationPercentage,
                    allocation_method = @AllocationMethod,
                    source = @Source,
                    confidence_score = @ConfidenceScore
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM transaction_tags WHERE id = @id", new { id });
        }

        // ==================== By Transaction ====================

        public async Task<IEnumerable<TransactionTag>> GetByTransactionAsync(Guid transactionId, string transactionType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TransactionTag>(
                @"SELECT tt.*, t.name as TagName, t.color as TagColor, t.tag_group as TagGroup
                  FROM transaction_tags tt
                  INNER JOIN tags t ON tt.tag_id = t.id
                  WHERE tt.transaction_id = @transactionId AND tt.transaction_type = @transactionType
                  ORDER BY tt.created_at",
                new { transactionId, transactionType });
        }

        public async Task<IEnumerable<TransactionTag>> GetByTransactionsAsync(IEnumerable<Guid> transactionIds, string transactionType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TransactionTag>(
                @"SELECT tt.*, t.name as TagName, t.color as TagColor
                  FROM transaction_tags tt
                  INNER JOIN tags t ON tt.tag_id = t.id
                  WHERE tt.transaction_id = ANY(@transactionIds) AND tt.transaction_type = @transactionType
                  ORDER BY tt.transaction_id, tt.created_at",
                new { transactionIds = transactionIds.ToArray(), transactionType });
        }

        public async Task RemoveAllFromTransactionAsync(Guid transactionId, string transactionType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM transaction_tags WHERE transaction_id = @transactionId AND transaction_type = @transactionType",
                new { transactionId, transactionType });
        }

        public async Task RemoveTagFromTransactionAsync(Guid transactionId, string transactionType, Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"DELETE FROM transaction_tags
                  WHERE transaction_id = @transactionId
                    AND transaction_type = @transactionType
                    AND tag_id = @tagId",
                new { transactionId, transactionType, tagId });
        }

        // ==================== By Tag ====================

        public async Task<IEnumerable<TransactionTag>> GetByTagAsync(Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TransactionTag>(
                "SELECT * FROM transaction_tags WHERE tag_id = @tagId ORDER BY created_at DESC",
                new { tagId });
        }

        public async Task<(IEnumerable<TransactionTag> Items, int TotalCount)> GetByTagPagedAsync(
            Guid tagId,
            int pageNumber,
            int pageSize,
            string? transactionType = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;

            var sql = @"
                SELECT * FROM transaction_tags
                WHERE tag_id = @tagId
                  AND (@transactionType IS NULL OR transaction_type = @transactionType)
                  AND (@fromDate IS NULL OR created_at >= @fromDate)
                  AND (@toDate IS NULL OR created_at <= @toDate)
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset;

                SELECT COUNT(*) FROM transaction_tags
                WHERE tag_id = @tagId
                  AND (@transactionType IS NULL OR transaction_type = @transactionType)
                  AND (@fromDate IS NULL OR created_at >= @fromDate)
                  AND (@toDate IS NULL OR created_at <= @toDate)";

            using var multi = await connection.QueryMultipleAsync(sql,
                new { tagId, transactionType, fromDate, toDate, pageSize, offset });

            var items = await multi.ReadAsync<TransactionTag>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        // ==================== By Source ====================

        public async Task<IEnumerable<TransactionTag>> GetByRuleAsync(Guid attributionRuleId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TransactionTag>(
                "SELECT * FROM transaction_tags WHERE attribution_rule_id = @attributionRuleId ORDER BY created_at DESC",
                new { attributionRuleId });
        }

        public async Task<IEnumerable<TransactionTag>> GetPendingAiSuggestionsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TransactionTag>(
                @"SELECT tt.*, t.name as TagName, t.color as TagColor
                  FROM transaction_tags tt
                  INNER JOIN tags t ON tt.tag_id = t.id
                  WHERE t.company_id = @companyId AND tt.source = 'ai_suggested'
                  ORDER BY tt.created_at DESC",
                new { companyId });
        }

        // ==================== Validation ====================

        public async Task<bool> ExistsAsync(Guid transactionId, string transactionType, Guid tagId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM transaction_tags
                    WHERE transaction_id = @transactionId
                      AND transaction_type = @transactionType
                      AND tag_id = @tagId
                )",
                new { transactionId, transactionType, tagId });
        }

        // ==================== Bulk Operations ====================

        public async Task<IEnumerable<TransactionTag>> AddManyAsync(IEnumerable<TransactionTag> transactionTags)
        {
            var results = new List<TransactionTag>();
            foreach (var tt in transactionTags)
            {
                results.Add(await AddAsync(tt));
            }
            return results;
        }

        public async Task ReplaceTransactionTagsAsync(Guid transactionId, string transactionType, IEnumerable<TransactionTag> newTags)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Remove existing tags
                await connection.ExecuteAsync(
                    "DELETE FROM transaction_tags WHERE transaction_id = @transactionId AND transaction_type = @transactionType",
                    new { transactionId, transactionType },
                    transaction);

                // Add new tags
                foreach (var tag in newTags)
                {
                    tag.TransactionId = transactionId;
                    tag.TransactionType = transactionType;
                    tag.Id = Guid.NewGuid();
                    tag.CreatedAt = DateTime.UtcNow;

                    await connection.ExecuteAsync(@"
                        INSERT INTO transaction_tags (
                            id, transaction_id, transaction_type, tag_id,
                            allocated_amount, allocation_percentage, allocation_method,
                            source, attribution_rule_id, confidence_score, created_at, created_by
                        ) VALUES (
                            @Id, @TransactionId, @TransactionType, @TagId,
                            @AllocatedAmount, @AllocationPercentage, @AllocationMethod,
                            @Source, @AttributionRuleId, @ConfidenceScore, @CreatedAt, @CreatedBy
                        )",
                        tag, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==================== Statistics ====================

        public async Task<TagAllocationSummary> GetTagAllocationSummaryAsync(
            Guid tagId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT
                    t.id as TagId,
                    t.name as TagName,
                    COUNT(tt.id) as TransactionCount,
                    COALESCE(SUM(tt.allocated_amount), 0) as TotalAllocatedAmount,
                    t.budget_amount as BudgetAmount,
                    t.budget_amount - COALESCE(SUM(tt.allocated_amount), 0) as BudgetVariance
                FROM tags t
                LEFT JOIN transaction_tags tt ON t.id = tt.tag_id
                    AND (@fromDate IS NULL OR tt.created_at >= @fromDate)
                    AND (@toDate IS NULL OR tt.created_at <= @toDate)
                WHERE t.id = @tagId
                GROUP BY t.id, t.name, t.budget_amount";

            return await connection.QuerySingleOrDefaultAsync<TagAllocationSummary>(sql,
                new { tagId, fromDate, toDate }) ?? new TagAllocationSummary { TagId = tagId };
        }

        public async Task<IEnumerable<TagTransactionTypeBreakdown>> GetTagBreakdownByTransactionTypeAsync(
            Guid tagId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                WITH totals AS (
                    SELECT SUM(allocated_amount) as total
                    FROM transaction_tags
                    WHERE tag_id = @tagId
                      AND (@fromDate IS NULL OR created_at >= @fromDate)
                      AND (@toDate IS NULL OR created_at <= @toDate)
                )
                SELECT
                    transaction_type as TransactionType,
                    COUNT(*) as Count,
                    COALESCE(SUM(allocated_amount), 0) as TotalAmount,
                    CASE WHEN t.total > 0
                        THEN ROUND((COALESCE(SUM(allocated_amount), 0) / t.total) * 100, 2)
                        ELSE 0
                    END as Percentage
                FROM transaction_tags, totals t
                WHERE tag_id = @tagId
                  AND (@fromDate IS NULL OR created_at >= @fromDate)
                  AND (@toDate IS NULL OR created_at <= @toDate)
                GROUP BY transaction_type, t.total
                ORDER BY TotalAmount DESC";

            return await connection.QueryAsync<TagTransactionTypeBreakdown>(sql,
                new { tagId, fromDate, toDate });
        }
    }
}
