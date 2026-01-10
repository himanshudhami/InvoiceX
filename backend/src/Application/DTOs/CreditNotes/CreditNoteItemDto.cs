using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CreditNotes
{
    public class CreditNoteItemDto
    {
        public Guid? Id { get; set; }
        public Guid? OriginalInvoiceItemId { get; set; }
        public Guid? ProductId { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice cannot be negative")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "TaxRate must be between 0 and 100")]
        public decimal? TaxRate { get; set; }

        [Range(0, 100, ErrorMessage = "DiscountRate must be between 0 and 100")]
        public decimal? DiscountRate { get; set; }

        public decimal LineTotal { get; set; }
        public int? SortOrder { get; set; }

        // GST Compliance fields
        [StringLength(8, ErrorMessage = "HsnSacCode cannot exceed 8 characters")]
        public string? HsnSacCode { get; set; }

        public bool IsService { get; set; }

        [Range(0, 100, ErrorMessage = "CgstRate must be between 0 and 100")]
        public decimal? CgstRate { get; set; }
        public decimal? CgstAmount { get; set; }

        [Range(0, 100, ErrorMessage = "SgstRate must be between 0 and 100")]
        public decimal? SgstRate { get; set; }
        public decimal? SgstAmount { get; set; }

        [Range(0, 100, ErrorMessage = "IgstRate must be between 0 and 100")]
        public decimal? IgstRate { get; set; }
        public decimal? IgstAmount { get; set; }

        [Range(0, 100, ErrorMessage = "CessRate must be between 0 and 100")]
        public decimal? CessRate { get; set; }
        public decimal? CessAmount { get; set; }
    }
}
