using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeSalaryTransactions
{
    /// <summary>
    /// Data transfer object for bulk upload of Employee Salary Transactions
    /// </summary>
    public class BulkEmployeeSalaryTransactionsDto
    {
        /// <summary>
        /// List of salary transactions to create
        /// </summary>
        [Required(ErrorMessage = "Salary transactions list is required")]
        [MinLength(1, ErrorMessage = "At least one salary transaction is required")]
        public List<CreateEmployeeSalaryTransactionsDto> SalaryTransactions { get; set; } = new();

        /// <summary>
        /// Whether to skip validation errors and process valid records
        /// </summary>
        public bool SkipValidationErrors { get; set; } = false;

        /// <summary>
        /// Whether to overwrite existing salary records for the same employee and month
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// Created By
        /// </summary>
        [StringLength(255, ErrorMessage = "Created by cannot exceed 255 characters")]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Result of bulk upload operation
    /// </summary>
    public class BulkUploadResultDto
    {
        /// <summary>
        /// Number of records successfully processed
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of records that failed processing
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Total number of records in the upload
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// List of validation errors and which rows they occurred on
        /// </summary>
        public List<BulkUploadErrorDto> Errors { get; set; } = new();

        /// <summary>
        /// List of successfully created salary transaction IDs
        /// </summary>
        public List<Guid> CreatedIds { get; set; } = new();
    }

    /// <summary>
    /// Error details for bulk upload
    /// </summary>
    public class BulkUploadErrorDto
    {
        /// <summary>
        /// Row number where the error occurred (1-based)
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Employee name or identifier for reference
        /// </summary>
        public string? EmployeeReference { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Field name where the error occurred (if applicable)
        /// </summary>
        public string? FieldName { get; set; }
    }
}