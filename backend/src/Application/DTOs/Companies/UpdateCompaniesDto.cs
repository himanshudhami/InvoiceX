using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Companies
{
    /// <summary>
    /// Data transfer object for updating Companies
    /// </summary>
    public class UpdateCompaniesDto
    {
/// <summary>
        /// Name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;
/// <summary>
        /// LogoUrl
        /// </summary>
        [StringLength(255, ErrorMessage = "LogoUrl cannot exceed 255 characters")]
        public string? LogoUrl { get; set; }
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
        /// Website
        /// </summary>
        [StringLength(255, ErrorMessage = "Website cannot exceed 255 characters")]
        public string? Website { get; set; }
/// <summary>
        /// TaxNumber
        /// </summary>
        [StringLength(255, ErrorMessage = "TaxNumber cannot exceed 255 characters")]
        public string? TaxNumber { get; set; }
/// <summary>
        /// PaymentInstructions
        /// </summary>
        public string? PaymentInstructions { get; set; }
/// <summary>
        /// SignatureType (drawn, typed, uploaded)
        /// </summary>
        [StringLength(50, ErrorMessage = "SignatureType cannot exceed 50 characters")]
        public string? SignatureType { get; set; }
/// <summary>
        /// SignatureData (base64 for images, text for typed, font info for typed)
        /// </summary>
        public string? SignatureData { get; set; }
/// <summary>
        /// SignatureName (display name for the signature)
        /// </summary>
        [StringLength(255, ErrorMessage = "SignatureName cannot exceed 255 characters")]
        public string? SignatureName { get; set; }
/// <summary>
        /// SignatureFont (font family for typed signatures)
        /// </summary>
        [StringLength(100, ErrorMessage = "SignatureFont cannot exceed 100 characters")]
        public string? SignatureFont { get; set; }
/// <summary>
        /// SignatureColor (color for typed signatures)
        /// </summary>
        [StringLength(50, ErrorMessage = "SignatureColor cannot exceed 50 characters")]
        public string? SignatureColor { get; set; }
}
}