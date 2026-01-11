using Application.Interfaces.Audit;
using Application.Interfaces.Ledger;
using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace WebApi.Controllers
{
    /// <summary>
    /// API endpoints for General Ledger operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LedgerController : ControllerBase
    {
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IPostingRuleRepository _ruleRepository;
        private readonly IAutoPostingService _autoPostingService;
        private readonly ITrialBalanceService _trialBalanceService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LedgerController> _logger;

        public LedgerController(
            IChartOfAccountRepository accountRepository,
            IJournalEntryRepository journalRepository,
            IPostingRuleRepository ruleRepository,
            IAutoPostingService autoPostingService,
            ITrialBalanceService trialBalanceService,
            IAuditService auditService,
            IConfiguration configuration,
            ILogger<LedgerController> logger)
        {
            _accountRepository = accountRepository;
            _journalRepository = journalRepository;
            _ruleRepository = ruleRepository;
            _autoPostingService = autoPostingService;
            _trialBalanceService = trialBalanceService;
            _auditService = auditService;
            _configuration = configuration;
            _logger = logger;
        }

        // ==================== Chart of Accounts ====================

        /// <summary>
        /// Get all accounts for a company
        /// </summary>
        [HttpGet("accounts/company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<ChartOfAccount>), 200)]
        public async Task<IActionResult> GetAccounts(Guid companyId)
        {
            var accounts = await _accountRepository.GetByCompanyIdAsync(companyId);
            return Ok(accounts);
        }

        /// <summary>
        /// Get account hierarchy (tree structure)
        /// </summary>
        [HttpGet("accounts/company/{companyId}/hierarchy")]
        [ProducesResponseType(typeof(IEnumerable<ChartOfAccount>), 200)]
        public async Task<IActionResult> GetAccountHierarchy(Guid companyId)
        {
            var accounts = await _accountRepository.GetHierarchyAsync(companyId);
            return Ok(accounts);
        }

        /// <summary>
        /// Get account by ID
        /// </summary>
        [HttpGet("accounts/{id}")]
        [ProducesResponseType(typeof(ChartOfAccount), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAccount(Guid id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound($"Account {id} not found");
            }
            return Ok(account);
        }

        /// <summary>
        /// Get account by code
        /// </summary>
        [HttpGet("accounts/company/{companyId}/code/{code}")]
        [ProducesResponseType(typeof(ChartOfAccount), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAccountByCode(Guid companyId, string code)
        {
            var account = await _accountRepository.GetByCodeAsync(companyId, code);
            if (account == null)
            {
                return NotFound($"Account with code {code} not found");
            }
            return Ok(account);
        }

        /// <summary>
        /// Initialize default chart of accounts for a company
        /// </summary>
        [HttpPost("accounts/company/{companyId}/initialize")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InitializeAccounts(Guid companyId)
        {
            if (await _accountRepository.HasAccountsAsync(companyId))
            {
                return BadRequest("Company already has chart of accounts");
            }

            await _accountRepository.InitializeDefaultAccountsAsync(companyId);
            _logger.LogInformation("Initialized chart of accounts for company {CompanyId}", companyId);

            return Ok(new { message = "Chart of accounts initialized successfully" });
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        [HttpPost("accounts")]
        [ProducesResponseType(typeof(ChartOfAccount), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateAccount([FromBody] ChartOfAccount account)
        {
            if (string.IsNullOrEmpty(account.AccountCode) || string.IsNullOrEmpty(account.AccountName))
            {
                return BadRequest("Account code and name are required");
            }

            var existing = await _accountRepository.GetByCodeAsync(
                account.CompanyId ?? Guid.Empty, account.AccountCode);
            if (existing != null)
            {
                return BadRequest($"Account with code {account.AccountCode} already exists");
            }

            var created = await _accountRepository.AddAsync(account);

            // Audit trail
            if (created.CompanyId.HasValue)
            {
                await _auditService.AuditCreateAsync(created, created.Id, created.CompanyId.Value, $"{created.AccountCode} - {created.AccountName}");
            }

            return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
        }

        /// <summary>
        /// Update an account
        /// </summary>
        [HttpPut("accounts/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] ChartOfAccount account)
        {
            var existing = await _accountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Account {id} not found");
            }

            if (existing.IsSystemAccount)
            {
                return BadRequest("Cannot modify system account");
            }

            // Capture old state for audit
            var oldAccount = new ChartOfAccount
            {
                Id = existing.Id,
                CompanyId = existing.CompanyId,
                AccountCode = existing.AccountCode,
                AccountName = existing.AccountName,
                AccountType = existing.AccountType,
                IsActive = existing.IsActive
            };

            account.Id = id;
            await _accountRepository.UpdateAsync(account);

            // Audit trail
            if (existing.CompanyId.HasValue)
            {
                await _auditService.AuditUpdateAsync(oldAccount, account, id, existing.CompanyId.Value, $"{account.AccountCode} - {account.AccountName}");
            }

            return Ok(new { message = "Account updated successfully" });
        }

        // ==================== Journal Entries ====================

        /// <summary>
        /// Get journal entries for a company
        /// </summary>
        [HttpGet("journals/company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<JournalEntry>), 200)]
        public async Task<IActionResult> GetJournals(
            Guid companyId,
            [FromQuery] string? financialYear = null,
            [FromQuery] int? periodMonth = null)
        {
            IEnumerable<JournalEntry> entries;

            if (!string.IsNullOrEmpty(financialYear))
            {
                entries = await _journalRepository.GetByPeriodAsync(companyId, financialYear, periodMonth);
            }
            else
            {
                entries = await _journalRepository.GetByCompanyIdAsync(companyId);
            }

            return Ok(entries);
        }

        /// <summary>
        /// Get journal entry by ID with lines
        /// </summary>
        [HttpGet("journals/{id}")]
        [ProducesResponseType(typeof(JournalEntry), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetJournal(Guid id)
        {
            var entry = await _journalRepository.GetByIdWithLinesAsync(id);
            if (entry == null)
            {
                return NotFound($"Journal entry {id} not found");
            }
            return Ok(entry);
        }

        /// <summary>
        /// Create a new journal entry
        /// </summary>
        [HttpPost("journals")]
        [ProducesResponseType(typeof(JournalEntry), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateJournal([FromBody] JournalEntry entry)
        {
            if (entry.Lines == null || !entry.Lines.Any())
            {
                return BadRequest("Journal entry must have at least one line");
            }

            var totalDebit = entry.Lines.Sum(l => l.DebitAmount);
            var totalCredit = entry.Lines.Sum(l => l.CreditAmount);

            if (Math.Abs(totalDebit - totalCredit) >= 0.01m)
            {
                return BadRequest($"Journal entry is not balanced. Debits: {totalDebit}, Credits: {totalCredit}");
            }

            entry.TotalDebit = totalDebit;
            entry.TotalCredit = totalCredit;
            entry.Status = "draft";

            var created = await _journalRepository.AddAsync(entry);

            // Audit trail
            if (created.CompanyId != Guid.Empty)
            {
                await _auditService.AuditCreateAsync(created, created.Id, created.CompanyId, created.JournalNumber ?? $"JE-{created.Id}");
            }

            return CreatedAtAction(nameof(GetJournal), new { id = created.Id }, created);
        }

        /// <summary>
        /// Post a draft journal entry
        /// </summary>
        [HttpPost("journals/{id}/post")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostJournal(Guid id, [FromQuery] Guid? postedBy = null)
        {
            var entry = await _journalRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return NotFound($"Journal entry {id} not found");
            }

            if (entry.Status != "draft")
            {
                return BadRequest("Only draft entries can be posted");
            }

            await _journalRepository.PostAsync(id, postedBy ?? Guid.Empty);
            _logger.LogInformation("Posted journal entry {JournalNumber}", entry.JournalNumber);

            return Ok(new { message = "Journal entry posted successfully" });
        }

        /// <summary>
        /// Reverse a posted journal entry
        /// </summary>
        [HttpPost("journals/{id}/reverse")]
        [ProducesResponseType(typeof(JournalEntry), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReverseJournal(
            Guid id,
            [FromQuery] Guid reversedBy,
            [FromQuery] string? reason = null)
        {
            try
            {
                var reversal = await _autoPostingService.ReverseEntryAsync(id, reversedBy, reason);
                if (reversal == null)
                {
                    return BadRequest("Failed to create reversal entry");
                }

                _logger.LogInformation(
                    "Reversed journal entry {OriginalId} with reversal {ReversalNumber}",
                    id, reversal.JournalNumber);

                return Ok(reversal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== Reports ====================

        /// <summary>
        /// Get trial balance
        /// </summary>
        [HttpGet("reports/trial-balance/{companyId}")]
        [ProducesResponseType(typeof(TrialBalanceReport), 200)]
        public async Task<IActionResult> GetTrialBalance(
            Guid companyId,
            [FromQuery] DateOnly? asOfDate = null,
            [FromQuery] bool includeZeroBalances = false)
        {
            var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
            var report = await _trialBalanceService.GetTrialBalanceAsync(
                companyId, date, includeZeroBalances);
            return Ok(report);
        }

        /// <summary>
        /// Get account ledger
        /// </summary>
        [HttpGet("reports/account-ledger/{accountId}")]
        [ProducesResponseType(typeof(AccountLedgerReport), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAccountLedger(
            Guid accountId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            try
            {
                var report = await _trialBalanceService.GetAccountLedgerAsync(accountId, fromDate, toDate);
                return Ok(report);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get income statement (P&L)
        /// </summary>
        [HttpGet("reports/income-statement/{companyId}")]
        [ProducesResponseType(typeof(IncomeStatementReport), 200)]
        public async Task<IActionResult> GetIncomeStatement(
            Guid companyId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            var report = await _trialBalanceService.GetIncomeStatementAsync(companyId, fromDate, toDate);
            return Ok(report);
        }

        /// <summary>
        /// Get balance sheet
        /// </summary>
        [HttpGet("reports/balance-sheet/{companyId}")]
        [ProducesResponseType(typeof(BalanceSheetReport), 200)]
        public async Task<IActionResult> GetBalanceSheet(
            Guid companyId,
            [FromQuery] DateOnly? asOfDate = null)
        {
            var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
            var report = await _trialBalanceService.GetBalanceSheetAsync(companyId, date);
            return Ok(report);
        }

        // ==================== Data Quality Reports ====================

        /// <summary>
        /// Get abnormal balance report - accounts with balances opposite to their normal balance
        /// </summary>
        [HttpGet("reports/abnormal-balances/{companyId}")]
        [ProducesResponseType(typeof(AbnormalBalanceReport), 200)]
        public async Task<IActionResult> GetAbnormalBalances(Guid companyId)
        {
            var report = await _trialBalanceService.GetAbnormalBalancesAsync(companyId);
            return Ok(report);
        }

        /// <summary>
        /// Get abnormal balance alert summary for dashboard
        /// </summary>
        [HttpGet("alerts/abnormal-balances/{companyId}")]
        [ProducesResponseType(typeof(AbnormalBalanceAlertSummary), 200)]
        public async Task<IActionResult> GetAbnormalBalanceAlert(Guid companyId)
        {
            var alert = await _trialBalanceService.GetAbnormalBalanceAlertAsync(companyId);
            return Ok(alert);
        }

        // ==================== Posting Rules ====================

        /// <summary>
        /// Get posting rules for a company
        /// </summary>
        [HttpGet("posting-rules/company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<PostingRule>), 200)]
        public async Task<IActionResult> GetPostingRules(Guid companyId)
        {
            var rules = await _ruleRepository.GetByCompanyIdAsync(companyId);
            return Ok(rules);
        }

        /// <summary>
        /// Initialize default posting rules for a company
        /// </summary>
        [HttpPost("posting-rules/company/{companyId}/initialize")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InitializePostingRules(Guid companyId)
        {
            if (await _ruleRepository.HasRulesAsync(companyId))
            {
                return BadRequest("Company already has posting rules");
            }

            await _ruleRepository.InitializeDefaultRulesAsync(companyId);
            _logger.LogInformation("Initialized posting rules for company {CompanyId}", companyId);

            return Ok(new { message = "Posting rules initialized successfully" });
        }

        // ==================== Auto-Posting ====================

        /// <summary>
        /// Check if auto-posting is enabled for a company
        /// </summary>
        [HttpGet("auto-posting/status/{companyId}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetAutoPostingStatus(Guid companyId)
        {
            var isEnabled = await _autoPostingService.IsAutoPostingEnabledAsync(companyId);
            return Ok(new { companyId, isAutoPostingEnabled = isEnabled });
        }

        /// <summary>
        /// Manually trigger auto-posting for an invoice
        /// </summary>
        [HttpPost("auto-posting/invoice/{invoiceId}")]
        [ProducesResponseType(typeof(JournalEntry), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AutoPostInvoice(Guid invoiceId, [FromQuery] Guid? postedBy = null)
        {
            var entry = await _autoPostingService.PostInvoiceAsync(invoiceId, postedBy);
            if (entry == null)
            {
                return BadRequest("Failed to create journal entry for invoice");
            }
            return Ok(entry);
        }

        /// <summary>
        /// Manually trigger auto-posting for a payment
        /// </summary>
        [HttpPost("auto-posting/payment/{paymentId}")]
        [ProducesResponseType(typeof(JournalEntry), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AutoPostPayment(Guid paymentId, [FromQuery] Guid? postedBy = null)
        {
            var entry = await _autoPostingService.PostPaymentAsync(paymentId, postedBy);
            if (entry == null)
            {
                return BadRequest("Failed to create journal entry for payment");
            }
            return Ok(entry);
        }

        /// <summary>
        /// Manually trigger auto-posting for a vendor invoice
        /// </summary>
        [HttpPost("auto-posting/vendor-invoice/{vendorInvoiceId}")]
        [ProducesResponseType(typeof(JournalEntry), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AutoPostVendorInvoice(Guid vendorInvoiceId, [FromQuery] Guid? postedBy = null)
        {
            var entry = await _autoPostingService.PostVendorInvoiceAsync(vendorInvoiceId, postedBy);
            if (entry == null)
            {
                return BadRequest("Failed to create journal entry for vendor invoice");
            }
            return Ok(entry);
        }

        /// <summary>
        /// Backfill journal entries for all unposted transactions in a company.
        /// This processes invoices, vendor invoices, and contractor payments that don't have journal entries.
        /// </summary>
        [HttpPost("auto-posting/backfill/{companyId}")]
        [ProducesResponseType(typeof(BackfillResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> BackfillJournalEntries(Guid companyId, [FromQuery] Guid? postedBy = null)
        {
            var result = new BackfillResult();
            var userId = postedBy ?? Guid.Empty;

            try
            {
                // Get unposted invoices
                var unpostedInvoices = await GetUnpostedInvoicesAsync(companyId);
                foreach (var invoiceId in unpostedInvoices)
                {
                    try
                    {
                        var entry = await _autoPostingService.PostInvoiceAsync(invoiceId, userId);
                        if (entry != null)
                        {
                            result.InvoicesPosted++;
                        }
                        else
                        {
                            result.InvoicesFailed++;
                            result.Errors.Add($"Invoice {invoiceId}: No matching posting rule");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.InvoicesFailed++;
                        result.Errors.Add($"Invoice {invoiceId}: {ex.Message}");
                    }
                }

                // Get unposted vendor invoices
                var unpostedVendorInvoices = await GetUnpostedVendorInvoicesAsync(companyId);
                foreach (var vendorInvoiceId in unpostedVendorInvoices)
                {
                    try
                    {
                        var entry = await _autoPostingService.PostVendorInvoiceAsync(vendorInvoiceId, userId);
                        if (entry != null)
                        {
                            result.VendorInvoicesPosted++;
                        }
                        else
                        {
                            result.VendorInvoicesFailed++;
                            result.Errors.Add($"Vendor Invoice {vendorInvoiceId}: No matching posting rule");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.VendorInvoicesFailed++;
                        result.Errors.Add($"Vendor Invoice {vendorInvoiceId}: {ex.Message}");
                    }
                }

                _logger.LogInformation(
                    "Backfill completed for company {CompanyId}: {InvPosted} invoices, {VIPosted} vendor invoices posted",
                    companyId, result.InvoicesPosted, result.VendorInvoicesPosted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill failed for company {CompanyId}", companyId);
                return BadRequest($"Backfill failed: {ex.Message}");
            }
        }

        private async Task<IEnumerable<Guid>> GetUnpostedInvoicesAsync(Guid companyId)
        {
            using var connection = new Npgsql.NpgsqlConnection(GetConnectionString());
            var sql = @"
                SELECT i.id FROM invoices i
                WHERE i.company_id = @companyId
                AND i.status IN ('finalized', 'sent', 'paid')
                AND NOT EXISTS (
                    SELECT 1 FROM journal_entries je
                    WHERE je.source_type = 'invoices' AND je.source_id = i.id
                )";
            return await Dapper.SqlMapper.QueryAsync<Guid>(connection, sql, new { companyId });
        }

        private async Task<IEnumerable<Guid>> GetUnpostedVendorInvoicesAsync(Guid companyId)
        {
            using var connection = new Npgsql.NpgsqlConnection(GetConnectionString());
            var sql = @"
                SELECT vi.id FROM vendor_invoices vi
                WHERE vi.company_id = @companyId
                AND vi.status IN ('approved', 'partially_paid', 'paid')
                AND NOT EXISTS (
                    SELECT 1 FROM journal_entries je
                    WHERE je.source_type = 'vendor_invoices' AND je.source_id = vi.id
                )";
            return await Dapper.SqlMapper.QueryAsync<Guid>(connection, sql, new { companyId });
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=invoice_app;Username=postgres;Password=postgres";
        }

        /// <summary>
        /// Direct test of GetBestMatchingRuleAsync - bypasses AutoPostingService entirely.
        /// </summary>
        [HttpGet("auto-posting/test-rule-match/{invoiceId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> TestRuleMatch(Guid invoiceId)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(GetConnectionString());
                var invoice = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(
                    connection,
                    "SELECT id, invoice_number, invoice_type, supply_type, company_id FROM invoices WHERE id = @invoiceId",
                    new { invoiceId });

                if (invoice == null)
                    return NotFound("Invoice not found");

                var isExport = invoice.supply_type == "export" || invoice.invoice_type == "export";
                var isIntraState = invoice.supply_type == "intra_state";

                var sourceData = new Dictionary<string, object>
                {
                    ["company_id"] = invoice.company_id?.ToString() ?? "",
                    ["invoice_type"] = invoice.invoice_type ?? "b2b",
                    ["is_export"] = isExport,
                    ["is_intra_state"] = isIntraState,
                    ["total_amount"] = 0m,
                    ["subtotal"] = 0m,
                    ["total_cgst"] = 0m,
                    ["total_sgst"] = 0m,
                    ["total_igst"] = 0m,
                    ["source_number"] = invoice.invoice_number ?? ""
                };

                var companyId = (Guid)invoice.company_id;
                var transactionDate = DateOnly.FromDateTime(DateTime.Today);

                // Call GetBestMatchingRuleAsync directly on the repository
                var rule = await _ruleRepository.GetBestMatchingRuleAsync(
                    companyId, "invoice", "on_finalize", sourceData, transactionDate);

                return Ok(new
                {
                    invoiceId,
                    invoiceType = invoice.invoice_type,
                    supplyType = invoice.supply_type,
                    sourceData,
                    ruleFound = rule != null,
                    ruleCode = rule?.RuleCode,
                    ruleName = rule?.RuleName,
                    ruleConditionsJson = rule?.ConditionsJson
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.ToString() });
            }
        }

        /// <summary>
        /// Diagnostic endpoint to test rule matching for a specific invoice without posting.
        /// Shows exactly what conditions are being matched and which rules are found.
        /// </summary>
        [HttpGet("auto-posting/diagnose/invoice/{invoiceId}")]
        [ProducesResponseType(typeof(RuleMatchDiagnostic), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DiagnoseInvoiceRuleMatching(Guid invoiceId)
        {
            var diagnostic = new RuleMatchDiagnostic { InvoiceId = invoiceId };

            try
            {
                // Get invoice
                using var connection = new Npgsql.NpgsqlConnection(GetConnectionString());
                var invoice = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(
                    connection,
                    "SELECT id, invoice_number, invoice_type, supply_type, company_id, total_amount, total_cgst, total_sgst, total_igst FROM invoices WHERE id = @invoiceId",
                    new { invoiceId });

                if (invoice == null)
                    return NotFound($"Invoice {invoiceId} not found");

                diagnostic.InvoiceNumber = invoice.invoice_number;
                diagnostic.InvoiceType = invoice.invoice_type;
                diagnostic.SupplyType = invoice.supply_type;

                // Build source conditions (same logic as AutoPostingService)
                var isExport = invoice.supply_type == "export" || invoice.invoice_type == "export";
                var isIntraState = invoice.supply_type == "intra_state";

                diagnostic.SourceConditions = new Dictionary<string, object>
                {
                    ["company_id"] = invoice.company_id?.ToString() ?? "",
                    ["invoice_type"] = invoice.invoice_type ?? "b2b",
                    ["is_export"] = isExport,
                    ["is_intra_state"] = isIntraState,
                    ["total_amount"] = invoice.total_amount ?? 0m,
                    ["total_cgst"] = invoice.total_cgst ?? 0m,
                    ["total_sgst"] = invoice.total_sgst ?? 0m,
                    ["total_igst"] = invoice.total_igst ?? 0m
                };

                // Get matching rules
                var companyId = (Guid)invoice.company_id;
                var transactionDate = DateOnly.FromDateTime(DateTime.Today);

                var rules = await _ruleRepository.FindMatchingRulesAsync(
                    companyId, "invoice", "on_finalize", transactionDate);

                diagnostic.RulesFound = rules.Count();

                foreach (var rule in rules)
                {
                    var ruleDiag = new RuleDiagnostic
                    {
                        RuleCode = rule.RuleCode,
                        RuleName = rule.RuleName,
                        ConditionsJson = rule.ConditionsJson,
                        ConditionsEmpty = string.IsNullOrEmpty(rule.ConditionsJson)
                    };

                    if (string.IsNullOrEmpty(rule.ConditionsJson))
                    {
                        ruleDiag.Matched = true;
                        ruleDiag.MatchReason = "No conditions - matches all";
                        diagnostic.MatchedRuleCode ??= rule.RuleCode;
                    }
                    else
                    {
                        // Try to match conditions
                        try
                        {
                            var ruleConditions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(rule.ConditionsJson);
                            if (ruleConditions == null)
                            {
                                ruleDiag.Matched = true;
                                ruleDiag.MatchReason = "Conditions parsed as null - matches all";
                            }
                            else
                            {
                                var allMatch = true;
                                var reasons = new List<string>();

                                foreach (var kvp in ruleConditions)
                                {
                                    if (!diagnostic.SourceConditions.TryGetValue(kvp.Key, out var actualValue))
                                    {
                                        allMatch = false;
                                        reasons.Add($"Missing key: {kvp.Key}");
                                        continue;
                                    }

                                    var expectedStr = kvp.Value.ToString();
                                    var actualStr = actualValue?.ToString() ?? "null";

                                    // Handle boolean comparison
                                    if (kvp.Value.ValueKind == System.Text.Json.JsonValueKind.True ||
                                        kvp.Value.ValueKind == System.Text.Json.JsonValueKind.False)
                                    {
                                        bool ruleBoolean = kvp.Value.GetBoolean();
                                        bool actualBoolean = actualValue switch
                                        {
                                            bool b => b,
                                            string s => bool.TryParse(s, out var parsed) && parsed,
                                            _ => false
                                        };

                                        if (ruleBoolean != actualBoolean)
                                        {
                                            allMatch = false;
                                            reasons.Add($"{kvp.Key}: expected {ruleBoolean}, got {actualBoolean}");
                                        }
                                        else
                                        {
                                            reasons.Add($"{kvp.Key}: {ruleBoolean} == {actualBoolean} ✓");
                                        }
                                    }
                                    else
                                    {
                                        // String comparison
                                        var ruleValue = kvp.Value.GetString();
                                        if (!string.Equals(ruleValue, actualStr, StringComparison.OrdinalIgnoreCase))
                                        {
                                            allMatch = false;
                                            reasons.Add($"{kvp.Key}: expected '{ruleValue}', got '{actualStr}'");
                                        }
                                        else
                                        {
                                            reasons.Add($"{kvp.Key}: '{ruleValue}' == '{actualStr}' ✓");
                                        }
                                    }
                                }

                                ruleDiag.Matched = allMatch;
                                ruleDiag.MatchReason = string.Join("; ", reasons);
                                if (allMatch && diagnostic.MatchedRuleCode == null)
                                {
                                    diagnostic.MatchedRuleCode = rule.RuleCode;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ruleDiag.Matched = false;
                            ruleDiag.MatchReason = $"Parse error: {ex.Message}";
                        }
                    }

                    diagnostic.Rules.Add(ruleDiag);
                }

                return Ok(diagnostic);
            }
            catch (Exception ex)
            {
                diagnostic.MatchError = ex.ToString();
                return Ok(diagnostic);
            }
        }
    }

    /// <summary>
    /// Result of a backfill operation
    /// </summary>
    public class BackfillResult
    {
        public int InvoicesPosted { get; set; }
        public int InvoicesFailed { get; set; }
        public int VendorInvoicesPosted { get; set; }
        public int VendorInvoicesFailed { get; set; }
        public List<string> Errors { get; set; } = new();

        public int TotalPosted => InvoicesPosted + VendorInvoicesPosted;
        public int TotalFailed => InvoicesFailed + VendorInvoicesFailed;
    }

    /// <summary>
    /// Diagnostic result for rule matching
    /// </summary>
    public class RuleMatchDiagnostic
    {
        public Guid InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceType { get; set; }
        public string? SupplyType { get; set; }
        public string SourceType { get; set; } = "invoice";
        public string TriggerEvent { get; set; } = "on_finalize";
        public Dictionary<string, object> SourceConditions { get; set; } = new();
        public int RulesFound { get; set; }
        public List<RuleDiagnostic> Rules { get; set; } = new();
        public string? MatchedRuleCode { get; set; }
        public string? MatchError { get; set; }
    }

    public class RuleDiagnostic
    {
        public string RuleCode { get; set; } = "";
        public string RuleName { get; set; } = "";
        public string? ConditionsJson { get; set; }
        public bool ConditionsEmpty { get; set; }
        public bool Matched { get; set; }
        public string? MatchReason { get; set; }
    }
}
