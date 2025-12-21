using Application.DTOs.Reports;
using Core.Common;

namespace Application.Interfaces.Reports
{
    /// <summary>
    /// Service interface for export-related reports and dashboards
    /// Covers receivables ageing, forex, FEMA compliance, and realization tracking
    /// </summary>
    public interface IExportReportingService
    {
        // ==================== Receivables Ageing ====================

        /// <summary>
        /// Get export receivables ageing report (in foreign currency + INR)
        /// </summary>
        Task<Result<ExportReceivablesAgeingReportDto>> GetReceivablesAgeingAsync(
            Guid companyId,
            DateOnly? asOfDate = null);

        /// <summary>
        /// Get customer-wise export receivables
        /// </summary>
        Task<Result<IEnumerable<CustomerExportReceivableDto>>> GetCustomerWiseReceivablesAsync(
            Guid companyId,
            DateOnly? asOfDate = null);

        // ==================== Forex Reports ====================

        /// <summary>
        /// Get forex gain/loss report (realized + unrealized)
        /// </summary>
        Task<Result<ForexGainLossReportDto>> GetForexGainLossReportAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Get unrealized forex position (open receivables at current rates)
        /// </summary>
        Task<Result<UnrealizedForexPositionDto>> GetUnrealizedForexPositionAsync(
            Guid companyId,
            DateOnly asOfDate,
            decimal currentExchangeRate);

        // ==================== FEMA Compliance Dashboard ====================

        /// <summary>
        /// Get FEMA compliance dashboard data
        /// </summary>
        Task<Result<FemaComplianceDashboardDto>> GetFemaComplianceDashboardAsync(Guid companyId);

        /// <summary>
        /// Get FEMA violation alerts
        /// </summary>
        Task<Result<IEnumerable<FemaViolationAlertDto>>> GetFemaViolationAlertsAsync(Guid companyId);

        // ==================== Export Realization ====================

        /// <summary>
        /// Get export realization tracking report
        /// </summary>
        Task<Result<ExportRealizationReportDto>> GetExportRealizationReportAsync(
            Guid companyId,
            string? financialYear = null);

        /// <summary>
        /// Get monthly export realization trend
        /// </summary>
        Task<Result<IEnumerable<MonthlyRealizationTrendDto>>> GetRealizationTrendAsync(
            Guid companyId,
            int months = 12);

        // ==================== Combined Export Dashboard ====================

        /// <summary>
        /// Get comprehensive export dashboard with all key metrics
        /// </summary>
        Task<Result<ExportDashboardDto>> GetExportDashboardAsync(Guid companyId);
    }
}
