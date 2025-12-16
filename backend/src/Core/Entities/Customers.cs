namespace Core.Entities
{
    /// <summary>
    /// Customer entity - represents a customer with Indian GST compliance fields
    /// </summary>
    public class Customers
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? TaxNumber { get; set; }
        public string? Notes { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? PaymentTerms { get; set; }
        public bool? IsActive { get; set; }

        // ==================== Indian GST Compliance ====================

        /// <summary>
        /// GST Identification Number (15 characters)
        /// Required for B2B customers in India
        /// </summary>
        public string? Gstin { get; set; }

        /// <summary>
        /// GST State Code (2 digits)
        /// Used for determining CGST/SGST vs IGST
        /// </summary>
        public string? GstStateCode { get; set; }

        /// <summary>
        /// Customer type for GST classification
        /// Values: 'b2b' (GST registered), 'b2c' (unregistered/consumer), 'overseas' (export), 'sez' (SEZ unit)
        /// </summary>
        public string? CustomerType { get; set; }

        /// <summary>
        /// Whether the customer is GST registered
        /// </summary>
        public bool IsGstRegistered { get; set; }

        /// <summary>
        /// PAN Number (for TDS purposes)
        /// Required when TDS is applicable
        /// </summary>
        public string? PanNumber { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}