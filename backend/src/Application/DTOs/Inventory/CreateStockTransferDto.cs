using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Stock Transfer
    /// </summary>
    public class CreateStockTransferDto
    {
        /// <summary>
        /// Company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Transfer number (auto-generated if not provided)
        /// </summary>
        [StringLength(100, ErrorMessage = "Transfer number cannot exceed 100 characters")]
        public string? TransferNumber { get; set; }

        /// <summary>
        /// Transfer date
        /// </summary>
        [Required(ErrorMessage = "Transfer date is required")]
        public DateOnly TransferDate { get; set; }

        /// <summary>
        /// Source warehouse ID
        /// </summary>
        [Required(ErrorMessage = "Source warehouse is required")]
        public Guid FromWarehouseId { get; set; }

        /// <summary>
        /// Destination warehouse ID
        /// </summary>
        [Required(ErrorMessage = "Destination warehouse is required")]
        public Guid ToWarehouseId { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Tally Voucher GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyVoucherGuid { get; set; }

        /// <summary>
        /// User creating the transfer
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Transfer line items
        /// </summary>
        [Required(ErrorMessage = "At least one item is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<StockTransferItemDto> Items { get; set; } = new();
    }
}
