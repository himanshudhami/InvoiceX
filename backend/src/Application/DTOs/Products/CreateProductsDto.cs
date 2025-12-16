using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Products
{
    /// <summary>
    /// Data transfer object for creating Products
    /// </summary>
    public class CreateProductsDto
    {
/// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }
/// <summary>
        /// Name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;
/// <summary>
        /// Description
        /// </summary>
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
/// <summary>
        /// Sku
        /// </summary>
        [StringLength(255, ErrorMessage = "Sku cannot exceed 255 characters")]
        public string? Sku { get; set; }
/// <summary>
        /// Category
        /// </summary>
        [StringLength(255, ErrorMessage = "Category cannot exceed 255 characters")]
        public string? Category { get; set; }
/// <summary>
        /// Type
        /// </summary>
        [StringLength(255, ErrorMessage = "Type cannot exceed 255 characters")]
        public string? Type { get; set; }
/// <summary>
        /// UnitPrice
        /// </summary>
        [Required(ErrorMessage = "UnitPrice is required")]
        [Range(1, int.MaxValue, ErrorMessage = "UnitPrice must be greater than 0")]
        public decimal UnitPrice { get; set; }
/// <summary>
        /// Unit
        /// </summary>
        [StringLength(255, ErrorMessage = "Unit cannot exceed 255 characters")]
        public string? Unit { get; set; }
/// <summary>
        /// TaxRate
        /// </summary>
        public decimal? TaxRate { get; set; }
/// <summary>
        /// IsActive
        /// </summary>
        public bool? IsActive { get; set; }
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