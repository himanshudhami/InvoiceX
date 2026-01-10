using System.Text;
using System.Text.Json;
using Application.DTOs.Audit;
using Core.Entities.Audit;
using Core.Interfaces.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;

namespace WebApi.Controllers.Audit
{
    [ApiController]
    [Route("api/audit")]
    [Authorize]
    [Produces("application/json")]
    public class AuditTrailController : CompanyAuthorizedController
    {
        private readonly IAuditTrailRepository _repository;

        public AuditTrailController(IAuditTrailRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedAuditResponse), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] AuditTrailQueryParams query)
        {
            var (companyId, error) = GetEffectiveCompanyIdWithValidation(query.CompanyId);
            if (error != null) return StatusCode(403, new { error });
            if (!companyId.HasValue) return CompanyIdNotFoundResponse();

            var (items, totalCount) = await _repository.GetPagedAsync(
                companyId.Value, query.PageNumber, query.PageSize,
                query.EntityType, query.EntityId, query.Operation, query.ActorId,
                query.FromDate, query.ToDate, query.Search);

            return Ok(new PagedAuditResponse
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            });
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AuditTrailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entry = await _repository.GetByIdAsync(id);
            if (entry == null) return NotFound(new { error = "Audit entry not found" });
            if (!HasCompanyAccess(entry.CompanyId)) return AccessDeniedDifferentCompanyResponse("Audit entry");

            return Ok(MapToDto(entry));
        }

        [HttpGet("entity/{entityType}/{entityId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<AuditTrailDto>), 200)]
        public async Task<IActionResult> GetEntityHistory(string entityType, Guid entityId)
        {
            var entries = await _repository.GetByEntityAsync(entityType, entityId);
            return Ok(entries.Where(e => HasCompanyAccess(e.CompanyId)).Select(MapToDto));
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(IEnumerable<AuditTrailDto>), 200)]
        public async Task<IActionResult> GetRecent([FromQuery] Guid? companyId, [FromQuery] int limit = 50)
        {
            var (effectiveCompanyId, error) = GetEffectiveCompanyIdWithValidation(companyId);
            if (error != null) return StatusCode(403, new { error });
            if (!effectiveCompanyId.HasValue) return CompanyIdNotFoundResponse();

            var entries = await _repository.GetRecentAsync(effectiveCompanyId.Value, Math.Min(limit, 100));
            return Ok(entries.Select(MapToDto));
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(AuditTrailSummaryDto), 200)]
        public async Task<IActionResult> GetStats(
            [FromQuery] Guid? companyId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var (effectiveCompanyId, error) = GetEffectiveCompanyIdWithValidation(companyId);
            if (error != null) return StatusCode(403, new { error });
            if (!effectiveCompanyId.HasValue) return CompanyIdNotFoundResponse();

            var counts = await _repository.GetOperationCountsAsync(effectiveCompanyId.Value, fromDate, toDate);

            return Ok(new AuditTrailSummaryDto
            {
                TotalEntries = counts.Values.Sum(),
                CreateCount = counts.GetValueOrDefault(AuditOperations.Create, 0),
                UpdateCount = counts.GetValueOrDefault(AuditOperations.Update, 0),
                DeleteCount = counts.GetValueOrDefault(AuditOperations.Delete, 0)
            });
        }

        [HttpGet("export")]
        [Produces("text/csv")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportCsv([FromQuery] AuditTrailExportRequest request)
        {
            var (companyId, error) = GetEffectiveCompanyIdWithValidation(request.CompanyId);
            if (error != null) return StatusCode(403, new { error });
            if (!companyId.HasValue) return CompanyIdNotFoundResponse();

            var entries = await _repository.GetByDateRangeAsync(
                companyId.Value, request.FromDate, request.ToDate, request.EntityType);

            var csv = GenerateCsv(entries);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv",
                $"audit_trail_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.csv");
        }

        [HttpGet("entity-types")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        public IActionResult GetEntityTypes() => Ok(AuditEntityTypes.All);

        private static AuditTrailDto MapToDto(AuditTrail entry) => new()
        {
            Id = entry.Id,
            CompanyId = entry.CompanyId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            EntityDisplayName = entry.EntityDisplayName,
            Operation = entry.Operation,
            OldValues = ParseJson(entry.OldValues),
            NewValues = ParseJson(entry.NewValues),
            ChangedFields = entry.ChangedFields,
            ActorId = entry.ActorId,
            ActorName = entry.ActorName,
            ActorEmail = entry.ActorEmail,
            ActorIp = entry.ActorIp,
            CorrelationId = entry.CorrelationId,
            RequestPath = entry.RequestPath,
            RequestMethod = entry.RequestMethod,
            CreatedAt = entry.CreatedAt
        };

        private static object? ParseJson(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return json; }
        }

        private static string GenerateCsv(IEnumerable<AuditTrail> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Entity Type,Entity ID,Entity Name,Operation,Actor Name,Actor Email,Actor IP,Changed Fields,Correlation ID");

            foreach (var e in entries)
            {
                var fields = e.ChangedFields != null ? string.Join("; ", e.ChangedFields) : "";
                sb.AppendLine($"\"{e.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{e.EntityType}\",\"{e.EntityId}\"," +
                    $"\"{Escape(e.EntityDisplayName)}\",\"{e.Operation}\",\"{Escape(e.ActorName)}\"," +
                    $"\"{Escape(e.ActorEmail)}\",\"{e.ActorIp}\",\"{Escape(fields)}\",\"{e.CorrelationId}\"");
            }

            return sb.ToString();
        }

        private static string Escape(string? value) => value?.Replace("\"", "\"\"") ?? "";
    }
}
