using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Warehouse
    /// </summary>
    public class CreateWarehouseDto
    {
        /// <summary>
        /// Company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Warehouse name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Warehouse code
        /// </summary>
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string? Code { get; set; }

        /// <summary>
        /// Street address
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string? State { get; set; }

        /// <summary>
        /// PIN/Zip Code
        /// </summary>
        [StringLength(20, ErrorMessage = "PIN Code cannot exceed 20 characters")]
        public string? PinCode { get; set; }

        /// <summary>
        /// Whether this is the default warehouse
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Parent warehouse ID for hierarchy
        /// </summary>
        public Guid? ParentWarehouseId { get; set; }

        /// <summary>
        /// Whether the warehouse is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tally Godown GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyGodownGuid { get; set; }

        /// <summary>
        /// Tally Godown Name for migration
        /// </summary>
        [StringLength(255, ErrorMessage = "Tally Name cannot exceed 255 characters")]
        public string? TallyGodownName { get; set; }
    }
}
