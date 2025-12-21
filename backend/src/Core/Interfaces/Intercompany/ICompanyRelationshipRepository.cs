using Core.Entities.Intercompany;

namespace Core.Interfaces.Intercompany
{
    /// <summary>
    /// Repository interface for company relationships
    /// </summary>
    public interface ICompanyRelationshipRepository
    {
        Task<CompanyRelationship?> GetByIdAsync(Guid id);
        Task<IEnumerable<CompanyRelationship>> GetAllAsync();
        Task<(IEnumerable<CompanyRelationship> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<CompanyRelationship> AddAsync(CompanyRelationship entity);
        Task UpdateAsync(CompanyRelationship entity);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Get all subsidiaries of a parent company
        /// </summary>
        Task<IEnumerable<CompanyRelationship>> GetSubsidiariesAsync(Guid parentCompanyId);

        /// <summary>
        /// Get parent company of a subsidiary
        /// </summary>
        Task<CompanyRelationship?> GetParentAsync(Guid childCompanyId);

        /// <summary>
        /// Get all active relationships for a company (as parent or child)
        /// </summary>
        Task<IEnumerable<CompanyRelationship>> GetRelationshipsForCompanyAsync(Guid companyId);

        /// <summary>
        /// Check if two companies are related (in same group)
        /// </summary>
        Task<bool> AreCompaniesRelatedAsync(Guid companyId1, Guid companyId2);

        /// <summary>
        /// Get all companies in the same group as the given company
        /// </summary>
        Task<IEnumerable<Guid>> GetGroupCompanyIdsAsync(Guid companyId);
    }
}
