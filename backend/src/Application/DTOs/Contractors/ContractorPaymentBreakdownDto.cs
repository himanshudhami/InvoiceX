namespace Application.DTOs.Contractors
{
    /// <summary>
    /// Summarizes total payments made to contractors
    /// </summary>
    public class ContractorPaymentBreakdownDto
    {
        /// <summary>
        /// Total net amount paid across all contractors
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Total gross amount before TDS
        /// </summary>
        public decimal TotalGross { get; set; }

        /// <summary>
        /// Total TDS deducted
        /// </summary>
        public decimal TotalTds { get; set; }

        /// <summary>
        /// Count of contractors who have received payments
        /// </summary>
        public int ContractorCount { get; set; }

        /// <summary>
        /// Count of total payments made
        /// </summary>
        public int PaymentCount { get; set; }

        /// <summary>
        /// List of per-contractor payment summaries
        /// </summary>
        public List<ContractorPaymentDetailDto> Contractors { get; set; } = new();
    }

    /// <summary>
    /// Payment summary for a single contractor
    /// </summary>
    public class ContractorPaymentDetailDto
    {
        /// <summary>
        /// Contractor/Party ID
        /// </summary>
        public Guid ContractorId { get; set; }

        /// <summary>
        /// Contractor name
        /// </summary>
        public string ContractorName { get; set; } = string.Empty;

        /// <summary>
        /// Total net amount paid to this contractor
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Total gross amount before TDS
        /// </summary>
        public decimal TotalGross { get; set; }

        /// <summary>
        /// Total TDS deducted
        /// </summary>
        public decimal TotalTds { get; set; }

        /// <summary>
        /// Number of payments made to this contractor
        /// </summary>
        public int PaymentCount { get; set; }

        /// <summary>
        /// Date of last payment
        /// </summary>
        public DateTime? LastPaymentDate { get; set; }
    }
}
