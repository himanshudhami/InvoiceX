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
    /// Dapper implementation of IFileStorageRepository
    /// </summary>
    public class FileStorageRepository : IFileStorageRepository
    {
        private readonly string _connectionString;

        public FileStorageRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<FileStorageEntity?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT fs.*, u.display_name as uploader_name
                FROM file_storage fs
                LEFT JOIN users u ON fs.uploaded_by = u.id
                WHERE fs.id = @id AND fs.is_deleted = FALSE";

            return await connection.QueryFirstOrDefaultAsync<FileStorageEntity>(sql, new { id });
        }

        public async Task<FileStorageEntity?> GetByPathAsync(string storagePath)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT fs.*, u.display_name as uploader_name
                FROM file_storage fs
                LEFT JOIN users u ON fs.uploaded_by = u.id
                WHERE fs.storage_path = @storagePath AND fs.is_deleted = FALSE";

            return await connection.QueryFirstOrDefaultAsync<FileStorageEntity>(sql, new { storagePath });
        }

        public async Task<IEnumerable<FileStorageEntity>> GetByEntityAsync(string entityType, Guid entityId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT fs.*, u.display_name as uploader_name
                FROM file_storage fs
                LEFT JOIN users u ON fs.uploaded_by = u.id
                WHERE fs.entity_type = @entityType
                  AND fs.entity_id = @entityId
                  AND fs.is_deleted = FALSE
                ORDER BY fs.created_at DESC";

            return await connection.QueryAsync<FileStorageEntity>(sql, new { entityType, entityId });
        }

        public async Task<IEnumerable<FileStorageEntity>> GetByUploaderAsync(Guid uploaderId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT * FROM file_storage
                WHERE uploaded_by = @uploaderId AND is_deleted = FALSE
                ORDER BY created_at DESC";

            return await connection.QueryAsync<FileStorageEntity>(sql, new { uploaderId });
        }

        public async Task<(IEnumerable<FileStorageEntity> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? entityType = null,
            bool includeDeleted = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "fs.company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!includeDeleted)
            {
                whereClauses.Add("fs.is_deleted = FALSE");
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                whereClauses.Add("fs.entity_type = @entityType");
                parameters.Add("entityType", entityType);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClauses.Add("(fs.original_filename ILIKE @searchTerm OR fs.stored_filename ILIKE @searchTerm)");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (pageNumber - 1) * pageSize;

            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            var sql = $@"
                SELECT fs.*, u.display_name as uploader_name
                FROM file_storage fs
                LEFT JOIN users u ON fs.uploaded_by = u.id
                WHERE {whereClause}
                ORDER BY fs.created_at DESC
                OFFSET @offset LIMIT @limit";

            var countSql = $@"
                SELECT COUNT(*) FROM file_storage fs
                WHERE {whereClause}";

            var items = await connection.QueryAsync<FileStorageEntity>(sql, parameters);
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<FileStorageEntity> AddAsync(FileStorageEntity entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            const string sql = @"
                INSERT INTO file_storage (
                    id, company_id, original_filename, stored_filename, storage_path,
                    storage_provider, file_size, mime_type, checksum, uploaded_by,
                    entity_type, entity_id, is_deleted, created_at
                ) VALUES (
                    @Id, @CompanyId, @OriginalFilename, @StoredFilename, @StoragePath,
                    @StorageProvider, @FileSize, @MimeType, @Checksum, @UploadedBy,
                    @EntityType, @EntityId, @IsDeleted, @CreatedAt
                )
                RETURNING *";

            return await connection.QueryFirstAsync<FileStorageEntity>(sql, entity);
        }

        public async Task UpdateAsync(FileStorageEntity entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                UPDATE file_storage SET
                    entity_type = @EntityType,
                    entity_id = @EntityId
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task SoftDeleteAsync(Guid id, Guid deletedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                UPDATE file_storage SET
                    is_deleted = TRUE,
                    deleted_at = @deletedAt,
                    deleted_by = @deletedBy
                WHERE id = @id";

            await connection.ExecuteAsync(sql, new
            {
                id,
                deletedBy,
                deletedAt = DateTime.UtcNow
            });
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM file_storage WHERE id = @id", new { id });
        }

        public async Task RestoreAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                UPDATE file_storage SET
                    is_deleted = FALSE,
                    deleted_at = NULL,
                    deleted_by = NULL
                WHERE id = @id";

            await connection.ExecuteAsync(sql, new { id });
        }

        public async Task<FileStorageStats> GetStatsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT
                    COUNT(*) as total_files,
                    COALESCE(SUM(file_size), 0) as total_size_bytes
                FROM file_storage
                WHERE company_id = @companyId AND is_deleted = FALSE";

            const string byEntityTypeSql = @"
                SELECT entity_type, COUNT(*) as count
                FROM file_storage
                WHERE company_id = @companyId AND is_deleted = FALSE AND entity_type IS NOT NULL
                GROUP BY entity_type";

            const string byMimeTypeSql = @"
                SELECT mime_type, COUNT(*) as count
                FROM file_storage
                WHERE company_id = @companyId AND is_deleted = FALSE
                GROUP BY mime_type";

            var totals = await connection.QueryFirstAsync<dynamic>(sql, new { companyId });
            var byEntityType = await connection.QueryAsync<dynamic>(byEntityTypeSql, new { companyId });
            var byMimeType = await connection.QueryAsync<dynamic>(byMimeTypeSql, new { companyId });

            return new FileStorageStats
            {
                TotalFiles = (int)totals.total_files,
                TotalSizeBytes = (long)totals.total_size_bytes,
                FilesByEntityType = byEntityType.ToDictionary(
                    x => (string)x.entity_type,
                    x => (int)x.count),
                FilesByMimeType = byMimeType.ToDictionary(
                    x => (string)x.mime_type,
                    x => (int)x.count)
            };
        }
    }
}
