using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Stock Item
    /// </summary>
    public class CreateStockItemDto
    {
        /// <summary>
        /// Company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Stock item name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stock Keeping Unit (SKU)
        /// </summary>
        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string? Sku { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Stock group ID
        /// </summary>
        public Guid? StockGroupId { get; set; }

        /// <summary>
        /// Base unit of measure ID
        /// </summary>
        [Required(ErrorMessage = "Base unit is required")]
        public Guid BaseUnitId { get; set; }

        /// <summary>
        /// HSN/SAC Code for GST
        /// </summary>
        [StringLength(20, ErrorMessage = "HSN/SAC Code cannot exceed 20 characters")]
        public string? HsnSacCode { get; set; }

        /// <summary>
        /// GST Rate percentage
        /// </summary>
        [Range(0, 100, ErrorMessage = "GST rate must be between 0 and 100")]
        public decimal GstRate { get; set; } = 18;

        /// <summary>
        /// Opening quantity
        /// </summary>
        public decimal OpeningQuantity { get; set; }

        /// <summary>
        /// Opening value
        /// </summary>
        public decimal OpeningValue { get; set; }

        /// <summary>
        /// Reorder level (triggers low stock alert)
        /// </summary>
        public decimal? ReorderLevel { get; set; }

        /// <summary>
        /// Reorder quantity (suggested quantity to order)
        /// </summary>
        public decimal? ReorderQuantity { get; set; }

        /// <summary>
        /// Whether batch/lot tracking is enabled
        /// </summary>
        public bool IsBatchEnabled { get; set; }

        /// <summary>
        /// Valuation method: fifo, lifo, weighted_avg
        /// </summary>
        [StringLength(20, ErrorMessage = "Valuation method cannot exceed 20 characters")]
        public string ValuationMethod { get; set; } = "weighted_avg";

        /// <summary>
        /// Whether the stock item is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tally Stock Item GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyStockItemGuid { get; set; }

        /// <summary>
        /// Tally Stock Item Name for migration
        /// </summary>
        [StringLength(255, ErrorMessage = "Tally Name cannot exceed 255 characters")]
        public string? TallyStockItemName { get; set; }

        /// <summary>
        /// Unit conversions (alternate units)
        /// </summary>
        public List<UnitConversionDto>? UnitConversions { get; set; }
    }
}
