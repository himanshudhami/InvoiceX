using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Employees
{
    /// <summary>
    /// Data transfer object for resigning an employee
    /// </summary>
    public class ResignEmployeeDto
    {
        /// <summary>
        /// The employee's last working day
        /// </summary>
        [Required(ErrorMessage = "Last working day is required")]
        public DateTime LastWorkingDay { get; set; }

        /// <summary>
        /// Optional reason for resignation
        /// </summary>
        [StringLength(1000, ErrorMessage = "Resignation reason cannot exceed 1000 characters")]
        public string? ResignationReason { get; set; }
    }
}
