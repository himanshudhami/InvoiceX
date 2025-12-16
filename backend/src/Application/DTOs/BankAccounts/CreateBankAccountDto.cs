using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankAccounts
{
    /// <summary>
    /// Data transfer object for creating a bank account
    /// </summary>
    public class CreateBankAccountDto
    {
        /// <summary>
        /// Company that owns this bank account
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Display name for the account (e.g., "HDFC Current Account")
        /// </summary>
        [Required(ErrorMessage = "Account name is required")]
        [StringLength(255, ErrorMessage = "Account name cannot exceed 255 characters")]
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Bank account number
        /// </summary>
        [Required(ErrorMessage = "Account number is required")]
        [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Name of the bank
        /// </summary>
        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(255, ErrorMessage = "Bank name cannot exceed 255 characters")]
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// IFSC code for Indian bank accounts
        /// </summary>
        [StringLength(20, ErrorMessage = "IFSC code cannot exceed 20 characters")]
        public string? IfscCode { get; set; }

        /// <summary>
        /// Branch name/location
        /// </summary>
        [StringLength(255, ErrorMessage = "Branch name cannot exceed 255 characters")]
        public string? BranchName { get; set; }

        /// <summary>
        /// Account type: current, savings, cc (cash credit), foreign
        /// </summary>
        [StringLength(50, ErrorMessage = "Account type cannot exceed 50 characters")]
        public string AccountType { get; set; } = "current";

        /// <summary>
        /// Currency of the account (default INR)
        /// </summary>
        [StringLength(10, ErrorMessage = "Currency code cannot exceed 10 characters")]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Opening balance when account was added to the system
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Current balance (usually same as opening balance when creating)
        /// </summary>
        public decimal? CurrentBalance { get; set; }

        /// <summary>
        /// Date of the balance snapshot
        /// </summary>
        public DateOnly? AsOfDate { get; set; }

        /// <summary>
        /// Whether this is the primary account for the company
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Additional notes about the account
        /// </summary>
        public string? Notes { get; set; }
    }
}
