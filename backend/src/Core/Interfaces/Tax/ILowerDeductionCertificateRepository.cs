using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Repository interface for Lower Deduction Certificates (Form 13)
    /// </summary>
    public interface ILowerDeductionCertificateRepository
    {
        Task<LowerDeductionCertificate?> GetByIdAsync(Guid id);
        Task<IEnumerable<LowerDeductionCertificate>> GetAllAsync();
        Task<(IEnumerable<LowerDeductionCertificate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null);
        Task<LowerDeductionCertificate> AddAsync(LowerDeductionCertificate entity);
        Task UpdateAsync(LowerDeductionCertificate entity);
        Task DeleteAsync(Guid id);

        // ==================== Company Queries ====================

        /// <summary>
        /// Get all certificates for a company
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetByCompanyAsync(Guid companyId);

        /// <summary>
        /// Get active certificates for a company
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetActiveByCompanyAsync(Guid companyId);

        // ==================== Certificate Validation ====================

        /// <summary>
        /// Get valid certificate for a deductee, section, and date
        /// This is the primary method for TDS calculation
        /// </summary>
        Task<LowerDeductionCertificate?> GetValidCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate);

        /// <summary>
        /// Check if a valid certificate exists
        /// </summary>
        Task<bool> HasValidCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate);

        /// <summary>
        /// Validate certificate for a specific amount
        /// </summary>
        Task<LdcValidationResult> ValidateCertificateAsync(
            Guid companyId,
            string deducteePan,
            string tdsSection,
            DateOnly transactionDate,
            decimal amount);

        // ==================== Deductee Queries ====================

        /// <summary>
        /// Get certificates by deductee PAN
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetByDeducteePanAsync(Guid companyId, string deducteePan);

        /// <summary>
        /// Get certificates by deductee ID
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetByDeducteeIdAsync(Guid deducteeId);

        // ==================== Section Queries ====================

        /// <summary>
        /// Get certificates by TDS section
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetBySectionAsync(Guid companyId, string tdsSection);

        // ==================== Status Queries ====================

        /// <summary>
        /// Get certificates by status
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetByStatusAsync(Guid companyId, string status);

        /// <summary>
        /// Get expiring certificates (within n days)
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetExpiringAsync(Guid companyId, int daysAhead);

        /// <summary>
        /// Get exhausted certificates (threshold reached)
        /// </summary>
        Task<IEnumerable<LowerDeductionCertificate>> GetExhaustedAsync(Guid companyId);

        // ==================== Utilization ====================

        /// <summary>
        /// Update utilized amount
        /// </summary>
        Task UpdateUtilizedAmountAsync(Guid id, decimal additionalAmount);

        /// <summary>
        /// Record certificate usage (with audit trail)
        /// </summary>
        Task<Guid> RecordUsageAsync(LdcUsageRecord usage);

        /// <summary>
        /// Get usage history for a certificate
        /// </summary>
        Task<IEnumerable<LdcUsageRecord>> GetUsageHistoryAsync(Guid certificateId);

        // ==================== Status Updates ====================

        /// <summary>
        /// Update certificate status
        /// </summary>
        Task UpdateStatusAsync(Guid id, string status, string? reason = null);

        /// <summary>
        /// Revoke a certificate
        /// </summary>
        Task RevokeCertificateAsync(Guid id, string reason);
    }

    /// <summary>
    /// Certificate validation result
    /// </summary>
    public class LdcValidationResult
    {
        public bool IsValid { get; set; }
        public Guid? CertificateId { get; set; }
        public string? CertificateNumber { get; set; }
        public string? CertificateType { get; set; }
        public decimal NormalRate { get; set; }
        public decimal CertificateRate { get; set; }
        public decimal? RemainingThreshold { get; set; }
        public string? ValidationMessage { get; set; }

        /// <summary>
        /// Calculate TDS savings for a gross amount
        /// </summary>
        public decimal CalculateSavings(decimal grossAmount)
        {
            if (!IsValid) return 0;
            var normalTds = Math.Round(grossAmount * NormalRate / 100, 0);
            var certTds = Math.Round(grossAmount * CertificateRate / 100, 0);
            return normalTds - certTds;
        }
    }

    /// <summary>
    /// Certificate usage record for audit trail
    /// </summary>
    public class LdcUsageRecord
    {
        public Guid Id { get; set; }
        public Guid CertificateId { get; set; }
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public Guid? TransactionId { get; set; }
        public string? TransactionNumber { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NormalTdsAmount { get; set; }
        public decimal ActualTdsAmount { get; set; }
        public decimal TdsSavings { get; set; }
        public decimal CumulativeUtilized { get; set; }
        public decimal? RemainingThreshold { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}
