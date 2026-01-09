namespace Core.Entities
{
    public class EmployeeSalaryTransactions
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? CompanyId { get; set; }
        public int SalaryMonth { get; set; }
        public int SalaryYear { get; set; }
        
        // Salary Components
        public decimal BasicSalary { get; set; }
        public decimal Hra { get; set; }
        public decimal Conveyance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal Lta { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal GrossSalary { get; set; }
        
        // Deductions
        public decimal PfEmployee { get; set; }
        public decimal PfEmployer { get; set; }
        public decimal Pt { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal OtherDeductions { get; set; }
        
        // Final amounts
        public decimal NetSalary { get; set; }
        
        // Payment details
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "bank_transfer";
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = "pending";
        public Guid? BankTransactionId { get; set; }
        public DateTime? ReconciledAt { get; set; }
        public string? ReconciledBy { get; set; }
        
        // Additional fields
        public string? Remarks { get; set; }
        public string Currency { get; set; } = "INR";
        
        // Transaction type: salary, consulting, bonus, reimbursement, gift
        public string TransactionType { get; set; } = "salary";
        
        // Audit fields
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        
        // Navigation property
        public Employees? Employee { get; set; }
    }
}
