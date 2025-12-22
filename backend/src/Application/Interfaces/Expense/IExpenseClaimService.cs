using Application.DTOs.Expense;
using Core.Common;

namespace Application.Interfaces.Expense
{
    /// <summary>
    /// Service interface for expense claim operations.
    /// </summary>
    public interface IExpenseClaimService
    {
        /// <summary>
        /// Get an expense claim by ID with attachments.
        /// </summary>
        Task<Result<ExpenseClaimDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get expense claims for an employee.
        /// </summary>
        Task<Result<IEnumerable<ExpenseClaimDto>>> GetByEmployeeAsync(Guid employeeId);

        /// <summary>
        /// Get paginated expense claims for a company (Admin view).
        /// </summary>
        Task<Result<(IEnumerable<ExpenseClaimDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            ExpenseClaimFilterRequest request);

        /// <summary>
        /// Get paginated expense claims for an employee (Portal view).
        /// </summary>
        Task<Result<(IEnumerable<ExpenseClaimDto> Items, int TotalCount)>> GetPagedByEmployeeAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? status = null);

        /// <summary>
        /// Get pending approval claims for a manager.
        /// </summary>
        Task<Result<IEnumerable<ExpenseClaimDto>>> GetPendingForManagerAsync(Guid managerId);

        /// <summary>
        /// Create a new expense claim (draft).
        /// </summary>
        Task<Result<ExpenseClaimDto>> CreateAsync(Guid companyId, Guid employeeId, CreateExpenseClaimDto dto);

        /// <summary>
        /// Update a draft expense claim.
        /// </summary>
        Task<Result<ExpenseClaimDto>> UpdateAsync(Guid id, Guid employeeId, UpdateExpenseClaimDto dto);

        /// <summary>
        /// Submit an expense claim for approval.
        /// </summary>
        Task<Result<ExpenseClaimDto>> SubmitAsync(Guid id, Guid employeeId);

        /// <summary>
        /// Cancel an expense claim (only draft or submitted).
        /// </summary>
        Task<Result<bool>> CancelAsync(Guid id, Guid employeeId);

        /// <summary>
        /// Approve an expense claim.
        /// </summary>
        Task<Result<ExpenseClaimDto>> ApproveAsync(Guid id, Guid approverId);

        /// <summary>
        /// Reject an expense claim.
        /// </summary>
        Task<Result<ExpenseClaimDto>> RejectAsync(Guid id, Guid rejecterId, RejectExpenseClaimDto dto);

        /// <summary>
        /// Mark an expense claim as reimbursed.
        /// </summary>
        /// <param name="id">The expense claim ID.</param>
        /// <param name="dto">Reimbursement details including optional proof attachments.</param>
        /// <param name="reimbursedBy">User ID of the admin performing reimbursement.</param>
        Task<Result<ExpenseClaimDto>> ReimburseAsync(Guid id, ReimburseExpenseClaimDto dto, Guid? reimbursedBy = null);

        /// <summary>
        /// Delete a draft expense claim.
        /// </summary>
        Task<Result<bool>> DeleteAsync(Guid id, Guid employeeId);

        /// <summary>
        /// Add an attachment to an expense claim.
        /// </summary>
        Task<Result<ExpenseAttachmentDto>> AddAttachmentAsync(
            Guid expenseId,
            Guid employeeId,
            AddExpenseAttachmentDto dto);

        /// <summary>
        /// Remove an attachment from an expense claim.
        /// </summary>
        Task<Result<bool>> RemoveAttachmentAsync(Guid expenseId, Guid attachmentId, Guid employeeId);

        /// <summary>
        /// Get attachments for an expense claim.
        /// </summary>
        Task<Result<IEnumerable<ExpenseAttachmentDto>>> GetAttachmentsAsync(Guid expenseId);

        /// <summary>
        /// Get expense summary for a company.
        /// </summary>
        Task<Result<ExpenseSummaryDto>> GetSummaryAsync(
            Guid companyId,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Update expense claim status from approval workflow callback.
        /// Called by ExpenseStatusHandler when workflow completes.
        /// </summary>
        Task<Result> UpdateStatusFromWorkflowAsync(
            Guid expenseId,
            string status,
            Guid actionBy,
            string? reason = null);
    }
}
