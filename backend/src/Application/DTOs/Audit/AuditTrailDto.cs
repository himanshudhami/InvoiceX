namespace Application.DTOs.Audit
{
    public class AuditTrailDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string? EntityDisplayName { get; set; }
        public string Operation { get; set; } = string.Empty;
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public string[]? ChangedFields { get; set; }
        public Guid ActorId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorEmail { get; set; }
        public string? ActorIp { get; set; }
        public string? CorrelationId { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditTrailQueryParams
    {
        public Guid CompanyId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? Operation { get; set; }
        public Guid? ActorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }

    public class AuditTrailExportRequest
    {
        public Guid CompanyId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EntityType { get; set; }
    }

    public class AuditTrailSummaryDto
    {
        public int TotalEntries { get; set; }
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class PagedAuditResponse
    {
        public IEnumerable<AuditTrailDto> Items { get; set; } = Enumerable.Empty<AuditTrailDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
