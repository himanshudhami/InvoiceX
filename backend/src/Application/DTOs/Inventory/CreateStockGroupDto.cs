using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    /// <summary>
    /// Data transfer object for creating a Stock Group
    /// </summary>
    public class CreateStockGroupDto
    {
        /// <summary>
        /// Company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Stock group name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parent stock group ID for hierarchy
        /// </summary>
        public Guid? ParentStockGroupId { get; set; }

        /// <summary>
        /// Whether the stock group is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tally Stock Group GUID for migration
        /// </summary>
        [StringLength(100, ErrorMessage = "Tally GUID cannot exceed 100 characters")]
        public string? TallyStockGroupGuid { get; set; }

        /// <summary>
        /// Tally Stock Group Name for migration
        /// </summary>
        [StringLength(255, ErrorMessage = "Tally Name cannot exceed 255 characters")]
        public string? TallyStockGroupName { get; set; }
    }
}
