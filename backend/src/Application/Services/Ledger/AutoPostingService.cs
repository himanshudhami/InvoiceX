using Application.Interfaces.Ledger;
using Core.Entities.Ledger;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Services.Ledger
{
    /// <summary>
    /// Service for automatically posting journal entries based on business events.
    /// Uses posting rules to determine account mappings.
    ///
    /// Note: Expense claim posting has been moved to dedicated ExpensePostingService
    /// following SOLID/SRP principles for better separation of concerns.
    /// </summary>
    public class AutoPostingService : IAutoPostingService
    {
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IPostingRuleRepository _ruleRepository;
        private readonly IInvoicesRepository _invoiceRepository;
        private readonly IPaymentsRepository _paymentRepository;
        private readonly IVendorInvoicesRepository _vendorInvoiceRepository;
        private readonly IVendorPaymentsRepository _vendorPaymentRepository;
        private readonly ILogger<AutoPostingService> _logger;

        public AutoPostingService(
            IChartOfAccountRepository accountRepository,
            IJournalEntryRepository journalRepository,
            IPostingRuleRepository ruleRepository,
            IInvoicesRepository invoiceRepository,
            IPaymentsRepository paymentRepository,
            IVendorInvoicesRepository vendorInvoiceRepository,
            IVendorPaymentsRepository vendorPaymentRepository,
            ILogger<AutoPostingService> logger)
        {
            _accountRepository = accountRepository;
            _journalRepository = journalRepository;
            _ruleRepository = ruleRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _vendorInvoiceRepository = vendorInvoiceRepository;
            _vendorPaymentRepository = vendorPaymentRepository;
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
                    ["invoice_amount_inr"] = invoice.InvoiceAmountInr ?? invoice.TotalAmount,
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
                    ["company_id"] = payment.CompanyId?.ToString() ?? "",
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

        // Note: Dedicated posting services exist for specific domains (SOLID/SRP):
        // - IPayrollPostingService: Payroll journal entries (accrual, disbursement, statutory)
        // - IContractorPostingService: Contractor payment journal entries
        // - IExpensePostingService: Expense claim journal entries

        public async Task<JournalEntry?> PostExpenseAsync(
            Guid expenseId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            // TODO: Implement general expense posting (not expense claims)
            _logger.LogWarning("General expense auto-posting not yet implemented");
            return null;
        }

        public async Task<JournalEntry?> PostVendorInvoiceAsync(
            Guid vendorInvoiceId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            try
            {
                var vendorInvoice = await _vendorInvoiceRepository.GetByIdAsync(vendorInvoiceId);
                if (vendorInvoice == null)
                {
                    _logger.LogWarning("Vendor invoice {VendorInvoiceId} not found for auto-posting", vendorInvoiceId);
                    return null;
                }

                // Determine supply type conditions
                var isIntraState = vendorInvoice.SupplyType == "intra_state";
                var isRcm = vendorInvoice.RcmApplicable;
                var hasTds = vendorInvoice.TdsApplicable && (vendorInvoice.TdsAmount ?? 0) > 0;

                // Build source data from vendor invoice
                var sourceData = new Dictionary<string, object>
                {
                    ["company_id"] = vendorInvoice.CompanyId.ToString(),
                    ["vendor_id"] = vendorInvoice.PartyId.ToString(),
                    ["invoice_type"] = vendorInvoice.InvoiceType ?? "regular",
                    ["is_intra_state"] = isIntraState,
                    ["is_rcm"] = isRcm,
                    ["tds_applicable"] = hasTds,
                    ["tds_section"] = vendorInvoice.TdsSection ?? "194C",
                    ["itc_eligible"] = vendorInvoice.ItcEligible,
                    ["total_amount"] = vendorInvoice.TotalAmount,
                    ["subtotal"] = vendorInvoice.Subtotal,
                    ["total_cgst"] = vendorInvoice.TotalCgst,
                    ["total_sgst"] = vendorInvoice.TotalSgst,
                    ["total_igst"] = vendorInvoice.TotalIgst,
                    ["tds_amount"] = vendorInvoice.TdsAmount ?? 0,
                    ["net_payable"] = vendorInvoice.TotalAmount - (vendorInvoice.TdsAmount ?? 0),
                    ["source_number"] = vendorInvoice.InvoiceNumber
                };

                return await PostFromSourceAsync(
                    "vendor_invoice",
                    vendorInvoiceId,
                    sourceData,
                    postedBy,
                    autoPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-posting vendor invoice {VendorInvoiceId}", vendorInvoiceId);
                throw;
            }
        }

        public async Task<JournalEntry?> PostVendorPaymentAsync(
            Guid vendorPaymentId,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            try
            {
                var vendorPayment = await _vendorPaymentRepository.GetByIdAsync(vendorPaymentId);
                if (vendorPayment == null)
                {
                    _logger.LogWarning("Vendor payment {VendorPaymentId} not found for auto-posting", vendorPaymentId);
                    return null;
                }

                var hasTds = vendorPayment.TdsApplicable && (vendorPayment.TdsAmount ?? 0) > 0;

                // Determine payment type
                var paymentType = vendorPayment.PaymentType ?? "bill_payment";
                var isAdvance = paymentType == "advance_paid";

                // Build source data from vendor payment
                var sourceData = new Dictionary<string, object>
                {
                    ["company_id"] = vendorPayment.CompanyId.ToString(),
                    ["vendor_id"] = vendorPayment.PartyId.ToString(),
                    ["payment_type"] = paymentType,
                    ["is_advance"] = isAdvance,
                    ["tds_applicable"] = hasTds,
                    ["tds_section"] = vendorPayment.TdsSection ?? "194C",
                    ["amount"] = vendorPayment.Amount,
                    ["gross_amount"] = vendorPayment.GrossAmount ?? vendorPayment.Amount,
                    ["tds_amount"] = vendorPayment.TdsAmount ?? 0,
                    ["net_amount"] = vendorPayment.Amount,
                    ["payment_method"] = vendorPayment.PaymentMethod ?? "bank_transfer",
                    ["source_number"] = vendorPayment.ReferenceNumber ?? vendorPayment.Id.ToString()[..8]
                };

                return await PostFromSourceAsync(
                    "vendor_payment",
                    vendorPaymentId,
                    sourceData,
                    postedBy,
                    autoPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-posting vendor payment {VendorPaymentId}", vendorPaymentId);
                throw;
            }
        }

        public async Task<JournalEntry?> PostFromSourceAsync(
            string sourceType,
            Guid sourceId,
            Dictionary<string, object> sourceData,
            Guid? postedBy = null,
            bool autoPost = true)
        {
            return await PostFromSourceWithTriggerAsync(
                sourceType, sourceId, sourceData, "on_finalize", postedBy, autoPost);
        }

        /// <summary>
        /// Post from source with a specific trigger event
        /// </summary>
        private async Task<JournalEntry?> PostFromSourceWithTriggerAsync(
            string sourceType,
            Guid sourceId,
            Dictionary<string, object> sourceData,
            string triggerEvent,
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

                // Get company ID from source data
                if (!sourceData.TryGetValue("company_id", out var companyIdObj) ||
                    !Guid.TryParse(companyIdObj?.ToString(), out var companyId))
                {
                    _logger.LogWarning("Company ID not found in source data for {SourceType}", sourceType);
                    return null;
                }

                // Find matching posting rule with specific trigger
                var transactionDate = DateOnly.FromDateTime(DateTime.Today);
                var rule = await _ruleRepository.GetBestMatchingRuleAsync(
                    companyId,
                    sourceType,
                    triggerEvent,
                    sourceData,
                    transactionDate);

                if (rule == null)
                {
                    _logger.LogWarning(
                        "No posting rule found for {SourceType} with trigger {TriggerEvent} and conditions {Conditions}",
                        sourceType, triggerEvent, JsonSerializer.Serialize(sourceData));
                    return null;
                }

                // Parse posting template (use case-insensitive for camelCase JSON)
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var template = JsonSerializer.Deserialize<PostingTemplate>(rule.PostingTemplate, jsonOptions);
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
                    Description = BuildDescription(
                        template.NarrationTemplate ?? template.NarrationTemplateCamel ??
                        template.DescriptionTemplate ?? template.DescriptionTemplateCamel,
                        sourceData),
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
                    // Resolve account code from source data or static values
                    var accountCode = ResolveAccountCode(lineTemplate, sourceData);
                    if (string.IsNullOrEmpty(accountCode))
                    {
                        _logger.LogWarning("Could not resolve account code for template line");
                        continue;
                    }

                    var account = await _accountRepository.GetByCodeAsync(companyId, accountCode);
                    if (account == null)
                    {
                        _logger.LogError(
                            "Account {AccountCode} not found for company {CompanyId}",
                            accountCode, companyId);
                        continue;
                    }

                    // Determine debit/credit amounts
                    decimal debitAmount = 0;
                    decimal creditAmount = 0;

                    var debitField = lineTemplate.DebitField ?? lineTemplate.DebitFieldCamel;
                    var creditField = lineTemplate.CreditField ?? lineTemplate.CreditFieldCamel;
                    var amountField = lineTemplate.AmountField ?? lineTemplate.AmountFieldCamel;

                    if (!string.IsNullOrEmpty(debitField))
                    {
                        debitAmount = GetAmountFromSourceData(sourceData, debitField);
                    }
                    else if (!string.IsNullOrEmpty(creditField))
                    {
                        creditAmount = GetAmountFromSourceData(sourceData, creditField);
                    }
                    else if (!string.IsNullOrEmpty(amountField))
                    {
                        // Legacy support for Side + AmountField
                        var amount = GetAmountFromSourceData(sourceData, amountField);
                        if (lineTemplate.Side == "debit")
                            debitAmount = amount;
                        else
                            creditAmount = amount;
                    }

                    // Skip zero-amount lines
                    if (debitAmount <= 0 && creditAmount <= 0) continue;

                    // Build description from template
                    var description = BuildDescription(
                        lineTemplate.DescriptionTemplate ?? lineTemplate.DescriptionTemplateCamel ??
                        lineTemplate.Description,
                        sourceData);

                    var line = new JournalEntryLine
                    {
                        AccountId = account.Id,
                        DebitAmount = debitAmount,
                        CreditAmount = creditAmount,
                        Description = description,
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
                    RuleSnapshot = JsonSerializer.Serialize(new { rule.ConditionsJson, rule.PostingTemplate }),
                    ComputedBy = postedBy,
                    Success = true
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

        /// <summary>
        /// Resolves account code from template line using source data
        /// Priority: 1) AccountCodeField from source data, 2) AccountCode, 3) AccountCodeFallback
        /// </summary>
        private static string? ResolveAccountCode(PostingTemplateLine lineTemplate, Dictionary<string, object> sourceData)
        {
            // 1. Try to get account code from source data field
            var accountCodeField = lineTemplate.AccountCodeField ?? lineTemplate.AccountCodeFieldCamel;
            if (!string.IsNullOrEmpty(accountCodeField))
            {
                if (sourceData.TryGetValue(accountCodeField, out var fieldValue))
                {
                    var codeFromField = fieldValue?.ToString();
                    if (!string.IsNullOrEmpty(codeFromField))
                    {
                        return codeFromField;
                    }
                }
            }

            // 2. Use static account code if specified
            var accountCode = lineTemplate.AccountCode ?? lineTemplate.AccountCodeCamel;
            if (!string.IsNullOrEmpty(accountCode))
            {
                return accountCode;
            }

            // 3. Use fallback account code
            var accountCodeFallback = lineTemplate.AccountCodeFallback ?? lineTemplate.AccountCodeFallbackCamel;
            if (!string.IsNullOrEmpty(accountCodeFallback))
            {
                return accountCodeFallback;
            }

            return null;
        }
    }

    /// <summary>
    /// Posting template structure - matches migration 091 template format
    /// </summary>
    internal class PostingTemplate
    {
        [JsonPropertyName("description_template")]
        public string? DescriptionTemplate { get; set; }

        [JsonPropertyName("descriptionTemplate")]
        public string? DescriptionTemplateCamel { get; set; }

        [JsonPropertyName("narration_template")]
        public string? NarrationTemplate { get; set; }

        [JsonPropertyName("narrationTemplate")]
        public string? NarrationTemplateCamel { get; set; }

        [JsonPropertyName("lines")]
        public List<PostingTemplateLine>? Lines { get; set; }
    }

    /// <summary>
    /// Template line supporting dynamic account resolution from source data
    /// JSON property names match snake_case format from database
    /// </summary>
    internal class PostingTemplateLine
    {
        // Account resolution - checked in order:
        // 1. AccountCodeField - get from source data (e.g., "expense_account")
        // 2. AccountCode - static account code (e.g., "1141")
        // 3. AccountCodeFallback - fallback if above are empty (e.g., "5100")
        [JsonPropertyName("account_code_field")]
        public string? AccountCodeField { get; set; }

        [JsonPropertyName("accountCodeField")]
        public string? AccountCodeFieldCamel { get; set; }

        [JsonPropertyName("account_code")]
        public string? AccountCode { get; set; }

        [JsonPropertyName("accountCode")]
        public string? AccountCodeCamel { get; set; }

        [JsonPropertyName("account_code_fallback")]
        public string? AccountCodeFallback { get; set; }

        [JsonPropertyName("accountCodeFallback")]
        public string? AccountCodeFallbackCamel { get; set; }

        // Amount fields - one of these should be set to determine debit/credit
        [JsonPropertyName("debit_field")]
        public string? DebitField { get; set; }

        [JsonPropertyName("debitField")]
        public string? DebitFieldCamel { get; set; }

        [JsonPropertyName("credit_field")]
        public string? CreditField { get; set; }

        [JsonPropertyName("creditField")]
        public string? CreditFieldCamel { get; set; }

        // Legacy support for simple templates
        [JsonPropertyName("side")]
        public string? Side { get; set; }

        [JsonPropertyName("amount_field")]
        public string? AmountField { get; set; }

        [JsonPropertyName("amountField")]
        public string? AmountFieldCamel { get; set; }

        // Description template with placeholders
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("description_template")]
        public string? DescriptionTemplate { get; set; }

        [JsonPropertyName("descriptionTemplate")]
        public string? DescriptionTemplateCamel { get; set; }

        // Subledger tracking
        [JsonPropertyName("subledger_type")]
        public string? SubledgerType { get; set; }

        [JsonPropertyName("subledgerType")]
        public string? SubledgerTypeCamel { get; set; }

        [JsonPropertyName("subledger_id_field")]
        public string? SubledgerIdField { get; set; }

        [JsonPropertyName("subledgerIdField")]
        public string? SubledgerIdFieldCamel { get; set; }
    }
}
