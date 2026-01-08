using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Quotes
{
    /// <summary>
    /// Data transfer object for updating Quotes
    /// </summary>
    public class UpdateQuotesDto
    {
        /// <summary>
        /// Id
        /// </summary>
        [Required(ErrorMessage = "Id is required")]
        public Guid Id { get; set; }
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
        /// ExpiryDate
        /// </summary>
        public DateOnly ExpiryDate { get; set; }
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
        /// DiscountType
        /// </summary>
        [StringLength(255, ErrorMessage = "DiscountType cannot exceed 255 characters")]
        public string? DiscountType { get; set; }
        /// <summary>
        /// DiscountValue
        /// </summary>
        public decimal? DiscountValue { get; set; }
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
        /// <summary>
        /// PaymentInstructions
        /// </summary>
        public string? PaymentInstructions { get; set; }
        /// <summary>
        /// PoNumber
        /// </summary>
        [StringLength(255, ErrorMessage = "PoNumber cannot exceed 255 characters")]
        public string? PoNumber { get; set; }
        /// <summary>
        /// ProjectName
        /// </summary>
        [StringLength(255, ErrorMessage = "ProjectName cannot exceed 255 characters")]
        public string? ProjectName { get; set; }
        /// <summary>
        /// SentAt
        /// </summary>
        public DateTime? SentAt { get; set; }
        /// <summary>
        /// ViewedAt
        /// </summary>
        public DateTime? ViewedAt { get; set; }
        /// <summary>
        /// AcceptedAt
        /// </summary>
        public DateTime? AcceptedAt { get; set; }
        /// <summary>
        /// RejectedAt
        /// </summary>
        public DateTime? RejectedAt { get; set; }
        /// <summary>
        /// RejectedReason
        /// </summary>
        [StringLength(255, ErrorMessage = "RejectedReason cannot exceed 255 characters")]
        public string? RejectedReason { get; set; }
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
