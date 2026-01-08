using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TdsReceivable
{
    /// <summary>
    /// DTO for updating a TDS Receivable entry
    /// </summary>
    public class UpdateTdsReceivableDto
    {
        /// <summary>
        /// Financial year (e.g., '2024-25')
        /// </summary>
        [StringLength(10, ErrorMessage = "Financial year cannot exceed 10 characters")]
        public string? FinancialYear { get; set; }

        /// <summary>
        /// Quarter (Q1, Q2, Q3, Q4)
        /// </summary>
        [StringLength(5, ErrorMessage = "Quarter cannot exceed 5 characters")]
        public string? Quarter { get; set; }

        /// <summary>
        /// Customer ID
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Name of the deductor
        /// </summary>
        [StringLength(255, ErrorMessage = "Deductor name cannot exceed 255 characters")]
        public string? DeductorName { get; set; }

        /// <summary>
        /// TAN of the deductor
        /// </summary>
        [StringLength(20, ErrorMessage = "TAN cannot exceed 20 characters")]
        public string? DeductorTan { get; set; }

        /// <summary>
        /// PAN of the deductor
        /// </summary>
        [StringLength(15, ErrorMessage = "PAN cannot exceed 15 characters")]
        public string? DeductorPan { get; set; }

        /// <summary>
        /// Payment date
        /// </summary>
        public DateOnly? PaymentDate { get; set; }

        /// <summary>
        /// TDS section (194J, 194C, etc.)
        /// </summary>
        [StringLength(20, ErrorMessage = "TDS section cannot exceed 20 characters")]
        public string? TdsSection { get; set; }

        /// <summary>
        /// Gross amount before TDS
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Gross amount must be positive")]
        public decimal? GrossAmount { get; set; }

        /// <summary>
        /// TDS rate (percentage)
        /// </summary>
        [Range(0, 100, ErrorMessage = "TDS rate must be between 0 and 100")]
        public decimal? TdsRate { get; set; }

        /// <summary>
        /// TDS amount deducted
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "TDS amount must be non-negative")]
        public decimal? TdsAmount { get; set; }

        /// <summary>
        /// Net amount received
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Net received must be non-negative")]
        public decimal? NetReceived { get; set; }

        /// <summary>
        /// TDS certificate number
        /// </summary>
        [StringLength(100, ErrorMessage = "Certificate number cannot exceed 100 characters")]
        public string? CertificateNumber { get; set; }

        /// <summary>
        /// Certificate date
        /// </summary>
        public DateOnly? CertificateDate { get; set; }

        /// <summary>
        /// Whether certificate has been downloaded
        /// </summary>
        public bool? CertificateDownloaded { get; set; }

        /// <summary>
        /// Linked payment ID
        /// </summary>
        public Guid? PaymentId { get; set; }

        /// <summary>
        /// Linked invoice ID
        /// </summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string? Notes { get; set; }
    }
}
