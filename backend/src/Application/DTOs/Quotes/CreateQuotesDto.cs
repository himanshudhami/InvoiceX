using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Quotes
{
    /// <summary>
    /// Data transfer object for creating Quotes
    /// </summary>
    public class CreateQuotesDto
    {
        /// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }
        /// <summary>
        /// PartyId
        /// </summary>
        public Guid? PartyId { get; set; }
        /// <summary>
        /// QuoteNumber
        /// </summary>
        [Required(ErrorMessage = "QuoteNumber is required")]
        [StringLength(255, ErrorMessage = "QuoteNumber cannot exceed 255 characters")]
        public string QuoteNumber { get; set; } = string.Empty;
        /// <summary>
        /// QuoteDate
        /// </summary>
        public DateOnly QuoteDate { get; set; }
        /// <summary>
        /// ValidUntil
        /// </summary>
        public DateOnly? ValidUntil { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        [StringLength(255, ErrorMessage = "Status cannot exceed 255 characters")]
        public string? Status { get; set; }
        /// <summary>
        /// Subtotal
        /// </summary>
        [Required(ErrorMessage = "Subtotal is required")]
        public decimal Subtotal { get; set; }
        /// <summary>
        /// DiscountAmount
        /// </summary>
        public decimal? DiscountAmount { get; set; }
        /// <summary>
        /// TaxAmount
        /// </summary>
        public decimal? TaxAmount { get; set; }
        /// <summary>
        /// TotalAmount
        /// </summary>
        [Required(ErrorMessage = "TotalAmount is required")]
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// Currency
        /// </summary>
        [StringLength(255, ErrorMessage = "Currency cannot exceed 255 characters")]
        public string? Currency { get; set; }
        /// <summary>
        /// Notes
        /// </summary>
        [StringLength(255, ErrorMessage = "Notes cannot exceed 255 characters")]
        public string? Notes { get; set; }
        /// <summary>
        /// Terms
        /// </summary>
        [StringLength(255, ErrorMessage = "Terms cannot exceed 255 characters")]
        public string? Terms { get; set; }
    }
}
