using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Interfaces;
using Core.Common;

namespace Application.Services
{
    /// <summary>
    /// Service for Bank Reconciliation Statement generation
    /// </summary>
    public class BrsService : IBrsService
    {
        private readonly IBankTransactionRepository _repository;
        private readonly IBankAccountRepository _bankAccountRepository;

        public BrsService(
            IBankTransactionRepository repository,
            IBankAccountRepository bankAccountRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
        }

        /// <inheritdoc />
        public async Task<Result<BankReconciliationStatementDto>> GenerateBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {bankAccountId} not found");

            var brs = new BankReconciliationStatementDto
            {
                BankAccountId = bankAccountId,
                BankAccountName = bankAccount.AccountName ?? $"{bankAccount.BankName} - {bankAccount.AccountNumber}",
                AsOfDate = asOfDate,
                BankStatementBalance = bankAccount.CurrentBalance
            };

            var allTxns = await _repository.GetByDateRangeAsync(bankAccountId, DateOnly.MinValue, asOfDate);
            var transactions = allTxns.ToList();

            brs.TotalTransactions = transactions.Count;
            brs.ReconciledTransactions = transactions.Count(t => t.IsReconciled);
            brs.UnreconciledTransactions = transactions.Count(t => !t.IsReconciled);

            // Unreconciled bank credits (in bank but not in books)
            var unreconciledCredits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "credit").ToList();
            brs.BankCreditsNotInBooks = unreconciledCredits.Sum(t => t.Amount);
            brs.BankCreditsNotInBooksItems = unreconciledCredits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Unreconciled bank debits (in bank but not in books)
            var unreconciledDebits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "debit").ToList();
            brs.BankDebitsNotInBooks = unreconciledDebits.Sum(t => t.Amount);
            brs.BankDebitsNotInBooksItems = unreconciledDebits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Calculate book balance from reconciled transactions
            var reconciledCredits = transactions.Where(t => t.IsReconciled && t.TransactionType == "credit").Sum(t => t.Amount);
            var reconciledDebits = transactions.Where(t => t.IsReconciled && t.TransactionType == "debit").Sum(t => t.Amount);
            brs.BookBalance = reconciledCredits - reconciledDebits;

            // Deposits in transit and outstanding cheques (would need integration with payment/expense systems)
            brs.DepositsInTransit = 0;
            brs.OutstandingCheques = 0;

            // Calculate adjusted balances
            brs.AdjustedBankBalance = brs.BankStatementBalance + brs.DepositsInTransit - brs.OutstandingCheques;
            brs.AdjustedBookBalance = brs.BookBalance + brs.BankCreditsNotInBooks - brs.BankDebitsNotInBooks;

            return Result<BankReconciliationStatementDto>.Success(brs);
        }
    }
}
