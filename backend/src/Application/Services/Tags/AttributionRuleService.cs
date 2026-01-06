using Application.DTOs.Tags;
using Application.Interfaces.Tags;
using Core.Common;
using Core.Entities.Tags;
using Core.Interfaces.Tags;
using System.Text.Json;

namespace Application.Services.Tags
{
    public class AttributionRuleService : IAttributionRuleService
    {
        private readonly IAttributionRuleRepository _ruleRepository;
        private readonly ITagRepository _tagRepository;

        public AttributionRuleService(
            IAttributionRuleRepository ruleRepository,
            ITagRepository tagRepository)
        {
            _ruleRepository = ruleRepository;
            _tagRepository = tagRepository;
        }

        // ==================== Rule CRUD ====================

        public async Task<Result<AttributionRule>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Error.Validation("Rule ID cannot be empty");

            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
                return Error.NotFound($"Attribution rule with ID {id} not found");

            return Result<AttributionRule>.Success(rule);
        }

        public async Task<Result<IEnumerable<AttributionRule>>> GetByCompanyIdAsync(Guid companyId)
        {
            var rules = await _ruleRepository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<AttributionRule>>.Success(rules);
        }

        public async Task<Result<(IEnumerable<AttributionRule> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var result = await _ruleRepository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
            return Result<(IEnumerable<AttributionRule> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<AttributionRule>> CreateAsync(CreateAttributionRuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Error.Validation("Rule name is required");

            if (!dto.CompanyId.HasValue)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(dto.RuleType))
                return Error.Validation("Rule type is required");

            var validRuleTypes = new[] { "vendor", "customer", "account", "product", "keyword", "amount_range", "employee", "composite" };
            if (!validRuleTypes.Contains(dto.RuleType.ToLower()))
                return Error.Validation($"Invalid rule type. Must be one of: {string.Join(", ", validRuleTypes)}");

            // Check for duplicate name
            if (await _ruleRepository.NameExistsAsync(dto.CompanyId.Value, dto.Name))
                return Error.Conflict($"Rule '{dto.Name}' already exists");

            // Validate tag assignments
            if (dto.TagAssignments == null || !dto.TagAssignments.Any())
                return Error.Validation("At least one tag assignment is required");

            foreach (var assignment in dto.TagAssignments)
            {
                var tag = await _tagRepository.GetByIdAsync(assignment.TagId);
                if (tag == null)
                    return Error.NotFound($"Tag {assignment.TagId} not found");
                if (tag.CompanyId != dto.CompanyId.Value)
                    return Error.Validation($"Tag {assignment.TagId} belongs to a different company");
            }

            var rule = new AttributionRule
            {
                CompanyId = dto.CompanyId.Value,
                Name = dto.Name,
                Description = dto.Description,
                RuleType = dto.RuleType.ToLower(),
                AppliesTo = JsonSerializer.Serialize(dto.AppliesTo),
                Conditions = JsonSerializer.Serialize(dto.Conditions),
                TagAssignments = JsonSerializer.Serialize(dto.TagAssignments),
                AllocationMethod = dto.AllocationMethod,
                SplitMetric = dto.SplitMetric,
                Priority = dto.Priority,
                StopOnMatch = dto.StopOnMatch,
                OverwriteExisting = dto.OverwriteExisting,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo
            };

            var created = await _ruleRepository.AddAsync(rule);
            return Result<AttributionRule>.Success(created);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateAttributionRuleDto dto)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
                return Error.NotFound($"Attribution rule with ID {id} not found");

            // Check for duplicate name if changing
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != rule.Name)
            {
                if (await _ruleRepository.NameExistsAsync(rule.CompanyId, dto.Name, id))
                    return Error.Conflict($"Rule '{dto.Name}' already exists");
            }

            // Validate tag assignments if changing
            if (dto.TagAssignments != null)
            {
                foreach (var assignment in dto.TagAssignments)
                {
                    var tag = await _tagRepository.GetByIdAsync(assignment.TagId);
                    if (tag == null)
                        return Error.NotFound($"Tag {assignment.TagId} not found");
                    if (tag.CompanyId != rule.CompanyId)
                        return Error.Validation($"Tag {assignment.TagId} belongs to a different company");
                }
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Name)) rule.Name = dto.Name;
            if (dto.Description != null) rule.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.RuleType)) rule.RuleType = dto.RuleType.ToLower();
            if (dto.AppliesTo != null) rule.AppliesTo = JsonSerializer.Serialize(dto.AppliesTo);
            if (dto.Conditions != null) rule.Conditions = JsonSerializer.Serialize(dto.Conditions);
            if (dto.TagAssignments != null) rule.TagAssignments = JsonSerializer.Serialize(dto.TagAssignments);
            if (!string.IsNullOrWhiteSpace(dto.AllocationMethod)) rule.AllocationMethod = dto.AllocationMethod;
            if (dto.SplitMetric != null) rule.SplitMetric = dto.SplitMetric;
            if (dto.Priority.HasValue) rule.Priority = dto.Priority.Value;
            if (dto.StopOnMatch.HasValue) rule.StopOnMatch = dto.StopOnMatch.Value;
            if (dto.OverwriteExisting.HasValue) rule.OverwriteExisting = dto.OverwriteExisting.Value;
            if (dto.EffectiveFrom.HasValue) rule.EffectiveFrom = dto.EffectiveFrom;
            if (dto.EffectiveTo.HasValue) rule.EffectiveTo = dto.EffectiveTo;
            if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;

            await _ruleRepository.UpdateAsync(rule);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
                return Error.NotFound($"Attribution rule with ID {id} not found");

            await _ruleRepository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Rule Queries ====================

        public async Task<Result<IEnumerable<AttributionRule>>> GetActiveRulesAsync(Guid companyId)
        {
            var rules = await _ruleRepository.GetActiveRulesAsync(companyId);
            return Result<IEnumerable<AttributionRule>>.Success(rules);
        }

        public async Task<Result<IEnumerable<AttributionRule>>> GetRulesForTransactionTypeAsync(
            Guid companyId, string transactionType)
        {
            var rules = await _ruleRepository.GetRulesForTransactionTypeAsync(companyId, transactionType);
            return Result<IEnumerable<AttributionRule>>.Success(rules);
        }

        // ==================== Rule Testing ====================

        public async Task<Result<AutoAttributionResult>> TestRuleAsync(
            Guid ruleId, Guid transactionId, string transactionType)
        {
            // This would simulate running a rule against a transaction
            // without actually applying the tags
            var result = new AutoAttributionResult
            {
                TransactionId = transactionId,
                TransactionType = transactionType,
                AppliedTags = new List<AppliedTagResult>(),
                Success = true,
                Message = "Rule test - no tags actually applied"
            };

            // TODO: Implement full test logic
            return Result<AutoAttributionResult>.Success(result);
        }

        // ==================== Rule Statistics ====================

        public async Task<Result<IEnumerable<RulePerformanceSummary>>> GetRulePerformanceAsync(Guid companyId)
        {
            var performance = await _ruleRepository.GetRulePerformanceSummaryAsync(companyId);
            return Result<IEnumerable<RulePerformanceSummary>>.Success(performance);
        }

        // ==================== Bulk Operations ====================

        public async Task<Result> ReorderPrioritiesAsync(
            Guid companyId, IEnumerable<(Guid RuleId, int NewPriority)> priorities)
        {
            await _ruleRepository.ReorderPrioritiesAsync(companyId, priorities);
            return Result.Success();
        }
    }
}
