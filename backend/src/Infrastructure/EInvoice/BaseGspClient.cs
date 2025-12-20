using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Microsoft.Extensions.Logging;

namespace Infrastructure.EInvoice
{
    /// <summary>
    /// Base class for GSP client implementations.
    /// Provides common functionality for HTTP calls, logging, and error handling.
    /// </summary>
    public abstract class BaseGspClient : IEInvoiceGspClient
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        public abstract string ProviderName { get; }
        protected abstract string SandboxBaseUrl { get; }
        protected abstract string ProductionBaseUrl { get; }

        protected BaseGspClient(HttpClient httpClient, ILogger logger)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected string GetBaseUrl(string environment)
        {
            return environment.ToLowerInvariant() == "production"
                ? ProductionBaseUrl
                : SandboxBaseUrl;
        }

        public abstract Task<GspAuthResult> AuthenticateAsync(EInvoiceCredentials credentials);
        public abstract Task<GspGenerateIrnResult> GenerateIrnAsync(EInvoiceCredentials credentials, IrpInvoiceSchema invoiceData);
        public abstract Task<GspCancelIrnResult> CancelIrnAsync(EInvoiceCredentials credentials, string irn, string cancelReason, string cancelRemarks);
        public abstract Task<GspGetIrnResult> GetIrnByDocNoAsync(EInvoiceCredentials credentials, string docType, string docNo, string docDate);
        public abstract Task<GspGetIrnResult> GetIrnDetailsAsync(EInvoiceCredentials credentials, string irn);
        public abstract Task<GspEwayBillResult> GenerateEwayBillAsync(EInvoiceCredentials credentials, string irn, EwayBillDetails ewayBillData);
        public abstract Task<GspGstinResult> ValidateGstinAsync(EInvoiceCredentials credentials, string gstin);

        /// <summary>
        /// Validates that the auth token is still valid
        /// </summary>
        protected bool IsTokenValid(EInvoiceCredentials credentials)
        {
            if (string.IsNullOrEmpty(credentials.AuthToken))
                return false;

            if (!credentials.TokenExpiry.HasValue)
                return false;

            // Add 5 minute buffer for token expiry
            return credentials.TokenExpiry.Value > DateTime.UtcNow.AddMinutes(5);
        }

        /// <summary>
        /// Computes SHA256 hash of the request for audit logging
        /// </summary>
        protected static string ComputeRequestHash(string requestJson)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(requestJson);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Sends HTTP request and handles common error scenarios
        /// </summary>
        protected async Task<HttpResponseMessage> SendRequestAsync(
            HttpMethod method,
            string url,
            string? authToken,
            object? body = null,
            Dictionary<string, string>? headers = null)
        {
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await HttpClient.SendAsync(request);
        }

        /// <summary>
        /// Parses error response from GSP
        /// </summary>
        protected static (string? ErrorCode, string? ErrorMessage) ParseErrorResponse(JsonElement element)
        {
            string? errorCode = null;
            string? errorMessage = null;

            if (element.TryGetProperty("errorCode", out var ec))
                errorCode = ec.GetString();
            else if (element.TryGetProperty("error_code", out var ec2))
                errorCode = ec2.GetString();
            else if (element.TryGetProperty("ErrorCode", out var ec3))
                errorCode = ec3.GetString();

            if (element.TryGetProperty("errorMessage", out var em))
                errorMessage = em.GetString();
            else if (element.TryGetProperty("error_message", out var em2))
                errorMessage = em2.GetString();
            else if (element.TryGetProperty("ErrorMessage", out var em3))
                errorMessage = em3.GetString();
            else if (element.TryGetProperty("message", out var m))
                errorMessage = m.GetString();

            return (errorCode, errorMessage);
        }
    }
}
