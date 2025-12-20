using Core.Abstractions;
using Core.Entities.Expense;

namespace Application.Services.Expense
{
    /// <summary>
    /// Wrapper class to make ExpenseClaim participate in the approval workflow.
    /// Implements IApprovableActivity to integrate with the generic approval engine.
    /// </summary>
    public class ExpenseClaimApprovableActivity : IApprovableActivity
    {
        private readonly ExpenseClaim _claim;
        private readonly string? _categoryName;
        private readonly Func<Task> _onApproved;
        private readonly Func<string, Guid, Task> _onRejected;
        private readonly Func<Task> _onCancelled;

        public ExpenseClaimApprovableActivity(
            ExpenseClaim claim,
            string? categoryName,
            Func<Task> onApproved,
            Func<string, Guid, Task> onRejected,
            Func<Task> onCancelled)
        {
            _claim = claim ?? throw new ArgumentNullException(nameof(claim));
            _categoryName = categoryName;
            _onApproved = onApproved ?? throw new ArgumentNullException(nameof(onApproved));
            _onRejected = onRejected ?? throw new ArgumentNullException(nameof(onRejected));
            _onCancelled = onCancelled ?? throw new ArgumentNullException(nameof(onCancelled));
        }

        public string ActivityType => ActivityTypes.Expense;

        public Guid ActivityId => _claim.Id;

        public Guid RequestorId => _claim.EmployeeId;

        public Guid CompanyId => _claim.CompanyId;

        /// <summary>
        /// Provides a display-friendly title for the approval list.
        /// Format: "Expense Claim - ₹5,000 (Travel)"
        /// </summary>
        public string GetDisplayTitle()
        {
            var amount = _claim.Amount.ToString("N2");
            var category = _categoryName ?? "Expense";
            return $"Expense Claim - ₹{amount} ({category})";
        }

        public async Task OnApprovedAsync()
        {
            await _onApproved();
        }

        public async Task OnRejectedAsync(string reason, Guid rejectedBy)
        {
            await _onRejected(reason, rejectedBy);
        }

        public async Task OnCancelledAsync()
        {
            await _onCancelled();
        }

        /// <summary>
        /// Returns context data for evaluating conditional workflow steps.
        /// Example: Skip manager approval for expenses under ₹1000
        /// </summary>
        public Dictionary<string, object> GetConditionContext()
        {
            return new Dictionary<string, object>
            {
                ["amount"] = _claim.Amount,
                ["currency"] = _claim.Currency,
                ["category_id"] = _claim.CategoryId.ToString(),
                ["category_name"] = _categoryName ?? string.Empty,
                ["expense_date"] = _claim.ExpenseDate.ToString("yyyy-MM-dd"),
                ["title"] = _claim.Title
            };
        }
    }
}
