using Core.Entities.Gst;
using Core.Entities.Ledger;
using Core.Interfaces.Expense;
using Core.Interfaces.Gst;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Gst
{
    /// <summary>
    /// Service for posting RCM (Reverse Charge Mechanism) journal entries.
    ///
    /// Implements two-stage journal model as per GST Act Section 9(3), 9(4):
    /// - Stage 1: RCM liability recognition on expense/purchase
    /// - Stage 2: RCM payment to government + ITC claim (if eligible)
    ///
    /// References:
    /// - GST Act Section 9(3), 9(4) - RCM provisions
    /// - Notification 13/2017 - Central Tax (Rate) - RCM categories
    /// - Section 17(5) - Blocked ITC categories
    /// - ClearTax: https://cleartax.in/s/reverse-charge-mechanism-gst
    /// </summary>
    public class RcmPostingService : IRcmPostingService
    {
        private readonly IRcmTransactionRepository _rcmRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IExpenseClaimRepository _expenseRepository;
        private readonly ILogger<RcmPostingService> _logger;

        /// <summary>
        /// Account codes following standard Indian chart of accounts
        /// </summary>
        private static class AccountCodes
        {
            // RCM Liability Accounts (2xxx)
            public const string RcmCgstPayable = "2256";
            public const string RcmSgstPayable = "2257";
            public const string RcmIgstPayable = "2258";

            // RCM ITC Claimable (1xxx)
            public const string RcmCgstItcClaimable = "1146";
            public const string RcmSgstItcClaimable = "1147";
            public const string RcmIgstItcClaimable = "1148";

            // Regular ITC Accounts (for claiming after payment)
            public const string CgstInput = "1141";
            public const string SgstInput = "1142";
            public const string IgstInput = "1143";

            // ITC Blocked Account
            public const string ItcBlocked = "1149";

            // Default expense account
            public const string GeneralExpenses = "5100";

            // Default vendor payable
            public const string SundryCreditors = "2105";
        }

        public RcmPostingService(
            IRcmTransactionRepository rcmRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            IExpenseClaimRepository expenseRepository,
            ILogger<RcmPostingService> logger)
        {
            _rcmRepository = rcmRepository ?? throw new ArgumentNullException(nameof(rcmRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== Stage 1: Liability Recognition ====================

        /// <summary>
        /// Creates RCM liability entry when expense with RCM is recorded.
        ///
        /// Journal Entry (Intra-State):
        /// Dr. Expense Account              [Base Amount]
        ///     Cr. Vendor Payable           [Base Amount]
        ///     Cr. RCM CGST Payable (2256)  [CGST Amount]
        ///     Cr. RCM SGST Payable (2257)  [SGST Amount]
        ///
        /// Journal Entry (Inter-State):
        /// Dr. Expense Account              [Base Amount]
        ///     Cr. Vendor Payable           [Base Amount]
        ///     Cr. RCM IGST Payable (2258)  [IGST Amount]
        /// </summary>
        public async Task<RcmPostingResult> PostRcmLiabilityAsync(RcmLiabilityRequest request, Guid? postedBy = null)
        {
            try
            {
                // Validate request
                if (request.TaxableValue <= 0)
                {
                    return RcmPostingResult.Failed("Taxable value must be greater than zero");
                }

                if (string.IsNullOrEmpty(request.RcmCategoryCode))
                {
                    return RcmPostingResult.Failed("RCM category code is required");
                }

                // Idempotency check
                if (request.SourceId.HasValue)
                {
                    var existing = await _rcmRepository.GetBySourceAsync(request.SourceType, request.SourceId.Value);
                    if (existing != null)
                    {
                        _logger.LogInformation(
                            "RCM transaction already exists for source {SourceType}/{SourceId}. Returning existing.",
                            request.SourceType, request.SourceId);
                        return RcmPostingResult.Succeeded(existing);
                    }
                }

                var idempotencyKey = $"RCM_LIABILITY_{request.SourceType}_{request.SourceId ?? Guid.NewGuid()}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation(
                        "Journal already exists for RCM liability {IdempotencyKey}. Returning existing.",
                        idempotencyKey);
                }

                // Calculate total RCM tax
                var totalRcmTax = request.CgstAmount + request.SgstAmount + request.IgstAmount + request.CessAmount;

                // Determine journal date
                var journalDate = request.VendorInvoiceDate.HasValue
                    ? DateOnly.FromDateTime(request.VendorInvoiceDate.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);
                var returnPeriod = GetReturnPeriod(journalDate);

                // Create RCM transaction record
                var rcmTransaction = new RcmTransaction
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    FinancialYear = financialYear,
                    ReturnPeriod = returnPeriod,
                    SourceType = request.SourceType,
                    SourceId = request.SourceId,
                    SourceNumber = request.SourceNumber,
                    VendorName = request.VendorName,
                    VendorGstin = request.VendorGstin,
                    VendorPan = request.VendorPan,
                    VendorStateCode = request.VendorStateCode,
                    VendorInvoiceNumber = request.VendorInvoiceNumber,
                    VendorInvoiceDate = request.VendorInvoiceDate,
                    RcmCategoryCode = request.RcmCategoryCode,
                    PlaceOfSupply = request.PlaceOfSupply,
                    SupplyType = request.SupplyType,
                    HsnSacCode = request.HsnSacCode,
                    Description = request.Description,
                    TaxableValue = request.TaxableValue,
                    CgstRate = request.CgstRate,
                    CgstAmount = request.CgstAmount,
                    SgstRate = request.SgstRate,
                    SgstAmount = request.SgstAmount,
                    IgstRate = request.IgstRate,
                    IgstAmount = request.IgstAmount,
                    CessRate = request.CessRate,
                    CessAmount = request.CessAmount,
                    TotalRcmTax = totalRcmTax,
                    ItcEligible = request.ItcEligible,
                    Status = RcmTransactionStatus.Pending,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = postedBy
                };

                // Build narration
                var narration = BuildLiabilityNarration(request, totalRcmTax);

                // Create journal entry
                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "rcm_transaction",
                    SourceNumber = request.VendorInvoiceNumber ?? request.SourceNumber,
                    Description = $"RCM Liability - {request.RcmCategoryCode} - {request.VendorName}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "RCM_LIABILITY",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Add journal lines
                await AddLiabilityLines(entry, request, totalRcmTax);

                // Calculate and validate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "RCM liability journal is not balanced. Debit: {Debit:C}, Credit: {Credit:C}",
                        entry.TotalDebit, entry.TotalCredit);
                    return RcmPostingResult.Failed("Journal entry is not balanced");
                }

                if (!entry.Lines.Any())
                {
                    _logger.LogError("No journal lines created for RCM liability. Check if accounts exist.");
                    return RcmPostingResult.Failed("No journal lines created. Check account configuration.");
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update RCM transaction with journal reference
                rcmTransaction.LiabilityRecognized = true;
                rcmTransaction.LiabilityRecognizedAt = DateTime.UtcNow;
                rcmTransaction.LiabilityJournalId = savedEntry.Id;
                rcmTransaction.Status = RcmTransactionStatus.LiabilityCreated;

                // Save RCM transaction
                var savedRcm = await _rcmRepository.AddAsync(rcmTransaction);

                // Update journal with RCM transaction reference
                savedEntry.SourceId = savedRcm.Id;
                await _journalRepository.UpdateAsync(savedEntry);

                _logger.LogInformation(
                    "Created RCM liability journal {JournalNumber} for {Category} - {VendorName}. " +
                    "Taxable: {Taxable:C}, RCM Tax: {RcmTax:C}",
                    savedEntry.JournalNumber, request.RcmCategoryCode, request.VendorName,
                    request.TaxableValue, totalRcmTax);

                return RcmPostingResult.Succeeded(savedRcm, savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating RCM liability entry");
                throw;
            }
        }

        public async Task<RcmPostingResult> PostRcmLiabilityFromExpenseAsync(Guid expenseClaimId, Guid? postedBy = null)
        {
            try
            {
                var claim = await _expenseRepository.GetByIdAsync(expenseClaimId);
                if (claim == null)
                {
                    return RcmPostingResult.Failed($"Expense claim {expenseClaimId} not found");
                }

                // Check if RCM already exists for this expense
                var existing = await _rcmRepository.GetBySourceAsync("expense_claim", expenseClaimId);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "RCM transaction already exists for expense claim {ClaimId}",
                        expenseClaimId);
                    return RcmPostingResult.Succeeded(existing);
                }

                // Build RCM request from expense claim
                var request = new RcmLiabilityRequest
                {
                    CompanyId = claim.CompanyId,
                    SourceType = "expense_claim",
                    SourceId = expenseClaimId,
                    SourceNumber = claim.ClaimNumber,
                    VendorName = claim.VendorName ?? "Unknown Vendor",
                    VendorGstin = claim.VendorGstin,
                    VendorInvoiceNumber = claim.InvoiceNumber,
                    VendorInvoiceDate = claim.InvoiceDate,
                    RcmCategoryCode = DetermineRcmCategory(claim),
                    PlaceOfSupply = "", // Derived from company location if needed
                    SupplyType = claim.SupplyType ?? "intra_state",
                    HsnSacCode = claim.HsnSacCode,
                    Description = claim.Title,
                    TaxableValue = claim.BaseAmount ?? (claim.Amount - claim.TotalGstAmount),
                    CgstRate = claim.CgstRate,
                    CgstAmount = claim.CgstAmount,
                    SgstRate = claim.SgstRate,
                    SgstAmount = claim.SgstAmount,
                    IgstRate = claim.IgstRate,
                    IgstAmount = claim.IgstAmount,
                    ExpenseAccountCode = AccountCodes.GeneralExpenses,
                    VendorPayableAccountCode = AccountCodes.SundryCreditors,
                    ItcEligible = claim.ItcEligible,
                    Notes = $"RCM from expense claim {claim.ClaimNumber}"
                };

                return await PostRcmLiabilityAsync(request, postedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating RCM from expense claim {ExpenseClaimId}", expenseClaimId);
                throw;
            }
        }

        // ==================== Stage 2: RCM Payment + ITC ====================

        /// <summary>
        /// Posts RCM payment to government and claims ITC (if eligible).
        ///
        /// Journal Entry (Intra-State, ITC Eligible):
        /// Dr. RCM CGST Payable (2256)     [CGST]
        /// Dr. RCM SGST Payable (2257)     [SGST]
        /// Dr. CGST Input (1141)           [CGST - ITC Claim]
        /// Dr. SGST Input (1142)           [SGST - ITC Claim]
        ///     Cr. Bank Account            [Total RCM Tax]
        ///     Cr. RCM CGST ITC (1146)     [CGST - ITC Claim]
        ///     Cr. RCM SGST ITC (1147)     [SGST - ITC Claim]
        ///
        /// If ITC is blocked (Section 17(5)):
        /// Dr. RCM CGST Payable (2256)     [CGST]
        /// Dr. RCM SGST Payable (2257)     [SGST]
        /// Dr. ITC Blocked (1149)          [Total Tax - as expense]
        ///     Cr. Bank Account            [Total RCM Tax]
        /// </summary>
        public async Task<RcmPostingResult> PostRcmPaymentAsync(
            Guid rcmTransactionId,
            RcmPaymentRequest paymentDetails,
            Guid? postedBy = null)
        {
            try
            {
                var rcmTransaction = await _rcmRepository.GetByIdAsync(rcmTransactionId);
                if (rcmTransaction == null)
                {
                    return RcmPostingResult.Failed($"RCM transaction {rcmTransactionId} not found");
                }

                // Validate state
                if (!rcmTransaction.LiabilityRecognized)
                {
                    return RcmPostingResult.Failed("RCM liability must be recognized before payment");
                }

                if (rcmTransaction.RcmPaid)
                {
                    _logger.LogInformation("RCM {RcmId} already paid", rcmTransactionId);
                    return RcmPostingResult.Succeeded(rcmTransaction);
                }

                var idempotencyKey = $"RCM_PAYMENT_{rcmTransactionId}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation(
                        "Payment journal already exists for RCM {RcmId}",
                        rcmTransactionId);
                }

                var journalDate = DateOnly.FromDateTime(paymentDetails.PaymentDate);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                // Build narration
                var narration = BuildPaymentNarration(rcmTransaction, paymentDetails);

                // Create payment journal entry
                var entry = new JournalEntry
                {
                    CompanyId = rcmTransaction.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "rcm_transaction",
                    SourceId = rcmTransactionId,
                    SourceNumber = rcmTransaction.VendorInvoiceNumber ?? rcmTransaction.SourceNumber,
                    Description = $"RCM Payment - {rcmTransaction.RcmCategoryCode} - {rcmTransaction.VendorName}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "RCM_PAYMENT",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Add payment journal lines
                await AddPaymentLines(entry, rcmTransaction, paymentDetails);

                // Calculate and validate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "RCM payment journal is not balanced. Debit: {Debit:C}, Credit: {Credit:C}",
                        entry.TotalDebit, entry.TotalCredit);
                    return RcmPostingResult.Failed("Journal entry is not balanced");
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update RCM transaction
                rcmTransaction.RcmPaid = true;
                rcmTransaction.RcmPaymentDate = paymentDetails.PaymentDate;
                rcmTransaction.RcmPaymentJournalId = savedEntry.Id;
                rcmTransaction.RcmPaymentReference = paymentDetails.PaymentReference;
                rcmTransaction.Status = RcmTransactionStatus.RcmPaid;
                rcmTransaction.UpdatedAt = DateTime.UtcNow;

                // Handle ITC claim if requested
                if (paymentDetails.ClaimItcNow && rcmTransaction.ItcEligible && !rcmTransaction.ItcBlocked)
                {
                    rcmTransaction.ItcClaimed = true;
                    rcmTransaction.ItcClaimDate = paymentDetails.PaymentDate;
                    rcmTransaction.ItcClaimJournalId = savedEntry.Id; // Same journal for combined posting
                    rcmTransaction.ItcClaimPeriod = paymentDetails.ItcClaimPeriod ?? GetReturnPeriod(journalDate);
                    rcmTransaction.Status = RcmTransactionStatus.ItcClaimed;
                }

                await _rcmRepository.UpdateAsync(rcmTransaction);

                _logger.LogInformation(
                    "Created RCM payment journal {JournalNumber} for {RcmId}. " +
                    "RCM Tax: {RcmTax:C}, ITC Claimed: {ItcClaimed}",
                    savedEntry.JournalNumber, rcmTransactionId,
                    rcmTransaction.TotalRcmTax, rcmTransaction.ItcClaimed);

                return RcmPostingResult.Succeeded(rcmTransaction, savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting RCM payment for {RcmTransactionId}", rcmTransactionId);
                throw;
            }
        }

        public async Task<RcmPostingResult> ClaimItcAsync(Guid rcmTransactionId, string claimPeriod, Guid? postedBy = null)
        {
            try
            {
                var rcmTransaction = await _rcmRepository.GetByIdAsync(rcmTransactionId);
                if (rcmTransaction == null)
                {
                    return RcmPostingResult.Failed($"RCM transaction {rcmTransactionId} not found");
                }

                if (!rcmTransaction.RcmPaid)
                {
                    return RcmPostingResult.Failed("RCM must be paid before claiming ITC");
                }

                if (rcmTransaction.ItcClaimed)
                {
                    _logger.LogInformation("ITC already claimed for RCM {RcmId}", rcmTransactionId);
                    return RcmPostingResult.Succeeded(rcmTransaction);
                }

                if (!rcmTransaction.ItcEligible || rcmTransaction.ItcBlocked)
                {
                    return RcmPostingResult.Failed("ITC is not eligible or blocked for this transaction");
                }

                // Update RCM transaction
                rcmTransaction.ItcClaimed = true;
                rcmTransaction.ItcClaimDate = DateTime.UtcNow;
                rcmTransaction.ItcClaimPeriod = claimPeriod;
                rcmTransaction.Status = RcmTransactionStatus.ItcClaimed;
                rcmTransaction.UpdatedAt = DateTime.UtcNow;

                await _rcmRepository.UpdateAsync(rcmTransaction);

                _logger.LogInformation(
                    "ITC claimed for RCM {RcmId} in period {Period}",
                    rcmTransactionId, claimPeriod);

                return RcmPostingResult.Succeeded(rcmTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming ITC for RCM {RcmTransactionId}", rcmTransactionId);
                throw;
            }
        }

        public async Task<RcmPostingResult> BlockItcAsync(Guid rcmTransactionId, string blockReason, Guid? postedBy = null)
        {
            try
            {
                var rcmTransaction = await _rcmRepository.GetByIdAsync(rcmTransactionId);
                if (rcmTransaction == null)
                {
                    return RcmPostingResult.Failed($"RCM transaction {rcmTransactionId} not found");
                }

                if (rcmTransaction.ItcClaimed)
                {
                    return RcmPostingResult.Failed("Cannot block ITC that has already been claimed");
                }

                rcmTransaction.ItcBlocked = true;
                rcmTransaction.ItcBlockedReason = blockReason;
                rcmTransaction.ItcEligible = false;
                rcmTransaction.Status = RcmTransactionStatus.ItcBlocked;
                rcmTransaction.UpdatedAt = DateTime.UtcNow;

                await _rcmRepository.UpdateAsync(rcmTransaction);

                _logger.LogInformation(
                    "ITC blocked for RCM {RcmId}. Reason: {Reason}",
                    rcmTransactionId, blockReason);

                return RcmPostingResult.Succeeded(rcmTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking ITC for RCM {RcmTransactionId}", rcmTransactionId);
                throw;
            }
        }

        // ==================== Reversal ====================

        public async Task<JournalEntry?> ReverseLiabilityAsync(
            Guid rcmTransactionId,
            Guid reversedBy,
            string reason)
        {
            try
            {
                var rcmTransaction = await _rcmRepository.GetByIdAsync(rcmTransactionId);
                if (rcmTransaction == null)
                {
                    _logger.LogWarning("RCM transaction {RcmId} not found for reversal", rcmTransactionId);
                    return null;
                }

                if (rcmTransaction.RcmPaid)
                {
                    _logger.LogWarning("Cannot reverse RCM liability after payment for {RcmId}", rcmTransactionId);
                    return null;
                }

                if (!rcmTransaction.LiabilityJournalId.HasValue)
                {
                    _logger.LogWarning("No liability journal to reverse for RCM {RcmId}", rcmTransactionId);
                    return null;
                }

                var reversalEntry = await _journalRepository.CreateReversalAsync(
                    rcmTransaction.LiabilityJournalId.Value,
                    reversedBy,
                    reason);

                if (reversalEntry != null)
                {
                    rcmTransaction.Status = RcmTransactionStatus.Cancelled;
                    rcmTransaction.UpdatedAt = DateTime.UtcNow;
                    await _rcmRepository.UpdateAsync(rcmTransaction);
                }

                return reversalEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing RCM liability for {RcmTransactionId}", rcmTransactionId);
                throw;
            }
        }

        public async Task<JournalEntry?> ReversePaymentAsync(
            Guid rcmTransactionId,
            Guid reversedBy,
            string reason)
        {
            try
            {
                var rcmTransaction = await _rcmRepository.GetByIdAsync(rcmTransactionId);
                if (rcmTransaction == null)
                {
                    _logger.LogWarning("RCM transaction {RcmId} not found for payment reversal", rcmTransactionId);
                    return null;
                }

                if (!rcmTransaction.RcmPaymentJournalId.HasValue)
                {
                    _logger.LogWarning("No payment journal to reverse for RCM {RcmId}", rcmTransactionId);
                    return null;
                }

                var reversalEntry = await _journalRepository.CreateReversalAsync(
                    rcmTransaction.RcmPaymentJournalId.Value,
                    reversedBy,
                    reason);

                if (reversalEntry != null)
                {
                    rcmTransaction.RcmPaid = false;
                    rcmTransaction.RcmPaymentDate = null;
                    rcmTransaction.RcmPaymentJournalId = null;
                    rcmTransaction.ItcClaimed = false;
                    rcmTransaction.ItcClaimDate = null;
                    rcmTransaction.ItcClaimJournalId = null;
                    rcmTransaction.Status = RcmTransactionStatus.LiabilityCreated;
                    rcmTransaction.UpdatedAt = DateTime.UtcNow;
                    await _rcmRepository.UpdateAsync(rcmTransaction);
                }

                return reversalEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing RCM payment for {RcmTransactionId}", rcmTransactionId);
                throw;
            }
        }

        // ==================== Queries ====================

        public async Task<IEnumerable<RcmTransaction>> GetPendingPaymentsAsync(Guid companyId)
        {
            return await _rcmRepository.GetPendingPaymentAsync(companyId);
        }

        public async Task<IEnumerable<RcmTransaction>> GetPendingItcClaimsAsync(Guid companyId)
        {
            return await _rcmRepository.GetPaidAwaitingItcClaimAsync(companyId);
        }

        public async Task<RcmPeriodSummary> GetPeriodSummaryAsync(Guid companyId, string returnPeriod)
        {
            return await _rcmRepository.GetPeriodSummaryAsync(companyId, returnPeriod);
        }

        // ==================== Validation ====================

        public async Task<RcmValidationResult> ValidateRcmApplicabilityAsync(RcmValidationRequest request)
        {
            // Check if vendor is unregistered (no GSTIN) - RCM may apply
            var isUnregisteredVendor = string.IsNullOrEmpty(request.VendorGstin);

            // Get RCM category based on HSN/SAC code
            var category = await GetRcmCategoryAsync(request.HsnSacCode ?? "", request.VendorGstin);

            if (category != null)
            {
                return new RcmValidationResult
                {
                    IsRcmApplicable = true,
                    RcmCategoryCode = category.CategoryCode,
                    CategoryName = category.CategoryName,
                    NotificationReference = category.NotificationNumber,
                    ApplicableRate = category.DefaultGstRate,
                    Reason = $"RCM applicable as per {category.NotificationNumber}"
                };
            }

            // For unregistered vendors above threshold
            if (isUnregisteredVendor && request.TaxableValue > 5000)
            {
                return new RcmValidationResult
                {
                    IsRcmApplicable = true,
                    RcmCategoryCode = RcmCategoryCodes.UnregisteredPerson,
                    CategoryName = "Purchase from Unregistered Person",
                    NotificationReference = "Section 9(4) CGST Act",
                    ApplicableRate = 18, // Default GST rate
                    Reason = "RCM applicable for purchase from unregistered person"
                };
            }

            return new RcmValidationResult
            {
                IsRcmApplicable = false,
                Reason = "RCM not applicable for this transaction"
            };
        }

        public Task<RcmCategoryInfo?> GetRcmCategoryAsync(string hsnSacCode, string? vendorGstin = null)
        {
            // Common RCM categories based on SAC codes
            // This would typically be a database lookup
            RcmCategoryInfo? category = hsnSacCode switch
            {
                "998211" or "9982" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Legal,
                    CategoryName = "Legal Services",
                    NotificationNumber = "Notification 13/2017 Sr. 2",
                    HsnSacCodes = "998211",
                    DefaultGstRate = 18
                },
                "998212" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Legal,
                    CategoryName = "Arbitration Services",
                    NotificationNumber = "Notification 13/2017 Sr. 2",
                    HsnSacCodes = "998212",
                    DefaultGstRate = 18
                },
                "998513" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Security,
                    CategoryName = "Security Services",
                    NotificationNumber = "Notification 13/2017 Sr. 14",
                    HsnSacCodes = "998513",
                    DefaultGstRate = 18
                },
                "996511" or "996512" or "996513" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Gta,
                    CategoryName = "Goods Transport Agency",
                    NotificationNumber = "Notification 13/2017 Sr. 1",
                    HsnSacCodes = "9965",
                    DefaultGstRate = 5
                },
                "997212" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.GovernmentRent,
                    CategoryName = "Renting of Immovable Property by Government",
                    NotificationNumber = "Notification 13/2017 Sr. 5",
                    HsnSacCodes = "997212",
                    DefaultGstRate = 18
                },
                "998599" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.RecoveryAgent,
                    CategoryName = "Recovery Agent Services",
                    NotificationNumber = "Notification 13/2017 Sr. 8",
                    HsnSacCodes = "998599",
                    DefaultGstRate = 18
                },
                "997331" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Director,
                    CategoryName = "Director Services",
                    NotificationNumber = "Notification 13/2017 Sr. 6",
                    HsnSacCodes = "997331",
                    DefaultGstRate = 18
                },
                "997159" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Author,
                    CategoryName = "Services by Author/Music Composer",
                    NotificationNumber = "Notification 13/2017 Sr. 9",
                    HsnSacCodes = "997159",
                    DefaultGstRate = 18
                },
                "998397" => new RcmCategoryInfo
                {
                    CategoryCode = RcmCategoryCodes.Sponsorship,
                    CategoryName = "Sponsorship Services",
                    NotificationNumber = "Notification 13/2017 Sr. 4",
                    HsnSacCodes = "998397",
                    DefaultGstRate = 18
                },
                _ => null
            };

            return Task.FromResult(category);
        }

        // ==================== Private Helper Methods ====================

        private async Task AddLiabilityLines(
            JournalEntry entry,
            RcmLiabilityRequest request,
            decimal totalRcmTax)
        {
            var companyId = request.CompanyId;

            // DEBIT: Expense Account (base amount + RCM tax as expense)
            var expenseAccountCode = !string.IsNullOrEmpty(request.ExpenseAccountCode)
                ? request.ExpenseAccountCode
                : AccountCodes.GeneralExpenses;

            // Total expense = Base amount + RCM tax (RCM tax is an expense until payment)
            var totalExpense = request.TaxableValue + totalRcmTax;

            await AddLineIfAccountExists(entry, companyId, expenseAccountCode,
                totalExpense, 0,
                $"Expense: {request.Description ?? request.RcmCategoryCode}");

            // CREDIT: Vendor Payable (base amount only)
            var vendorPayableCode = !string.IsNullOrEmpty(request.VendorPayableAccountCode)
                ? request.VendorPayableAccountCode
                : AccountCodes.SundryCreditors;

            await AddLineIfAccountExists(entry, companyId, vendorPayableCode,
                0, request.TaxableValue,
                $"Payable to {request.VendorName}",
                "vendor", request.VendorId);

            // CREDIT: RCM GST Payable accounts
            if (request.SupplyType == "intra_state")
            {
                // Intra-state: CGST + SGST
                if (request.CgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmCgstPayable,
                        0, request.CgstAmount,
                        $"RCM CGST Payable @ {request.CgstRate}%");
                }

                if (request.SgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmSgstPayable,
                        0, request.SgstAmount,
                        $"RCM SGST Payable @ {request.SgstRate}%");
                }
            }
            else
            {
                // Inter-state: IGST
                if (request.IgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmIgstPayable,
                        0, request.IgstAmount,
                        $"RCM IGST Payable @ {request.IgstRate}%");
                }
            }
        }

        private async Task AddPaymentLines(
            JournalEntry entry,
            RcmTransaction rcm,
            RcmPaymentRequest payment)
        {
            var companyId = rcm.CompanyId;

            // DEBIT: Clear RCM Liability accounts
            if (rcm.SupplyType == "intra_state")
            {
                if (rcm.CgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmCgstPayable,
                        rcm.CgstAmount, 0,
                        $"RCM CGST Payment - {rcm.VendorName}");
                }

                if (rcm.SgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmSgstPayable,
                        rcm.SgstAmount, 0,
                        $"RCM SGST Payment - {rcm.VendorName}");
                }
            }
            else
            {
                if (rcm.IgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmIgstPayable,
                        rcm.IgstAmount, 0,
                        $"RCM IGST Payment - {rcm.VendorName}");
                }
            }

            // DEBIT: ITC accounts (if eligible and claiming now)
            if (payment.ClaimItcNow && rcm.ItcEligible && !rcm.ItcBlocked)
            {
                if (rcm.SupplyType == "intra_state")
                {
                    if (rcm.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.CgstInput,
                            rcm.CgstAmount, 0,
                            $"CGST ITC on RCM - {rcm.VendorName}");
                    }

                    if (rcm.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.SgstInput,
                            rcm.SgstAmount, 0,
                            $"SGST ITC on RCM - {rcm.VendorName}");
                    }
                }
                else
                {
                    if (rcm.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.IgstInput,
                            rcm.IgstAmount, 0,
                            $"IGST ITC on RCM - {rcm.VendorName}");
                    }
                }
            }
            else if (rcm.ItcBlocked)
            {
                // ITC Blocked - goes to expense
                await AddLineIfAccountExists(entry, companyId, AccountCodes.ItcBlocked,
                    rcm.TotalRcmTax, 0,
                    $"ITC Blocked (Sec 17(5)) - {rcm.ItcBlockedReason}");
            }

            // CREDIT: Bank Account (total RCM tax)
            await AddLineIfAccountExists(entry, companyId, payment.BankAccountCode,
                0, rcm.TotalRcmTax,
                $"RCM Payment - {payment.PaymentReference ?? rcm.VendorInvoiceNumber}");

            // CREDIT: Offset ITC claimable accounts (if ITC was claimed)
            if (payment.ClaimItcNow && rcm.ItcEligible && !rcm.ItcBlocked)
            {
                if (rcm.SupplyType == "intra_state")
                {
                    if (rcm.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmCgstItcClaimable,
                            0, rcm.CgstAmount,
                            $"RCM CGST ITC Claimed - {rcm.VendorName}");
                    }

                    if (rcm.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmSgstItcClaimable,
                            0, rcm.SgstAmount,
                            $"RCM SGST ITC Claimed - {rcm.VendorName}");
                    }
                }
                else
                {
                    if (rcm.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.RcmIgstItcClaimable,
                            0, rcm.IgstAmount,
                            $"RCM IGST ITC Claimed - {rcm.VendorName}");
                    }
                }
            }
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

        private static string BuildLiabilityNarration(RcmLiabilityRequest request, decimal totalRcmTax)
        {
            var narration = $"Being RCM liability recognized for {request.RcmCategoryCode}. ";
            narration += $"Vendor: {request.VendorName}. ";

            if (!string.IsNullOrEmpty(request.VendorGstin))
            {
                narration += $"GSTIN: {request.VendorGstin}. ";
            }
            else
            {
                narration += "Unregistered Vendor. ";
            }

            if (!string.IsNullOrEmpty(request.VendorInvoiceNumber))
            {
                narration += $"Invoice: {request.VendorInvoiceNumber}. ";
            }

            narration += $"Taxable Value: Rs.{request.TaxableValue:N2}. ";
            narration += $"RCM Tax: Rs.{totalRcmTax:N2}. ";

            if (request.SupplyType == "intra_state")
            {
                narration += $"CGST: Rs.{request.CgstAmount:N2} @ {request.CgstRate}%, ";
                narration += $"SGST: Rs.{request.SgstAmount:N2} @ {request.SgstRate}%. ";
            }
            else
            {
                narration += $"IGST: Rs.{request.IgstAmount:N2} @ {request.IgstRate}%. ";
            }

            if (!request.ItcEligible)
            {
                narration += "ITC NOT ELIGIBLE. ";
            }

            return narration;
        }

        private static string BuildPaymentNarration(RcmTransaction rcm, RcmPaymentRequest payment)
        {
            var narration = $"Being RCM payment to government for {rcm.RcmCategoryCode}. ";
            narration += $"Vendor: {rcm.VendorName}. ";

            if (!string.IsNullOrEmpty(rcm.VendorInvoiceNumber))
            {
                narration += $"Invoice: {rcm.VendorInvoiceNumber}. ";
            }

            narration += $"RCM Tax Paid: Rs.{rcm.TotalRcmTax:N2}. ";

            if (!string.IsNullOrEmpty(payment.PaymentReference))
            {
                narration += $"Payment Ref: {payment.PaymentReference}. ";
            }

            if (!string.IsNullOrEmpty(payment.ChallanNumber))
            {
                narration += $"Challan: {payment.ChallanNumber}. ";
            }

            if (payment.ClaimItcNow && rcm.ItcEligible && !rcm.ItcBlocked)
            {
                narration += $"ITC claimed in period {payment.ItcClaimPeriod ?? GetReturnPeriod(DateOnly.FromDateTime(payment.PaymentDate))}. ";
            }
            else if (rcm.ItcBlocked)
            {
                narration += $"ITC BLOCKED - {rcm.ItcBlockedReason}. ";
            }

            return narration;
        }

        private static string DetermineRcmCategory(Core.Entities.Expense.ExpenseClaim claim)
        {
            // Determine RCM category based on expense claim attributes
            // This is a simplified implementation - in production, this would be more sophisticated

            var hsnSac = claim.HsnSacCode?.ToUpper() ?? "";
            var description = claim.Description?.ToUpper() ?? "";
            var title = claim.Title?.ToUpper() ?? "";

            if (hsnSac.StartsWith("9982") || description.Contains("LEGAL") || title.Contains("LEGAL"))
                return RcmCategoryCodes.Legal;

            if (hsnSac.StartsWith("998513") || description.Contains("SECURITY") || title.Contains("SECURITY"))
                return RcmCategoryCodes.Security;

            if (hsnSac.StartsWith("9965") || description.Contains("TRANSPORT") || title.Contains("GTA"))
                return RcmCategoryCodes.Gta;

            if (description.Contains("DIRECTOR") || title.Contains("DIRECTOR FEE"))
                return RcmCategoryCodes.Director;

            if (string.IsNullOrEmpty(claim.VendorGstin))
                return RcmCategoryCodes.UnregisteredPerson;

            return RcmCategoryCodes.UnregisteredPerson;
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

        private static string GetReturnPeriod(DateOnly date)
        {
            return $"{date:MMM-yyyy}";
        }
    }
}
