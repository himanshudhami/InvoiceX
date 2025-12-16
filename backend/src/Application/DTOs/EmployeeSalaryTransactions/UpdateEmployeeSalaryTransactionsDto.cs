using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeSalaryTransactions
{
    /// <summary>
    /// Data transfer object for updating Employee Salary Transactions
    /// </summary>
    public class UpdateEmployeeSalaryTransactionsDto
    {
        /// <summary>
        /// Basic Salary
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Basic salary must be non-negative")]
        public decimal BasicSalary { get; set; }

        /// <summary>
        /// House Rent Allowance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "HRA must be non-negative")]
        public decimal Hra { get; set; }

        /// <summary>
        /// Conveyance Allowance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Conveyance must be non-negative")]
        public decimal Conveyance { get; set; }

        /// <summary>
        /// Medical Allowance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Medical allowance must be non-negative")]
        public decimal MedicalAllowance { get; set; }

        /// <summary>
        /// Special Allowance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Special allowance must be non-negative")]
        public decimal SpecialAllowance { get; set; }

        /// <summary>
        /// Leave Travel Allowance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "LTA must be non-negative")]
        public decimal Lta { get; set; }

        /// <summary>
        /// Other Allowances
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Other allowances must be non-negative")]
        public decimal OtherAllowances { get; set; }

        /// <summary>
        /// Gross Salary (calculated automatically)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Gross salary must be non-negative")]
        public decimal GrossSalary { get; set; }

        /// <summary>
        /// Provident Fund - Employee Contribution
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "PF Employee must be non-negative")]
        public decimal PfEmployee { get; set; }

        /// <summary>
        /// Provident Fund - Employer Contribution
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "PF Employer must be non-negative")]
        public decimal PfEmployer { get; set; }

        /// <summary>
        /// Professional Tax
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "PT must be non-negative")]
        public decimal Pt { get; set; }

        /// <summary>
        /// Income Tax
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Income tax must be non-negative")]
        public decimal IncomeTax { get; set; }

        /// <summary>
        /// Other Deductions
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Other deductions must be non-negative")]
        public decimal OtherDeductions { get; set; }

        /// <summary>
        /// Net Salary (calculated automatically)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Net salary must be non-negative")]
        public decimal NetSalary { get; set; }

        /// <summary>
        /// Payment Date
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Payment Method
        /// </summary>
        [RegularExpression("^(bank_transfer|cash|check)$", ErrorMessage = "Payment method must be bank_transfer, cash, or check")]
        public string PaymentMethod { get; set; } = "bank_transfer";

        /// <summary>
        /// Payment Reference (transaction ID, check number, etc.)
        /// </summary>
        [StringLength(255, ErrorMessage = "Payment reference cannot exceed 255 characters")]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [RegularExpression("^(pending|processed|paid|cancelled)$", ErrorMessage = "Status must be pending, processed, paid, or cancelled")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Remarks/Notes
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Transaction Type (salary, consulting, bonus, reimbursement, gift)
        /// </summary>
        [RegularExpression("^(salary|consulting|bonus|reimbursement|gift)$", ErrorMessage = "Transaction type must be salary, consulting, bonus, reimbursement, or gift")]
        public string? TransactionType { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [StringLength(255, ErrorMessage = "Updated by cannot exceed 255 characters")]
        public string? UpdatedBy { get; set; }
    }
}