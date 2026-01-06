using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for updating a Stock Batch
    /// </summary>
    public class UpdateStockBatchDto
    {
        /// <summary>
        /// Batch/Lot number
        /// </summary>
        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(100, ErrorMessage = "Batch number cannot exceed 100 characters")]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Manufacturing date
        /// </summary>
        public DateOnly? ManufacturingDate { get; set; }

        /// <summary>
        /// Expiry date
        /// </summary>
        public DateOnly? ExpiryDate { get; set; }

        /// <summary>
        /// Tally Batch GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyBatchGuid { get; set; }
    }
}
