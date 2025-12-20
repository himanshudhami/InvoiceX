using Application.Interfaces.Ledger;
using Core.Entities.Ledger;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services.Ledger
{
    /// <summary>
    /// Service for automatically posting journal entries based on business events
    /// Uses posting rules to determine account mappings
    /// </summary>
    public class AutoPostingService : IAutoPostingService
    {
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IPostingRuleRepository _ruleRepository;
        private readonly IInvoicesRepository _invoiceRepository;
        private readonly IPaymentsRepository _paymentRepository;
        private readonly ILogger<AutoPostingService> _logger;

        public AutoPostingService(
            IChartOfAccountRepository accountRepository,
            IJournalEntryRepository journalRepository,
            IPostingRuleRepository ruleRepository,
            IInvoicesRepository invoiceRepository,
            IPaymentsRepository paymentRepository,
            ILogger<AutoPostingService> logger)
        {
            _accountRepository = accountRepository;
            _journalRepository = journalRepository;
            _ruleRepository = ruleRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<JournalEntry?> PostInvoiceAsync(
            Guid invoiceId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found for auto-posting", invoiceId);
                    return null;
                }

                // Determine export and intra-state status from SupplyType
                var isExport = invoice.SupplyType == "export" || invoice.InvoiceType == "export";
                var isIntraState = invoice.SupplyType == "intra_state";

                // Build source data from invoice
                var sourceData = new Dictionary<string, object>
                {
                    ["company_id"] = invoice.CompanyId?.ToString() ?? "",
                    ["invoice_type"] = invoice.InvoiceType ?? "b2b",
                    ["is_export"] = isExport,
                    ["is_intra_state"] = isIntraState,
                    ["total_amount"] = invoice.TotalAmount,
                    ["subtotal"] = invoice.Subtotal,
                    ["total_cgst"] = invoice.TotalCgst,
                    ["total_sgst"] = invoice.TotalSgst,
                    ["total_igst"] = invoice.TotalIgst,
                    ["customer_name"] = "Customer", // TODO: Fetch from customer entity if needed
                    ["source_number"] = invoice.InvoiceNumber
                };

                return await PostFromSourceAsync(
                    "invoice",
                    invoiceId,
                    sourceData,
                    postedBy,
                    autoPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-posting invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<JournalEntry?> PostPaymentAsync(
            Guid paymentId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found for auto-posting", paymentId);
                    return null;
                }

                var hasTds = (payment.TdsAmount ?? 0) > 0;
                var tdsSection = payment.TdsSection ?? "194J";

                var sourceData = new Dictionary<string, object>
                {
                    ["tds_applicable"] = hasTds,
                    ["tds_section"] = tdsSection,
                    ["is_advance"] = payment.InvoiceId == null,
                    ["amount"] = payment.Amount,
                    ["net_amount"] = payment.Amount,
                    ["gross_amount"] = payment.Amount + (payment.TdsAmount ?? 0),
                    ["tds_amount"] = payment.TdsAmount ?? 0,
                    ["customer_name"] = "Customer", // TODO: Get from invoice/customer
                    ["payment_reference"] = payment.PaymentMethod ?? "Payment",
                    ["source_number"] = payment.Id.ToString()[..8]
                };

                return await PostFromSourceAsync(
                    "payment",
                    paymentId,
                    sourceData,
                    postedBy,
                    autoPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-posting payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<JournalEntry?> PostPayrollAsync(
            Guid payrollRunId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            // TODO: Implement payroll posting when payroll entity is available
            _logger.LogWarning("Payroll auto-posting not yet implemented");
            return null;
        }

        public async Task<JournalEntry?> PostExpenseAsync(
            Guid expenseId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            // TODO: Implement expense posting when expense entity is available
            _logger.LogWarning("Expense auto-posting not yet implemented");
            return null;
        }

        public async Task<JournalEntry?> PostFromSourceAsync(
            string sourceType,
            Guid sourceId,
            Dictionary<string, object> sourceData,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            try
            {
                // Check if already posted
                if (await _journalRepository.HasEntriesForSourceAsync(sourceType, sourceId))
                {
                    _logger.LogInformation(
                        "Journal entry already exists for {SourceType} {SourceId}",
                        sourceType, sourceId);
                    return null;
                }

                // Get company ID from source data or first available
                if (!sourceData.TryGetValue("company_id", out var companyIdObj) ||
                    !Guid.TryParse(companyIdObj?.ToString(), out var companyId))
                {
                    _logger.LogWarning("Company ID not found in source data for {SourceType}", sourceType);
                    return null;
                }

                // Find matching posting rule
                var transactionDate = DateOnly.FromDateTime(DateTime.Today);
                var rule = await _ruleRepository.GetBestMatchingRuleAsync(
                    companyId,
                    sourceType,
                    "on_finalize", // Default trigger
                    sourceData,
                    transactionDate);

                if (rule == null)
                {
                    _logger.LogWarning(
                        "No posting rule found for {SourceType} with conditions {Conditions}",
                        sourceType, JsonSerializer.Serialize(sourceData));
                    return null;
                }

                // Parse posting template
                var template = JsonSerializer.Deserialize<PostingTemplate>(rule.PostingTemplate);
                if (template == null || template.Lines == null || !template.Lines.Any())
                {
                    _logger.LogError("Invalid posting template for rule {RuleCode}", rule.RuleCode);
                    return null;
                }

                // Get financial year
                var financialYear = GetFinancialYear(transactionDate);
                var periodMonth = GetPeriodMonth(transactionDate);

                // Build journal entry
                var journalEntry = new JournalEntry
                {
                    CompanyId = companyId,
                    JournalDate = transactionDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = sourceType,
                    SourceId = sourceId,
                    SourceNumber = sourceData.TryGetValue("source_number", out var srcNum) ? srcNum?.ToString() : null,
                    Description = BuildDescription(template.DescriptionTemplate, sourceData),
                    Status = autoPost ? "posted" : "draft",
                    PostedAt = autoPost ? DateTime.UtcNow : null,
                    PostedBy = autoPost ? postedBy : null,
                    RulePackVersion = rule.FinancialYear,
                    RuleCode = rule.RuleCode,
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                decimal totalDebit = 0;
                decimal totalCredit = 0;

                // Build journal lines
                foreach (var lineTemplate in template.Lines)
                {
                    var account = await _accountRepository.GetByCodeAsync(companyId, lineTemplate.AccountCode);
                    if (account == null)
                    {
                        _logger.LogError(
                            "Account {AccountCode} not found for company {CompanyId}",
                            lineTemplate.AccountCode, companyId);
                        continue;
                    }

                    var amount = GetAmountFromSourceData(sourceData, lineTemplate.AmountField);
                    if (amount <= 0) continue;

                    var line = new JournalEntryLine
                    {
                        AccountId = account.Id,
                        DebitAmount = lineTemplate.Side == "debit" ? amount : 0,
                        CreditAmount = lineTemplate.Side == "credit" ? amount : 0,
                        Description = lineTemplate.Description,
                        Currency = "INR",
                        ExchangeRate = 1
                    };

                    journalEntry.Lines.Add(line);
                    totalDebit += line.DebitAmount;
                    totalCredit += line.CreditAmount;
                }

                if (!journalEntry.Lines.Any())
                {
                    _logger.LogWarning("No valid journal lines created for {SourceType} {SourceId}", sourceType, sourceId);
                    return null;
                }

                journalEntry.TotalDebit = totalDebit;
                journalEntry.TotalCredit = totalCredit;

                // Validate balanced
                if (Math.Abs(totalDebit - totalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Journal entry for {SourceType} {SourceId} is not balanced. Debit: {Debit}, Credit: {Credit}",
                        sourceType, sourceId, totalDebit, totalCredit);
                    return null;
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(journalEntry);

                // Log rule usage
                await _ruleRepository.LogUsageAsync(new PostingRuleUsageLog
                {
                    PostingRuleId = rule.Id,
                    JournalEntryId = savedEntry.Id,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    ConditionsSnapshot = rule.ConditionsJson,
                    TemplateSnapshot = rule.PostingTemplate,
                    AppliedBy = postedBy
                });

                _logger.LogInformation(
                    "Created journal entry {JournalNumber} for {SourceType} {SourceId}",
                    savedEntry.JournalNumber, sourceType, sourceId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating journal entry for {SourceType} {SourceId}", sourceType, sourceId);
                throw;
            }
        }

        public async Task<JournalEntry?> ReverseEntryAsync(Guid journalEntryId, Guid reversedBy, string? reason = null)
        {
            try
            {
                return await _journalRepository.CreateReversalAsync(journalEntryId, reversedBy, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing journal entry {JournalEntryId}", journalEntryId);
                throw;
            }
        }

        public async Task<bool> IsAutoPostingEnabledAsync(Guid companyId)
        {
            // For now, check if the company has chart of accounts initialized
            return await _accountRepository.HasAccountsAsync(companyId);
        }

        // ==================== Helper Methods ====================

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            // April = 1, March = 12
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }

        private static string BuildDescription(string? template, Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(template)) return "Auto-posted entry";

            var result = template;
            foreach (var kvp in data)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            return result;
        }

        private static decimal GetAmountFromSourceData(Dictionary<string, object> data, string fieldName)
        {
            if (!data.TryGetValue(fieldName, out var value)) return 0;

            return value switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                long l => l,
                string s when decimal.TryParse(s, out var parsed) => parsed,
                JsonElement je when je.TryGetDecimal(out var jd) => jd,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Posting template structure
    /// </summary>
    internal class PostingTemplate
    {
        public string? DescriptionTemplate { get; set; }
        public List<PostingTemplateLine>? Lines { get; set; }
    }

    internal class PostingTemplateLine
    {
        public string AccountCode { get; set; } = string.Empty;
        public string Side { get; set; } = "debit";
        public string AmountField { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
