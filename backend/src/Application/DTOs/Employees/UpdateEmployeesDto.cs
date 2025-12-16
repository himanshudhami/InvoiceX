using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Employees
{
    /// <summary>
    /// Data transfer object for updating Employees
    /// </summary>
    public class UpdateEmployeesDto
    {
        /// <summary>
        /// Employee Name
        /// </summary>
        [Required(ErrorMessage = "Employee name is required")]
        [StringLength(255, ErrorMessage = "Employee name cannot exceed 255 characters")]
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// Email address
        /// </summary>
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string? Email { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        public string? Phone { get; set; }

        /// <summary>
        /// Company-specific employee ID
        /// </summary>
        [StringLength(100, ErrorMessage = "Employee ID cannot exceed 100 characters")]
        public string? EmployeeId { get; set; }

        /// <summary>
        /// Department
        /// </summary>
        [StringLength(255, ErrorMessage = "Department cannot exceed 255 characters")]
        public string? Department { get; set; }

        /// <summary>
        /// Job designation/title
        /// </summary>
        [StringLength(255, ErrorMessage = "Designation cannot exceed 255 characters")]
        public string? Designation { get; set; }

        /// <summary>
        /// Date of hire
        /// </summary>
        public DateTime? HireDate { get; set; }

        /// <summary>
        /// Employee status (active, inactive, terminated, permanent)
        /// </summary>
        [Required]
        [RegularExpression("^(active|inactive|terminated|permanent)$", ErrorMessage = "Status must be active, inactive, terminated, or permanent")]
        public string Status { get; set; } = "active";

        /// <summary>
        /// Bank account number
        /// </summary>
        [StringLength(100, ErrorMessage = "Bank account number cannot exceed 100 characters")]
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// Bank name
        /// </summary>
        [StringLength(255, ErrorMessage = "Bank name cannot exceed 255 characters")]
        public string? BankName { get; set; }

        /// <summary>
        /// IFSC Code for Indian banks
        /// </summary>
        [StringLength(20, ErrorMessage = "IFSC code cannot exceed 20 characters")]
        public string? IfscCode { get; set; }

        /// <summary>
        /// PAN Number for Indian taxation
        /// </summary>
        [StringLength(20, ErrorMessage = "PAN number cannot exceed 20 characters")]
        public string? PanNumber { get; set; }

        /// <summary>
        /// Address line 1
        /// </summary>
        [StringLength(255, ErrorMessage = "Address line 1 cannot exceed 255 characters")]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Address line 2
        /// </summary>
        [StringLength(255, ErrorMessage = "Address line 2 cannot exceed 255 characters")]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string? State { get; set; }

        /// <summary>
        /// ZIP/Postal code
        /// </summary>
        [StringLength(20, ErrorMessage = "Zip code cannot exceed 20 characters")]
        public string? ZipCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; } = "India";

        /// <summary>
        /// Contract type (e.g., Permanent, Full, Contract)
        /// </summary>
        [StringLength(100, ErrorMessage = "Contract type cannot exceed 100 characters")]
        public string? ContractType { get; set; }

        /// <summary>
        /// Company name (legacy field, kept for backward compatibility)
        /// </summary>
        [StringLength(255, ErrorMessage = "Company cannot exceed 255 characters")]
        public string? Company { get; set; }

        /// <summary>
        /// Company ID (foreign key to companies table)
        /// </summary>
        public Guid? CompanyId { get; set; }
    }
}