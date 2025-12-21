using Application.DTOs.Forex;
using Core.Common;
using Core.Entities.Forex;

namespace Application.Interfaces.Forex
{
    /// <summary>
    /// Service interface for LUT (Letter of Undertaking) management
    /// LUT is required for zero-rated export supplies under GST
    /// </summary>
    public interface ILutService
    {
        // ==================== CRUD Operations ====================

        /// <summary>
        /// Get LUT by ID
        /// </summary>
        Task<Result<LutRegister>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get paginated LUTs with filtering
        /// </summary>
        Task<Result<(IEnumerable<LutRegister> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? financialYear = null,
            string? status = null);

        /// <summary>
        /// Create a new LUT entry
        /// </summary>
        Task<Result<LutRegister>> CreateAsync(CreateLutDto dto);

        /// <summary>
        /// Update an existing LUT
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateLutDto dto);

        /// <summary>
        /// Delete a LUT (soft delete - marks as cancelled)
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        // ==================== Validation ====================

        /// <summary>
        /// Check if a valid LUT exists for a company on a given date
        /// </summary>
        Task<Result<LutValidationResultDto>> ValidateLutForInvoiceAsync(Guid companyId, DateOnly invoiceDate);

        /// <summary>
        /// Get the active LUT for a company in a financial year
        /// </summary>
        Task<Result<LutRegister>> GetActiveAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get valid LUT for a specific date
        /// </summary>
        Task<Result<LutRegister>> GetValidForDateAsync(Guid companyId, DateOnly date);

        // ==================== Status Management ====================

        /// <summary>
        /// Expire LUTs that have passed their validity date
        /// </summary>
        Task<Result<int>> ExpireOldLutsAsync();

        /// <summary>
        /// Supersede an existing LUT with a new one (renewal)
        /// </summary>
        Task<Result<LutRegister>> RenewLutAsync(Guid existingLutId, CreateLutDto newLutDto);

        /// <summary>
        /// Cancel a LUT
        /// </summary>
        Task<Result> CancelLutAsync(Guid id, string? reason);

        // ==================== Reporting ====================

        /// <summary>
        /// Get LUT utilization report (invoices raised under each LUT)
        /// </summary>
        Task<Result<LutUtilizationReportDto>> GetUtilizationReportAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get LUT compliance summary
        /// </summary>
        Task<Result<LutComplianceSummaryDto>> GetComplianceSummaryAsync(Guid companyId);

        /// <summary>
        /// Get upcoming LUT expiries
        /// </summary>
        Task<Result<IEnumerable<LutExpiryAlertDto>>> GetExpiryAlertsAsync(
            Guid? companyId = null,
            int daysBeforeExpiry = 30);
    }
}
