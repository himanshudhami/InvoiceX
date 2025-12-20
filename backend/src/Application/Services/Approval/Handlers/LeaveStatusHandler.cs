using Application.Interfaces.Approval;
using Application.Interfaces.Leave;
using Core.Abstractions;
using Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.Approval.Handlers
{
    /// <summary>
    /// Handles leave application status updates when approval workflow completes.
    /// Uses IServiceProvider to lazily resolve ILeaveService and break circular dependency.
    /// </summary>
    public class LeaveStatusHandler : IActivityStatusHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveStatusHandler>? _logger;

        public LeaveStatusHandler(
            IServiceProvider serviceProvider,
            ILogger<LeaveStatusHandler>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        private ILeaveService GetLeaveService() => _serviceProvider.GetRequiredService<ILeaveService>();

        public string ActivityType => ActivityTypes.Leave;

        public async Task<Result> OnApprovedAsync(Guid activityId, Guid approvedBy)
        {
            _logger?.LogInformation(
                "Leave application {LeaveId} approved by {ApprovedBy}",
                activityId, approvedBy);

            return await GetLeaveService().UpdateLeaveStatusAsync(activityId, "approved");
        }

        public async Task<Result> OnRejectedAsync(Guid activityId, Guid rejectedBy, string reason)
        {
            _logger?.LogInformation(
                "Leave application {LeaveId} rejected by {RejectedBy}: {Reason}",
                activityId, rejectedBy, reason);

            return await GetLeaveService().UpdateLeaveStatusAsync(activityId, "rejected", reason);
        }

        public async Task<Result> OnCancelledAsync(Guid activityId, Guid cancelledBy, string? reason = null)
        {
            _logger?.LogInformation(
                "Leave application {LeaveId} cancelled by {CancelledBy}",
                activityId, cancelledBy);

            return await GetLeaveService().UpdateLeaveStatusAsync(activityId, "cancelled", reason);
        }
    }
}
