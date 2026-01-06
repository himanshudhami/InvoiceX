namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for Stock Ledger report entry
    /// </summary>
    public class StockLedgerDto
    {
        /// <summary>
        /// Movement ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Movement date
        /// </summary>
        public DateOnly MovementDate { get; set; }

        /// <summary>
        /// Movement type
        /// </summary>
        public string MovementType { get; set; } = string.Empty;

        /// <summary>
        /// Source document type
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// Source document number
        /// </summary>
        public string? SourceNumber { get; set; }

        /// <summary>
        /// Warehouse name
        /// </summary>
        public string? WarehouseName { get; set; }

        /// <summary>
        /// Batch number
        /// </summary>
        public string? BatchNumber { get; set; }

        /// <summary>
        /// Quantity (in)
        /// </summary>
        public decimal? QuantityIn { get; set; }

        /// <summary>
        /// Quantity (out)
        /// </summary>
        public decimal? QuantityOut { get; set; }

        /// <summary>
        /// Rate per unit
        /// </summary>
        public decimal? Rate { get; set; }

        /// <summary>
        /// Value (in)
        /// </summary>
        public decimal? ValueIn { get; set; }

        /// <summary>
        /// Value (out)
        /// </summary>
        public decimal? ValueOut { get; set; }

        /// <summary>
        /// Running quantity balance
        /// </summary>
        public decimal RunningQuantity { get; set; }

        /// <summary>
        /// Running value balance
        /// </summary>
        public decimal RunningValue { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }
    }
}
