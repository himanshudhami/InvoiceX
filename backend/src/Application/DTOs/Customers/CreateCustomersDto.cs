using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Customers
{
    /// <summary>
    /// Data transfer object for creating Customers
    /// </summary>
    public class CreateCustomersDto
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
/// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime? CreatedAt { get; set; }
/// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
}
}