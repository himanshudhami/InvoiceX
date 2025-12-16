using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TdsReceivable
{
    /// <summary>
    /// DTO for updating TDS entry status
    /// </summary>
    public class UpdateStatusDto
    {
        /// <summary>
        /// New status (pending, matched, claimed, disputed, written_off)
        /// </summary>
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// ITR in which this was claimed (e.g., 'ITR-2024-25')
        /// </summary>
        [StringLength(50, ErrorMessage = "Claimed in return cannot exceed 50 characters")]
        public string? ClaimedInReturn { get; set; }
    }
}
