using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Customers
{
    /// <summary>
    /// Data transfer object for updating Customers
    /// </summary>
    public class UpdateCustomersDto
    {
/// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }
/// <summary>
        /// Name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;
/// <summary>
        /// CompanyName
        /// </summary>
        [StringLength(255, ErrorMessage = "CompanyName cannot exceed 255 characters")]
        public string? CompanyName { get; set; }
/// <summary>
        /// Email
        /// </summary>
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string? Email { get; set; }
/// <summary>
        /// Phone
        /// </summary>
        [StringLength(255, ErrorMessage = "Phone cannot exceed 255 characters")]
        public string? Phone { get; set; }
/// <summary>
        /// AddressLine1
        /// </summary>
        [StringLength(255, ErrorMessage = "AddressLine1 cannot exceed 255 characters")]
        public string? AddressLine1 { get; set; }
/// <summary>
        /// AddressLine2
        /// </summary>
        [StringLength(255, ErrorMessage = "AddressLine2 cannot exceed 255 characters")]
        public string? AddressLine2 { get; set; }
/// <summary>
        /// City
        /// </summary>
        [StringLength(255, ErrorMessage = "City cannot exceed 255 characters")]
        public string? City { get; set; }
/// <summary>
        /// State
        /// </summary>
        [StringLength(255, ErrorMessage = "State cannot exceed 255 characters")]
        public string? State { get; set; }
/// <summary>
        /// ZipCode
        /// </summary>
        [StringLength(255, ErrorMessage = "ZipCode cannot exceed 255 characters")]
        public string? ZipCode { get; set; }
/// <summary>
        /// Country
        /// </summary>
        [StringLength(255, ErrorMessage = "Country cannot exceed 255 characters")]
        public string? Country { get; set; }
/// <summary>
        /// TaxNumber
        /// </summary>
        [StringLength(255, ErrorMessage = "TaxNumber cannot exceed 255 characters")]
        public string? TaxNumber { get; set; }
/// <summary>
        /// Notes
        /// </summary>
        [StringLength(255, ErrorMessage = "Notes cannot exceed 255 characters")]
        public string? Notes { get; set; }
/// <summary>
        /// CreditLimit
        /// </summary>
        public decimal? CreditLimit { get; set; }
/// <summary>
        /// PaymentTerms
        /// </summary>
        public int? PaymentTerms { get; set; }
/// <summary>
        /// IsActive
        /// </summary>
        public bool? IsActive { get; set; }

        // ==================== Indian GST Compliance ====================

        /// <summary>
        /// GST Identification Number (15 characters)
        /// </summary>
        [StringLength(20, ErrorMessage = "GSTIN cannot exceed 20 characters")]
        public string? Gstin { get; set; }

        /// <summary>
        /// GST State Code (2 digits)
        /// </summary>
        [StringLength(5, ErrorMessage = "GST State Code cannot exceed 5 characters")]
        public string? GstStateCode { get; set; }

        /// <summary>
        /// Customer type for GST classification (b2b, b2c, overseas, sez)
        /// </summary>
        [StringLength(20, ErrorMessage = "Customer Type cannot exceed 20 characters")]
        public string? CustomerType { get; set; }

        /// <summary>
        /// Whether the customer is GST registered
        /// </summary>
        public bool IsGstRegistered { get; set; }

        /// <summary>
        /// PAN Number (for TDS purposes)
        /// </summary>
        [StringLength(15, ErrorMessage = "PAN cannot exceed 15 characters")]
        public string? PanNumber { get; set; }
}
}