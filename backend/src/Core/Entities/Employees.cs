namespace Core.Entities
{
    public class Employees
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public DateTime? HireDate { get; set; }
        public string Status { get; set; } = "active";
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? IfscCode { get; set; }
        public string? PanNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string Country { get; set; } = "India";
        public string? ContractType { get; set; }
        public string? Company { get; set; }
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Employment type: 'employee' or 'contractor'
        /// </summary>
        public string EmploymentType { get; set; } = "employee";

        /// <summary>
        /// Tally ledger GUID for migration tracking
        /// </summary>
        public string? TallyLedgerGuid { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Hierarchy fields
        public Guid? ManagerId { get; set; }
        public int ReportingLevel { get; set; } = 0;
        public bool IsManager { get; set; } = false;

        // Denormalized field for display (populated via JOIN, not stored in DB)
        public string? ManagerName { get; set; }

        // Navigation properties (for service layer use, not DB mapping)
        public Employees? Manager { get; set; }
        public ICollection<Employees>? DirectReports { get; set; }
    }
}