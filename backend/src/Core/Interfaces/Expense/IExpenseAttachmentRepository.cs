using Core.Entities.Expense;

namespace Core.Interfaces.Expense
{
    /// <summary>
    /// Repository interface for expense attachment operations.
    /// </summary>
    public interface IExpenseAttachmentRepository
    {
        /// <summary>
        /// Get an attachment by ID.
        /// </summary>
        Task<ExpenseAttachment?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all attachments for an expense claim.
        /// </summary>
        Task<IEnumerable<ExpenseAttachment>> GetByExpenseAsync(Guid expenseId);

        /// <summary>
        /// Add an attachment to an expense claim.
        /// </summary>
        Task<ExpenseAttachment> AddAsync(ExpenseAttachment attachment);

        /// <summary>
        /// Delete an attachment.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Set an attachment as primary (and unset others).
        /// </summary>
        Task SetPrimaryAsync(Guid expenseId, Guid attachmentId);

        /// <summary>
        /// Get the count of attachments for an expense.
        /// </summary>
        Task<int> GetCountAsync(Guid expenseId);
    }
}
