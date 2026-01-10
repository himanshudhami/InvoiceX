using Application.DTOs.Tax;
using Core.Common;

namespace Application.Interfaces.Tax
{
    /// <summary>
    /// Service interface for Advance Tax (Section 207) operations
    /// </summary>
    public interface IAdvanceTaxService
    {
        // ==================== Assessment Operations ====================

        /// <summary>
        /// Compute and create advance tax assessment for a company and FY
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> ComputeAssessmentAsync(CreateAdvanceTaxAssessmentDto request, Guid userId);

        /// <summary>
        /// Get assessment by ID with schedules and payments
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> GetAssessmentByIdAsync(Guid id);

        /// <summary>
        /// Get assessment for a company and FY
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> GetAssessmentAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get all assessments for a company
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxAssessmentDto>>> GetAssessmentsByCompanyAsync(Guid companyId);

        /// <summary>
        /// Update assessment projections and recompute tax
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> UpdateAssessmentAsync(Guid id, UpdateAdvanceTaxAssessmentDto request, Guid userId);

        /// <summary>
        /// Activate an assessment (move from draft to active)
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> ActivateAssessmentAsync(Guid id, Guid userId);

        /// <summary>
        /// Finalize an assessment (after FY end)
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> FinalizeAssessmentAsync(Guid id, Guid userId);

        /// <summary>
        /// Delete assessment (only if draft)
        /// </summary>
        Task<Result<bool>> DeleteAssessmentAsync(Guid id);

        /// <summary>
        /// Refresh YTD actuals from ledger and optionally auto-project remaining months
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> RefreshYtdAsync(RefreshYtdRequest request, Guid userId);

        /// <summary>
        /// Get YTD financials with trend-based projections (preview before refresh)
        /// </summary>
        Task<Result<YtdFinancialsDto>> GetYtdFinancialsPreviewAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Refresh TDS receivable and TCS credit from respective modules
        /// </summary>
        Task<Result<AdvanceTaxAssessmentDto>> RefreshTdsTcsAsync(Guid assessmentId, Guid userId);

        /// <summary>
        /// Get TDS/TCS preview (current values from modules without saving)
        /// </summary>
        Task<Result<TdsTcsPreviewDto>> GetTdsTcsPreviewAsync(Guid companyId, string financialYear);

        // ==================== Schedule Operations ====================

        /// <summary>
        /// Get payment schedule for an assessment
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxScheduleDto>>> GetPaymentScheduleAsync(Guid assessmentId);

        /// <summary>
        /// Recalculate schedules after payment or tax change
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxScheduleDto>>> RecalculateSchedulesAsync(Guid assessmentId);

        // ==================== Payment Operations ====================

        /// <summary>
        /// Record advance tax payment
        /// </summary>
        Task<Result<AdvanceTaxPaymentDto>> RecordPaymentAsync(RecordAdvanceTaxPaymentDto request, Guid userId);

        /// <summary>
        /// Get payments for an assessment
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxPaymentDto>>> GetPaymentsAsync(Guid assessmentId);

        /// <summary>
        /// Delete a payment
        /// </summary>
        Task<Result<bool>> DeletePaymentAsync(Guid paymentId);

        // ==================== Interest Calculations ====================

        /// <summary>
        /// Calculate interest under Section 234B (shortfall in advance tax)
        /// </summary>
        Task<Result<decimal>> Calculate234BInterestAsync(Guid assessmentId);

        /// <summary>
        /// Calculate interest under Section 234C (deferment of advance tax)
        /// </summary>
        Task<Result<decimal>> Calculate234CInterestAsync(Guid assessmentId);

        /// <summary>
        /// Get full interest breakdown
        /// </summary>
        Task<Result<InterestCalculationDto>> GetInterestBreakdownAsync(Guid assessmentId);

        // ==================== Scenario Analysis ====================

        /// <summary>
        /// Run a what-if scenario
        /// </summary>
        Task<Result<AdvanceTaxScenarioDto>> RunScenarioAsync(RunScenarioDto request, Guid userId);

        /// <summary>
        /// Get scenarios for an assessment
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxScenarioDto>>> GetScenariosAsync(Guid assessmentId);

        /// <summary>
        /// Delete a scenario
        /// </summary>
        Task<Result<bool>> DeleteScenarioAsync(Guid scenarioId);

        // ==================== Dashboard & Reports ====================

        /// <summary>
        /// Get advance tax tracker/dashboard for a company and FY
        /// </summary>
        Task<Result<AdvanceTaxTrackerDto>> GetTrackerAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get tax computation breakdown
        /// </summary>
        Task<Result<TaxComputationDto>> GetTaxComputationAsync(Guid assessmentId);

        /// <summary>
        /// Get assessments with pending payments
        /// </summary>
        Task<Result<IEnumerable<AdvanceTaxAssessmentDto>>> GetPendingPaymentsAsync(Guid? companyId = null);
    }
}
