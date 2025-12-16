using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Invoices
{
    /// <summary>
    /// Data transfer object for creating Invoices
    /// </summary>
    public class CreateInvoicesDto
    {
/// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }
/// <summary>
        /// CustomerId
        /// </summary>
        public Guid? CustomerId { get; set; }
/// <summary>
        /// InvoiceNumber
        /// </summary>
        [Required(ErrorMessage = "InvoiceNumber is required")]
        [StringLength(255, ErrorMessage = "InvoiceNumber cannot exceed 255 characters")]
        public string InvoiceNumber { get; set; } = string.Empty;
/// <summary>
        /// InvoiceDate
        /// </summary>
        public DateOnly InvoiceDate { get; set; }
/// <summary>
        /// DueDate
        /// </summary>
        public DateOnly DueDate { get; set; }
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
        /// TaxAmount
        /// </summary>
        public decimal? TaxAmount { get; set; }
/// <summary>
        /// DiscountAmount
        /// </summary>
        public decimal? DiscountAmount { get; set; }
/// <summary>
        /// TotalAmount
        /// </summary>
        [Required(ErrorMessage = "TotalAmount is required")]
        public decimal TotalAmount { get; set; }
/// <summary>
        /// PaidAmount
        /// </summary>
        public decimal? PaidAmount { get; set; }
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
        /// PaidAt
        /// </summary>
        public DateTime? PaidAt { get; set; }
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