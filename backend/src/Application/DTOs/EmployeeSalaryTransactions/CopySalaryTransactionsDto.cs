using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeSalaryTransactions
{
    /// <summary>
    /// Data transfer object for copying salary transactions from one period to another
    /// </summary>
    public class CopySalaryTransactionsDto
    {
        /// <summary>
        /// Source salary month (1-12)
        /// </summary>
        [Required]
        [Range(1, 12, ErrorMessage = "Source salary month must be between 1 and 12")]
        public int SourceMonth { get; set; }

        /// <summary>
        /// Source salary year
        /// </summary>
        [Required]
        [Range(2000, 2100, ErrorMessage = "Source salary year must be between 2000 and 2100")]
        public int SourceYear { get; set; }

        /// <summary>
        /// Target salary month (1-12)
        /// </summary>
        [Required]
        [Range(1, 12, ErrorMessage = "Target salary month must be between 1 and 12")]
        public int TargetMonth { get; set; }

        /// <summary>
        /// Target salary year
        /// </summary>
        [Required]
        [Range(2000, 2100, ErrorMessage = "Target salary year must be between 2000 and 2100")]
        public int TargetYear { get; set; }

        /// <summary>
        /// Company ID to filter transactions (optional - if not provided, copies all companies)
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// How to handle duplicates: 'skip' = skip existing, 'overwrite' = overwrite existing, 'skip_and_report' = skip and report
        /// </summary>
        [RegularExpression("^(skip|overwrite|skip_and_report)$", ErrorMessage = "Duplicate handling must be skip, overwrite, or skip_and_report")]
        public string DuplicateHandling { get; set; } = "skip";

        /// <summary>
        /// Whether to reset payment status and dates for copied transactions
        /// </summary>
        public bool ResetPaymentInfo { get; set; } = true;

        /// <summary>
        /// Created By
        /// </summary>
        [StringLength(255, ErrorMessage = "Created by cannot exceed 255 characters")]
        public string? CreatedBy { get; set; }
    }
}

