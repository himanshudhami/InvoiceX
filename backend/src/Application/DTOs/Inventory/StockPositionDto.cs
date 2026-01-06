namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for stock position/balance
    /// </summary>
    public class StockPositionDto
    {
        /// <summary>
        /// Stock item ID
        /// </summary>
        public Guid StockItemId { get; set; }

        /// <summary>
        /// Stock item name
        /// </summary>
        public string StockItemName { get; set; } = string.Empty;

        /// <summary>
        /// SKU
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Warehouse ID
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Warehouse name
        /// </summary>
        public string? WarehouseName { get; set; }

        /// <summary>
        /// Current quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Current value
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Average rate
        /// </summary>
        public decimal AverageRate => Quantity != 0 ? Value / Quantity : 0;

        /// <summary>
        /// Unit name
        /// </summary>
        public string? UnitName { get; set; }

        /// <summary>
        /// Unit symbol
        /// </summary>
        public string? UnitSymbol { get; set; }

        /// <summary>
        /// Reorder level
        /// </summary>
        public decimal? ReorderLevel { get; set; }

        /// <summary>
        /// Is below reorder level
        /// </summary>
        public bool IsBelowReorderLevel => ReorderLevel.HasValue && Quantity <= ReorderLevel.Value;
    }
}
