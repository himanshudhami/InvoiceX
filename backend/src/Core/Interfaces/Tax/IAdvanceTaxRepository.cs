using Core.Entities.Tax;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Repository interface for Advance Tax (Section 207) operations
    /// </summary>
    public interface IAdvanceTaxRepository
    {
        // ==================== Assessment CRUD ====================

        Task<AdvanceTaxAssessment?> GetAssessmentByIdAsync(Guid id);
        Task<AdvanceTaxAssessment?> GetAssessmentByCompanyAndFYAsync(Guid companyId, string financialYear);
        Task<IEnumerable<AdvanceTaxAssessment>> GetAssessmentsByCompanyAsync(Guid companyId);
        Task<(IEnumerable<AdvanceTaxAssessment> Items, int TotalCount)> GetAssessmentsPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? status = null,
            string? financialYear = null);
        Task<AdvanceTaxAssessment> CreateAssessmentAsync(AdvanceTaxAssessment assessment);
        Task UpdateAssessmentAsync(AdvanceTaxAssessment assessment);
        Task DeleteAssessmentAsync(Guid id);

        // ==================== Schedule Operations ====================

        Task<IEnumerable<AdvanceTaxSchedule>> GetSchedulesByAssessmentAsync(Guid assessmentId);
        Task<AdvanceTaxSchedule?> GetScheduleByIdAsync(Guid id);
        Task<AdvanceTaxSchedule?> GetScheduleByQuarterAsync(Guid assessmentId, int quarter);
        Task CreateSchedulesAsync(IEnumerable<AdvanceTaxSchedule> schedules);
        Task UpdateScheduleAsync(AdvanceTaxSchedule schedule);
        Task DeleteSchedulesByAssessmentAsync(Guid assessmentId);

        // ==================== Payment Operations ====================

        Task<IEnumerable<AdvanceTaxPayment>> GetPaymentsByAssessmentAsync(Guid assessmentId);
        Task<AdvanceTaxPayment?> GetPaymentByIdAsync(Guid id);
        Task<AdvanceTaxPayment> CreatePaymentAsync(AdvanceTaxPayment payment);
        Task UpdatePaymentAsync(AdvanceTaxPayment payment);
        Task DeletePaymentAsync(Guid id);

        // ==================== Scenario Operations ====================

        Task<IEnumerable<AdvanceTaxScenario>> GetScenariosByAssessmentAsync(Guid assessmentId);
        Task<AdvanceTaxScenario?> GetScenarioByIdAsync(Guid id);
        Task<AdvanceTaxScenario> CreateScenarioAsync(AdvanceTaxScenario scenario);
        Task UpdateScenarioAsync(AdvanceTaxScenario scenario);
        Task DeleteScenarioAsync(Guid id);

        // ==================== Summary & Reports ====================

        /// <summary>
        /// Get total advance tax paid for a company in a financial year
        /// </summary>
        Task<decimal> GetTotalAdvanceTaxPaidAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get assessments with pending payments (due date passed, not fully paid)
        /// </summary>
        Task<IEnumerable<AdvanceTaxAssessment>> GetAssessmentsWithPendingPaymentsAsync(Guid? companyId = null);

        /// <summary>
        /// Get upcoming payment deadlines
        /// </summary>
        Task<IEnumerable<AdvanceTaxSchedule>> GetUpcomingPaymentDeadlinesAsync(
            DateOnly fromDate,
            DateOnly toDate,
            Guid? companyId = null);

        // ==================== YTD Ledger Integration ====================

        /// <summary>
        /// Get YTD income and expenses from journal entries for advance tax computation.
        /// Queries journal_entry_lines joined with chart_of_accounts by account_type.
        /// </summary>
        Task<(decimal YtdIncome, decimal YtdExpenses)> GetYtdFinancialsFromLedgerAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);
    }
}
