using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IEmployeeSalaryStructureRepository
    {
        Task<EmployeeSalaryStructure?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmployeeSalaryStructure>> GetAllAsync();
        Task<EmployeeSalaryStructure?> GetCurrentByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<EmployeeSalaryStructure>> GetHistoryByEmployeeIdAsync(Guid employeeId);
        Task<EmployeeSalaryStructure?> GetEffectiveAsOfDateAsync(Guid employeeId, DateTime asOfDate);
        Task<IEnumerable<EmployeeSalaryStructure>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<EmployeeSalaryStructure>> GetActiveStructuresAsync(Guid? companyId = null);
        Task<(IEnumerable<EmployeeSalaryStructure> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<EmployeeSalaryStructure> AddAsync(EmployeeSalaryStructure entity);
        Task UpdateAsync(EmployeeSalaryStructure entity);
        Task DeleteAsync(Guid id);
        Task DeactivatePreviousStructuresAsync(Guid employeeId, DateTime effectiveFrom, Guid? excludeId = null);
        Task<bool> HasOverlappingStructureAsync(Guid employeeId, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeId = null);
    }
}
