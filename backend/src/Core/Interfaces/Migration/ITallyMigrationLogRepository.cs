using Core.Entities.Migration;

namespace Core.Interfaces.Migration
{
    public interface ITallyMigrationLogRepository
    {
        Task<TallyMigrationLog?> GetByIdAsync(Guid id);
        Task<IEnumerable<TallyMigrationLog>> GetByBatchIdAsync(Guid batchId);
        Task<(IEnumerable<TallyMigrationLog> Items, int TotalCount)> GetPagedByBatchIdAsync(
            Guid batchId,
            int pageNumber,
            int pageSize,
            string? recordType = null,
            string? status = null,
            string? sortBy = null,
            bool sortDescending = false);
        Task<TallyMigrationLog> AddAsync(TallyMigrationLog log);
        Task<IEnumerable<TallyMigrationLog>> BulkAddAsync(IEnumerable<TallyMigrationLog> logs);
        Task UpdateAsync(TallyMigrationLog log);
        Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, Guid? targetId = null);
        Task DeleteByBatchIdAsync(Guid batchId);

        // Query helpers
        Task<TallyMigrationLog?> GetByTallyGuidAsync(Guid batchId, string tallyGuid);
        Task<IEnumerable<TallyMigrationLog>> GetFailedByBatchIdAsync(Guid batchId);
        Task<IEnumerable<TallyMigrationLog>> GetSuspenseByBatchIdAsync(Guid batchId);

        // Statistics
        Task<Dictionary<string, int>> GetCountsByStatusAsync(Guid batchId);
        Task<Dictionary<string, int>> GetCountsByRecordTypeAsync(Guid batchId);
        Task<decimal> GetTotalAmountDifferenceAsync(Guid batchId);
    }
}
