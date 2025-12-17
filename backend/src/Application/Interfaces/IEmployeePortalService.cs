using Application.DTOs.Portal;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Employee Portal operations.
    /// All operations are scoped to the authenticated employee.
    /// </summary>
    public interface IEmployeePortalService
    {
        // ==================== Profile ====================

        /// <summary>
        /// Get the authenticated employee's profile
        /// </summary>
        Task<Result<EmployeeProfileDto>> GetMyProfileAsync(Guid employeeId);

        // ==================== Dashboard ====================

        /// <summary>
        /// Get dashboard data for the employee portal
        /// </summary>
        Task<Result<PortalDashboardDto>> GetDashboardAsync(Guid employeeId);

        // ==================== Payslips ====================

        /// <summary>
        /// Get all payslips for the employee
        /// </summary>
        Task<Result<IEnumerable<PayslipSummaryDto>>> GetMyPayslipsAsync(Guid employeeId, int? year = null);

        /// <summary>
        /// Get a specific payslip detail
        /// </summary>
        Task<Result<PayslipDetailDto>> GetPayslipDetailAsync(Guid employeeId, Guid payslipId);

        /// <summary>
        /// Get payslip by month and year
        /// </summary>
        Task<Result<PayslipDetailDto>> GetPayslipByMonthAsync(Guid employeeId, int month, int year);

        // ==================== Assets ====================

        /// <summary>
        /// Get all assets currently assigned to the employee
        /// </summary>
        Task<Result<IEnumerable<MyAssetDto>>> GetMyAssetsAsync(Guid employeeId);

        /// <summary>
        /// Get asset assignment history for the employee
        /// </summary>
        Task<Result<IEnumerable<MyAssetDto>>> GetMyAssetHistoryAsync(Guid employeeId);

        // ==================== Tax Declarations ====================

        /// <summary>
        /// Get all tax declarations for the employee
        /// </summary>
        Task<Result<IEnumerable<TaxDeclarationSummaryDto>>> GetMyTaxDeclarationsAsync(Guid employeeId);

        /// <summary>
        /// Get a specific tax declaration detail
        /// </summary>
        Task<Result<TaxDeclarationDetailDto>> GetTaxDeclarationDetailAsync(Guid employeeId, Guid declarationId);

        /// <summary>
        /// Get tax declaration for a specific financial year
        /// </summary>
        Task<Result<TaxDeclarationDetailDto>> GetTaxDeclarationByYearAsync(Guid employeeId, string financialYear);

        /// <summary>
        /// Update tax declaration (only if status is draft or rejected)
        /// </summary>
        Task<Result<TaxDeclarationDetailDto>> UpdateTaxDeclarationAsync(Guid employeeId, Guid declarationId, UpdateTaxDeclarationDto dto);

        /// <summary>
        /// Submit tax declaration for verification
        /// </summary>
        Task<Result<TaxDeclarationDetailDto>> SubmitTaxDeclarationAsync(Guid employeeId, Guid declarationId);

        // ==================== Subscriptions ====================

        /// <summary>
        /// Get all subscriptions assigned to the employee
        /// </summary>
        Task<Result<IEnumerable<MySubscriptionDto>>> GetMySubscriptionsAsync(Guid employeeId);
    }
}
