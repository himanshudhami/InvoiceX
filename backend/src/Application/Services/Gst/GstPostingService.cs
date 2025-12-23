using Core.Entities.Ledger;
using Core.Interfaces.Gst;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Gst
{
    /// <summary>
    /// Service for GST posting operations beyond standard invoice/expense posting.
    ///
    /// Handles:
    /// - ITC Blocked tracking (Section 17(5) CGST Act)
    /// - Credit Note GST adjustment
    /// - Debit Note GST adjustment
    /// - ITC Reversal (Rule 42/43 CGST Rules)
    /// - UTGST posting for Union Territories
    /// - GST TDS/TCS (Section 51/52)
    ///
    /// References:
    /// - Section 17(5) CGST Act - Blocked ITC
    /// - Rule 42/43 CGST Rules - ITC Reversal
    /// - ClearTax: https://cleartax.in/s/blocked-itc-section-17-5-cgst-act
    /// </summary>
    public class GstPostingService : IGstPostingService
    {
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly ILogger<GstPostingService> _logger;

        /// <summary>
        /// Account codes following standard Indian chart of accounts
        /// </summary>
        private static class AccountCodes
        {
            // GST Input Accounts
            public const string CgstInput = "1141";
            public const string SgstInput = "1142";
            public const string IgstInput = "1143";
            public const string CessInput = "1144";
            public const string UtgstInput = "1145";

            // GST Output Accounts
            public const string CgstOutput = "2251";
            public const string SgstOutput = "2252";
            public const string IgstOutput = "2253";
            public const string CessOutput = "2254";
            public const string UtgstOutput = "2255";

            // ITC Blocked
            public const string ItcBlocked = "1149";

            // GST TDS/TCS
            public const string GstTdsPayable = "2260";
            public const string GstTdsReceivable = "1155";
            public const string GstTcsPayable = "2261";

            // Expenses
            public const string ItcReversalExpense = "5800";
        }

        /// <summary>
        /// Blocked ITC categories per Section 17(5)
        /// </summary>
        private static readonly Dictionary<string, ItcBlockedCategory> BlockedCategories = new()
        {
            ["MOTOR_VEHICLE"] = new ItcBlockedCategory
            {
                CategoryCode = "MOTOR_VEHICLE",
                CategoryName = "Motor Vehicles and Conveyances",
                SectionReference = "Section 17(5)(a)",
                Description = "Motor vehicles except for transportation, driving training, or sale",
                HsnSacCodes = "8702,8703,8704",
                IsActive = true
            },
            ["FOOD_BEVERAGES"] = new ItcBlockedCategory
            {
                CategoryCode = "FOOD_BEVERAGES",
                CategoryName = "Food, Beverages, Outdoor Catering",
                SectionReference = "Section 17(5)(b)(i)",
                Description = "Food and beverages, outdoor catering, beauty treatment, health services",
                HsnSacCodes = "9963",
                IsActive = true
            },
            ["HEALTH_FITNESS"] = new ItcBlockedCategory
            {
                CategoryCode = "HEALTH_FITNESS",
                CategoryName = "Health and Fitness Centre",
                SectionReference = "Section 17(5)(b)(ii)",
                Description = "Membership of club, health and fitness centre",
                HsnSacCodes = "9996",
                IsActive = true
            },
            ["TRAVEL_BENEFITS"] = new ItcBlockedCategory
            {
                CategoryCode = "TRAVEL_BENEFITS",
                CategoryName = "Travel Benefits to Employees",
                SectionReference = "Section 17(5)(b)(iii)",
                Description = "Travel benefits extended to employees on vacation",
                HsnSacCodes = "9964",
                IsActive = true
            },
            ["WORKS_CONTRACT"] = new ItcBlockedCategory
            {
                CategoryCode = "WORKS_CONTRACT",
                CategoryName = "Works Contract for Immovable Property",
                SectionReference = "Section 17(5)(c)",
                Description = "Works contract services for construction of immovable property",
                HsnSacCodes = "9954",
                IsActive = true
            },
            ["IMMOVABLE_PROPERTY"] = new ItcBlockedCategory
            {
                CategoryCode = "IMMOVABLE_PROPERTY",
                CategoryName = "Construction of Immovable Property",
                SectionReference = "Section 17(5)(d)",
                Description = "Goods/services for construction of immovable property on own account",
                IsActive = true
            },
            ["PERSONAL_USE"] = new ItcBlockedCategory
            {
                CategoryCode = "PERSONAL_USE",
                CategoryName = "Personal Consumption",
                SectionReference = "Section 17(5)(g)",
                Description = "Goods or services used for personal consumption",
                IsActive = true
            },
            ["LOST_STOLEN"] = new ItcBlockedCategory
            {
                CategoryCode = "LOST_STOLEN",
                CategoryName = "Goods Lost, Stolen, Destroyed",
                SectionReference = "Section 17(5)(h)",
                Description = "Goods lost, stolen, destroyed, written off or disposed as gift",
                IsActive = true
            },
            ["FREE_SAMPLES"] = new ItcBlockedCategory
            {
                CategoryCode = "FREE_SAMPLES",
                CategoryName = "Free Samples and Gifts",
                SectionReference = "Section 17(5)(h)",
                Description = "Goods disposed of by way of gift or free samples",
                IsActive = true
            }
        };

        public GstPostingService(
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            ILogger<GstPostingService> logger)
        {
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== ITC Blocked (Section 17(5)) ====================

        public Task<ItcBlockedCheckResult> CheckItcBlockedAsync(ItcBlockedCheckRequest request)
        {
            // Check by HSN/SAC code
            if (!string.IsNullOrEmpty(request.HsnSacCode))
            {
                foreach (var category in BlockedCategories.Values)
                {
                    if (!string.IsNullOrEmpty(category.HsnSacCodes) &&
                        category.HsnSacCodes.Split(',').Any(c => request.HsnSacCode.StartsWith(c.Trim())))
                    {
                        return Task.FromResult(new ItcBlockedCheckResult
                        {
                            IsBlocked = true,
                            BlockedCategoryCode = category.CategoryCode,
                            BlockedCategoryName = category.CategoryName,
                            SectionReference = category.SectionReference,
                            Reason = category.Description
                        });
                    }
                }
            }

            // Check by category name/description
            if (!string.IsNullOrEmpty(request.ExpenseCategory) || !string.IsNullOrEmpty(request.Description))
            {
                var searchText = $"{request.ExpenseCategory} {request.Description}".ToUpper();

                if (searchText.Contains("MOTOR") || searchText.Contains("VEHICLE") || searchText.Contains("CAR"))
                {
                    var cat = BlockedCategories["MOTOR_VEHICLE"];
                    return Task.FromResult(new ItcBlockedCheckResult
                    {
                        IsBlocked = true,
                        BlockedCategoryCode = cat.CategoryCode,
                        BlockedCategoryName = cat.CategoryName,
                        SectionReference = cat.SectionReference,
                        Reason = cat.Description
                    });
                }

                if (searchText.Contains("FOOD") || searchText.Contains("CATERING") || searchText.Contains("RESTAURANT"))
                {
                    var cat = BlockedCategories["FOOD_BEVERAGES"];
                    return Task.FromResult(new ItcBlockedCheckResult
                    {
                        IsBlocked = true,
                        BlockedCategoryCode = cat.CategoryCode,
                        BlockedCategoryName = cat.CategoryName,
                        SectionReference = cat.SectionReference,
                        Reason = cat.Description
                    });
                }

                if (searchText.Contains("GYM") || searchText.Contains("FITNESS") || searchText.Contains("CLUB"))
                {
                    var cat = BlockedCategories["HEALTH_FITNESS"];
                    return Task.FromResult(new ItcBlockedCheckResult
                    {
                        IsBlocked = true,
                        BlockedCategoryCode = cat.CategoryCode,
                        BlockedCategoryName = cat.CategoryName,
                        SectionReference = cat.SectionReference,
                        Reason = cat.Description
                    });
                }

                if (searchText.Contains("CONSTRUCTION") || searchText.Contains("BUILDING") || searchText.Contains("WORKS CONTRACT"))
                {
                    var cat = BlockedCategories["WORKS_CONTRACT"];
                    return Task.FromResult(new ItcBlockedCheckResult
                    {
                        IsBlocked = true,
                        BlockedCategoryCode = cat.CategoryCode,
                        BlockedCategoryName = cat.CategoryName,
                        SectionReference = cat.SectionReference,
                        Reason = cat.Description
                    });
                }
            }

            return Task.FromResult(new ItcBlockedCheckResult
            {
                IsBlocked = false,
                Reason = "ITC is eligible for this transaction"
            });
        }

        /// <summary>
        /// Posts ITC blocked entry.
        ///
        /// Journal Entry:
        /// Dr. ITC Blocked - Sec 17(5) (1149)     [Total Blocked GST]
        ///     Cr. CGST Input (1141)               [Blocked CGST]
        ///     Cr. SGST Input (1142)               [Blocked SGST]
        /// OR (Inter-state):
        ///     Cr. IGST Input (1143)               [Blocked IGST]
        /// </summary>
        public async Task<GstPostingResult> PostItcBlockedAsync(ItcBlockedRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalBlocked = request.CgstAmount + request.SgstAmount + request.IgstAmount + request.CessAmount;
                if (totalBlocked <= 0)
                {
                    return GstPostingResult.Failed("Blocked amount must be greater than zero");
                }

                var idempotencyKey = $"ITC_BLOCKED_{request.SourceType}_{request.SourceId ?? Guid.NewGuid()}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation("Journal already exists for ITC blocked {IdempotencyKey}", idempotencyKey);
                    return GstPostingResult.Succeeded(existingJournal);
                }

                var financialYear = GetFinancialYear(request.TransactionDate);
                var periodMonth = GetPeriodMonth(request.TransactionDate);

                // Build narration
                var narration = BuildItcBlockedNarration(request, totalBlocked);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = request.SourceType ?? "itc_blocked",
                    SourceId = request.SourceId,
                    SourceNumber = request.SourceNumber,
                    Description = $"ITC Blocked - {request.BlockedCategoryCode}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "ITC_BLOCKED",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: ITC Blocked account
                await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.ItcBlocked,
                    totalBlocked, 0,
                    $"ITC Blocked u/s 17(5) - {request.BlockedCategoryCode}");

                // CREDIT: GST Input accounts
                if (request.CgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstInput,
                        0, request.CgstAmount,
                        $"CGST Input blocked");
                }

                if (request.SgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstInput,
                        0, request.SgstAmount,
                        $"SGST Input blocked");
                }

                if (request.IgstAmount > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstInput,
                        0, request.IgstAmount,
                        $"IGST Input blocked");
                }

                if (request.CessAmount > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CessInput,
                        0, request.CessAmount,
                        $"Cess Input blocked");
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    return GstPostingResult.Failed("Journal entry is not balanced");
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created ITC blocked journal {JournalNumber} for {Category}. Amount: {Amount:C}",
                    savedEntry.JournalNumber, request.BlockedCategoryCode, totalBlocked);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting ITC blocked entry");
                throw;
            }
        }

        public Task<IEnumerable<ItcBlockedCategory>> GetBlockedCategoriesAsync()
        {
            return Task.FromResult(BlockedCategories.Values.AsEnumerable());
        }

        // ==================== Credit/Debit Notes ====================

        /// <summary>
        /// Posts Credit Note GST adjustment.
        ///
        /// For Sales Credit Note (we issued):
        /// Dr. CGST Output Payable (2251)          [CGST Reduction]
        /// Dr. SGST Output Payable (2252)          [SGST Reduction]
        ///     Cr. Customer (to reduce receivable) [Total]
        ///
        /// For Purchase Credit Note (we received):
        /// Dr. Vendor (to reduce payable)          [Total]
        ///     Cr. CGST Input (1141)               [ITC Reversal]
        ///     Cr. SGST Input (1142)               [ITC Reversal]
        /// </summary>
        public async Task<GstPostingResult> PostCreditNoteGstAsync(CreditNoteGstRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalGst = request.CgstAmount + request.SgstAmount + request.IgstAmount + request.CessAmount;
                if (totalGst <= 0)
                {
                    return GstPostingResult.Failed("GST amount must be greater than zero");
                }

                var idempotencyKey = $"CN_GST_{request.CreditNoteNumber}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation("Journal already exists for credit note {CreditNoteNumber}", request.CreditNoteNumber);
                    return GstPostingResult.Succeeded(existingJournal);
                }

                var financialYear = GetFinancialYear(request.CreditNoteDate);
                var periodMonth = GetPeriodMonth(request.CreditNoteDate);

                var narration = BuildCreditNoteNarration(request, totalGst);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.CreditNoteDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "credit_note",
                    SourceNumber = request.CreditNoteNumber,
                    Description = $"Credit Note GST Adjustment - {request.CreditNoteNumber}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "CREDIT_NOTE_GST",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: GST Output accounts (reduce liability)
                if (request.SupplyType == "intra_state")
                {
                    if (request.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstOutput,
                            request.CgstAmount, 0,
                            $"CGST Output adjustment - CN {request.CreditNoteNumber}");
                    }

                    if (request.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstOutput,
                            request.SgstAmount, 0,
                            $"SGST Output adjustment - CN {request.CreditNoteNumber}");
                    }
                }
                else
                {
                    if (request.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstOutput,
                            request.IgstAmount, 0,
                            $"IGST Output adjustment - CN {request.CreditNoteNumber}");
                    }
                }

                if (request.CessAmount > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CessOutput,
                        request.CessAmount, 0,
                        $"Cess Output adjustment - CN {request.CreditNoteNumber}");
                }

                // CREDIT: GST Input accounts (ITC reversal for purchase credit notes)
                // Note: This is simplified - actual implementation would depend on whether
                // this is a sales CN (reduce output) or purchase CN (reduce input)
                if (request.SupplyType == "intra_state")
                {
                    if (request.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstInput,
                            0, request.CgstAmount,
                            $"CGST ITC reversal - CN {request.CreditNoteNumber}");
                    }

                    if (request.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstInput,
                            0, request.SgstAmount,
                            $"SGST ITC reversal - CN {request.CreditNoteNumber}");
                    }
                }
                else
                {
                    if (request.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstInput,
                            0, request.IgstAmount,
                            $"IGST ITC reversal - CN {request.CreditNoteNumber}");
                    }
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    return GstPostingResult.Failed("Journal entry is not balanced");
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created credit note GST journal {JournalNumber} for {CreditNoteNumber}. GST: {GstAmount:C}",
                    savedEntry.JournalNumber, request.CreditNoteNumber, totalGst);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting credit note GST for {CreditNoteNumber}", request.CreditNoteNumber);
                throw;
            }
        }

        public async Task<GstPostingResult> PostDebitNoteGstAsync(DebitNoteGstRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalGst = request.CgstAmount + request.SgstAmount + request.IgstAmount + request.CessAmount;
                if (totalGst <= 0)
                {
                    return GstPostingResult.Failed("GST amount must be greater than zero");
                }

                var idempotencyKey = $"DN_GST_{request.DebitNoteNumber}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation("Journal already exists for debit note {DebitNoteNumber}", request.DebitNoteNumber);
                    return GstPostingResult.Succeeded(existingJournal);
                }

                var financialYear = GetFinancialYear(request.DebitNoteDate);
                var periodMonth = GetPeriodMonth(request.DebitNoteDate);

                var narration = BuildDebitNoteNarration(request, totalGst);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.DebitNoteDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "debit_note",
                    SourceNumber = request.DebitNoteNumber,
                    Description = $"Debit Note GST Adjustment - {request.DebitNoteNumber}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "DEBIT_NOTE_GST",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: GST Input accounts (additional ITC)
                if (request.SupplyType == "intra_state")
                {
                    if (request.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstInput,
                            request.CgstAmount, 0,
                            $"CGST Input - DN {request.DebitNoteNumber}");
                    }

                    if (request.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstInput,
                            request.SgstAmount, 0,
                            $"SGST Input - DN {request.DebitNoteNumber}");
                    }
                }
                else
                {
                    if (request.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstInput,
                            request.IgstAmount, 0,
                            $"IGST Input - DN {request.DebitNoteNumber}");
                    }
                }

                // CREDIT: GST Output accounts (increase liability)
                if (request.SupplyType == "intra_state")
                {
                    if (request.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstOutput,
                            0, request.CgstAmount,
                            $"CGST Output - DN {request.DebitNoteNumber}");
                    }

                    if (request.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstOutput,
                            0, request.SgstAmount,
                            $"SGST Output - DN {request.DebitNoteNumber}");
                    }
                }
                else
                {
                    if (request.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstOutput,
                            0, request.IgstAmount,
                            $"IGST Output - DN {request.DebitNoteNumber}");
                    }
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    return GstPostingResult.Failed("Journal entry is not balanced");
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created debit note GST journal {JournalNumber} for {DebitNoteNumber}. GST: {GstAmount:C}",
                    savedEntry.JournalNumber, request.DebitNoteNumber, totalGst);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting debit note GST for {DebitNoteNumber}", request.DebitNoteNumber);
                throw;
            }
        }

        // ==================== ITC Reversal (Rule 42/43) ====================

        /// <summary>
        /// Posts ITC reversal entry per Rule 42/43.
        ///
        /// Journal Entry:
        /// Dr. ITC Reversal Expense (5800)         [Total Reversal]
        ///     Cr. CGST Input (1141)               [CGST Reversal]
        ///     Cr. SGST Input (1142)               [SGST Reversal]
        /// </summary>
        public async Task<GstPostingResult> PostItcReversalAsync(ItcReversalRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalReversal = request.CgstReversal + request.SgstReversal + request.IgstReversal + request.CessReversal;
                if (totalReversal <= 0)
                {
                    return GstPostingResult.Failed("Reversal amount must be greater than zero");
                }

                var idempotencyKey = $"ITC_REVERSAL_{request.ReversalRule}_{request.ReturnPeriod}";
                var existingJournal = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingJournal != null)
                {
                    _logger.LogInformation("Journal already exists for ITC reversal {IdempotencyKey}", idempotencyKey);
                    return GstPostingResult.Succeeded(existingJournal);
                }

                var financialYear = GetFinancialYear(request.ReversalDate);
                var periodMonth = GetPeriodMonth(request.ReversalDate);

                var narration = BuildItcReversalNarration(request, totalReversal);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.ReversalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "itc_reversal",
                    Description = $"ITC Reversal - {request.ReversalRule} - {request.ReturnPeriod}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "ITC_REVERSAL",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: ITC Reversal Expense
                await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.ItcReversalExpense,
                    totalReversal, 0,
                    $"ITC Reversal per {request.ReversalRule} - {request.ReturnPeriod}");

                // CREDIT: GST Input accounts
                if (request.CgstReversal > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstInput,
                        0, request.CgstReversal,
                        $"CGST Reversal per {request.ReversalRule}");
                }

                if (request.SgstReversal > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstInput,
                        0, request.SgstReversal,
                        $"SGST Reversal per {request.ReversalRule}");
                }

                if (request.IgstReversal > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstInput,
                        0, request.IgstReversal,
                        $"IGST Reversal per {request.ReversalRule}");
                }

                if (request.CessReversal > 0)
                {
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CessInput,
                        0, request.CessReversal,
                        $"Cess Reversal per {request.ReversalRule}");
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    return GstPostingResult.Failed("Journal entry is not balanced");
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created ITC reversal journal {JournalNumber} for {Rule} - {Period}. Amount: {Amount:C}",
                    savedEntry.JournalNumber, request.ReversalRule, request.ReturnPeriod, totalReversal);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting ITC reversal");
                throw;
            }
        }

        public Task<ItcReversalCalculation> CalculateItcReversalAsync(ItcReversalCalculationRequest request)
        {
            // Rule 42 - Common Credit Reversal
            // ITC to reverse = Total Common ITC x (Exempt Turnover / Total Turnover)
            if (request.ReversalRule == "Rule42" && request.TotalTurnover > 0)
            {
                var exemptRatio = (request.ExemptTurnover ?? 0) / request.TotalTurnover.Value;
                var totalReversal = (request.TotalCommonCredit ?? 0) * exemptRatio;

                // Assume 50% CGST, 50% SGST for intra-state (simplified)
                var cgstReversal = Math.Round(totalReversal / 2, 0);
                var sgstReversal = totalReversal - cgstReversal;

                return Task.FromResult(new ItcReversalCalculation
                {
                    ReversalRule = "Rule42",
                    TotalItcAvailable = request.TotalCommonCredit ?? 0,
                    ItcToReverse = totalReversal,
                    CgstReversal = cgstReversal,
                    SgstReversal = sgstReversal,
                    IgstReversal = 0,
                    CessReversal = 0,
                    CalculationDetails = $"Common Credit Reversal: {request.TotalCommonCredit:N2} x ({request.ExemptTurnover:N2} / {request.TotalTurnover:N2}) = {totalReversal:N2}"
                });
            }

            // Rule 43 - Capital Goods Reversal
            // ITC to reverse per quarter = (Total ITC / Useful Life) x Exempt Use %
            if (request.ReversalRule == "Rule43" && request.UsefulLife > 0)
            {
                var quarterlyCredit = (request.CapitalGoodsValue ?? 0) / request.UsefulLife.Value;
                var exemptUseRatio = (request.ExemptUsePercentage ?? 0) / 100;
                var totalReversal = quarterlyCredit * exemptUseRatio;

                var cgstReversal = Math.Round(totalReversal / 2, 0);
                var sgstReversal = totalReversal - cgstReversal;

                return Task.FromResult(new ItcReversalCalculation
                {
                    ReversalRule = "Rule43",
                    TotalItcAvailable = request.CapitalGoodsValue ?? 0,
                    ItcToReverse = totalReversal,
                    CgstReversal = cgstReversal,
                    SgstReversal = sgstReversal,
                    IgstReversal = 0,
                    CessReversal = 0,
                    CalculationDetails = $"Capital Goods Reversal: ({request.CapitalGoodsValue:N2} / {request.UsefulLife} quarters) x {request.ExemptUsePercentage}% = {totalReversal:N2}"
                });
            }

            return Task.FromResult(new ItcReversalCalculation
            {
                ReversalRule = request.ReversalRule,
                ItcToReverse = 0,
                CalculationDetails = "Unable to calculate - insufficient data"
            });
        }

        // ==================== UTGST Posting ====================

        public async Task<GstPostingResult> PostUtgstAsync(UtgstRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalGst = request.CgstAmount + request.UtgstAmount;
                if (totalGst <= 0)
                {
                    return GstPostingResult.Failed("GST amount must be greater than zero");
                }

                var idempotencyKey = $"UTGST_{request.TransactionType}_{request.SourceId ?? Guid.NewGuid()}";
                var financialYear = GetFinancialYear(request.TransactionDate);
                var periodMonth = GetPeriodMonth(request.TransactionDate);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = request.SourceType ?? "utgst",
                    SourceId = request.SourceId,
                    SourceNumber = request.SourceNumber,
                    Description = $"UTGST {request.TransactionType} - {request.UnionTerritory}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "UTGST",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                if (request.TransactionType == "input")
                {
                    // UTGST Input
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstInput,
                        request.CgstAmount, 0,
                        $"CGST Input - UT {request.UnionTerritory}");

                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.UtgstInput,
                        request.UtgstAmount, 0,
                        $"UTGST Input - {request.UnionTerritory}");
                }
                else
                {
                    // UTGST Output
                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstOutput,
                        0, request.CgstAmount,
                        $"CGST Output - UT {request.UnionTerritory}");

                    await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.UtgstOutput,
                        0, request.UtgstAmount,
                        $"UTGST Output - {request.UnionTerritory}");
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created UTGST journal {JournalNumber} for {UT}. Amount: {Amount:C}",
                    savedEntry.JournalNumber, request.UnionTerritory, totalGst);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting UTGST");
                throw;
            }
        }

        // ==================== GST TDS/TCS ====================

        public async Task<GstPostingResult> PostGstTdsAsync(GstTdsRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalTds = request.CgstTdsAmount + request.SgstTdsAmount + request.IgstTdsAmount;
                if (totalTds <= 0)
                {
                    return GstPostingResult.Failed("TDS amount must be greater than zero");
                }

                var idempotencyKey = $"GST_TDS_{request.InvoiceNumber ?? Guid.NewGuid().ToString()}";
                var financialYear = GetFinancialYear(request.TransactionDate);
                var periodMonth = GetPeriodMonth(request.TransactionDate);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "gst_tds",
                    SourceId = request.InvoiceId,
                    SourceNumber = request.InvoiceNumber,
                    Description = $"GST TDS u/s 51 - {request.DeductorName}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "GST_TDS",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: GST TDS Receivable
                await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.GstTdsReceivable,
                    totalTds, 0,
                    $"GST TDS Receivable from {request.DeductorName}");

                // CREDIT: GST Output accounts (reduce liability)
                if (request.SupplyType == "intra_state")
                {
                    if (request.CgstTdsAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.CgstOutput,
                            0, request.CgstTdsAmount,
                            $"CGST TDS @ {request.TdsRate}%");
                    }

                    if (request.SgstTdsAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.SgstOutput,
                            0, request.SgstTdsAmount,
                            $"SGST TDS @ {request.TdsRate}%");
                    }
                }
                else
                {
                    if (request.IgstTdsAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.IgstOutput,
                            0, request.IgstTdsAmount,
                            $"IGST TDS @ {request.TdsRate}%");
                    }
                }

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created GST TDS journal {JournalNumber} for {Deductor}. TDS: {TdsAmount:C}",
                    savedEntry.JournalNumber, request.DeductorName, totalTds);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting GST TDS");
                throw;
            }
        }

        public async Task<GstPostingResult> PostGstTcsAsync(GstTcsRequest request, Guid? postedBy = null)
        {
            try
            {
                var totalTcs = request.CgstTcsAmount + request.SgstTcsAmount + request.IgstTcsAmount;
                if (totalTcs <= 0)
                {
                    return GstPostingResult.Failed("TCS amount must be greater than zero");
                }

                var idempotencyKey = $"GST_TCS_{request.OperatorName}_{request.TransactionDate}";
                var financialYear = GetFinancialYear(request.TransactionDate);
                var periodMonth = GetPeriodMonth(request.TransactionDate);

                var entry = new JournalEntry
                {
                    CompanyId = request.CompanyId,
                    JournalDate = request.TransactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "gst_tcs",
                    Description = $"GST TCS u/s 52 - {request.OperatorName}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "GST_TCS",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // DEBIT: GST TCS Payable (expense/asset)
                await AddLineIfAccountExists(entry, request.CompanyId, AccountCodes.GstTcsPayable,
                    totalTcs, 0,
                    $"GST TCS Paid to {request.OperatorName}");

                // CREDIT: Bank/Cash
                // Note: This is simplified - would need bank account in actual implementation

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created GST TCS journal {JournalNumber} for {Operator}. TCS: {TcsAmount:C}",
                    savedEntry.JournalNumber, request.OperatorName, totalTcs);

                return GstPostingResult.Succeeded(savedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting GST TCS");
                throw;
            }
        }

        // ==================== Summary & Reports ====================

        public Task<ItcBlockedSummary> GetItcBlockedSummaryAsync(Guid companyId, string returnPeriod)
        {
            // This would typically query the database for ITC blocked transactions
            // For now, return an empty summary
            return Task.FromResult(new ItcBlockedSummary
            {
                ReturnPeriod = returnPeriod,
                CategoryBreakdown = new List<ItcBlockedCategorySummary>()
            });
        }

        public Task<ItcAvailabilityReport> GetItcAvailabilityReportAsync(Guid companyId, string returnPeriod)
        {
            // This would typically query the database for ITC summary
            // For now, return an empty report
            return Task.FromResult(new ItcAvailabilityReport
            {
                ReturnPeriod = returnPeriod
            });
        }

        // ==================== Private Helper Methods ====================

        private async Task AddLineIfAccountExists(
            JournalEntry entry,
            Guid companyId,
            string accountCode,
            decimal debitAmount,
            decimal creditAmount,
            string description)
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
                ExchangeRate = 1
            });
        }

        private static string BuildItcBlockedNarration(ItcBlockedRequest request, decimal totalBlocked)
        {
            var narration = $"Being ITC blocked u/s 17(5) for {request.BlockedCategoryCode}. ";
            narration += $"Taxable Value: Rs.{request.TaxableValue:N2}. ";
            narration += $"Blocked GST: Rs.{totalBlocked:N2}. ";

            if (!string.IsNullOrEmpty(request.Description))
            {
                narration += $"Description: {request.Description}. ";
            }

            if (!string.IsNullOrEmpty(request.Notes))
            {
                narration += $"Notes: {request.Notes}.";
            }

            return narration;
        }

        private static string BuildCreditNoteNarration(CreditNoteGstRequest request, decimal totalGst)
        {
            var narration = $"Being GST adjustment for Credit Note {request.CreditNoteNumber}. ";

            if (!string.IsNullOrEmpty(request.OriginalInvoiceNumber))
            {
                narration += $"Original Invoice: {request.OriginalInvoiceNumber}. ";
            }

            if (!string.IsNullOrEmpty(request.PartyName))
            {
                narration += $"Party: {request.PartyName}. ";
            }

            narration += $"Taxable Value: Rs.{request.TaxableValue:N2}. ";
            narration += $"GST Adjustment: Rs.{totalGst:N2}. ";

            if (!string.IsNullOrEmpty(request.Reason))
            {
                narration += $"Reason: {request.Reason}.";
            }

            return narration;
        }

        private static string BuildDebitNoteNarration(DebitNoteGstRequest request, decimal totalGst)
        {
            var narration = $"Being GST adjustment for Debit Note {request.DebitNoteNumber}. ";

            if (!string.IsNullOrEmpty(request.OriginalInvoiceNumber))
            {
                narration += $"Original Invoice: {request.OriginalInvoiceNumber}. ";
            }

            if (!string.IsNullOrEmpty(request.PartyName))
            {
                narration += $"Party: {request.PartyName}. ";
            }

            narration += $"Taxable Value: Rs.{request.TaxableValue:N2}. ";
            narration += $"GST Adjustment: Rs.{totalGst:N2}. ";

            if (!string.IsNullOrEmpty(request.Reason))
            {
                narration += $"Reason: {request.Reason}.";
            }

            return narration;
        }

        private static string BuildItcReversalNarration(ItcReversalRequest request, decimal totalReversal)
        {
            var narration = $"Being ITC reversal per {request.ReversalRule} for period {request.ReturnPeriod}. ";
            narration += $"Total Reversal: Rs.{totalReversal:N2}. ";

            if (!string.IsNullOrEmpty(request.Reason))
            {
                narration += $"Reason: {request.Reason}.";
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
    }
}
