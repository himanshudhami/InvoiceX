using Application.Interfaces;
using Application.Interfaces.Approval;
using Core.Abstractions;
using Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.Approval.Handlers
{
    /// <summary>
    /// Handles asset request status updates when approval workflow completes.
    /// Uses IServiceProvider to lazily resolve IAssetRequestService and break circular dependency.
    /// </summary>
    public class AssetRequestStatusHandler : IActivityStatusHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AssetRequestStatusHandler>? _logger;

        public AssetRequestStatusHandler(
            IServiceProvider serviceProvider,
            ILogger<AssetRequestStatusHandler>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        private IAssetRequestService GetAssetRequestService() => _serviceProvider.GetRequiredService<IAssetRequestService>();

        public string ActivityType => ActivityTypes.AssetRequest;

        public async Task<Result> OnApprovedAsync(Guid activityId, Guid approvedBy)
        {
            _logger?.LogInformation(
                "Asset request {RequestId} approved by {ApprovedBy}",
                activityId, approvedBy);

            return await GetAssetRequestService().UpdateStatusAsync(activityId, "approved");
        }

        public async Task<Result> OnRejectedAsync(Guid activityId, Guid rejectedBy, string reason)
        {
            _logger?.LogInformation(
                "Asset request {RequestId} rejected by {RejectedBy}: {Reason}",
                activityId, rejectedBy, reason);

            return await GetAssetRequestService().UpdateStatusAsync(activityId, "rejected", reason);
        }

        public async Task<Result> OnCancelledAsync(Guid activityId, Guid cancelledBy, string? reason = null)
        {
            _logger?.LogInformation(
                "Asset request {RequestId} cancelled by {CancelledBy}",
                activityId, cancelledBy);

            return await GetAssetRequestService().UpdateStatusAsync(activityId, "cancelled", reason);
        }
    }
}
