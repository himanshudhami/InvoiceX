using Core.Entities.Ledger;
using Core.Entities.Tax;
using Core.Interfaces.Ledger;
using Core.Interfaces.Tax;
using Microsoft.Extensions.Logging;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service for TCS (Tax Collected at Source) operations.
    ///
    /// TCS is collected by seller from buyer at the time of sale per Section 206C.
    /// Common scenarios: Sale of goods > 50L, scrap, motor vehicles, forest produce.
    ///
    /// References:
    /// - Section 206C of Income Tax Act
    /// - Form 27EQ quarterly return
    /// - ClearTax: https://cleartax.in/s/tcs-tax-collected-source
    /// </summary>
    public class TcsService : ITcsService
    {
        private readonly ITcsTransactionRepository _tcsRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly ILogger<TcsService> _logger;

        /// <summary>
        /// Account codes following standard Indian chart of accounts
        /// </summary>
        private static class AccountCodes
        {
            // TCS Payable (collected from customers)
            public const string TcsPayable = "2280";
            public const string TcsPayable206C1H = "2282";
            public const string TcsPayable206C1Scrap = "2283";
            public const string TcsPayable206C1FMotor = "2284";
            public const string TcsPayable206C1GLrs = "2285";
            public const string TcsPayable206C1GTour = "2286";

            // TCS Receivable (paid to vendors)
            public const string TcsReceivable = "1160";
            public const string TcsReceivable206C1H = "1161";

            // Default customer receivable
            public const string SundryDebtors = "1310";
        }

        /// <summary>
        /// TCS section rate configuration (FY 2024-25)
        /// </summary>
        private static readonly Dictionary<string, TcsSectionConfig> SectionConfigs = new()
        {
            [TcsSectionCodes.SaleOver50L] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.SaleOver50L,
                Description = "Sale of goods exceeding Rs.50 lakhs",
                Rate = 0.1m,
                HigherRate = 1.0m, // For non-PAN cases
                ThresholdAmount = 5000000m,
                ApplicableGoodsServices = "All goods (except exports)",
                AccountCode = AccountCodes.TcsPayable206C1H
            },
            [TcsSectionCodes.Scrap] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.Scrap,
                Description = "Sale of scrap",
                Rate = 1.0m,
                HigherRate = 2.0m,
                ThresholdAmount = null,
                ApplicableGoodsServices = "Scrap materials",
                AccountCode = AccountCodes.TcsPayable206C1Scrap
            },
            [TcsSectionCodes.MotorVehicle] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.MotorVehicle,
                Description = "Sale of motor vehicle exceeding Rs.10 lakhs",
                Rate = 1.0m,
                HigherRate = 2.0m,
                ThresholdAmount = 1000000m,
                ApplicableGoodsServices = "Motor vehicles > 10L",
                AccountCode = AccountCodes.TcsPayable206C1FMotor
            },
            [TcsSectionCodes.ForeignRemittance] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.ForeignRemittance,
                Description = "Foreign remittance under LRS",
                Rate = 5.0m,
                HigherRate = 20.0m, // Above Rs.7L
                ThresholdAmount = 700000m,
                ApplicableGoodsServices = "Foreign remittance",
                AccountCode = AccountCodes.TcsPayable206C1GLrs
            },
            [TcsSectionCodes.OverseasTour] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.OverseasTour,
                Description = "Overseas tour package",
                Rate = 5.0m,
                HigherRate = 20.0m,
                ThresholdAmount = 700000m,
                ApplicableGoodsServices = "Tour packages",
                AccountCode = AccountCodes.TcsPayable206C1GTour
            },
            [TcsSectionCodes.Liquor] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.Liquor,
                Description = "Sale of liquor for human consumption",
                Rate = 1.0m,
                HigherRate = 2.0m,
                ThresholdAmount = null,
                ApplicableGoodsServices = "Liquor",
                AccountCode = AccountCodes.TcsPayable
            },
            [TcsSectionCodes.ForestProduce] = new TcsSectionConfig
            {
                SectionCode = TcsSectionCodes.ForestProduce,
                Description = "Sale of forest produce",
                Rate = 2.5m,
                HigherRate = 5.0m,
                ThresholdAmount = null,
                ApplicableGoodsServices = "Timber, tendu leaves, etc.",
                AccountCode = AccountCodes.TcsPayable
            }
        };

        public TcsService(
            ITcsTransactionRepository tcsRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            ILogger<TcsService> logger)
        {
            _tcsRepository = tcsRepository ?? throw new ArgumentNullException(nameof(tcsRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== TCS Calculation ====================

        public async Task<TcsCalculationResult> CalculateTcsAsync(TcsCalculationRequest request)
        {
            try
            {
                if (!SectionConfigs.TryGetValue(request.SectionCode, out var config))
                {
                    return new TcsCalculationResult
                    {
                        IsApplicable = false,
                        Reason = $"Unknown TCS section: {request.SectionCode}"
                    };
                }

                decimal? cumulativeValue = null;
                decimal effectiveRate = config.Rate;

                // Check for higher rate if PAN not available
                if (string.IsNullOrEmpty(request.CustomerPan))
                {
                    effectiveRate = config.HigherRate ?? config.Rate * 2;
                }

                // Check cumulative threshold for 206C(1H)
                if (request.SectionCode == TcsSectionCodes.SaleOver50L && request.CheckCumulativeThreshold)
                {
                    cumulativeValue = await GetCumulativeValueAsync(
                        request.CompanyId,
                        request.CustomerPan ?? "",
                        request.FinancialYear);

                    // TCS applies only on amount exceeding threshold
                    if (cumulativeValue + request.TransactionValue <= (config.ThresholdAmount ?? 0))
                    {
                        return new TcsCalculationResult
                        {
                            IsApplicable = false,
                            SectionCode = request.SectionCode,
                            SectionDescription = config.Description,
                            TransactionValue = request.TransactionValue,
                            TcsRate = 0,
                            TcsAmount = 0,
                            CumulativeValue = cumulativeValue,
                            ThresholdAmount = config.ThresholdAmount,
                            Reason = $"Cumulative value ({cumulativeValue + request.TransactionValue:N2}) below threshold ({config.ThresholdAmount:N2})"
                        };
                    }

                    // Calculate TCS only on amount exceeding threshold
                    var taxableAmount = (cumulativeValue ?? 0) < config.ThresholdAmount
                        ? ((cumulativeValue ?? 0) + request.TransactionValue) - config.ThresholdAmount!.Value
                        : request.TransactionValue;

                    var tcsAmount = Math.Round(taxableAmount * effectiveRate / 100, 0);

                    return new TcsCalculationResult
                    {
                        IsApplicable = true,
                        SectionCode = request.SectionCode,
                        SectionDescription = config.Description,
                        TransactionValue = request.TransactionValue,
                        TcsRate = effectiveRate,
                        TcsAmount = tcsAmount,
                        CumulativeValue = cumulativeValue,
                        ThresholdAmount = config.ThresholdAmount,
                        Reason = $"TCS @ {effectiveRate}% on {taxableAmount:N2} (amount exceeding threshold)"
                    };
                }
                else
                {
                    // No threshold check - TCS on full amount
                    var tcsAmount = Math.Round(request.TransactionValue * effectiveRate / 100, 0);

                    return new TcsCalculationResult
                    {
                        IsApplicable = true,
                        SectionCode = request.SectionCode,
                        SectionDescription = config.Description,
                        TransactionValue = request.TransactionValue,
                        TcsRate = effectiveRate,
                        TcsAmount = tcsAmount,
                        ThresholdAmount = config.ThresholdAmount,
                        Reason = $"TCS @ {effectiveRate}% on {request.TransactionValue:N2}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating TCS for section {SectionCode}", request.SectionCode);
                throw;
            }
        }

        public async Task<bool> IsTcsApplicableAsync(Guid companyId, string customerPan, string financialYear)
        {
            if (string.IsNullOrEmpty(customerPan))
                return false;

            var cumulativeValue = await GetCumulativeValueAsync(companyId, customerPan, financialYear);
            return cumulativeValue > 5000000m; // Rs.50L threshold for 206C(1H)
        }

        public async Task<decimal> GetCumulativeValueAsync(Guid companyId, string customerPan, string financialYear)
        {
            if (string.IsNullOrEmpty(customerPan))
                return 0;

            return await _tcsRepository.GetCumulativePartyValueAsync(companyId, customerPan, financialYear);
        }

        // ==================== TCS Collection ====================

        /// <summary>
        /// Posts TCS collection entry when invoice is raised.
        ///
        /// Journal Entry:
        /// Dr. Customer Receivable            [Invoice Amount + TCS]
        ///     Cr. TCS Payable (2282)         [TCS Amount]
        ///
        /// Note: The Sales Revenue credit is posted separately with the invoice.
        /// This entry only handles the TCS component.
        /// </summary>
        public async Task<TcsPostingResult> PostTcsCollectionAsync(TcsCollectionRequest request, Guid? postedBy = null)
        {
            try
            {
                if (request.TcsAmount <= 0)
                {
                    return TcsPostingResult.Failed("TCS amount must be greater than zero");
                }

                // Idempotency check
                if (request.InvoiceId.HasValue)
                {
                    var existing = await _tcsRepository.GetByInvoiceAsync(request.InvoiceId.Value);
                    if (existing != null)
                    {
                        _logger.LogInformation(
                            "TCS transaction already exists for invoice {InvoiceId}. Returning existing.",
                            request.InvoiceId);
                        return TcsPostingResult.Succeeded(existing);
                    }
                }

                var idempotencyKey = $"TCS_COLLECTION_{request.InvoiceId ?? Guid.NewGuid()}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation("Journal already exists for TCS {IdempotencyKey}", idempotencyKey);
                }

                var financialYear = GetFinancialYear(request.TransactionDate);
                var quarter = GetQuarter(request.TransactionDate);
                var periodMonth = GetPeriodMonth(request.TransactionDate);

                // Create TCS transaction record
                var tcsTransaction = new TcsTransaction
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    TransactionType = "collected",
                    SectionCode = request.SectionCode,
                    TransactionDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    Quarter = quarter,
                    PartyType = "customer",
                    PartyId = request.CustomerId,
                    PartyName = request.CustomerName,
                    PartyPan = request.CustomerPan,
                    PartyGstin = request.CustomerGstin,
                    TransactionValue = request.TransactionValue,
                    TcsRate = request.TcsRate,
                    TcsAmount = request.TcsAmount,
                    InvoiceId = request.InvoiceId,
                    Status = TcsTransactionStatus.Collected,
                    CollectedAt = DateTime.UtcNow,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = postedBy
                };

                // Update cumulative value if PAN available
                if (!string.IsNullOrEmpty(request.CustomerPan))
                {
                    var cumulative = await GetCumulativeValueAsync(request.CompanyId, request.CustomerPan, financialYear);
                    tcsTransaction.CumulativeValueFy = cumulative + request.TransactionValue;
                    tcsTransaction.ThresholdAmount = 5000000m; // Rs.50L threshold
                }

                // Build narration
                var narration = BuildCollectionNarration(request);

                // Create journal entry
                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "tcs_transaction",
                    SourceNumber = request.InvoiceNumber,
                    Description = $"TCS Collection - {request.SectionCode} - {request.CustomerName}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "TCS_COLLECTION",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Add journal lines
                await AddCollectionLines(entry, request);

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "TCS collection journal is not balanced. Debit: {Debit:C}, Credit: {Credit:C}",
                        entry.TotalDebit, entry.TotalCredit);
                    return TcsPostingResult.Failed("Journal entry is not balanced");
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update TCS transaction with journal reference
                tcsTransaction.JournalEntryId = savedEntry.Id;

                // Save TCS transaction
                var savedTcs = await _tcsRepository.AddAsync(tcsTransaction);

                // Update journal with TCS transaction reference
                savedEntry.SourceId = savedTcs.Id;
                await _journalRepository.UpdateAsync(savedEntry);

                _logger.LogInformation(
                    "Created TCS collection journal {JournalNumber} for {SectionCode} - {CustomerName}. " +
                    "Transaction Value: {Value:C}, TCS: {TcsAmount:C}",
                    savedEntry.JournalNumber, request.SectionCode, request.CustomerName,
                    request.TransactionValue, request.TcsAmount);

                return TcsPostingResult.Succeeded(savedTcs, savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting TCS collection");
                throw;
            }
        }

        public async Task<TcsPostingResult> PostTcsFromInvoiceAsync(Guid invoiceId, Guid? postedBy = null)
        {
            // This would typically integrate with InvoiceService
            // For now, return a not implemented result
            _logger.LogWarning("PostTcsFromInvoiceAsync not fully implemented. Invoice: {InvoiceId}", invoiceId);
            return TcsPostingResult.Failed("Integration with Invoice service not yet implemented");
        }

        // ==================== TCS Remittance ====================

        /// <summary>
        /// Posts TCS remittance to government.
        ///
        /// Journal Entry:
        /// Dr. TCS Payable (2282)             [TCS Amount]
        ///     Cr. Bank Account               [TCS Amount]
        /// </summary>
        public async Task<TcsPostingResult> PostTcsRemittanceAsync(
            Guid tcsTransactionId,
            TcsRemittanceRequest remittanceDetails,
            Guid? postedBy = null)
        {
            try
            {
                var tcsTransaction = await _tcsRepository.GetByIdAsync(tcsTransactionId);
                if (tcsTransaction == null)
                {
                    return TcsPostingResult.Failed($"TCS transaction {tcsTransactionId} not found");
                }

                if (tcsTransaction.Status == TcsTransactionStatus.Remitted)
                {
                    _logger.LogInformation("TCS {TcsId} already remitted", tcsTransactionId);
                    return TcsPostingResult.Succeeded(tcsTransaction);
                }

                var idempotencyKey = $"TCS_REMITTANCE_{tcsTransactionId}";
                var journalDate = DateOnly.FromDateTime(remittanceDetails.RemittanceDate);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                // Build narration
                var narration = BuildRemittanceNarration(tcsTransaction, remittanceDetails);

                // Create remittance journal entry
                var entry = new JournalEntry
                {
                    CompanyId = tcsTransaction.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "tcs_remittance",
                    SourceId = tcsTransactionId,
                    Description = $"TCS Remittance - {tcsTransaction.SectionCode} - Challan {remittanceDetails.ChallanNumber}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "TCS_REMITTANCE",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Get TCS payable account
                var tcsPayableCode = GetTcsPayableAccountCode(tcsTransaction.SectionCode);
                await AddLineIfAccountExists(entry, tcsTransaction.CompanyId, tcsPayableCode,
                    tcsTransaction.TcsAmount, 0,
                    $"TCS Remittance - {tcsTransaction.PartyName}");

                // Credit bank account
                await AddLineIfAccountExists(entry, tcsTransaction.CompanyId, remittanceDetails.BankAccountCode,
                    0, tcsTransaction.TcsAmount,
                    $"TCS Payment - Challan {remittanceDetails.ChallanNumber}");

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "TCS remittance journal is not balanced. Debit: {Debit:C}, Credit: {Credit:C}",
                        entry.TotalDebit, entry.TotalCredit);
                    return TcsPostingResult.Failed("Journal entry is not balanced");
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update TCS transaction
                tcsTransaction.Status = TcsTransactionStatus.Remitted;
                tcsTransaction.RemittedAt = remittanceDetails.RemittanceDate;
                tcsTransaction.ChallanNumber = remittanceDetails.ChallanNumber;
                tcsTransaction.BsrCode = remittanceDetails.BsrCode;
                tcsTransaction.UpdatedAt = DateTime.UtcNow;

                await _tcsRepository.UpdateAsync(tcsTransaction);

                _logger.LogInformation(
                    "Created TCS remittance journal {JournalNumber} for {TcsId}. " +
                    "Amount: {TcsAmount:C}, Challan: {Challan}",
                    savedEntry.JournalNumber, tcsTransactionId,
                    tcsTransaction.TcsAmount, remittanceDetails.ChallanNumber);

                return TcsPostingResult.Succeeded(tcsTransaction, savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting TCS remittance for {TcsTransactionId}", tcsTransactionId);
                throw;
            }
        }

        public async Task<TcsBulkRemittanceResult> PostBulkRemittanceAsync(
            IEnumerable<Guid> tcsTransactionIds,
            TcsRemittanceRequest remittanceDetails,
            Guid? postedBy = null)
        {
            var result = new TcsBulkRemittanceResult();
            var transactionList = tcsTransactionIds.ToList();
            result.TotalTransactions = transactionList.Count;

            foreach (var tcsId in transactionList)
            {
                var postResult = await PostTcsRemittanceAsync(tcsId, remittanceDetails, postedBy);
                if (postResult.Success)
                {
                    result.SuccessCount++;
                    result.TotalAmount += postResult.Transaction?.TcsAmount ?? 0;
                }
                else
                {
                    result.FailedCount++;
                    result.Errors.Add($"TCS {tcsId}: {postResult.ErrorMessage}");
                }
            }

            result.Success = result.FailedCount == 0;
            return result;
        }

        // ==================== TCS Received ====================

        public async Task<TcsPostingResult> RecordTcsPaidAsync(TcsPaidRequest request, Guid? postedBy = null)
        {
            try
            {
                var idempotencyKey = $"TCS_PAID_{request.PaymentId ?? Guid.NewGuid()}";
                var financialYear = GetFinancialYear(request.TransactionDate);
                var quarter = GetQuarter(request.TransactionDate);

                // Create TCS transaction record
                var tcsTransaction = new TcsTransaction
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    TransactionType = "paid",
                    SectionCode = request.SectionCode,
                    TransactionDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    Quarter = quarter,
                    PartyType = "vendor",
                    PartyId = request.VendorId,
                    PartyName = request.VendorName,
                    PartyPan = request.VendorPan,
                    PartyGstin = request.VendorGstin,
                    TransactionValue = request.TransactionValue,
                    TcsRate = request.TcsRate,
                    TcsAmount = request.TcsAmount,
                    PaymentId = request.PaymentId,
                    Status = TcsTransactionStatus.Collected,
                    CollectedAt = DateTime.UtcNow,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = postedBy
                };

                // Save TCS transaction (no separate journal entry needed as it's part of payment)
                var savedTcs = await _tcsRepository.AddAsync(tcsTransaction);

                _logger.LogInformation(
                    "Recorded TCS paid for {SectionCode} - {VendorName}. " +
                    "Transaction Value: {Value:C}, TCS: {TcsAmount:C}",
                    request.SectionCode, request.VendorName,
                    request.TransactionValue, request.TcsAmount);

                return TcsPostingResult.Succeeded(savedTcs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording TCS paid");
                throw;
            }
        }

        // ==================== Form 27EQ ====================

        public async Task MarkForm27EqFiledAsync(IEnumerable<Guid> transactionIds, string acknowledgement)
        {
            foreach (var id in transactionIds)
            {
                await _tcsRepository.MarkForm27EqFiledAsync(id, acknowledgement);
            }
        }

        public async Task<TcsQuarterlySummary> GetForm27EqDataAsync(Guid companyId, string financialYear, string quarter)
        {
            return await _tcsRepository.GetQuarterlySummaryAsync(companyId, financialYear, quarter);
        }

        // ==================== Queries ====================

        public async Task<IEnumerable<TcsTransaction>> GetPendingRemittanceAsync(Guid companyId)
        {
            return await _tcsRepository.GetPendingRemittanceAsync(companyId);
        }

        public async Task<TcsQuarterlySummary> GetQuarterlySummaryAsync(Guid companyId, string financialYear, string quarter)
        {
            return await _tcsRepository.GetQuarterlySummaryAsync(companyId, financialYear, quarter);
        }

        // ==================== Reversal ====================

        public async Task<JournalEntry?> ReverseCollectionAsync(Guid tcsTransactionId, Guid reversedBy, string reason)
        {
            try
            {
                var tcsTransaction = await _tcsRepository.GetByIdAsync(tcsTransactionId);
                if (tcsTransaction == null)
                {
                    _logger.LogWarning("TCS transaction {TcsId} not found for reversal", tcsTransactionId);
                    return null;
                }

                if (tcsTransaction.Status == TcsTransactionStatus.Remitted)
                {
                    _logger.LogWarning("Cannot reverse TCS {TcsId} after remittance", tcsTransactionId);
                    return null;
                }

                if (!tcsTransaction.JournalEntryId.HasValue)
                {
                    _logger.LogWarning("No journal to reverse for TCS {TcsId}", tcsTransactionId);
                    return null;
                }

                var reversalEntry = await _journalRepository.CreateReversalAsync(
                    tcsTransaction.JournalEntryId.Value,
                    reversedBy,
                    reason);

                if (reversalEntry != null)
                {
                    tcsTransaction.Status = TcsTransactionStatus.Cancelled;
                    tcsTransaction.UpdatedAt = DateTime.UtcNow;
                    await _tcsRepository.UpdateAsync(tcsTransaction);
                }

                return reversalEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing TCS collection for {TcsTransactionId}", tcsTransactionId);
                throw;
            }
        }

        // ==================== Section Configuration ====================

        public Task<TcsSectionInfo?> GetSectionInfoAsync(string sectionCode)
        {
            if (!SectionConfigs.TryGetValue(sectionCode, out var config))
            {
                return Task.FromResult<TcsSectionInfo?>(null);
            }

            return Task.FromResult<TcsSectionInfo?>(new TcsSectionInfo
            {
                SectionCode = config.SectionCode,
                Description = config.Description,
                Rate = config.Rate,
                HigherRate = config.HigherRate,
                ThresholdAmount = config.ThresholdAmount,
                ApplicableGoodsServices = config.ApplicableGoodsServices,
                AccountCode = config.AccountCode,
                IsActive = true
            });
        }

        public Task<IEnumerable<TcsSectionInfo>> GetAllSectionsAsync()
        {
            var sections = SectionConfigs.Values.Select(c => new TcsSectionInfo
            {
                SectionCode = c.SectionCode,
                Description = c.Description,
                Rate = c.Rate,
                HigherRate = c.HigherRate,
                ThresholdAmount = c.ThresholdAmount,
                ApplicableGoodsServices = c.ApplicableGoodsServices,
                AccountCode = c.AccountCode,
                IsActive = true
            });

            return Task.FromResult(sections);
        }

        // ==================== Private Helper Methods ====================

        private async Task AddCollectionLines(JournalEntry entry, TcsCollectionRequest request)
        {
            var companyId = request.CompanyId;

            // DEBIT: Customer Receivable
            var customerAccountCode = !string.IsNullOrEmpty(request.CustomerReceivableAccountCode)
                ? request.CustomerReceivableAccountCode
                : AccountCodes.SundryDebtors;

            await AddLineIfAccountExists(entry, companyId, customerAccountCode,
                request.TcsAmount, 0,
                $"TCS from {request.CustomerName} - {request.InvoiceNumber}",
                "customer", request.CustomerId);

            // CREDIT: TCS Payable
            var tcsPayableCode = GetTcsPayableAccountCode(request.SectionCode);
            await AddLineIfAccountExists(entry, companyId, tcsPayableCode,
                0, request.TcsAmount,
                $"TCS Payable {request.SectionCode} @ {request.TcsRate}%");
        }

        private async Task AddLineIfAccountExists(
            JournalEntry entry,
            Guid companyId,
            string accountCode,
            decimal debitAmount,
            decimal creditAmount,
            string description,
            string? subledgerType = null,
            Guid? subledgerId = null)
        {
            var account = await _accountRepository.GetByCodeAsync(companyId, accountCode);
            if (account == null)
            {
                _logger.LogWarning(
                    "Account {AccountCode} not found for company {CompanyId}. Skipping line: {Description}",
                    accountCode, companyId, description);
                return;
            }

            entry.Lines!.Add(new JournalEntryLine
            {
                AccountId = account.Id,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                Description = description,
                Currency = "INR",
                ExchangeRate = 1,
                SubledgerType = subledgerType,
                SubledgerId = subledgerId
            });
        }

        private static string GetTcsPayableAccountCode(string sectionCode)
        {
            return SectionConfigs.TryGetValue(sectionCode, out var config)
                ? config.AccountCode ?? AccountCodes.TcsPayable
                : AccountCodes.TcsPayable;
        }

        private static string BuildCollectionNarration(TcsCollectionRequest request)
        {
            var narration = $"Being TCS collected u/s {request.SectionCode}. ";
            narration += $"Customer: {request.CustomerName}. ";

            if (!string.IsNullOrEmpty(request.CustomerPan))
            {
                narration += $"PAN: {request.CustomerPan}. ";
            }
            else
            {
                narration += "PAN NOT AVAILABLE (Higher Rate Applied). ";
            }

            if (!string.IsNullOrEmpty(request.InvoiceNumber))
            {
                narration += $"Invoice: {request.InvoiceNumber}. ";
            }

            narration += $"Transaction Value: Rs.{request.TransactionValue:N2}. ";
            narration += $"TCS @ {request.TcsRate}%: Rs.{request.TcsAmount:N2}.";

            return narration;
        }

        private static string BuildRemittanceNarration(TcsTransaction tcs, TcsRemittanceRequest remittance)
        {
            var narration = $"Being TCS remittance to government u/s {tcs.SectionCode}. ";
            narration += $"Collected from: {tcs.PartyName}. ";
            narration += $"TCS Amount: Rs.{tcs.TcsAmount:N2}. ";
            narration += $"Challan No: {remittance.ChallanNumber}. ";

            if (!string.IsNullOrEmpty(remittance.BsrCode))
            {
                narration += $"BSR Code: {remittance.BsrCode}. ";
            }

            if (!string.IsNullOrEmpty(remittance.PaymentReference))
            {
                narration += $"Payment Ref: {remittance.PaymentReference}.";
            }

            return narration;
        }

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }

        private static string GetQuarter(DateOnly date)
        {
            return date.Month switch
            {
                >= 4 and <= 6 => "Q1",
                >= 7 and <= 9 => "Q2",
                >= 10 and <= 12 => "Q3",
                _ => "Q4"
            };
        }

        /// <summary>
        /// Internal configuration for TCS sections
        /// </summary>
        private class TcsSectionConfig
        {
            public string SectionCode { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public decimal Rate { get; init; }
            public decimal? HigherRate { get; init; }
            public decimal? ThresholdAmount { get; init; }
            public string? ApplicableGoodsServices { get; init; }
            public string? AccountCode { get; init; }
        }
    }
}
