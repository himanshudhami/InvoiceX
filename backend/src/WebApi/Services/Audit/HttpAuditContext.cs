using System.Security.Claims;
using Core.Interfaces.Audit;
using Microsoft.AspNetCore.Http;

namespace WebApi.Services.Audit
{
    /// <summary>
    /// HTTP request-based audit context. Extracts actor info from JWT claims and request metadata.
    /// </summary>
    public class HttpAuditContext : IAuditContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpAuditContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private HttpContext? Context => _httpContextAccessor.HttpContext;

        public Guid? ActorId => GetClaimGuid(ClaimTypes.NameIdentifier, "sub");

        public string? ActorName => GetClaimValue(ClaimTypes.Name, "name", "display_name");

        public string? ActorEmail => GetClaimValue(ClaimTypes.Email, "email");

        public string? ActorIp => GetForwardedIp() ?? Context?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();

        public string? CorrelationId =>
            GetHeaderValue(Context?.Response.Headers, "X-Correlation-Id") ??
            GetHeaderValue(Context?.Request.Headers, "X-Correlation-Id");

        public string? RequestPath => Context?.Request.Path.ToString();

        public string? RequestMethod => Context?.Request.Method;

        public bool HasActor => ActorId.HasValue;

        private Guid? GetClaimGuid(params string[] claimTypes)
        {
            var value = GetClaimValue(claimTypes);
            return value != null && Guid.TryParse(value, out var id) ? id : null;
        }

        private string? GetClaimValue(params string[] claimTypes)
        {
            foreach (var type in claimTypes)
            {
                var claim = Context?.User.FindFirst(type);
                if (claim != null)
                    return claim.Value;
            }
            return null;
        }

        private string? GetForwardedIp()
        {
            var forwardedFor = Context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(forwardedFor) ? null : forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        private static string? GetHeaderValue(IHeaderDictionary? headers, string key) =>
            headers?[key].FirstOrDefault();
    }
}
