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
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}