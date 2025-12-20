using System.Text.Json;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Microsoft.Extensions.Logging;

namespace Infrastructure.EInvoice
{
    /// <summary>
    /// ClearTax GSP client implementation for e-invoice operations.
    /// API Documentation: https://docs.cleartax.in/cleartax-docs/e-invoicing-api
    /// </summary>
    public class ClearTaxGspClient : BaseGspClient
    {
        public override string ProviderName => GspProviders.ClearTax;
        protected override string SandboxBaseUrl => "https://einvoicing.internal.cleartax.co/v2";
        protected override string ProductionBaseUrl => "https://api-einvoice.cleartax.in/v2";

        public ClearTaxGspClient(HttpClient httpClient, ILogger<ClearTaxGspClient> logger)
            : base(httpClient, logger)
        {
        }

        public override async Task<GspAuthResult> AuthenticateAsync(EInvoiceCredentials credentials)
        {
            try
            {
                var baseUrl = GetBaseUrl(credentials.Environment);
                var authUrl = $"{baseUrl}/eInvoice/auth";

                var authBody = new
                {
                    user_name = credentials.Username,
                    password = credentials.Password
                };

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var response = await SendRequestAsync(HttpMethod.Post, authUrl, null, authBody, headers);
                var content = await response.Content.ReadAsStringAsync();

                Logger.LogInformation("ClearTax Auth Response: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var (errorCode, errorMessage) = ParseClearTaxError(content);
                    return new GspAuthResult
                    {
                        Success = false,
                        ErrorCode = errorCode ?? response.StatusCode.ToString(),
                        ErrorMessage = errorMessage ?? "Authentication failed",
                        RawResponse = content
                    };
                }

                var json = JsonDocument.Parse(content);
                var data = json.RootElement;

                // ClearTax returns auth token in response
                var authToken = data.TryGetProperty("auth_token", out var at)
                    ? at.GetString()
                    : data.TryGetProperty("AuthToken", out var at2)
                        ? at2.GetString()
                        : null;

                var tokenExpiry = DateTime.UtcNow.AddHours(6); // ClearTax tokens typically last 6 hours
                if (data.TryGetProperty("token_expiry", out var te))
                {
                    if (DateTime.TryParse(te.GetString(), out var expiry))
                        tokenExpiry = expiry;
                }

                return new GspAuthResult
                {
                    Success = true,
                    AuthToken = authToken,
                    TokenExpiry = tokenExpiry,
                    Sek = data.TryGetProperty("sek", out var sek) ? sek.GetString() : null,
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax authentication failed");
                return new GspAuthResult
                {
                    Success = false,
                    ErrorCode = "AUTH_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspGenerateIrnResult> GenerateIrnAsync(
            EInvoiceCredentials credentials,
            IrpInvoiceSchema invoiceData)
        {
            try
            {
                // Ensure we have valid token
                if (!IsTokenValid(credentials))
                {
                    return new GspGenerateIrnResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired. Please re-authenticate."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var generateUrl = $"{baseUrl}/eInvoice/generate";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                // Wrap invoice in array as ClearTax expects batch format
                var requestBody = new[] { invoiceData };

                var response = await SendRequestAsync(HttpMethod.Post, generateUrl,
                    credentials.AuthToken, requestBody, headers);
                var content = await response.Content.ReadAsStringAsync();

                Logger.LogInformation("ClearTax Generate IRN Response: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var (errorCode, errorMessage) = ParseClearTaxError(content);
                    return new GspGenerateIrnResult
                    {
                        Success = false,
                        ErrorCode = errorCode ?? response.StatusCode.ToString(),
                        ErrorMessage = errorMessage ?? "IRN generation failed",
                        RawResponse = content
                    };
                }

                // Parse success response
                var json = JsonDocument.Parse(content);
                var results = json.RootElement;

                // ClearTax returns array of results
                if (results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
                {
                    var result = results[0];

                    // Check if this individual result has error
                    if (result.TryGetProperty("govt_response", out var govtResp))
                    {
                        if (govtResp.TryGetProperty("Success", out var success) && success.GetBoolean())
                        {
                            return new GspGenerateIrnResult
                            {
                                Success = true,
                                Irn = govtResp.TryGetProperty("Irn", out var irn) ? irn.GetString() : null,
                                AckNumber = govtResp.TryGetProperty("AckNo", out var ack) ? ack.GetString() : null,
                                AckDate = govtResp.TryGetProperty("AckDt", out var ackDt)
                                    ? ParseIndianDate(ackDt.GetString())
                                    : null,
                                SignedInvoice = govtResp.TryGetProperty("SignedInvoice", out var si)
                                    ? si.GetString()
                                    : null,
                                SignedQrCode = govtResp.TryGetProperty("SignedQRCode", out var sqr)
                                    ? sqr.GetString()
                                    : null,
                                RawResponse = content
                            };
                        }
                        else
                        {
                            var (errCode, errMsg) = ParseClearTaxError(govtResp.GetRawText());
                            return new GspGenerateIrnResult
                            {
                                Success = false,
                                ErrorCode = errCode,
                                ErrorMessage = errMsg,
                                RawResponse = content
                            };
                        }
                    }
                }

                return new GspGenerateIrnResult
                {
                    Success = false,
                    ErrorCode = "UNKNOWN_RESPONSE",
                    ErrorMessage = "Unexpected response format",
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax IRN generation failed");
                return new GspGenerateIrnResult
                {
                    Success = false,
                    ErrorCode = "GENERATE_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspCancelIrnResult> CancelIrnAsync(
            EInvoiceCredentials credentials,
            string irn,
            string cancelReason,
            string cancelRemarks)
        {
            try
            {
                if (!IsTokenValid(credentials))
                {
                    return new GspCancelIrnResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired. Please re-authenticate."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var cancelUrl = $"{baseUrl}/eInvoice/cancel";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var requestBody = new[]
                {
                    new
                    {
                        Irn = irn,
                        CnlRsn = cancelReason,
                        CnlRem = cancelRemarks
                    }
                };

                var response = await SendRequestAsync(HttpMethod.Post, cancelUrl,
                    credentials.AuthToken, requestBody, headers);
                var content = await response.Content.ReadAsStringAsync();

                Logger.LogInformation("ClearTax Cancel IRN Response: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var (errorCode, errorMessage) = ParseClearTaxError(content);
                    return new GspCancelIrnResult
                    {
                        Success = false,
                        ErrorCode = errorCode ?? response.StatusCode.ToString(),
                        ErrorMessage = errorMessage ?? "IRN cancellation failed",
                        RawResponse = content
                    };
                }

                var json = JsonDocument.Parse(content);
                var results = json.RootElement;

                if (results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
                {
                    var result = results[0];
                    if (result.TryGetProperty("govt_response", out var govtResp))
                    {
                        if (govtResp.TryGetProperty("Success", out var success) && success.GetBoolean())
                        {
                            return new GspCancelIrnResult
                            {
                                Success = true,
                                Irn = irn,
                                CancelDate = DateTime.UtcNow,
                                RawResponse = content
                            };
                        }
                        else
                        {
                            var (errCode, errMsg) = ParseClearTaxError(govtResp.GetRawText());
                            return new GspCancelIrnResult
                            {
                                Success = false,
                                ErrorCode = errCode,
                                ErrorMessage = errMsg,
                                RawResponse = content
                            };
                        }
                    }
                }

                return new GspCancelIrnResult
                {
                    Success = false,
                    ErrorCode = "UNKNOWN_RESPONSE",
                    ErrorMessage = "Unexpected response format",
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax IRN cancellation failed");
                return new GspCancelIrnResult
                {
                    Success = false,
                    ErrorCode = "CANCEL_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspGetIrnResult> GetIrnByDocNoAsync(
            EInvoiceCredentials credentials,
            string docType,
            string docNo,
            string docDate)
        {
            try
            {
                if (!IsTokenValid(credentials))
                {
                    return new GspGetIrnResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var url = $"{baseUrl}/eInvoice/get?doctype={docType}&docnum={docNo}&docdate={docDate}";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var response = await SendRequestAsync(HttpMethod.Get, url, credentials.AuthToken, null, headers);
                var content = await response.Content.ReadAsStringAsync();

                return ParseGetIrnResponse(content, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax GetIrnByDocNo failed");
                return new GspGetIrnResult
                {
                    Success = false,
                    ErrorCode = "GET_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspGetIrnResult> GetIrnDetailsAsync(
            EInvoiceCredentials credentials,
            string irn)
        {
            try
            {
                if (!IsTokenValid(credentials))
                {
                    return new GspGetIrnResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var url = $"{baseUrl}/eInvoice/get?irn={irn}";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var response = await SendRequestAsync(HttpMethod.Get, url, credentials.AuthToken, null, headers);
                var content = await response.Content.ReadAsStringAsync();

                return ParseGetIrnResponse(content, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax GetIrnDetails failed");
                return new GspGetIrnResult
                {
                    Success = false,
                    ErrorCode = "GET_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspEwayBillResult> GenerateEwayBillAsync(
            EInvoiceCredentials credentials,
            string irn,
            EwayBillDetails ewayBillData)
        {
            try
            {
                if (!IsTokenValid(credentials))
                {
                    return new GspEwayBillResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var url = $"{baseUrl}/eInvoice/ewaybill";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var requestBody = new
                {
                    Irn = irn,
                    Distance = ewayBillData.Distance,
                    TransMode = ewayBillData.TransMode,
                    TransId = ewayBillData.TransId,
                    TransName = ewayBillData.TransName,
                    TransDocDt = ewayBillData.TransDocDt,
                    TransDocNo = ewayBillData.TransDocNo,
                    VehNo = ewayBillData.VehNo,
                    VehType = ewayBillData.VehType
                };

                var response = await SendRequestAsync(HttpMethod.Post, url,
                    credentials.AuthToken, requestBody, headers);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var (errorCode, errorMessage) = ParseClearTaxError(content);
                    return new GspEwayBillResult
                    {
                        Success = false,
                        ErrorCode = errorCode,
                        ErrorMessage = errorMessage,
                        RawResponse = content
                    };
                }

                var json = JsonDocument.Parse(content);
                var data = json.RootElement;

                return new GspEwayBillResult
                {
                    Success = true,
                    EwayBillNumber = data.TryGetProperty("EwbNo", out var ewb) ? ewb.GetString() : null,
                    EwayBillDate = data.TryGetProperty("EwbDt", out var ewbDt)
                        ? ParseIndianDate(ewbDt.GetString())
                        : null,
                    ValidUntil = data.TryGetProperty("EwbValidTill", out var valid)
                        ? ParseIndianDate(valid.GetString())
                        : null,
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax E-way Bill generation failed");
                return new GspEwayBillResult
                {
                    Success = false,
                    ErrorCode = "EWAYBILL_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        public override async Task<GspGstinResult> ValidateGstinAsync(
            EInvoiceCredentials credentials,
            string gstin)
        {
            try
            {
                if (!IsTokenValid(credentials))
                {
                    return new GspGstinResult
                    {
                        Success = false,
                        ErrorCode = EInvoiceErrorCodes.TokenExpired,
                        ErrorMessage = "Auth token expired."
                    };
                }

                var baseUrl = GetBaseUrl(credentials.Environment);
                var url = $"{baseUrl}/eInvoice/master/gstin/{gstin}";

                var headers = new Dictionary<string, string>
                {
                    { "x-cleartax-auth-token", credentials.ClientSecret ?? "" },
                    { "gstin", GetGstinFromCredentials(credentials) }
                };

                var response = await SendRequestAsync(HttpMethod.Get, url, credentials.AuthToken, null, headers);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var (errorCode, errorMessage) = ParseClearTaxError(content);
                    return new GspGstinResult
                    {
                        Success = false,
                        ErrorCode = errorCode,
                        ErrorMessage = errorMessage,
                        RawResponse = content
                    };
                }

                var json = JsonDocument.Parse(content);
                var data = json.RootElement;

                return new GspGstinResult
                {
                    Success = true,
                    IsValid = true,
                    Gstin = gstin,
                    LegalName = data.TryGetProperty("LegalName", out var ln) ? ln.GetString() : null,
                    TradeName = data.TryGetProperty("TradeName", out var tn) ? tn.GetString() : null,
                    StateCode = data.TryGetProperty("StateCode", out var sc) ? sc.GetString() : null,
                    Status = data.TryGetProperty("Status", out var st) ? st.GetString() : null,
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ClearTax GSTIN validation failed");
                return new GspGstinResult
                {
                    Success = false,
                    ErrorCode = "GSTIN_EXCEPTION",
                    ErrorMessage = ex.Message
                };
            }
        }

        private GspGetIrnResult ParseGetIrnResponse(string content, bool isSuccess)
        {
            if (!isSuccess)
            {
                var (errorCode, errorMessage) = ParseClearTaxError(content);
                return new GspGetIrnResult
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage,
                    RawResponse = content
                };
            }

            try
            {
                var json = JsonDocument.Parse(content);
                var data = json.RootElement;

                return new GspGetIrnResult
                {
                    Success = true,
                    Irn = data.TryGetProperty("Irn", out var irn) ? irn.GetString() : null,
                    AckNumber = data.TryGetProperty("AckNo", out var ack) ? ack.GetString() : null,
                    AckDate = data.TryGetProperty("AckDt", out var ackDt)
                        ? ParseIndianDate(ackDt.GetString())
                        : null,
                    SignedInvoice = data.TryGetProperty("SignedInvoice", out var si) ? si.GetString() : null,
                    SignedQrCode = data.TryGetProperty("SignedQRCode", out var sqr) ? sqr.GetString() : null,
                    Status = data.TryGetProperty("Status", out var st) ? st.GetString() : null,
                    RawResponse = content
                };
            }
            catch
            {
                return new GspGetIrnResult
                {
                    Success = false,
                    ErrorCode = "PARSE_ERROR",
                    ErrorMessage = "Failed to parse response",
                    RawResponse = content
                };
            }
        }

        private static (string? ErrorCode, string? ErrorMessage) ParseClearTaxError(string content)
        {
            try
            {
                var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                // Handle array response
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    root = root[0];
                }

                // Check govt_response first
                if (root.TryGetProperty("govt_response", out var govtResp))
                {
                    if (govtResp.TryGetProperty("ErrorDetails", out var errors) &&
                        errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                    {
                        var firstError = errors[0];
                        var code = firstError.TryGetProperty("error_code", out var ec) ? ec.GetString() : null;
                        var msg = firstError.TryGetProperty("error_message", out var em) ? em.GetString() : null;
                        return (code, msg);
                    }
                }

                // Check direct error properties
                return ParseErrorResponse(root);
            }
            catch
            {
                return (null, content);
            }
        }

        private static string GetGstinFromCredentials(EInvoiceCredentials credentials)
        {
            // In production, this would come from the company's GSTIN
            return credentials.Username?.Substring(0, 15) ?? "";
        }

        private static DateTime? ParseIndianDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            // Indian date format: DD/MM/YYYY HH:mm:ss or DD-MM-YYYY
            if (DateTime.TryParseExact(dateStr, new[]
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd-MM-yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "dd-MM-yyyy",
                "yyyy-MM-ddTHH:mm:ss"
            }, null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }

            return DateTime.TryParse(dateStr, out var parsed) ? parsed : null;
        }
    }
}
