namespace Application.DTOs.Tags
{
    // ==================== Tag DTOs ====================

    public class CreateTagDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string TagGroup { get; set; } = "custom";
        public string? Description { get; set; }
        public Guid? ParentTagId { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public decimal? BudgetAmount { get; set; }
        public string? BudgetPeriod { get; set; }
        public string? BudgetYear { get; set; }
    }

    public class UpdateTagDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? TagGroup { get; set; }
        public string? Description { get; set; }
        public Guid? ParentTagId { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int? SortOrder { get; set; }
        public decimal? BudgetAmount { get; set; }
        public string? BudgetPeriod { get; set; }
        public string? BudgetYear { get; set; }
        public bool? IsActive { get; set; }
    }

    public class TagDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string TagGroup { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentTagId { get; set; }
        public string? ParentTagName { get; set; }
        public string? FullPath { get; set; }
        public int Level { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public decimal? BudgetAmount { get; set; }
        public string? BudgetPeriod { get; set; }
        public string? BudgetYear { get; set; }
        public bool IsActive { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAllocatedAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<TagDto>? Children { get; set; }
    }

    public class TagSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string TagGroup { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? FullPath { get; set; }
    }

    // ==================== Transaction Tag DTOs ====================

    public class ApplyTagDto
    {
        public Guid TagId { get; set; }
        public decimal? AllocatedAmount { get; set; }
        public decimal? AllocationPercentage { get; set; }
        public string AllocationMethod { get; set; } = "full";
    }

    public class ApplyTagsToTransactionDto
    {
        public Guid TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public List<ApplyTagDto> Tags { get; set; } = new();
        public bool ReplaceExisting { get; set; } = false;
    }

    public class TransactionTagDto
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? TagColor { get; set; }
        public string? TagGroup { get; set; }
        public decimal? AllocatedAmount { get; set; }
        public decimal? AllocationPercentage { get; set; }
        public string AllocationMethod { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== Attribution Rule DTOs ====================

    public class CreateAttributionRuleDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public List<string> AppliesTo { get; set; } = new() { "*" };
        public RuleConditionsDto Conditions { get; set; } = new();
        public List<TagAssignmentDto> TagAssignments { get; set; } = new();
        public string AllocationMethod { get; set; } = "single";
        public string? SplitMetric { get; set; }
        public int Priority { get; set; } = 100;
        public bool StopOnMatch { get; set; } = true;
        public bool OverwriteExisting { get; set; } = false;
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }

    public class UpdateAttributionRuleDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RuleType { get; set; }
        public List<string>? AppliesTo { get; set; }
        public RuleConditionsDto? Conditions { get; set; }
        public List<TagAssignmentDto>? TagAssignments { get; set; }
        public string? AllocationMethod { get; set; }
        public string? SplitMetric { get; set; }
        public int? Priority { get; set; }
        public bool? StopOnMatch { get; set; }
        public bool? OverwriteExisting { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AttributionRuleDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public List<string> AppliesTo { get; set; } = new();
        public RuleConditionsDto Conditions { get; set; } = new();
        public List<TagAssignmentDto> TagAssignments { get; set; } = new();
        public string AllocationMethod { get; set; } = string.Empty;
        public string? SplitMetric { get; set; }
        public int Priority { get; set; }
        public bool StopOnMatch { get; set; }
        public bool OverwriteExisting { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public int TimesApplied { get; set; }
        public DateTime? LastAppliedAt { get; set; }
        public decimal TotalAmountTagged { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ==================== Rule Conditions ====================

    public class RuleConditionsDto
    {
        // Vendor-based
        public List<Guid>? VendorIds { get; set; }
        public string? VendorNameContains { get; set; }

        // Customer-based
        public List<Guid>? CustomerIds { get; set; }
        public string? CustomerNameContains { get; set; }
        public string? CustomerType { get; set; }

        // Account-based
        public List<Guid>? AccountIds { get; set; }
        public string? AccountGroup { get; set; }

        // Product-based
        public List<Guid>? ProductIds { get; set; }
        public string? ProductCategory { get; set; }

        // Keyword-based
        public List<string>? DescriptionContains { get; set; }
        public string? MatchMode { get; set; } = "any"; // any, all

        // Amount-based
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        // Employee-based
        public List<Guid>? EmployeeIds { get; set; }
        public string? Department { get; set; }
    }

    public class TagAssignmentDto
    {
        public Guid TagId { get; set; }
        public string? TagName { get; set; }
        public string AllocationMethod { get; set; } = "full";
        public decimal? Value { get; set; } // Percentage or fixed amount
    }

    // ==================== Request/Response ====================

    public class TagsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public string? TagGroup { get; set; }
        public bool? IsActive { get; set; }
        public Guid? ParentTagId { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(TagGroup)) filters["tag_group"] = TagGroup;
            if (IsActive.HasValue) filters["is_active"] = IsActive.Value;
            if (ParentTagId.HasValue) filters["parent_tag_id"] = ParentTagId.Value;
            return filters;
        }
    }

    public class AttributionRulesFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public string? RuleType { get; set; }
        public bool? IsActive { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(RuleType)) filters["rule_type"] = RuleType;
            if (IsActive.HasValue) filters["is_active"] = IsActive.Value;
            return filters;
        }
    }

    // ==================== Auto-Attribution Response ====================

    public class AutoAttributionResult
    {
        public Guid TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public List<AppliedTagResult> AppliedTags { get; set; } = new();
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class AppliedTagResult
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public Guid? RuleId { get; set; }
        public string? RuleName { get; set; }
        public decimal? AllocatedAmount { get; set; }
        public decimal? AllocationPercentage { get; set; }
    }
}
