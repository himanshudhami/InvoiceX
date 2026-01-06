using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for updating a Stock Movement
    /// </summary>
    public class UpdateStockMovementDto
    {
        /// <summary>
        /// Movement date
        /// </summary>
        [Required(ErrorMessage = "Movement date is required")]
        public DateOnly MovementDate { get; set; }

        /// <summary>
        /// Movement type: purchase, sale, transfer_in, transfer_out, adjustment, opening
        /// </summary>
        [Required(ErrorMessage = "Movement type is required")]
        [StringLength(50, ErrorMessage = "Movement type cannot exceed 50 characters")]
        public string MovementType { get; set; } = string.Empty;

        /// <summary>
        /// Quantity (positive for in, negative for out)
        /// </summary>
        [Required(ErrorMessage = "Quantity is required")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Rate per unit
        /// </summary>
        public decimal? Rate { get; set; }

        /// <summary>
        /// Total value
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// Source document type
        /// </summary>
        [StringLength(50, ErrorMessage = "Source type cannot exceed 50 characters")]
        public string? SourceType { get; set; }

        /// <summary>
        /// Source document ID
        /// </summary>
        public Guid? SourceId { get; set; }

        /// <summary>
        /// Source document number
        /// </summary>
        [StringLength(100, ErrorMessage = "Source number cannot exceed 100 characters")]
        public string? SourceNumber { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }
    }
}
