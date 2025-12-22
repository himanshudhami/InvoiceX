using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Interfaces;
using Core.Common;

namespace Application.Services
{
    /// <summary>
    /// Service for managing outgoing payments view
    /// </summary>
    public class OutgoingPaymentsService : IOutgoingPaymentsService
    {
        private readonly IBankTransactionRepository _repository;

        public OutgoingPaymentsService(IBankTransactionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<OutgoingPaymentDto> Items, int TotalCount)>> GetOutgoingPaymentsAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            bool? reconciled = null,
            List<string>? types = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var paginationValidation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (paginationValidation.IsFailure)
                return paginationValidation.Error!;

            var (records, totalCount) = await _repository.GetOutgoingPaymentsAsync(
                companyId, pageNumber, pageSize, reconciled, types, fromDate, toDate);

            var dtos = records.Select(r => new OutgoingPaymentDto
            {
                Id = r.Id,
                Type = r.Type,
                TypeDisplay = GetTypeDisplayName(r.Type),
                PaymentDate = r.PaymentDate,
                Amount = r.Amount,
                PayeeName = r.PayeeName,
                Description = r.Description,
                DisplayName = BuildDisplayName(r),
                ReferenceNumber = r.ReferenceNumber,
                IsReconciled = r.IsReconciled,
                BankTransactionId = r.BankTransactionId,
                ReconciledAt = r.ReconciledAt,
                TdsAmount = r.TdsAmount,
                TdsSection = r.TdsSection,
                Category = r.Category,
                Status = r.Status
            }).ToList();

            return Result<(IEnumerable<OutgoingPaymentDto> Items, int TotalCount)>.Success((dtos, totalCount));
        }

        /// <inheritdoc />
        public async Task<Result<OutgoingPaymentsSummaryDto>> GetOutgoingPaymentsSummaryAsync(
            Guid companyId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var summary = await _repository.GetOutgoingPaymentsSummaryAsync(companyId, fromDate, toDate);

            var dto = new OutgoingPaymentsSummaryDto
            {
                TotalCount = summary.TotalCount,
                ReconciledCount = summary.ReconciledCount,
                UnreconciledCount = summary.UnreconciledCount,
                TotalAmount = summary.TotalAmount,
                ReconciledAmount = summary.ReconciledAmount,
                UnreconciledAmount = summary.UnreconciledAmount,
                ByType = summary.ByType.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new OutgoingPaymentTypeBreakdown
                    {
                        TypeDisplay = GetTypeDisplayName(kvp.Key),
                        Count = kvp.Value.Count,
                        Amount = kvp.Value.Amount,
                        ReconciledCount = kvp.Value.ReconciledCount,
                        UnreconciledCount = kvp.Value.Count - kvp.Value.ReconciledCount
                    })
            };

            return Result<OutgoingPaymentsSummaryDto>.Success(dto);
        }

        private static string GetTypeDisplayName(string type) => type switch
        {
            "salary" => "Salary",
            "contractor" => "Contractor Payment",
            "expense_claim" => "Expense Claim",
            "subscription" => "Subscription",
            "loan_payment" => "Loan Payment",
            "asset_maintenance" => "Asset Maintenance",
            _ => type
        };

        private static string BuildDisplayName(Core.Interfaces.OutgoingPaymentRecord record)
        {
            var typeName = GetTypeDisplayName(record.Type);
            var parts = new List<string> { typeName };

            if (!string.IsNullOrWhiteSpace(record.PayeeName))
                parts.Add(record.PayeeName);

            if (!string.IsNullOrWhiteSpace(record.Description) && record.Description.Length <= 50)
                parts.Add(record.Description);

            return string.Join(" - ", parts);
        }
    }
}
