using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Stock Batch
    /// </summary>
    public class CreateStockBatchDto
    {
        /// <summary>
        /// Stock item ID
        /// </summary>
        [Required(ErrorMessage = "Stock item is required")]
        public Guid StockItemId { get; set; }

        /// <summary>
        /// Warehouse ID
        /// </summary>
        [Required(ErrorMessage = "Warehouse is required")]
        public Guid WarehouseId { get; set; }

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
        /// Initial quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Initial value
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Tally Batch GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyBatchGuid { get; set; }
    }
}
