namespace Core.Entities
{
    /// <summary>
    /// Junction table for many-to-many relationship between Party and Tags
    /// Used for party classification (Consultant, Contractor, etc.)
    /// </summary>
    public class PartyTag
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public Guid TagId { get; set; }

        // ==================== Source Tracking ====================

        /// <summary>
        /// How this tag was assigned
        /// Values: manual (user assigned), migration (from Tally import), rule (auto-assigned by rule), ai_suggested
        /// </summary>
        public string Source { get; set; } = "manual";

        /// <summary>
        /// Confidence score for AI-suggested tags (0-100)
        /// Only applicable when Source = 'ai_suggested'
        /// </summary>
        public int? ConfidenceScore { get; set; }

        // ==================== Audit ====================

        /// <summary>
        /// When this tag was assigned
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Parent Party entity
        /// </summary>
        public Party? Party { get; set; }

        /// <summary>
        /// Associated Tag entity
        /// </summary>
        public Tags.Tag? Tag { get; set; }
    }
}
