using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Repository for managing ESI eligibility periods.
    /// Supports the 6-month ESI contribution period rule.
    /// </summary>
    public interface IEsiEligibilityRepository
    {
        /// <summary>
        /// Get eligibility period by ID
        /// </summary>
        Task<EsiEligibilityPeriod?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get active eligibility period for an employee as of a specific date
        /// </summary>
        Task<EsiEligibilityPeriod?> GetActiveForEmployeeAsync(Guid employeeId, DateTime asOfDate);

        /// <summary>
        /// Get all eligibility periods for an employee
        /// </summary>
        Task<IEnumerable<EsiEligibilityPeriod>> GetByEmployeeAsync(Guid employeeId);

        /// <summary>
        /// Get eligibility period for a specific employee and contribution period
        /// </summary>
        Task<EsiEligibilityPeriod?> GetByEmployeeAndPeriodAsync(Guid employeeId, DateTime periodStart);

        /// <summary>
        /// Create a new eligibility period
        /// </summary>
        Task<EsiEligibilityPeriod> AddAsync(EsiEligibilityPeriod period);

        /// <summary>
        /// Update an existing eligibility period (e.g., when salary crosses ceiling)
        /// </summary>
        Task UpdateAsync(EsiEligibilityPeriod period);

        /// <summary>
        /// Check if an employee is ESI eligible for a given payroll month/year.
        /// Considers the 6-month rule: if eligible at period start, must contribute until period end.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="companyId">Company ID</param>
        /// <param name="currentGross">Current month's gross salary</param>
        /// <param name="payrollMonth">Payroll month (1-12)</param>
        /// <param name="payrollYear">Payroll year</param>
        /// <param name="esiCeiling">ESI wage ceiling from company config</param>
        /// <returns>True if employee should contribute to ESI this month</returns>
        Task<bool> IsEligibleForPeriodAsync(
            Guid employeeId,
            Guid companyId,
            decimal currentGross,
            int payrollMonth,
            int payrollYear,
            decimal esiCeiling);

        /// <summary>
        /// Initialize or update eligibility period for the current contribution period.
        /// Should be called at the start of each payroll run.
        /// </summary>
        Task<EsiEligibilityPeriod> EnsureEligibilityPeriodAsync(
            Guid employeeId,
            Guid companyId,
            decimal currentGross,
            int payrollMonth,
            int payrollYear,
            decimal esiCeiling);
    }
}
