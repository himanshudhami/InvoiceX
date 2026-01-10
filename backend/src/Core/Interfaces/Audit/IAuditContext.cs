namespace Core.Interfaces.Audit
{
    /// <summary>
    /// Provides audit context for the current request.
    /// Captures actor information and request metadata for audit trail entries.
    /// </summary>
    public interface IAuditContext
    {
        Guid? ActorId { get; }
        string? ActorName { get; }
        string? ActorEmail { get; }
        string? ActorIp { get; }
        string? UserAgent { get; }
        string? CorrelationId { get; }
        string? RequestPath { get; }
        string? RequestMethod { get; }
        bool HasActor { get; }
    }
}
