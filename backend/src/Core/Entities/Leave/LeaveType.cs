namespace Core.Entities.Leave
{
    /// <summary>
    /// Leave type configuration (CL, SL, EL, etc.)
    /// </summary>
    public class LeaveType
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForwardAllowed { get; set; }
        public decimal MaxCarryForwardDays { get; set; }
        public bool EncashmentAllowed { get; set; }
        public decimal MaxEncashmentDays { get; set; }
        public bool RequiresApproval { get; set; } = true;
        public int MinDaysNotice { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public bool IsActive { get; set; } = true;
        public string ColorCode { get; set; } = "#3B82F6";
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
