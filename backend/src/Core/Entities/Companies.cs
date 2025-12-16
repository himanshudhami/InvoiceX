namespace Core.Entities
{
    /// <summary>
    /// Company entity - represents a business entity with Indian tax compliance fields
    /// </summary>
    public class Companies
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentInstructions { get; set; }
        public Guid? InvoiceTemplateId { get; set; }
        public string? SignatureType { get; set; }
        public string? SignatureData { get; set; }
        public string? SignatureName { get; set; }
        public string? SignatureFont { get; set; }
        public string? SignatureColor { get; set; }

        // ==================== Indian Tax Compliance ====================

        /// <summary>
        /// GST Identification Number (15 characters)
        /// Format: 2 digit state code + 10 digit PAN + 1 digit entity code + 1 digit check + Z
        /// Example: 27AAACP1234C1Z5
        /// </summary>
        public string? Gstin { get; set; }

        /// <summary>
        /// GST State Code (first 2 digits of GSTIN)
        /// Example: 27 for Maharashtra, 29 for Karnataka
        /// </summary>
        public string? GstStateCode { get; set; }

        /// <summary>
        /// Permanent Account Number (10 characters)
        /// Format: 5 letters + 4 digits + 1 letter
        /// Example: AAACP1234C
        /// </summary>
        public string? PanNumber { get; set; }

        /// <summary>
        /// Corporate Identity Number (21 characters for companies)
        /// Example: U74999MH2020PTC123456
        /// </summary>
        public string? CinNumber { get; set; }

        /// <summary>
        /// GST Registration Type
        /// Values: 'regular', 'composition', 'unregistered', 'overseas'
        /// </summary>
        public string? GstRegistrationType { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}