using Application.Interfaces.Intercompany;
using Core.Common;
using Core.Entities.Intercompany;
using Core.Interfaces;
using Core.Interfaces.Intercompany;

namespace Application.Services.Intercompany
{
    /// <summary>
    /// Service for managing intercompany transactions and reconciliation
    /// </summary>
    public class IntercompanyService : IIntercompanyService
    {
        private readonly ICompanyRelationshipRepository _relationshipRepository;
        private readonly IIntercompanyTransactionRepository _transactionRepository;
        private readonly IIntercompanyBalanceRepository _balanceRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly ICompaniesRepository _companiesRepository;

        public IntercompanyService(
            ICompanyRelationshipRepository relationshipRepository,
            IIntercompanyTransactionRepository transactionRepository,
            IIntercompanyBalanceRepository balanceRepository,
            ICustomersRepository customersRepository,
            ICompaniesRepository companiesRepository)
        {
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _balanceRepository = balanceRepository ?? throw new ArgumentNullException(nameof(balanceRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
        }

        /// <inheritdoc />
        public async Task<bool> IsIntercompanyTransactionAsync(Guid invoicingCompanyId, Guid customerId)
        {
            // Check if customer is linked to a company in the same group
            var customer = await _customersRepository.GetByIdAsync(customerId);
            if (customer?.CompanyId == null)
                return false;

            // Check if the customer's company is in the same group as the invoicing company
            return await _relationshipRepository.AreCompaniesRelatedAsync(invoicingCompanyId, customer.CompanyId.Value);
        }

        /// <inheritdoc />
        public async Task<Result<IntercompanyTransaction>> RecordInvoiceAsync(
            Guid invoiceId,
            Guid invoicingCompanyId,
            Guid customerCompanyId,
            decimal amount,
            string currency,
            DateOnly invoiceDate,
            string invoiceNumber)
        {
            var financialYear = GetFinancialYear(invoiceDate);

            // Create receivable entry in invoicing company's books
            var receivableTxn = new IntercompanyTransaction
            {
                CompanyId = invoicingCompanyId,
                CounterpartyCompanyId = customerCompanyId,
                TransactionDate = invoiceDate,
                FinancialYear = financialYear,
                TransactionType = "invoice",
                TransactionDirection = "receivable",
                SourceDocumentType = "invoice",
                SourceDocumentId = invoiceId,
                SourceDocumentNumber = invoiceNumber,
                Amount = amount,
                Currency = currency,
                AmountInInr = currency == "INR" ? amount : null,
                Description = $"Intercompany invoice {invoiceNumber}"
            };

            var createdReceivable = await _transactionRepository.AddAsync(receivableTxn);

            // Create payable entry in customer company's books
            var payableTxn = new IntercompanyTransaction
            {
                CompanyId = customerCompanyId,
                CounterpartyCompanyId = invoicingCompanyId,
                TransactionDate = invoiceDate,
                FinancialYear = financialYear,
                TransactionType = "invoice",
                TransactionDirection = "payable",
                SourceDocumentType = "invoice",
                SourceDocumentId = invoiceId,
                SourceDocumentNumber = invoiceNumber,
                Amount = amount,
                Currency = currency,
                AmountInInr = currency == "INR" ? amount : null,
                CounterpartyTransactionId = createdReceivable.Id,
                Description = $"Intercompany invoice {invoiceNumber}"
            };

            var createdPayable = await _transactionRepository.AddAsync(payableTxn);

            // Link the receivable to the payable
            createdReceivable.CounterpartyTransactionId = createdPayable.Id;
            await _transactionRepository.UpdateAsync(createdReceivable);

            // Update balances
            await _balanceRepository.UpdateBalanceAsync(invoicingCompanyId, customerCompanyId, invoiceDate, amount, true);
            await _balanceRepository.UpdateBalanceAsync(customerCompanyId, invoicingCompanyId, invoiceDate, amount, false);

            return Result<IntercompanyTransaction>.Success(createdReceivable);
        }

        /// <inheritdoc />
        public async Task<Result<IntercompanyTransaction>> RecordPaymentAsync(
            Guid paymentId,
            Guid payingCompanyId,
            Guid receivingCompanyId,
            decimal amount,
            string currency,
            DateOnly paymentDate,
            string? referenceNumber)
        {
            var financialYear = GetFinancialYear(paymentDate);

            // Create payment entry in paying company's books (reduces payable)
            var paymentOutTxn = new IntercompanyTransaction
            {
                CompanyId = payingCompanyId,
                CounterpartyCompanyId = receivingCompanyId,
                TransactionDate = paymentDate,
                FinancialYear = financialYear,
                TransactionType = "payment",
                TransactionDirection = "payable", // Reduces payable
                SourceDocumentType = "payment",
                SourceDocumentId = paymentId,
                SourceDocumentNumber = referenceNumber ?? paymentId.ToString()[..8],
                Amount = amount,
                Currency = currency,
                AmountInInr = currency == "INR" ? amount : null,
                Description = $"Intercompany payment {referenceNumber ?? paymentId.ToString()[..8]}"
            };

            var createdPaymentOut = await _transactionRepository.AddAsync(paymentOutTxn);

            // Create receipt entry in receiving company's books (reduces receivable)
            var receiptTxn = new IntercompanyTransaction
            {
                CompanyId = receivingCompanyId,
                CounterpartyCompanyId = payingCompanyId,
                TransactionDate = paymentDate,
                FinancialYear = financialYear,
                TransactionType = "payment",
                TransactionDirection = "receivable", // Reduces receivable
                SourceDocumentType = "payment",
                SourceDocumentId = paymentId,
                SourceDocumentNumber = referenceNumber ?? paymentId.ToString()[..8],
                Amount = amount,
                Currency = currency,
                AmountInInr = currency == "INR" ? amount : null,
                CounterpartyTransactionId = createdPaymentOut.Id,
                Description = $"Intercompany receipt {referenceNumber ?? paymentId.ToString()[..8]}"
            };

            var createdReceipt = await _transactionRepository.AddAsync(receiptTxn);

            // Link the payment to the receipt
            createdPaymentOut.CounterpartyTransactionId = createdReceipt.Id;
            await _transactionRepository.UpdateAsync(createdPaymentOut);

            // Update balances (payment reduces both sides)
            await _balanceRepository.UpdateBalanceAsync(payingCompanyId, receivingCompanyId, paymentDate, amount, true); // Debit reduces payable
            await _balanceRepository.UpdateBalanceAsync(receivingCompanyId, payingCompanyId, paymentDate, amount, false); // Credit reduces receivable

            return Result<IntercompanyTransaction>.Success(createdReceipt);
        }

        /// <inheritdoc />
        public async Task<Result<IntercompanyBalance>> GetBalanceAsync(Guid fromCompanyId, Guid toCompanyId)
        {
            var balance = await _balanceRepository.GetLatestBalanceAsync(fromCompanyId, toCompanyId);
            if (balance == null)
                return Error.NotFound("No balance found between these companies");

            return Result<IntercompanyBalance>.Success(balance);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<IntercompanyBalance>>> GetAllBalancesForCompanyAsync(Guid companyId)
        {
            var balances = await _balanceRepository.GetBalancesForCompanyAsync(companyId);
            return Result<IEnumerable<IntercompanyBalance>>.Success(balances);
        }

        /// <inheritdoc />
        public async Task<Result> ReconcileTransactionsAsync(Guid transactionId, Guid counterpartyTransactionId, Guid reconciledBy)
        {
            var txn1 = await _transactionRepository.GetByIdAsync(transactionId);
            var txn2 = await _transactionRepository.GetByIdAsync(counterpartyTransactionId);

            if (txn1 == null || txn2 == null)
                return Error.NotFound("One or both transactions not found");

            if (txn1.Amount != txn2.Amount)
                return Error.Validation("Transaction amounts do not match");

            await _transactionRepository.ReconcileTransactionsAsync(transactionId, counterpartyTransactionId, reconciledBy);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<int>> AutoReconcileAsync(Guid? companyId = null)
        {
            var unreconciled = await _transactionRepository.GetUnreconciledAsync(companyId);
            var transactions = unreconciled.ToList();
            var reconciledCount = 0;

            foreach (var txn in transactions.Where(t => !t.IsReconciled))
            {
                // Look for matching counterparty transaction
                var potentialMatches = transactions.Where(t =>
                    t.Id != txn.Id &&
                    !t.IsReconciled &&
                    t.CompanyId == txn.CounterpartyCompanyId &&
                    t.CounterpartyCompanyId == txn.CompanyId &&
                    t.Amount == txn.Amount &&
                    t.TransactionDate == txn.TransactionDate &&
                    t.SourceDocumentNumber == txn.SourceDocumentNumber);

                var match = potentialMatches.FirstOrDefault();
                if (match != null)
                {
                    await _transactionRepository.ReconcileTransactionsAsync(txn.Id, match.Id, Guid.Empty);
                    txn.IsReconciled = true;
                    match.IsReconciled = true;
                    reconciledCount += 2;
                }
            }

            return Result<int>.Success(reconciledCount);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<IntercompanyTransaction>>> GetUnreconciledTransactionsAsync(Guid? companyId = null)
        {
            var transactions = await _transactionRepository.GetUnreconciledAsync(companyId);
            return Result<IEnumerable<IntercompanyTransaction>>.Success(transactions);
        }

        /// <inheritdoc />
        public async Task<Result<IntercompanyReconciliationReport>> GetReconciliationReportAsync(
            Guid companyId,
            DateOnly asOfDate)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound("Company not found");

            var balances = await _balanceRepository.GetBalancesForCompanyAsync(companyId, asOfDate);
            var unreconciled = await _transactionRepository.GetUnreconciledAsync(companyId);

            var summaries = new List<IntercompanyBalanceSummary>();
            decimal totalReceivables = 0;
            decimal totalPayables = 0;

            foreach (var balance in balances.Where(b => b.FromCompanyId == companyId))
            {
                var counterparty = await _companiesRepository.GetByIdAsync(balance.ToCompanyId);
                var counterpartyBalance = await _balanceRepository.GetBalanceAsync(balance.ToCompanyId, companyId, asOfDate);

                var summary = new IntercompanyBalanceSummary
                {
                    CounterpartyId = balance.ToCompanyId,
                    CounterpartyName = counterparty?.Name ?? "Unknown",
                    OurBalance = balance.BalanceAmount,
                    TheirBalance = counterpartyBalance?.BalanceAmount * -1 ?? 0,
                    TransactionCount = balance.TransactionCount,
                    LastTransactionDate = balance.LastTransactionDate
                };

                summary.Difference = summary.OurBalance - summary.TheirBalance;
                summary.Status = Math.Abs(summary.Difference) < 0.01m ? "Matched" : "Unmatched";

                if (balance.BalanceAmount > 0)
                    totalReceivables += balance.BalanceAmount;
                else
                    totalPayables += Math.Abs(balance.BalanceAmount);

                summaries.Add(summary);
            }

            var unreconciledList = unreconciled.ToList();

            var report = new IntercompanyReconciliationReport
            {
                CompanyId = companyId,
                CompanyName = company.Name ?? string.Empty,
                AsOfDate = asOfDate,
                Balances = summaries,
                TotalReceivables = totalReceivables,
                TotalPayables = totalPayables,
                NetPosition = totalReceivables - totalPayables,
                UnreconciledCount = unreconciledList.Count,
                UnreconciledAmount = unreconciledList.Sum(t => t.Amount)
            };

            return Result<IntercompanyReconciliationReport>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<CompanyRelationship>>> GetGroupStructureAsync(Guid companyId)
        {
            var relationships = await _relationshipRepository.GetRelationshipsForCompanyAsync(companyId);
            return Result<IEnumerable<CompanyRelationship>>.Success(relationships);
        }

        /// <inheritdoc />
        public async Task<Result<CompanyRelationship>> AddRelationshipAsync(
            Guid parentCompanyId,
            Guid childCompanyId,
            string relationshipType,
            decimal ownershipPercentage,
            string consolidationMethod)
        {
            // Validate companies exist
            var parent = await _companiesRepository.GetByIdAsync(parentCompanyId);
            var child = await _companiesRepository.GetByIdAsync(childCompanyId);

            if (parent == null)
                return Error.NotFound("Parent company not found");
            if (child == null)
                return Error.NotFound("Child company not found");
            if (parentCompanyId == childCompanyId)
                return Error.Validation("A company cannot be its own parent");

            var relationship = new CompanyRelationship
            {
                ParentCompanyId = parentCompanyId,
                ChildCompanyId = childCompanyId,
                RelationshipType = relationshipType,
                OwnershipPercentage = ownershipPercentage,
                ConsolidationMethod = consolidationMethod,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                IsActive = true
            };

            var created = await _relationshipRepository.AddAsync(relationship);
            return Result<CompanyRelationship>.Success(created);
        }

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }
    }
}
