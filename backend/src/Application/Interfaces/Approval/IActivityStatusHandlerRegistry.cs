namespace Application.Interfaces.Approval
{
    /// <summary>
    /// Registry for activity status handlers.
    /// Resolves the correct handler based on activity type.
    /// </summary>
    public interface IActivityStatusHandlerRegistry
    {
        /// <summary>
        /// Gets the handler for a specific activity type
        /// </summary>
        /// <param name="activityType">The activity type (e.g., "leave", "asset_request")</param>
        /// <returns>The handler, or null if no handler is registered for this type</returns>
        IActivityStatusHandler? GetHandler(string activityType);

        /// <summary>
        /// Checks if a handler exists for the given activity type
        /// </summary>
        bool HasHandler(string activityType);
    }
}
