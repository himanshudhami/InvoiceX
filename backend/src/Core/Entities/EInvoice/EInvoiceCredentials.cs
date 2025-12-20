namespace Core.Entities.EInvoice
{
    /// <summary>
    /// Stores GSP API credentials for e-invoice integration.
    /// Secrets should be encrypted at the application level before storage.
    /// </summary>
    public class EInvoiceCredentials
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        /// <summary>
        /// GSP Provider: cleartax, iris, nic_direct
        /// </summary>
        public string GspProvider { get; set; } = "cleartax";

        /// <summary>
        /// Environment: sandbox, production
        /// </summary>
        public string Environment { get; set; } = "sandbox";

        // API Credentials
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; } // Encrypted
        public string? Username { get; set; }
        public string? Password { get; set; } // Encrypted

        // Token Management
        public string? AuthToken { get; set; } // Encrypted JWT
        public DateTime? TokenExpiry { get; set; }
        public string? Sek { get; set; } // Session Encryption Key for NIC direct

        // Configuration
        public bool AutoGenerateIrn { get; set; }
        public bool AutoCancelOnVoid { get; set; }
        public bool GenerateEwayBill { get; set; }

        /// <summary>
        /// E-invoice threshold in base currency (default: 5 Cr INR)
        /// </summary>
        public decimal EinvoiceThreshold { get; set; } = 50000000m;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// GSP Provider types for e-invoice integration
    /// </summary>
    public static class GspProviders
    {
        public const string ClearTax = "cleartax";
        public const string Iris = "iris";
        public const string NicDirect = "nic_direct";
    }

    /// <summary>
    /// E-invoice environments
    /// </summary>
    public static class EInvoiceEnvironments
    {
        public const string Sandbox = "sandbox";
        public const string Production = "production";
    }
}
