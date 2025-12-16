using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Employees
{
    /// <summary>
    /// Data transfer object for rejoining a resigned employee
    /// </summary>
    public class RejoinEmployeeDto
    {
        /// <summary>
        /// Optional new joining date (defaults to original joining date if not provided)
        /// </summary>
        public DateTime? RejoiningDate { get; set; }
    }
}
