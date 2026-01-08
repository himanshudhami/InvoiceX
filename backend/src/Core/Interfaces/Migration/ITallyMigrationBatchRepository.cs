using Core.Entities.Migration;

namespace Core.Interfaces.Migration
{
    public interface ITallyMigrationBatchRepository
    {
        Task<TallyMigrationBatch?> GetByIdAsync(Guid id);
        Task<TallyMigrationBatch?> GetByBatchNumberAsync(Guid companyId, string batchNumber);
        Task<IEnumerable<TallyMigrationBatch>> GetByCompanyIdAsync(Guid companyId);
        Task<(IEnumerable<TallyMigrationBatch> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? status = null,
            string? sortBy = null,
            bool sortDescending = false);
        Task<TallyMigrationBatch> AddAsync(TallyMigrationBatch batch);
        Task UpdateAsync(TallyMigrationBatch batch);
        Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null);
        Task UpdateCountsAsync(Guid id, TallyMigrationBatch batch);
        Task DeleteAsync(Guid id);

        // Progress tracking
        Task<TallyMigrationBatch?> GetLatestByCompanyIdAsync(Guid companyId);
        Task<TallyMigrationBatch?> GetLatestCompletedAsync(Guid companyId);

        // For incremental sync
        Task<DateOnly?> GetLastImportDateAsync(Guid companyId);

        // Generate unique batch number
        Task<string> GenerateBatchNumberAsync(Guid companyId);
    }
}
