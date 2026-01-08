using System.Text.Json;
using Core.Common;
using Core.Entities.EInvoice;
using Core.Interfaces;
using Core.Interfaces.EInvoice;
using Microsoft.Extensions.Logging;

namespace Application.Services.EInvoice
{
    /// <summary>
    /// Main E-Invoice service that orchestrates IRN generation, cancellation, and queue processing.
    /// Supports domestic (B2B, B2C, SEZ) and export invoice types.
    /// </summary>
    public class EInvoiceService : IEInvoiceService
    {
        private readonly IEInvoiceCredentialsRepository _credentialsRepository;
        private readonly IEInvoiceAuditLogRepository _auditLogRepository;
        private readonly IEInvoiceQueueRepository _queueRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly IInvoiceItemsRepository _invoiceItemsRepository;
        private readonly IEInvoiceGspClientFactory _gspClientFactory;
        private readonly ILogger<EInvoiceService> _logger;

        public EInvoiceService(
            IEInvoiceCredentialsRepository credentialsRepository,
            IEInvoiceAuditLogRepository auditLogRepository,
            IEInvoiceQueueRepository queueRepository,
            IInvoicesRepository invoicesRepository,
            ICompaniesRepository companiesRepository,
            ICustomersRepository customersRepository,
            IInvoiceItemsRepository invoiceItemsRepository,
            IEInvoiceGspClientFactory gspClientFactory,
            ILogger<EInvoiceService> logger)
        {
            _credentialsRepository = credentialsRepository;
            _auditLogRepository = auditLogRepository;
            _queueRepository = queueRepository;
            _invoicesRepository = invoicesRepository;
            _companiesRepository = companiesRepository;
            _customersRepository = customersRepository;
            _invoiceItemsRepository = invoiceItemsRepository;
            _gspClientFactory = gspClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Generates IRN for an invoice
        /// </summary>
        public async Task<Result<EInvoiceGenerationResult>> GenerateIrnAsync(
            Guid invoiceId,
            Guid? userId = null,
            string? ipAddress = null)
        {
            try
            {
                // Get invoice
                var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return Error.NotFound($"Invoice {invoiceId} not found");

                if (!invoice.CompanyId.HasValue)
                    return Error.Validation("Invoice has no associated company");

                // Check if already has IRN
                if (!string.IsNullOrEmpty(invoice.EInvoiceIrn))
                    return Error.Conflict($"Invoice already has IRN: {invoice.EInvoiceIrn}");

                // Get credentials
                var credentials = await _credentialsRepository.GetByCompanyIdAsync(
                    invoice.CompanyId.Value, "production");

                if (credentials == null)
                {
                    // Try sandbox
                    credentials = await _credentialsRepository.GetByCompanyIdAsync(
                        invoice.CompanyId.Value, "sandbox");
                }

                if (credentials == null)
                    return Error.NotFound("E-invoice credentials not configured for this company");

                // Get GSP client
                var gspClient = _gspClientFactory.GetClient(credentials.GspProvider);
                if (gspClient == null)
                    return Error.Internal($"GSP provider {credentials.GspProvider} not supported");

                // Ensure valid token
                if (string.IsNullOrEmpty(credentials.AuthToken) ||
                    !credentials.TokenExpiry.HasValue ||
                    credentials.TokenExpiry.Value <= DateTime.UtcNow.AddMinutes(5))
                {
                    var authResult = await gspClient.AuthenticateAsync(credentials);
                    if (!authResult.Success)
                    {
                        await LogAuditAsync(credentials, invoiceId, EInvoiceActionTypes.Auth,
                            null, authResult, userId, ipAddress);
                        return Error.Internal($"Authentication failed: {authResult.ErrorMessage}");
                    }

                    // Update token
                    await _credentialsRepository.UpdateTokenAsync(
                        credentials.Id,
                        authResult.AuthToken!,
                        authResult.TokenExpiry!.Value,
                        authResult.Sek);

                    credentials.AuthToken = authResult.AuthToken;
                    credentials.TokenExpiry = authResult.TokenExpiry;
                }

                // Build IRP schema
                var irpSchema = await BuildIrpSchemaAsync(invoice);

                // Generate IRN
                var startTime = DateTime.UtcNow;
                var result = await gspClient.GenerateIrnAsync(credentials, irpSchema);
                var responseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Log audit
                await LogAuditAsync(credentials, invoiceId, EInvoiceActionTypes.GenerateIrn,
                    irpSchema, result, userId, ipAddress, responseTimeMs);

                if (!result.Success)
                {
                    // Update invoice status
                    await UpdateInvoiceEInvoiceStatusAsync(invoice, "failed", result.ErrorCode, result.ErrorMessage);

                    return Error.Internal($"IRN generation failed: {result.ErrorMessage}");
                }

                // Update invoice with IRN details
                invoice.EInvoiceIrn = result.Irn;
                invoice.EInvoiceAckNumber = result.AckNumber;
                invoice.EInvoiceAckDate = result.AckDate;
                invoice.EInvoiceQrCode = result.SignedQrCode;
                invoice.EInvoiceSignedJson = result.SignedInvoice;
                invoice.EInvoiceStatus = "generated";
                invoice.EwayBillNumber = result.EwayBillNumber;
                invoice.EwayBillDate = result.EwayBillDate;
                invoice.EwayBillValidUntil = result.EwayBillValidUntil;

                await _invoicesRepository.UpdateAsync(invoice);

                _logger.LogInformation("IRN generated successfully for invoice {InvoiceId}: {Irn}",
                    invoiceId, result.Irn);

                return Result<EInvoiceGenerationResult>.Success(new EInvoiceGenerationResult
                {
                    Success = true,
                    Irn = result.Irn,
                    AckNumber = result.AckNumber,
                    AckDate = result.AckDate,
                    QrCode = result.SignedQrCode,
                    EwayBillNumber = result.EwayBillNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating IRN for invoice {InvoiceId}", invoiceId);
                return Error.Internal($"Error generating IRN: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels an existing IRN
        /// </summary>
        public async Task<Result<bool>> CancelIrnAsync(
            Guid invoiceId,
            string cancelReason,
            string cancelRemarks,
            Guid? userId = null,
            string? ipAddress = null)
        {
            try
            {
                var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return Error.NotFound($"Invoice {invoiceId} not found");

                if (string.IsNullOrEmpty(invoice.EInvoiceIrn))
                    return Error.Validation("Invoice does not have an IRN to cancel");

                if (invoice.EInvoiceStatus == "cancelled")
                    return Error.Conflict("IRN is already cancelled");

                if (!invoice.CompanyId.HasValue)
                    return Error.Validation("Invoice has no associated company");

                // Check 24-hour cancellation window
                if (invoice.EInvoiceAckDate.HasValue &&
                    (DateTime.UtcNow - invoice.EInvoiceAckDate.Value).TotalHours > 24)
                {
                    return Error.Validation("IRN cancellation window (24 hours) has expired");
                }

                var credentials = await _credentialsRepository.GetByCompanyIdAsync(
                    invoice.CompanyId.Value, "production")
                    ?? await _credentialsRepository.GetByCompanyIdAsync(
                        invoice.CompanyId.Value, "sandbox");

                if (credentials == null)
                    return Error.NotFound("E-invoice credentials not configured");

                var gspClient = _gspClientFactory.GetClient(credentials.GspProvider);
                if (gspClient == null)
                    return Error.Internal($"GSP provider {credentials.GspProvider} not supported");

                // Ensure valid token (simplified - should reuse auth logic)
                if (string.IsNullOrEmpty(credentials.AuthToken) ||
                    credentials.TokenExpiry <= DateTime.UtcNow.AddMinutes(5))
                {
                    var authResult = await gspClient.AuthenticateAsync(credentials);
                    if (!authResult.Success)
                        return Error.Internal($"Authentication failed: {authResult.ErrorMessage}");

                    await _credentialsRepository.UpdateTokenAsync(
                        credentials.Id, authResult.AuthToken!, authResult.TokenExpiry!.Value);
                    credentials.AuthToken = authResult.AuthToken;
                }

                var result = await gspClient.CancelIrnAsync(
                    credentials, invoice.EInvoiceIrn, cancelReason, cancelRemarks);

                await LogAuditAsync(credentials, invoiceId, EInvoiceActionTypes.CancelIrn,
                    new { Irn = invoice.EInvoiceIrn, CnlRsn = cancelReason, CnlRem = cancelRemarks },
                    result, userId, ipAddress);

                if (!result.Success)
                    return Error.Internal($"IRN cancellation failed: {result.ErrorMessage}");

                // Update invoice
                invoice.EInvoiceStatus = "cancelled";
                invoice.EInvoiceCancelDate = result.CancelDate ?? DateTime.UtcNow;
                invoice.EInvoiceCancelReason = cancelRemarks;
                await _invoicesRepository.UpdateAsync(invoice);

                _logger.LogInformation("IRN cancelled for invoice {InvoiceId}: {Irn}",
                    invoiceId, invoice.EInvoiceIrn);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling IRN for invoice {InvoiceId}", invoiceId);
                return Error.Internal($"Error cancelling IRN: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if e-invoice is applicable for an invoice
        /// </summary>
        public async Task<bool> IsEInvoiceApplicableAsync(Guid invoiceId)
        {
            var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
            if (invoice == null || !invoice.CompanyId.HasValue)
                return false;

            // B2C invoices generally don't require e-invoice
            if (invoice.InvoiceType == "domestic_b2c")
                return false;

            // Get company to check turnover threshold
            var company = await _companiesRepository.GetByIdAsync(invoice.CompanyId.Value);
            if (company == null)
                return false;

            // Get credentials to check threshold
            var credentials = await _credentialsRepository.GetByCompanyIdAsync(
                invoice.CompanyId.Value, "production");

            // Default threshold: 5 Cr INR
            var threshold = credentials?.EinvoiceThreshold ?? 50000000m;

            // For this demo, we assume e-invoice is applicable for B2B, SEZ, and Export
            return invoice.InvoiceType switch
            {
                "domestic_b2b" => true,
                "sez" => true,
                "export" => true,
                "deemed_export" => true,
                _ => false
            };
        }

        /// <summary>
        /// Queues an invoice for async IRN generation
        /// </summary>
        public async Task<Result<Guid>> QueueForIrnGenerationAsync(Guid invoiceId, int priority = 5)
        {
            var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice {invoiceId} not found");

            if (!invoice.CompanyId.HasValue)
                return Error.Validation("Invoice has no associated company");

            // Check if already in queue
            var existing = await _queueRepository.GetByInvoiceIdAsync(invoiceId, EInvoiceQueueStatus.Pending);
            if (existing != null)
                return Error.Conflict($"Invoice already queued: {existing.Id}");

            var queueItem = new EInvoiceQueue
            {
                CompanyId = invoice.CompanyId.Value,
                InvoiceId = invoiceId,
                ActionType = EInvoiceQueueActions.GenerateIrn,
                Priority = priority,
                Status = EInvoiceQueueStatus.Pending
            };

            await _queueRepository.AddAsync(queueItem);

            // Update invoice status
            invoice.EInvoiceStatus = "pending";
            await _invoicesRepository.UpdateAsync(invoice);

            return Result<Guid>.Success(queueItem.Id);
        }

        /// <summary>
        /// Builds IRP Schema from invoice data
        /// </summary>
        private async Task<IrpInvoiceSchema> BuildIrpSchemaAsync(Core.Entities.Invoices invoice)
        {
            var company = await _companiesRepository.GetByIdAsync(invoice.CompanyId!.Value);
            var customer = invoice.PartyId.HasValue
                ? await _customersRepository.GetByIdAsync(invoice.PartyId.Value)
                : null;

            // Get invoice items using paged query with filter
            var (items, _) = await _invoiceItemsRepository.GetPagedAsync(
                1, 1000, null, null, false,
                new Dictionary<string, object> { { "invoice_id", invoice.Id } });

            var schema = new IrpInvoiceSchema
            {
                Version = "1.1",
                TranDtls = new TransactionDetails
                {
                    TaxSch = "GST",
                    SupTyp = GetSupplyTypeCode(invoice),
                    RegRev = invoice.ReverseCharge ? "Y" : "N"
                },
                DocDtls = new DocumentDetails
                {
                    Typ = DocumentTypeCodes.Invoice,
                    No = invoice.InvoiceNumber,
                    Dt = invoice.InvoiceDate.ToString("dd/MM/yyyy")
                },
                SellerDtls = new PartyDetails
                {
                    Gstin = company?.Gstin ?? "URP",
                    LglNm = company?.Name ?? "",
                    TrdNm = company?.Name,
                    Addr1 = company?.AddressLine1 ?? "",
                    Loc = company?.City ?? "",
                    Pin = int.TryParse(company?.ZipCode, out var pin) ? pin : 0,
                    Stcd = company?.GstStateCode ?? GetStateCodeFromGstin(company?.Gstin)
                },
                BuyerDtls = new PartyDetails
                {
                    Gstin = customer?.Gstin ?? "URP",
                    LglNm = customer?.Name ?? "",
                    TrdNm = customer?.Name,
                    Addr1 = customer?.AddressLine1 ?? "",
                    Loc = customer?.City ?? "",
                    Pin = int.TryParse(customer?.ZipCode, out var cpin) ? cpin : 0,
                    Stcd = GetStateCodeFromGstin(customer?.Gstin) ?? IndianStateCodes.OtherCountry
                },
                ItemList = items.Select((item, index) =>
                {
                    var taxableAmt = item.Quantity * item.UnitPrice * (1 - (item.DiscountRate ?? 0) / 100);
                    var totalItemVal = taxableAmt + item.CgstAmount + item.SgstAmount + item.IgstAmount + item.CessAmount;
                    return new ItemDetails
                    {
                        SlNo = (index + 1).ToString(),
                        PrdDesc = item.Description ?? "",
                        IsServc = item.IsService ? "Y" : "N",
                        HsnCd = item.HsnSacCode ?? "9999",
                        Qty = item.Quantity,
                        Unit = "OTH",
                        UnitPrice = item.UnitPrice,
                        TotAmt = item.Quantity * item.UnitPrice,
                        Discount = item.Quantity * item.UnitPrice * (item.DiscountRate ?? 0) / 100,
                        AssAmt = taxableAmt,
                        GstRt = item.TaxRate ?? (item.CgstRate + item.SgstRate + item.IgstRate),
                        IgstAmt = item.IgstAmount,
                        CgstAmt = item.CgstAmount,
                        SgstAmt = item.SgstAmount,
                        CesAmt = item.CessAmount,
                        TotItemVal = totalItemVal
                    };
                }).ToList(),
                ValDtls = new ValueDetails
                {
                    AssVal = invoice.Subtotal,
                    CgstVal = invoice.TotalCgst,
                    SgstVal = invoice.TotalSgst,
                    IgstVal = invoice.TotalIgst,
                    CesVal = invoice.TotalCess,
                    Discount = invoice.DiscountAmount ?? 0,
                    TotInvVal = invoice.TotalAmount
                }
            };

            // Add export details for export invoices
            if (invoice.InvoiceType == "export" || invoice.SupplyType == "export")
            {
                schema.ExpDtls = new ExportDetails
                {
                    ShipBNo = invoice.ShippingBillNumber,
                    ShipBDt = invoice.ShippingBillDate?.ToString("dd/MM/yyyy"),
                    Port = invoice.PortCode,
                    ForCur = invoice.ForeignCurrency,
                    CntCode = IndianStateCodes.OtherCountry,
                    ExpDuty = invoice.ExportDuty
                };

                // Set buyer state code to 97 (Other Country) for exports
                schema.BuyerDtls.Stcd = IndianStateCodes.OtherCountry;
                schema.BuyerDtls.Pin = 999999;
            }

            return schema;
        }

        private static string GetSupplyTypeCode(Core.Entities.Invoices invoice)
        {
            return invoice.InvoiceType switch
            {
                "export" => invoice.ExportType == "EXPWP" ? SupplyTypeCodes.EXPWP : SupplyTypeCodes.EXPWOP,
                "sez" => invoice.SezCategory == "SEZWP" ? SupplyTypeCodes.SEZWP : SupplyTypeCodes.SEZWOP,
                "deemed_export" => SupplyTypeCodes.DEXP,
                "domestic_b2c" => SupplyTypeCodes.B2C,
                _ => SupplyTypeCodes.B2B
            };
        }

        private static string? GetStateCodeFromGstin(string? gstin)
        {
            if (string.IsNullOrEmpty(gstin) || gstin.Length < 2)
                return null;
            return gstin.Substring(0, 2);
        }

        private async Task UpdateInvoiceEInvoiceStatusAsync(
            Core.Entities.Invoices invoice,
            string status,
            string? errorCode = null,
            string? errorMessage = null)
        {
            invoice.EInvoiceStatus = status;
            await _invoicesRepository.UpdateAsync(invoice);
        }

        private async Task LogAuditAsync(
            EInvoiceCredentials credentials,
            Guid? invoiceId,
            string actionType,
            object? request,
            object? response,
            Guid? userId,
            string? ipAddress,
            int? responseTimeMs = null)
        {
            try
            {
                var gspResult = response as GspGenerateIrnResult;
                var cancelResult = response as GspCancelIrnResult;
                var authResult = response as GspAuthResult;

                var auditLog = new EInvoiceAuditLog
                {
                    CompanyId = credentials.CompanyId,
                    InvoiceId = invoiceId,
                    ActionType = actionType,
                    RequestTimestamp = DateTime.UtcNow,
                    RequestPayload = request != null
                        ? JsonDocument.Parse(JsonSerializer.Serialize(request))
                        : null,
                    ResponseStatus = (gspResult?.Success ?? cancelResult?.Success ?? authResult?.Success ?? false)
                        ? EInvoiceResponseStatus.Success
                        : EInvoiceResponseStatus.Error,
                    Irn = gspResult?.Irn,
                    AckNumber = gspResult?.AckNumber,
                    AckDate = gspResult?.AckDate,
                    ErrorCode = gspResult?.ErrorCode ?? cancelResult?.ErrorCode ?? authResult?.ErrorCode,
                    ErrorMessage = gspResult?.ErrorMessage ?? cancelResult?.ErrorMessage ?? authResult?.ErrorMessage,
                    GspProvider = credentials.GspProvider,
                    Environment = credentials.Environment,
                    UserId = userId,
                    IpAddress = ipAddress,
                    ResponseTimeMs = responseTimeMs
                };

                await _auditLogRepository.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log e-invoice audit for invoice {InvoiceId}", invoiceId);
            }
        }
    }

    /// <summary>
    /// Result of e-invoice generation
    /// </summary>
    public class EInvoiceGenerationResult
    {
        public bool Success { get; set; }
        public string? Irn { get; set; }
        public string? AckNumber { get; set; }
        public DateTime? AckDate { get; set; }
        public string? QrCode { get; set; }
        public string? EwayBillNumber { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// E-Invoice service interface
    /// </summary>
    public interface IEInvoiceService
    {
        Task<Result<EInvoiceGenerationResult>> GenerateIrnAsync(Guid invoiceId, Guid? userId = null, string? ipAddress = null);
        Task<Result<bool>> CancelIrnAsync(Guid invoiceId, string cancelReason, string cancelRemarks, Guid? userId = null, string? ipAddress = null);
        Task<bool> IsEInvoiceApplicableAsync(Guid invoiceId);
        Task<Result<Guid>> QueueForIrnGenerationAsync(Guid invoiceId, int priority = 5);
    }

    /// <summary>
    /// Factory for creating GSP clients based on provider
    /// </summary>
    public interface IEInvoiceGspClientFactory
    {
        IEInvoiceGspClient? GetClient(string providerName);
    }
}
