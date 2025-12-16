using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TaxRates
{
    /// <summary>
    /// Data transfer object for updating TaxRates
    /// </summary>
    public class UpdateTaxRatesDto
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
        /// Rate
        /// </summary>
        [Required(ErrorMessage = "Rate is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Rate must be greater than 0")]
        public decimal Rate { get; set; }
/// <summary>
        /// IsDefault
        /// </summary>
        public bool? IsDefault { get; set; }
/// <summary>
        /// IsActive
        /// </summary>
        public bool? IsActive { get; set; }
}
}