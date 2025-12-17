namespace Core.Entities.Leave
{
    /// <summary>
    /// Company holiday calendar entry
    /// </summary>
    public class Holiday
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Year { get; set; }

        /// <summary>
        /// Whether employees can work and get substitute leave
        /// </summary>
        public bool IsOptional { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
