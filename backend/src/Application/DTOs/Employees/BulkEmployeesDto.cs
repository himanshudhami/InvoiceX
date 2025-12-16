using System.ComponentModel.DataAnnotations;
using Application.DTOs.Employees;

namespace Application.DTOs.EmployeesBulk
{
    /// <summary>
    /// Request payload for bulk employee creation.
    /// </summary>
    public class BulkEmployeesDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one employee is required")]
        public List<CreateEmployeesDto> Employees { get; set; } = new();

        /// <summary>
        /// Whether to keep processing valid rows when validation fails on a row.
        /// </summary>
        public bool SkipValidationErrors { get; set; } = false;

        /// <summary>
        /// Created by (optional audit field).
        /// </summary>
        [StringLength(255, ErrorMessage = "Created by cannot exceed 255 characters")]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Result payload for bulk employee creation.
    /// </summary>
    public class BulkEmployeesResultDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalCount { get; set; }
        public List<BulkEmployeesErrorDto> Errors { get; set; } = new();
        public List<Guid> CreatedIds { get; set; } = new();
    }

    /// <summary>
    /// Error details for a specific row.
    /// </summary>
    public class BulkEmployeesErrorDto
    {
        public int RowNumber { get; set; }
        public string? EmployeeReference { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? FieldName { get; set; }
    }
}
