using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for Stock Transfer Item
    /// </summary>
    public class StockTransferItemDto
    {
        /// <summary>
        /// Item ID (optional for create)
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Stock item ID
        /// </summary>
        [Required(ErrorMessage = "Stock item is required")]
        public Guid StockItemId { get; set; }

        /// <summary>
        /// Batch ID (optional, for batch-tracked items)
        /// </summary>
        public Guid? BatchId { get; set; }

        /// <summary>
        /// Quantity to transfer
        /// </summary>
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
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
        /// Received quantity (for partial receipts)
        /// </summary>
        public decimal? ReceivedQuantity { get; set; }
    }
}
