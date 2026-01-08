using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for Unified Party management
    /// </summary>
    public interface IPartyRepository
    {
        // ==================== Basic CRUD ====================

        Task<Party?> GetByIdAsync(Guid id);
        Task<Party?> GetByIdWithProfilesAsync(Guid id);
        Task<IEnumerable<Party>> GetAllAsync();
        Task<Party> AddAsync(Party entity);
        Task UpdateAsync(Party entity);
        Task DeleteAsync(Guid id);

        // ==================== Company-scoped Queries ====================

        Task<IEnumerable<Party>> GetByCompanyIdAsync(Guid companyId, bool? isVendor = null, bool? isCustomer = null, bool? isEmployee = null);

        Task<(IEnumerable<Party> Items, int TotalCount)> GetPagedAsync(
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

        // ==================== Lookup Methods ====================

        Task<Party?> GetByNameAsync(Guid companyId, string name);
        Task<Party?> GetByPartyCodeAsync(Guid companyId, string partyCode);
        Task<Party?> GetByGstinAsync(Guid companyId, string gstin);
        Task<Party?> GetByPanAsync(Guid companyId, string panNumber);

        // ==================== Role-specific Queries ====================

        /// <summary>
        /// Get all vendors (parties with is_vendor = true)
        /// </summary>
        Task<IEnumerable<Party>> GetVendorsAsync(Guid companyId);

        /// <summary>
        /// Get all customers (parties with is_customer = true)
        /// </summary>
        Task<IEnumerable<Party>> GetCustomersAsync(Guid companyId);

        /// <summary>
        /// Get vendors with MSME registration
        /// </summary>
        Task<IEnumerable<Party>> GetMsmeVendorsAsync(Guid companyId);

        /// <summary>
        /// Get vendors where TDS is applicable
        /// </summary>
        Task<IEnumerable<Party>> GetTdsApplicableVendorsAsync(Guid companyId);

        // ==================== Profile Management ====================

        Task<PartyVendorProfile?> GetVendorProfileAsync(Guid partyId);
        Task<PartyCustomerProfile?> GetCustomerProfileAsync(Guid partyId);
        Task AddVendorProfileAsync(PartyVendorProfile profile);
        Task AddCustomerProfileAsync(PartyCustomerProfile profile);
        Task UpdateVendorProfileAsync(PartyVendorProfile profile);
        Task UpdateCustomerProfileAsync(PartyCustomerProfile profile);
        Task DeleteVendorProfileAsync(Guid partyId);
        Task DeleteCustomerProfileAsync(Guid partyId);

        // ==================== Tag Management ====================

        Task<IEnumerable<PartyTag>> GetTagsAsync(Guid partyId);
        Task AddTagAsync(Guid partyId, Guid tagId, string source = "manual", Guid? createdBy = null);
        Task RemoveTagAsync(Guid partyId, Guid tagId);
        Task<IEnumerable<Party>> GetPartiesByTagAsync(Guid companyId, Guid tagId);
        Task<IEnumerable<Party>> GetPartiesByTagGroupAsync(Guid companyId, string tagGroup);

        // ==================== Tally Migration ====================

        Task<Party?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid);
        Task<IEnumerable<Party>> GetByTallyGroupAsync(Guid companyId, string tallyGroupName);

        /// <summary>
        /// Gets a party by their original Tally ledger name (for payment classification)
        /// </summary>
        Task<Party?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName);

        // ==================== Balance/Outstanding ====================

        Task<decimal> GetVendorOutstandingBalanceAsync(Guid partyId);
        Task<decimal> GetCustomerOutstandingBalanceAsync(Guid partyId);

        // ==================== Bulk Operations ====================

        Task<IEnumerable<Party>> BulkAddAsync(IEnumerable<Party> parties);
        Task BulkUpdateRolesAsync(IEnumerable<Guid> partyIds, bool? isVendor = null, bool? isCustomer = null);

        /// <summary>
        /// Get parties by a list of IDs
        /// </summary>
        Task<IEnumerable<Party>> GetByIdsAsync(IEnumerable<Guid> ids);
    }
}
