using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IContractorPaymentRepository
    {
        Task<ContractorPayment?> GetByIdAsync(Guid id);
        Task<IEnumerable<ContractorPayment>> GetAllAsync();
        Task<IEnumerable<ContractorPayment>> GetByPartyIdAsync(Guid partyId);
        Task<IEnumerable<ContractorPayment>> GetByCompanyIdAsync(Guid companyId);
        Task<ContractorPayment?> GetByPartyAndMonthAsync(Guid partyId, int paymentMonth, int paymentYear);
        Task<IEnumerable<ContractorPayment>> GetByMonthYearAsync(int paymentMonth, int paymentYear, Guid? companyId = null);
        Task<IEnumerable<ContractorPayment>> GetByStatusAsync(string status, Guid? companyId = null);
        Task<IEnumerable<ContractorPayment>> GetByFinancialYearAsync(Guid partyId, string financialYear);
        Task<(IEnumerable<ContractorPayment> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<ContractorPayment> AddAsync(ContractorPayment entity);
        Task UpdateAsync(ContractorPayment entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ContractorPayment>> BulkAddAsync(IEnumerable<ContractorPayment> entities);
        Task<bool> ExistsForPartyAndMonthAsync(Guid partyId, int paymentMonth, int paymentYear, Guid? excludeId = null);
        Task UpdateStatusAsync(Guid id, string status);

        // Summary methods
        Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(int paymentMonth, int paymentYear, Guid? companyId = null);
        Task<Dictionary<string, decimal>> GetYtdSummaryAsync(Guid partyId, string financialYear);

        // Tally Migration
        Task<ContractorPayment?> GetByTallyGuidAsync(Guid companyId, string tallyVoucherGuid);
    }
}
