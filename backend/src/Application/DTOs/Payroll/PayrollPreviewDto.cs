namespace Application.DTOs.Payroll
{
    /// <summary>
    /// Preview of payroll calculation before processing
    /// </summary>
    public class PayrollPreviewDto
    {
        public Guid CompanyId { get; set; }
        public int PayrollMonth { get; set; }
        public int PayrollYear { get; set; }

        /// <summary>
        /// Number of employees with active salary structures
        /// </summary>
        public int EmployeeCount { get; set; }

        /// <summary>
        /// Total monthly gross salary for all employees
        /// </summary>
        public decimal TotalMonthlyGross { get; set; }

        /// <summary>
        /// Total PF employee contribution (estimated)
        /// </summary>
        public decimal TotalPfEmployee { get; set; }

        /// <summary>
        /// Total PF employer contribution
        /// </summary>
        public decimal TotalPfEmployer { get; set; }

        /// <summary>
        /// Total ESI employee contribution (estimated)
        /// </summary>
        public decimal TotalEsiEmployee { get; set; }

        /// <summary>
        /// Total ESI employer contribution
        /// </summary>
        public decimal TotalEsiEmployer { get; set; }

        /// <summary>
        /// Total Professional Tax (estimated)
        /// </summary>
        public decimal TotalPt { get; set; }

        /// <summary>
        /// Total TDS (estimated - actual depends on tax declarations)
        /// </summary>
        public decimal TotalTds { get; set; }

        /// <summary>
        /// Total deductions (PF + ESI + PT + TDS)
        /// </summary>
        public decimal TotalDeductions { get; set; }

        /// <summary>
        /// Total net pay (gross - deductions)
        /// </summary>
        public decimal TotalNetPay { get; set; }

        /// <summary>
        /// List of employees without salary structures (will be skipped)
        /// </summary>
        public List<string> EmployeesWithoutStructure { get; set; } = new();
    }
}
