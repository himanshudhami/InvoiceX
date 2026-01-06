using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Unit of Measure
    /// </summary>
    public class CreateUnitOfMeasureDto
    {
        /// <summary>
        /// Company ID (null for system units)
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Unit name (e.g., "Pieces", "Kilograms")
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unit symbol (e.g., "Pcs", "Kg")
        /// </summary>
        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Number of decimal places for this unit
        /// </summary>
        [Range(0, 6, ErrorMessage = "Decimal places must be between 0 and 6")]
        public int DecimalPlaces { get; set; } = 2;

        /// <summary>
        /// Tally Unit GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyUnitGuid { get; set; }

        /// <summary>
        /// Tally Unit Name for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally Name cannot exceed 100 characters")]
        public string? TallyUnitName { get; set; }
    }
}
