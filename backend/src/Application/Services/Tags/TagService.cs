using Application.DTOs.Tags;
using Application.Interfaces.Tags;
using Core.Common;
using Core.Entities.Tags;
using Core.Interfaces.Tags;
using System.Text.Json;

namespace Application.Services.Tags
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITransactionTagRepository _transactionTagRepository;
        private readonly IAttributionRuleRepository _ruleRepository;

        public TagService(
            ITagRepository tagRepository,
            ITransactionTagRepository transactionTagRepository,
            IAttributionRuleRepository ruleRepository)
        {
            _tagRepository = tagRepository;
            _transactionTagRepository = transactionTagRepository;
            _ruleRepository = ruleRepository;
        }

        // ==================== Tag CRUD ====================

        public async Task<Result<Tag>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Error.Validation("Tag ID cannot be empty");

            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
                return Error.NotFound($"Tag with ID {id} not found");

            return Result<Tag>.Success(tag);
        }

        public async Task<Result<IEnumerable<Tag>>> GetByCompanyIdAsync(Guid companyId)
        {
            var tags = await _tagRepository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<Tag>>.Success(tags);
        }

        public async Task<Result<(IEnumerable<Tag> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var result = await _tagRepository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
            return Result<(IEnumerable<Tag> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<Tag>> CreateAsync(CreateTagDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Error.Validation("Tag name is required");

            if (!dto.CompanyId.HasValue)
                return Error.Validation("Company ID is required");

            // Check for duplicate name
            if (await _tagRepository.NameExistsAsync(dto.CompanyId.Value, dto.Name, dto.ParentTagId))
                return Error.Conflict($"Tag '{dto.Name}' already exists in this location");

            // Check for duplicate code
            if (!string.IsNullOrEmpty(dto.Code) &&
                await _tagRepository.CodeExistsAsync(dto.CompanyId.Value, dto.Code))
                return Error.Conflict($"Tag code '{dto.Code}' already exists");

            // Validate parent exists if specified
            if (dto.ParentTagId.HasValue)
            {
                var parent = await _tagRepository.GetByIdAsync(dto.ParentTagId.Value);
                if (parent == null)
                    return Error.NotFound("Parent tag not found");
                if (parent.CompanyId != dto.CompanyId.Value)
                    return Error.Validation("Parent tag belongs to a different company");
            }

            var tag = new Tag
            {
                CompanyId = dto.CompanyId.Value,
                Name = dto.Name,
                Code = dto.Code,
                TagGroup = dto.TagGroup,
                Description = dto.Description,
                ParentTagId = dto.ParentTagId,
                Color = dto.Color,
                Icon = dto.Icon,
                SortOrder = dto.SortOrder,
                BudgetAmount = dto.BudgetAmount,
                BudgetPeriod = dto.BudgetPeriod,
                BudgetYear = dto.BudgetYear
            };

            var created = await _tagRepository.AddAsync(tag);
            return Result<Tag>.Success(created);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateTagDto dto)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
                return Error.NotFound($"Tag with ID {id} not found");

            // Check for duplicate name if changing
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != tag.Name)
            {
                var parentId = dto.ParentTagId ?? tag.ParentTagId;
                if (await _tagRepository.NameExistsAsync(tag.CompanyId, dto.Name, parentId, id))
                    return Error.Conflict($"Tag '{dto.Name}' already exists in this location");
            }

            // Check for duplicate code if changing
            if (!string.IsNullOrEmpty(dto.Code) && dto.Code != tag.Code)
            {
                if (await _tagRepository.CodeExistsAsync(tag.CompanyId, dto.Code, id))
                    return Error.Conflict($"Tag code '{dto.Code}' already exists");
            }

            // Prevent circular hierarchy
            if (dto.ParentTagId.HasValue && dto.ParentTagId.Value != tag.ParentTagId)
            {
                if (dto.ParentTagId.Value == id)
                    return Error.Validation("A tag cannot be its own parent");
                // TODO: Check for deeper circular references
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Name)) tag.Name = dto.Name;
            if (dto.Code != null) tag.Code = dto.Code;
            if (!string.IsNullOrWhiteSpace(dto.TagGroup)) tag.TagGroup = dto.TagGroup;
            if (dto.Description != null) tag.Description = dto.Description;
            if (dto.ParentTagId.HasValue) tag.ParentTagId = dto.ParentTagId;
            if (dto.Color != null) tag.Color = dto.Color;
            if (dto.Icon != null) tag.Icon = dto.Icon;
            if (dto.SortOrder.HasValue) tag.SortOrder = dto.SortOrder.Value;
            if (dto.BudgetAmount.HasValue) tag.BudgetAmount = dto.BudgetAmount;
            if (dto.BudgetPeriod != null) tag.BudgetPeriod = dto.BudgetPeriod;
            if (dto.BudgetYear != null) tag.BudgetYear = dto.BudgetYear;
            if (dto.IsActive.HasValue) tag.IsActive = dto.IsActive.Value;

            await _tagRepository.UpdateAsync(tag);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
                return Error.NotFound($"Tag with ID {id} not found");

            // Check for children
            if (await _tagRepository.HasChildrenAsync(id))
                return Error.Conflict("Cannot delete tag with child tags. Delete children first.");

            // Check for transactions
            if (await _tagRepository.HasTransactionsAsync(id))
                return Error.Conflict("Cannot delete tag that is applied to transactions. Remove tag from transactions first or deactivate instead.");

            await _tagRepository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Tag Queries ====================

        public async Task<Result<IEnumerable<Tag>>> GetByGroupAsync(Guid companyId, string tagGroup)
        {
            var tags = await _tagRepository.GetActiveByGroupAsync(companyId, tagGroup);
            return Result<IEnumerable<Tag>>.Success(tags);
        }

        public async Task<Result<IEnumerable<Tag>>> GetTagHierarchyAsync(Guid companyId, string? tagGroup = null)
        {
            var tags = await _tagRepository.GetTagHierarchyAsync(companyId, tagGroup);
            return Result<IEnumerable<Tag>>.Success(tags);
        }

        public async Task<Result<IEnumerable<TagSummaryDto>>> GetTagSummariesAsync(Guid companyId)
        {
            var tags = await _tagRepository.GetByCompanyIdAsync(companyId);
            var summaries = tags.Where(t => t.IsActive).Select(t => new TagSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                TagGroup = t.TagGroup,
                Color = t.Color,
                FullPath = t.FullPath
            });
            return Result<IEnumerable<TagSummaryDto>>.Success(summaries);
        }

        // ==================== Transaction Tagging ====================

        public async Task<Result<IEnumerable<TransactionTagDto>>> GetTransactionTagsAsync(Guid transactionId, string transactionType)
        {
            var tags = await _transactionTagRepository.GetByTransactionAsync(transactionId, transactionType);
            var dtos = tags.Select(tt => new TransactionTagDto
            {
                Id = tt.Id,
                TransactionId = tt.TransactionId,
                TransactionType = tt.TransactionType,
                TagId = tt.TagId,
                TagName = tt.Tag?.Name ?? string.Empty,
                TagColor = tt.Tag?.Color,
                TagGroup = tt.Tag?.TagGroup,
                AllocatedAmount = tt.AllocatedAmount,
                AllocationPercentage = tt.AllocationPercentage,
                AllocationMethod = tt.AllocationMethod,
                Source = tt.Source,
                ConfidenceScore = tt.ConfidenceScore,
                CreatedAt = tt.CreatedAt
            });
            return Result<IEnumerable<TransactionTagDto>>.Success(dtos);
        }

        public async Task<Result> ApplyTagsAsync(ApplyTagsToTransactionDto dto, Guid? userId = null)
        {
            if (dto.Tags == null || !dto.Tags.Any())
                return Error.Validation("At least one tag is required");

            var transactionTags = new List<TransactionTag>();
            foreach (var tagDto in dto.Tags)
            {
                var tag = await _tagRepository.GetByIdAsync(tagDto.TagId);
                if (tag == null)
                    return Error.NotFound($"Tag {tagDto.TagId} not found");

                transactionTags.Add(new TransactionTag
                {
                    TransactionId = dto.TransactionId,
                    TransactionType = dto.TransactionType,
                    TagId = tagDto.TagId,
                    AllocatedAmount = tagDto.AllocatedAmount,
                    AllocationPercentage = tagDto.AllocationPercentage,
                    AllocationMethod = tagDto.AllocationMethod,
                    Source = "manual",
                    CreatedBy = userId
                });
            }

            if (dto.ReplaceExisting)
            {
                await _transactionTagRepository.ReplaceTransactionTagsAsync(
                    dto.TransactionId, dto.TransactionType, transactionTags);
            }
            else
            {
                foreach (var tt in transactionTags)
                {
                    var exists = await _transactionTagRepository.ExistsAsync(
                        tt.TransactionId, tt.TransactionType, tt.TagId);
                    if (!exists)
                    {
                        await _transactionTagRepository.AddAsync(tt);
                    }
                }
            }

            return Result.Success();
        }

        public async Task<Result> RemoveTagAsync(Guid transactionId, string transactionType, Guid tagId)
        {
            await _transactionTagRepository.RemoveTagFromTransactionAsync(transactionId, transactionType, tagId);
            return Result.Success();
        }

        public async Task<Result> RemoveAllTagsAsync(Guid transactionId, string transactionType)
        {
            await _transactionTagRepository.RemoveAllFromTransactionAsync(transactionId, transactionType);
            return Result.Success();
        }

        // ==================== Auto-Attribution ====================

        public async Task<Result<AutoAttributionResult>> AutoAttributeAsync(
            Guid transactionId,
            string transactionType,
            decimal amount,
            Guid companyId,
            Guid? vendorId = null,
            Guid? customerId = null,
            Guid? accountId = null,
            string? description = null)
        {
            var result = new AutoAttributionResult
            {
                TransactionId = transactionId,
                TransactionType = transactionType,
                AppliedTags = new List<AppliedTagResult>()
            };

            // Get applicable rules
            var rules = await _ruleRepository.GetRulesForTransactionTypeAsync(companyId, transactionType);

            foreach (var rule in rules.OrderBy(r => r.Priority))
            {
                var matched = await EvaluateRule(rule, vendorId, customerId, accountId, description, amount);
                if (!matched) continue;

                // Parse tag assignments
                var assignments = JsonSerializer.Deserialize<List<TagAssignmentDto>>(rule.TagAssignments)
                    ?? new List<TagAssignmentDto>();

                foreach (var assignment in assignments)
                {
                    var tag = await _tagRepository.GetByIdAsync(assignment.TagId);
                    if (tag == null || !tag.IsActive) continue;

                    // Calculate allocation
                    decimal? allocatedAmount = null;
                    decimal? allocationPercentage = null;

                    switch (assignment.AllocationMethod.ToLower())
                    {
                        case "full":
                            allocatedAmount = amount;
                            break;
                        case "percentage":
                            allocationPercentage = assignment.Value;
                            allocatedAmount = amount * (assignment.Value ?? 100) / 100;
                            break;
                        case "amount":
                            allocatedAmount = assignment.Value;
                            break;
                    }

                    // Check if already tagged
                    var exists = await _transactionTagRepository.ExistsAsync(
                        transactionId, transactionType, assignment.TagId);

                    if (!exists || rule.OverwriteExisting)
                    {
                        if (exists)
                        {
                            await _transactionTagRepository.RemoveTagFromTransactionAsync(
                                transactionId, transactionType, assignment.TagId);
                        }

                        await _transactionTagRepository.AddAsync(new TransactionTag
                        {
                            TransactionId = transactionId,
                            TransactionType = transactionType,
                            TagId = assignment.TagId,
                            AllocatedAmount = allocatedAmount,
                            AllocationPercentage = allocationPercentage,
                            AllocationMethod = assignment.AllocationMethod,
                            Source = "rule",
                            AttributionRuleId = rule.Id
                        });

                        result.AppliedTags.Add(new AppliedTagResult
                        {
                            TagId = assignment.TagId,
                            TagName = tag.Name,
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            AllocatedAmount = allocatedAmount,
                            AllocationPercentage = allocationPercentage
                        });

                        // Update rule statistics
                        await _ruleRepository.UpdateRuleStatisticsAsync(rule.Id, allocatedAmount ?? 0);
                    }
                }

                if (rule.StopOnMatch) break;
            }

            result.Success = true;
            result.Message = result.AppliedTags.Any()
                ? $"Applied {result.AppliedTags.Count} tag(s)"
                : "No matching rules found";

            return Result<AutoAttributionResult>.Success(result);
        }

        private async Task<bool> EvaluateRule(
            AttributionRule rule,
            Guid? vendorId,
            Guid? customerId,
            Guid? accountId,
            string? description,
            decimal amount)
        {
            var conditions = JsonSerializer.Deserialize<RuleConditionsDto>(rule.Conditions)
                ?? new RuleConditionsDto();

            switch (rule.RuleType.ToLower())
            {
                case "vendor":
                    if (!vendorId.HasValue) return false;
                    if (conditions.VendorIds?.Any() == true && !conditions.VendorIds.Contains(vendorId.Value))
                        return false;
                    // TODO: Check vendor name contains
                    return true;

                case "customer":
                    if (!customerId.HasValue) return false;
                    if (conditions.CustomerIds?.Any() == true && !conditions.CustomerIds.Contains(customerId.Value))
                        return false;
                    return true;

                case "account":
                    if (!accountId.HasValue) return false;
                    if (conditions.AccountIds?.Any() == true && !conditions.AccountIds.Contains(accountId.Value))
                        return false;
                    return true;

                case "keyword":
                    if (string.IsNullOrEmpty(description)) return false;
                    if (conditions.DescriptionContains?.Any() != true) return false;
                    var descLower = description.ToLower();
                    var matchMode = conditions.MatchMode?.ToLower() ?? "any";
                    if (matchMode == "all")
                        return conditions.DescriptionContains.All(k => descLower.Contains(k.ToLower()));
                    return conditions.DescriptionContains.Any(k => descLower.Contains(k.ToLower()));

                case "amount_range":
                    if (conditions.MinAmount.HasValue && amount < conditions.MinAmount.Value) return false;
                    if (conditions.MaxAmount.HasValue && amount > conditions.MaxAmount.Value) return false;
                    return true;

                default:
                    return false;
            }
        }

        // ==================== Utilities ====================

        public async Task<Result> SeedDefaultTagsAsync(Guid companyId, Guid? userId = null)
        {
            await _tagRepository.SeedDefaultTagsAsync(companyId, userId);
            return Result.Success();
        }
    }
}
