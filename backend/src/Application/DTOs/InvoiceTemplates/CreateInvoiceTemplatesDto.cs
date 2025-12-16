using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.InvoiceTemplates
{
    /// <summary>
    /// Data transfer object for creating InvoiceTemplates
    /// </summary>
    public class CreateInvoiceTemplatesDto
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
        /// TemplateData
        /// </summary>
        [Required(ErrorMessage = "TemplateData is required")]
        [StringLength(255, ErrorMessage = "TemplateData cannot exceed 255 characters")]
        public string TemplateData { get; set; } = string.Empty;
/// <summary>
        /// IsDefault
        /// </summary>
        public bool? IsDefault { get; set; }
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