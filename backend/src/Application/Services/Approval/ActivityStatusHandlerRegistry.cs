using Application.Interfaces.Approval;
using Microsoft.Extensions.Logging;

namespace Application.Services.Approval
{
    /// <summary>
    /// Registry that holds all activity status handlers and resolves them by activity type.
    /// Handlers are registered via dependency injection.
    /// </summary>
    public class ActivityStatusHandlerRegistry : IActivityStatusHandlerRegistry
    {
        private readonly Dictionary<string, IActivityStatusHandler> _handlers;
        private readonly ILogger<ActivityStatusHandlerRegistry>? _logger;

        public ActivityStatusHandlerRegistry(
            IEnumerable<IActivityStatusHandler> handlers,
            ILogger<ActivityStatusHandlerRegistry>? logger = null)
        {
            _logger = logger;
            _handlers = new Dictionary<string, IActivityStatusHandler>(StringComparer.OrdinalIgnoreCase);

            foreach (var handler in handlers)
            {
                if (_handlers.ContainsKey(handler.ActivityType))
                {
                    _logger?.LogWarning(
                        "Duplicate handler registration for activity type '{ActivityType}'. Using the last registered handler.",
                        handler.ActivityType);
                }
                _handlers[handler.ActivityType] = handler;
                _logger?.LogDebug("Registered activity status handler for '{ActivityType}'", handler.ActivityType);
            }

            _logger?.LogInformation(
                "ActivityStatusHandlerRegistry initialized with {Count} handlers: {Types}",
                _handlers.Count,
                string.Join(", ", _handlers.Keys));
        }

        public IActivityStatusHandler? GetHandler(string activityType)
        {
            if (string.IsNullOrEmpty(activityType))
                return null;

            _handlers.TryGetValue(activityType, out var handler);
            return handler;
        }

        public bool HasHandler(string activityType)
        {
            if (string.IsNullOrEmpty(activityType))
                return false;

            return _handlers.ContainsKey(activityType);
        }
    }
}
