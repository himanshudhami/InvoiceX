using Core.Entities.EInvoice;

namespace Core.Interfaces.EInvoice
{
    /// <summary>
    /// GSP (GST Suvidha Provider) client interface for e-invoice operations.
    /// Implementations: ClearTax, IRIS, NIC Direct
    /// </summary>
    public interface IEInvoiceGspClient
    {
        /// <summary>
        /// GSP Provider name (cleartax, iris, nic_direct)
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Authenticate and get access token
        /// </summary>
        Task<GspAuthResult> AuthenticateAsync(EInvoiceCredentials credentials);

        /// <summary>
        /// Generate IRN for an invoice
        /// </summary>
        Task<GspGenerateIrnResult> GenerateIrnAsync(
            EInvoiceCredentials credentials,
            IrpInvoiceSchema invoiceData);

        /// <summary>
        /// Cancel an existing IRN
        /// </summary>
        Task<GspCancelIrnResult> CancelIrnAsync(
            EInvoiceCredentials credentials,
            string irn,
            string cancelReason,
            string cancelRemarks);

        /// <summary>
        /// Get IRN details by document number
        /// </summary>
        Task<GspGetIrnResult> GetIrnByDocNoAsync(
            EInvoiceCredentials credentials,
            string docType,
            string docNo,
            string docDate);

        /// <summary>
        /// Get IRN details by IRN
        /// </summary>
        Task<GspGetIrnResult> GetIrnDetailsAsync(
            EInvoiceCredentials credentials,
            string irn);

        /// <summary>
        /// Generate e-way bill along with e-invoice
        /// </summary>
        Task<GspEwayBillResult> GenerateEwayBillAsync(
            EInvoiceCredentials credentials,
            string irn,
            EwayBillDetails ewayBillData);

        /// <summary>
        /// Validate GSTIN
        /// </summary>
        Task<GspGstinResult> ValidateGstinAsync(
            EInvoiceCredentials credentials,
            string gstin);
    }

    /// <summary>
    /// Authentication result from GSP
    /// </summary>
    public class GspAuthResult
    {
        public bool Success { get; set; }
        public string? AuthToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public string? Sek { get; set; } // Session Encryption Key
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    /// <summary>
    /// IRN generation result from GSP
    /// </summary>
    public class GspGenerateIrnResult
    {
        public bool Success { get; set; }
        public string? Irn { get; set; }
        public string? AckNumber { get; set; }
        public DateTime? AckDate { get; set; }
        public string? SignedInvoice { get; set; }
        public string? SignedQrCode { get; set; }
        public string? QrCodeImage { get; set; } // Base64 encoded
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public List<GspValidationError>? ValidationErrors { get; set; }
        public object? RawResponse { get; set; }

        // E-way bill (if generated along with e-invoice)
        public string? EwayBillNumber { get; set; }
        public DateTime? EwayBillDate { get; set; }
        public DateTime? EwayBillValidUntil { get; set; }
    }

    /// <summary>
    /// IRN cancellation result from GSP
    /// </summary>
    public class GspCancelIrnResult
    {
        public bool Success { get; set; }
        public string? Irn { get; set; }
        public DateTime? CancelDate { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    /// <summary>
    /// Get IRN result from GSP
    /// </summary>
    public class GspGetIrnResult
    {
        public bool Success { get; set; }
        public string? Irn { get; set; }
        public string? AckNumber { get; set; }
        public DateTime? AckDate { get; set; }
        public string? SignedInvoice { get; set; }
        public string? SignedQrCode { get; set; }
        public string? Status { get; set; } // ACT (Active), CNL (Cancelled)
        public DateTime? CancelDate { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    /// <summary>
    /// E-way bill generation result
    /// </summary>
    public class GspEwayBillResult
    {
        public bool Success { get; set; }
        public string? EwayBillNumber { get; set; }
        public DateTime? EwayBillDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    /// <summary>
    /// GSTIN validation result
    /// </summary>
    public class GspGstinResult
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string? Gstin { get; set; }
        public string? LegalName { get; set; }
        public string? TradeName { get; set; }
        public string? StateCode { get; set; }
        public string? Status { get; set; } // Active, Cancelled, Suspended
        public string? RegistrationType { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    /// <summary>
    /// Validation error from GSP
    /// </summary>
    public class GspValidationError
    {
        public string? Field { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Common e-invoice error codes from IRP
    /// </summary>
    public static class EInvoiceErrorCodes
    {
        public const string DuplicateIrn = "2150";
        public const string InvalidGstin = "2148";
        public const string InvalidHsn = "2117";
        public const string TokenExpired = "105";
        public const string Unauthorized = "106";
        public const string IrnNotFound = "2163";
        public const string AlreadyCancelled = "2164";
        public const string CancellationWindowExpired = "2165";
        public const string InvalidInvoiceNumber = "2147";
        public const string ServerError = "500";
    }

    /// <summary>
    /// IRN cancellation reason codes
    /// </summary>
    public static class IrnCancelReasonCodes
    {
        public const string Duplicate = "1";
        public const string DataEntryMistake = "2";
        public const string OrderCancelled = "3";
        public const string Other = "4";
    }
}
