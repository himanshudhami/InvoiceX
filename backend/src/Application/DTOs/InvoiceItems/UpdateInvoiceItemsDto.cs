using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.InvoiceItems
{
    /// <summary>
    /// Data transfer object for updating InvoiceItems
    /// </summary>
    public class UpdateInvoiceItemsDto
    {
/// <summary>
        /// InvoiceId
        /// </summary>
        public Guid? InvoiceId { get; set; }
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

        // GST Compliance fields
        /// <summary>
        /// HSN code (goods) or SAC code (services) for GST
        /// </summary>
        [StringLength(20, ErrorMessage = "HSN/SAC code cannot exceed 20 characters")]
        public string? HsnSacCode { get; set; }

        /// <summary>
        /// True for SAC code (services), false for HSN code (goods)
        /// </summary>
        public bool? IsService { get; set; }

        /// <summary>
        /// Central GST rate percentage
        /// </summary>
        public decimal? CgstRate { get; set; }

        /// <summary>
        /// Central GST amount calculated
        /// </summary>
        public decimal? CgstAmount { get; set; }

        /// <summary>
        /// State GST rate percentage
        /// </summary>
        public decimal? SgstRate { get; set; }

        /// <summary>
        /// State GST amount calculated
        /// </summary>
        public decimal? SgstAmount { get; set; }

        /// <summary>
        /// Integrated GST rate percentage
        /// </summary>
        public decimal? IgstRate { get; set; }

        /// <summary>
        /// Integrated GST amount calculated
        /// </summary>
        public decimal? IgstAmount { get; set; }

        /// <summary>
        /// Cess rate percentage
        /// </summary>
        public decimal? CessRate { get; set; }

        /// <summary>
        /// Cess amount calculated
        /// </summary>
        public decimal? CessAmount { get; set; }
}
}