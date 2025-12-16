using Application.DTOs.Payroll;
using Core.Common;

namespace Application.Interfaces.Payroll
{
    /// <summary>
    /// Service interface for managing tax declarations with workflow support
    /// </summary>
    public interface ITaxDeclarationService
    {
        /// <summary>
        /// Create a new tax declaration
        /// </summary>
        Task<Result<EmployeeTaxDeclarationDto>> CreateAsync(
            CreateEmployeeTaxDeclarationDto dto,
            string createdBy);

        /// <summary>
        /// Update an existing tax declaration
        /// </summary>
        Task<Result> UpdateAsync(
            Guid id,
            UpdateEmployeeTaxDeclarationDto dto,
            string updatedBy);

        /// <summary>
        /// Submit a declaration for verification
        /// </summary>
        Task<Result> SubmitAsync(Guid id, string submittedBy);

        /// <summary>
        /// Verify a submitted declaration (HR action)
        /// </summary>
        Task<Result> VerifyAsync(Guid id, string verifiedBy);

        /// <summary>
        /// Reject a submitted declaration with reason (HR action)
        /// </summary>
        Task<Result> RejectAsync(Guid id, RejectDeclarationDto rejectDto, string rejectedBy);

        /// <summary>
        /// Revise a rejected declaration and resubmit
        /// </summary>
        Task<Result> ReviseAndResubmitAsync(
            Guid id,
            UpdateEmployeeTaxDeclarationDto dto,
            string submittedBy);

        /// <summary>
        /// Lock a declaration (prevents further changes)
        /// </summary>
        Task<Result> LockAsync(Guid id, string lockedBy);

        /// <summary>
        /// Unlock a declaration (admin action)
        /// </summary>
        Task<Result> UnlockAsync(Guid id, string unlockedBy);

        /// <summary>
        /// Get a tax declaration summary with capped values
        /// </summary>
        Task<Result<TaxDeclarationSummaryDto>> GetSummaryAsync(Guid id);

        /// <summary>
        /// Validate a declaration without saving
        /// </summary>
        Task<Result<TaxDeclarationSummaryDto>> ValidateAsync(
            CreateEmployeeTaxDeclarationDto dto,
            Guid? employeeId = null);

        /// <summary>
        /// Get audit history for a declaration
        /// </summary>
        Task<Result<IEnumerable<DeclarationHistoryDto>>> GetHistoryAsync(Guid declarationId);
    }
}
