using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for Unit Conversion
    /// </summary>
    public class UnitConversionDto
    {
        /// <summary>
        /// Unit conversion ID (optional for create)
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Source unit ID
        /// </summary>
        [Required(ErrorMessage = "From unit is required")]
        public Guid FromUnitId { get; set; }

        /// <summary>
        /// Target unit ID
        /// </summary>
        [Required(ErrorMessage = "To unit is required")]
        public Guid ToUnitId { get; set; }

        /// <summary>
        /// Conversion factor (1 FromUnit = X ToUnit)
        /// </summary>
        [Required(ErrorMessage = "Conversion factor is required")]
        [Range(0.000001, double.MaxValue, ErrorMessage = "Conversion factor must be greater than 0")]
        public decimal ConversionFactor { get; set; }
    }
}
