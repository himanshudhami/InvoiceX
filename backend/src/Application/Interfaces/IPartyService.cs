using Application.DTOs.Party;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Unified Party Management
    /// </summary>
    public interface IPartyService
    {
        // ==================== Basic CRUD ====================

        Task<Result<PartyDto>> GetByIdAsync(Guid id);
        Task<Result<PartyDto>> GetByIdWithProfilesAsync(Guid id);
        Task<Result<IEnumerable<PartyListDto>>> GetAllAsync();
        Task<Result<PartyDto>> CreateAsync(CreatePartyDto dto);
        Task<Result> UpdateAsync(Guid id, UpdatePartyDto dto);
        Task<Result> DeleteAsync(Guid id);

        // ==================== Company-scoped Queries ====================

        Task<Result<IEnumerable<PartyListDto>>> GetByCompanyIdAsync(
            Guid companyId,
            bool? isVendor = null,
            bool? isCustomer = null,
            bool? isEmployee = null);

        Task<Result<(IEnumerable<PartyListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            bool? isVendor = null,
            bool? isCustomer = null,
            bool? isEmployee = null,
            bool? isActive = null,
            Dictionary<string, object>? filters = null);

        // ==================== Role-specific Queries ====================

        Task<Result<IEnumerable<PartyListDto>>> GetVendorsAsync(Guid companyId);
        Task<Result<IEnumerable<PartyListDto>>> GetCustomersAsync(Guid companyId);
        Task<Result<IEnumerable<PartyListDto>>> GetMsmeVendorsAsync(Guid companyId);
        Task<Result<IEnumerable<PartyListDto>>> GetTdsApplicableVendorsAsync(Guid companyId);

        // ==================== Profile Management ====================

        Task<Result<PartyVendorProfileDto>> GetVendorProfileAsync(Guid partyId);
        Task<Result<PartyCustomerProfileDto>> GetCustomerProfileAsync(Guid partyId);
        Task<Result> UpdateVendorProfileAsync(Guid partyId, UpdatePartyVendorProfileDto dto);
        Task<Result> UpdateCustomerProfileAsync(Guid partyId, UpdatePartyCustomerProfileDto dto);

        // ==================== Tag Management ====================

        Task<Result<IEnumerable<PartyTagDto>>> GetTagsAsync(Guid partyId);
        Task<Result> AddTagAsync(Guid partyId, AddPartyTagDto dto, Guid? userId = null);
        Task<Result> RemoveTagAsync(Guid partyId, Guid tagId);

        // ==================== TDS Detection ====================

        Task<Result<TdsConfigurationDto?>> DetectTdsConfigurationAsync(Guid partyId);

        // ==================== Lookup Methods ====================

        Task<Result<PartyDto?>> GetByGstinAsync(Guid companyId, string gstin);
        Task<Result<PartyDto?>> GetByPanAsync(Guid companyId, string panNumber);
        Task<Result<PartyDto?>> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid);

        // ==================== Existence Check ====================

        Task<Result<bool>> ExistsAsync(Guid id);
    }

    /// <summary>
    /// Service interface for TDS detection using tag-driven approach
    /// Tags drive TDS behavior instead of hard-coded vendor types
    /// </summary>
    public interface ITdsDetectionService
    {
        // ==================== TDS Detection ====================

        /// <summary>
        /// Detect TDS configuration for a party based on tags
        /// </summary>
        Task<Result<TdsDetectionResultDto>> DetectTdsForPartyAsync(Guid partyId, decimal? paymentAmount = null);

        /// <summary>
        /// Detect TDS for a party with company validation
        /// </summary>
        Task<Result<TdsDetectionResultDto>> DetectTdsForPartyByIdAsync(Guid companyId, Guid partyId, decimal? paymentAmount = null);

        // ==================== TDS Tag Rule Management ====================

        /// <summary>
        /// Get all active TDS tag rules for a company
        /// </summary>
        Task<Result<IEnumerable<TdsTagRuleDto>>> GetTdsTagRulesAsync(Guid companyId);

        /// <summary>
        /// Get TDS tag rules with pagination
        /// </summary>
        Task<Result<(IEnumerable<TdsTagRuleDto> Items, int TotalCount)>> GetTdsTagRulesPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? tdsSection = null,
            bool? isActive = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Get TDS tag rule by ID
        /// </summary>
        Task<Result<TdsTagRuleDto>> GetTdsTagRuleByIdAsync(Guid ruleId);

        /// <summary>
        /// Get TDS tag rule by tag ID
        /// </summary>
        Task<Result<TdsTagRuleDto?>> GetTdsTagRuleByTagIdAsync(Guid tagId);

        /// <summary>
        /// Create a new TDS tag rule
        /// </summary>
        Task<Result<TdsTagRuleDto>> CreateTdsTagRuleAsync(Guid companyId, CreateTdsTagRuleDto dto, Guid? userId = null);

        /// <summary>
        /// Update an existing TDS tag rule
        /// </summary>
        Task<Result> UpdateTdsTagRuleAsync(Guid ruleId, UpdateTdsTagRuleDto dto);

        /// <summary>
        /// Delete a TDS tag rule
        /// </summary>
        Task<Result> DeleteTdsTagRuleAsync(Guid ruleId);

        // ==================== Seeding ====================

        /// <summary>
        /// Seed default TDS tags and rules for a company
        /// </summary>
        Task<Result> SeedTdsSystemAsync(Guid companyId);

        /// <summary>
        /// Check if TDS system has been seeded for a company
        /// </summary>
        Task<bool> HasTdsSystemSeededAsync(Guid companyId);
    }
}
