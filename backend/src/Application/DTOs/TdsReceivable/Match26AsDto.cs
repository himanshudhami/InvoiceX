using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TdsReceivable
{
    /// <summary>
    /// DTO for matching TDS entry with Form 26AS
    /// </summary>
    public class Match26AsDto
    {
        /// <summary>
        /// Amount as per Form 26AS
        /// </summary>
        [Required(ErrorMessage = "Form 26AS amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Form 26AS amount must be non-negative")]
        public decimal Form26AsAmount { get; set; }
    }
}
