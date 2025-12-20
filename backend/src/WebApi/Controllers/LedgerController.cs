using Application.Interfaces.Ledger;
using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Microsoft.AspNetCore.Mvc;

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
        private readonly ILogger<LedgerController> _logger;

        public LedgerController(
            IChartOfAccountRepository accountRepository,
            IJournalEntryRepository journalRepository,
            IPostingRuleRepository ruleRepository,
            IAutoPostingService autoPostingService,
            ITrialBalanceService trialBalanceService,
            ILogger<LedgerController> logger)
        {
            _accountRepository = accountRepository;
            _journalRepository = journalRepository;
            _ruleRepository = ruleRepository;
            _autoPostingService = autoPostingService;
            _trialBalanceService = trialBalanceService;
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

            account.Id = id;
            await _accountRepository.UpdateAsync(account);
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
    }
}
