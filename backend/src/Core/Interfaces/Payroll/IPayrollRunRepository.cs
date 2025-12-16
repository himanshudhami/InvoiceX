using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IPayrollRunRepository
    {
        Task<PayrollRun?> GetByIdAsync(Guid id);
        Task<IEnumerable<PayrollRun>> GetAllAsync();
        Task<PayrollRun?> GetByCompanyAndMonthAsync(Guid companyId, int payrollMonth, int payrollYear);
        Task<IEnumerable<PayrollRun>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<PayrollRun>> GetByFinancialYearAsync(string financialYear);
        Task<IEnumerable<PayrollRun>> GetByStatusAsync(string status);
        Task<PayrollRun?> GetLatestByCompanyAsync(Guid companyId);
        Task<(IEnumerable<PayrollRun> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<PayrollRun> AddAsync(PayrollRun entity);
        Task UpdateAsync(PayrollRun entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsForCompanyAndMonthAsync(Guid companyId, int payrollMonth, int payrollYear, Guid? excludeId = null);
        Task UpdateStatusAsync(Guid id, string status, string? updatedBy = null);
        Task UpdateTotalsAsync(Guid id, int totalEmployees, int totalContractors,
            decimal totalGross, decimal totalDeductions, decimal totalNet,
            decimal totalEmployerPf, decimal totalEmployerEsi, decimal totalEmployerCost);
    }
}
