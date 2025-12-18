using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for UserCompanyAssignment entities.
    /// Manages the many-to-many relationship between users and companies for Company Admins.
    /// </summary>
    public interface IUserCompanyAssignmentRepository
    {
        /// <summary>
        /// Get assignment by ID
        /// </summary>
        Task<UserCompanyAssignment?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all company assignments for a user
        /// </summary>
        Task<IEnumerable<UserCompanyAssignment>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Get all user assignments for a company
        /// </summary>
        Task<IEnumerable<UserCompanyAssignment>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get a specific user-company assignment
        /// </summary>
        Task<UserCompanyAssignment?> GetByUserAndCompanyAsync(Guid userId, Guid companyId);

        /// <summary>
        /// Get the primary company assignment for a user
        /// </summary>
        Task<UserCompanyAssignment?> GetPrimaryByUserIdAsync(Guid userId);

        /// <summary>
        /// Check if user has access to a specific company
        /// </summary>
        Task<bool> HasAccessToCompanyAsync(Guid userId, Guid companyId);

        /// <summary>
        /// Create a new company assignment for a user
        /// </summary>
        Task<UserCompanyAssignment> CreateAsync(UserCompanyAssignment assignment);

        /// <summary>
        /// Update an existing assignment
        /// </summary>
        Task UpdateAsync(UserCompanyAssignment assignment);

        /// <summary>
        /// Delete an assignment
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Delete all assignments for a user
        /// </summary>
        Task DeleteByUserIdAsync(Guid userId);

        /// <summary>
        /// Set a company as primary for a user (unsets other primaries)
        /// </summary>
        Task SetPrimaryAsync(Guid userId, Guid companyId);
    }
}
