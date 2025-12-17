namespace Core.Entities.Leave
{
    /// <summary>
    /// Leave balance per employee per leave type per financial year
    /// </summary>
    public class EmployeeLeaveBalance
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }

        /// <summary>
        /// Financial year (e.g., '2024-25')
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Balance at start of year
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Leaves accrued during the year (for monthly accrual)
        /// </summary>
        public decimal Accrued { get; set; }

        /// <summary>
        /// Leaves taken/approved
        /// </summary>
        public decimal Taken { get; set; }

        /// <summary>
        /// Leaves carried forward from previous year
        /// </summary>
        public decimal CarryForwarded { get; set; }

        /// <summary>
        /// Manual adjustments (+/-)
        /// </summary>
        public decimal Adjusted { get; set; }

        /// <summary>
        /// Leaves encashed
        /// </summary>
        public decimal Encashed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Calculated properties

        /// <summary>
        /// Total available balance
        /// </summary>
        public decimal AvailableBalance =>
            OpeningBalance + Accrued + CarryForwarded + Adjusted - Taken - Encashed;

        /// <summary>
        /// Total credited (opening + accrued + carry forward + adjustments)
        /// </summary>
        public decimal TotalCredited =>
            OpeningBalance + Accrued + CarryForwarded + Adjusted;

        /// <summary>
        /// Total utilized (taken + encashed)
        /// </summary>
        public decimal TotalUtilized => Taken + Encashed;

        // Navigation properties
        public Employees? Employee { get; set; }
        public LeaveType? LeaveType { get; set; }
    }
}
