using Core.Entities.Expense;
using Core.Interfaces.Expense;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Expense
{
    /// <summary>
    /// Dapper implementation of expense attachment repository.
    /// </summary>
    public class ExpenseAttachmentRepository : IExpenseAttachmentRepository
    {
        private readonly string _connectionString;

        public ExpenseAttachmentRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<ExpenseAttachment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExpenseAttachment>(
                @"SELECT ea.id, ea.expense_id, ea.file_storage_id, ea.description,
                         ea.is_primary, ea.created_at, ea.attachment_type, ea.uploaded_by,
                         fs.original_filename, fs.mime_type, fs.file_size, fs.storage_path,
                         '/api/files/download/' || REPLACE(fs.storage_path, '/', '%2F') as download_url
                  FROM expense_attachments ea
                  JOIN file_storage fs ON ea.file_storage_id = fs.id
                  WHERE ea.id = @id",
                new { id });
        }

        public async Task<IEnumerable<ExpenseAttachment>> GetByExpenseAsync(Guid expenseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ExpenseAttachment>(
                @"SELECT ea.id, ea.expense_id, ea.file_storage_id, ea.description,
                         ea.is_primary, ea.created_at, ea.attachment_type, ea.uploaded_by,
                         fs.original_filename, fs.mime_type, fs.file_size, fs.storage_path,
                         '/api/files/download/' || REPLACE(fs.storage_path, '/', '%2F') as download_url
                  FROM expense_attachments ea
                  JOIN file_storage fs ON ea.file_storage_id = fs.id
                  WHERE ea.expense_id = @expenseId
                  ORDER BY ea.attachment_type, ea.is_primary DESC, ea.created_at",
                new { expenseId });
        }

        public async Task<ExpenseAttachment> AddAsync(ExpenseAttachment attachment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"INSERT INTO expense_attachments
                        (id, expense_id, file_storage_id, description, is_primary, created_at, attachment_type, uploaded_by)
                        VALUES
                        (@Id, @ExpenseId, @FileStorageId, @Description, @IsPrimary, @CreatedAt, @AttachmentType, @UploadedBy)
                        RETURNING id";

            attachment.Id = attachment.Id == Guid.Empty ? Guid.NewGuid() : attachment.Id;
            attachment.CreatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(sql, attachment);

            // Re-fetch with joins
            return await GetByIdAsync(attachment.Id) ?? attachment;
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM expense_attachments WHERE id = @id",
                new { id });
        }

        public async Task SetPrimaryAsync(Guid expenseId, Guid attachmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // First, unset all as non-primary
                await connection.ExecuteAsync(
                    "UPDATE expense_attachments SET is_primary = FALSE WHERE expense_id = @expenseId",
                    new { expenseId },
                    transaction);

                // Then set the selected one as primary
                await connection.ExecuteAsync(
                    "UPDATE expense_attachments SET is_primary = TRUE WHERE id = @attachmentId",
                    new { attachmentId },
                    transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GetCountAsync(Guid expenseId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM expense_attachments WHERE expense_id = @expenseId",
                new { expenseId });
        }
    }
}
