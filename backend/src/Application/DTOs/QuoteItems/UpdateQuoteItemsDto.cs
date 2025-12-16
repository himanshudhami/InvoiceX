using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.QuoteItems
{
    /// <summary>
    /// Data transfer object for updating QuoteItems
    /// </summary>
    public class UpdateQuoteItemsDto
    {
        /// <summary>
        /// Id
        /// </summary>
        [Required(ErrorMessage = "Id is required")]
        public Guid Id { get; set; }
        /// <summary>
        /// QuoteId
        /// </summary>
        public Guid? QuoteId { get; set; }
        /// <summary>
        /// ProductId
        /// </summary>
        public Guid? ProductId { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        [Required(ErrorMessage = "Description is required")]
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Quantity
        /// </summary>
        [Required(ErrorMessage = "Quantity is required")]
        public decimal Quantity { get; set; }
        /// <summary>
        /// UnitPrice
        /// </summary>
        [Required(ErrorMessage = "UnitPrice is required")]
        public decimal UnitPrice { get; set; }
        /// <summary>
        /// TaxRate
        /// </summary>
        public decimal? TaxRate { get; set; }
        /// <summary>
        /// DiscountRate
        /// </summary>
        public decimal? DiscountRate { get; set; }
        /// <summary>
        /// LineTotal
        /// </summary>
        [Required(ErrorMessage = "LineTotal is required")]
        public decimal LineTotal { get; set; }
        /// <summary>
        /// SortOrder
        /// </summary>
        public int? SortOrder { get; set; }
        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime? CreatedAt { get; set; }
        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
