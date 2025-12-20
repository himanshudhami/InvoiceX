using Application.Interfaces.Approval;
using Application.Interfaces.Expense;
using Core.Abstractions;
using Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.Approval.Handlers
{
    /// <summary>
    /// Handles expense claim status updates when approval workflow completes.
    /// Uses IServiceProvider to lazily resolve IExpenseClaimService and break circular dependency.
    /// </summary>
    public class ExpenseStatusHandler : IActivityStatusHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpenseStatusHandler>? _logger;

        public ExpenseStatusHandler(
            IServiceProvider serviceProvider,
            ILogger<ExpenseStatusHandler>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        private IExpenseClaimService GetExpenseService() => _serviceProvider.GetRequiredService<IExpenseClaimService>();

        public string ActivityType => ActivityTypes.Expense;

        public async Task<Result> OnApprovedAsync(Guid activityId, Guid approvedBy)
        {
            _logger?.LogInformation(
                "Expense claim {ExpenseId} approved by {ApprovedBy}",
                activityId, approvedBy);

            return await GetExpenseService().UpdateStatusFromWorkflowAsync(activityId, "approved", approvedBy);
        }

        public async Task<Result> OnRejectedAsync(Guid activityId, Guid rejectedBy, string reason)
        {
            _logger?.LogInformation(
                "Expense claim {ExpenseId} rejected by {RejectedBy}: {Reason}",
                activityId, rejectedBy, reason);

            return await GetExpenseService().UpdateStatusFromWorkflowAsync(activityId, "rejected", rejectedBy, reason);
        }

        public async Task<Result> OnCancelledAsync(Guid activityId, Guid cancelledBy, string? reason = null)
        {
            _logger?.LogInformation(
                "Expense claim {ExpenseId} cancelled by {CancelledBy}",
                activityId, cancelledBy);

            return await GetExpenseService().UpdateStatusFromWorkflowAsync(activityId, "cancelled", cancelledBy, reason);
        }
    }
}
