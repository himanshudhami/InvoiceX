using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Repository interface for statutory payments (TDS/PF/ESI/PT challans)
    /// </summary>
    public interface IStatutoryPaymentRepository
    {
        /// <summary>
        /// Gets a statutory payment by ID
        /// </summary>
        Task<StatutoryPayment?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all statutory payments for a company
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetByCompanyAsync(Guid companyId);

        /// <summary>
        /// Gets pending statutory payments for a company
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetPendingAsync(Guid companyId);

        /// <summary>
        /// Gets overdue statutory payments for a company
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetOverdueAsync(Guid companyId);

        /// <summary>
        /// Gets statutory payment by company, type, and period
        /// </summary>
        Task<StatutoryPayment?> GetByPeriodAsync(
            Guid companyId,
            string paymentType,
            string financialYear,
            int periodMonth);

        /// <summary>
        /// Gets statutory payments for a financial year
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetByFinancialYearAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Adds a new statutory payment
        /// </summary>
        Task<StatutoryPayment> AddAsync(StatutoryPayment payment);

        /// <summary>
        /// Updates an existing statutory payment
        /// </summary>
        Task UpdateAsync(StatutoryPayment payment);

        /// <summary>
        /// Deletes a statutory payment (soft delete by setting status to cancelled)
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Gets pending statutory payments view for dashboard
        /// </summary>
        Task<IEnumerable<PendingStatutoryPaymentView>> GetPendingPaymentsViewAsync(
            Guid companyId);

        /// <summary>
        /// Gets pending statutory payments view filtered by status
        /// </summary>
        Task<IEnumerable<PendingStatutoryPaymentView>> GetPendingPaymentsViewAsync(
            Guid companyId,
            string? statusFilter);

        /// <summary>
        /// Gets statutory payment types lookup
        /// </summary>
        Task<IEnumerable<StatutoryPaymentType>> GetPaymentTypesAsync();

        /// <summary>
        /// Gets a specific payment type by code
        /// </summary>
        Task<StatutoryPaymentType?> GetPaymentTypeAsync(string code);

        /// <summary>
        /// Adds allocation linking statutory payment to payroll
        /// </summary>
        Task AddAllocationAsync(StatutoryPaymentAllocation allocation);

        /// <summary>
        /// Gets allocations for a statutory payment
        /// </summary>
        Task<IEnumerable<StatutoryPaymentAllocation>> GetAllocationsAsync(Guid statutoryPaymentId);

        /// <summary>
        /// Gets statutory payment by company, period month, year, and type
        /// </summary>
        Task<StatutoryPayment?> GetByPeriodAndTypeAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string paymentType);

        /// <summary>
        /// Gets pending payments by company and type
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetPendingByCompanyAsync(
            Guid companyId,
            string paymentType,
            string? financialYear = null);

        /// <summary>
        /// Gets paid payments by company and type
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetPaidByCompanyAsync(
            Guid companyId,
            string paymentType,
            string financialYear);

        /// <summary>
        /// Gets all payments by company, type and financial year
        /// </summary>
        Task<IEnumerable<StatutoryPayment>> GetByCompanyAndFyAsync(
            Guid companyId,
            string paymentType,
            string financialYear);

        /// <summary>
        /// Gets a statutory payment by its Tally voucher GUID (for migration duplicate detection)
        /// </summary>
        Task<StatutoryPayment?> GetByTallyGuidAsync(Guid companyId, string tallyGuid);
    }
}
