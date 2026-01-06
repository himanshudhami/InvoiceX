using Application.DTOs.Tags;
using Core.Common;
using Core.Entities.Tags;
using Core.Interfaces.Tags;

namespace Application.Interfaces.Tags
{
    public interface ITagService
    {
        // ==================== Tag CRUD ====================
        Task<Result<Tag>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<Tag>>> GetByCompanyIdAsync(Guid companyId);
        Task<Result<(IEnumerable<Tag> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<Tag>> CreateAsync(CreateTagDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateTagDto dto);
        Task<Result> DeleteAsync(Guid id);

        // ==================== Tag Queries ====================
        Task<Result<IEnumerable<Tag>>> GetByGroupAsync(Guid companyId, string tagGroup);
        Task<Result<IEnumerable<Tag>>> GetTagHierarchyAsync(Guid companyId, string? tagGroup = null);
        Task<Result<IEnumerable<TagSummaryDto>>> GetTagSummariesAsync(Guid companyId);

        // ==================== Transaction Tagging ====================
        Task<Result<IEnumerable<TransactionTagDto>>> GetTransactionTagsAsync(Guid transactionId, string transactionType);
        Task<Result> ApplyTagsAsync(ApplyTagsToTransactionDto dto, Guid? userId = null);
        Task<Result> RemoveTagAsync(Guid transactionId, string transactionType, Guid tagId);
        Task<Result> RemoveAllTagsAsync(Guid transactionId, string transactionType);

        // ==================== Auto-Attribution ====================
        Task<Result<AutoAttributionResult>> AutoAttributeAsync(
            Guid transactionId,
            string transactionType,
            decimal amount,
            Guid companyId,
            Guid? vendorId = null,
            Guid? customerId = null,
            Guid? accountId = null,
            string? description = null);

        // ==================== Utilities ====================
        Task<Result> SeedDefaultTagsAsync(Guid companyId, Guid? userId = null);
    }

    public interface IAttributionRuleService
    {
        // ==================== Rule CRUD ====================
        Task<Result<AttributionRule>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<AttributionRule>>> GetByCompanyIdAsync(Guid companyId);
        Task<Result<(IEnumerable<AttributionRule> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<AttributionRule>> CreateAsync(CreateAttributionRuleDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateAttributionRuleDto dto);
        Task<Result> DeleteAsync(Guid id);

        // ==================== Rule Queries ====================
        Task<Result<IEnumerable<AttributionRule>>> GetActiveRulesAsync(Guid companyId);
        Task<Result<IEnumerable<AttributionRule>>> GetRulesForTransactionTypeAsync(Guid companyId, string transactionType);

        // ==================== Rule Testing ====================
        Task<Result<AutoAttributionResult>> TestRuleAsync(Guid ruleId, Guid transactionId, string transactionType);

        // ==================== Rule Statistics ====================
        Task<Result<IEnumerable<RulePerformanceSummary>>> GetRulePerformanceAsync(Guid companyId);

        // ==================== Bulk Operations ====================
        Task<Result> ReorderPrioritiesAsync(Guid companyId, IEnumerable<(Guid RuleId, int NewPriority)> priorities);
    }
}
