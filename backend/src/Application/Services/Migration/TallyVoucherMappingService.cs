using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities;
using Core.Entities.Ledger;
using Core.Entities.Migration;
using Core.Entities.Tags;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Core.Interfaces.Migration;
using Core.Interfaces.Tags;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Service for mapping Tally vouchers to our transaction entities
    /// </summary>
    public class TallyVoucherMappingService : ITallyVoucherMappingService
    {
        private readonly ILogger<TallyVoucherMappingService> _logger;
        private readonly ITallyMigrationLogRepository _logRepository;
        private readonly ITallyMigrationBatchRepository _batchRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IVendorInvoicesRepository _vendorInvoiceRepository;
        private readonly IVendorPaymentsRepository _vendorPaymentRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly IVendorsRepository _vendorsRepository;
        private readonly IChartOfAccountRepository _coaRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ITransactionTagRepository _transactionTagRepository;
        private readonly ITallyPaymentClassifier _paymentClassifier;
        private readonly ITallyContractorPaymentMapper _contractorPaymentMapper;
        private readonly ITallyStatutoryPaymentMapper _statutoryPaymentMapper;
        private readonly ITallyBankTransactionMapper _bankTransactionMapper;

        public TallyVoucherMappingService(
            ILogger<TallyVoucherMappingService> logger,
            ITallyMigrationLogRepository logRepository,
            ITallyMigrationBatchRepository batchRepository,
            IInvoicesRepository invoicesRepository,
            IPaymentsRepository paymentsRepository,
            IVendorInvoicesRepository vendorInvoiceRepository,
            IVendorPaymentsRepository vendorPaymentRepository,
            IJournalEntryRepository journalRepository,
            ICustomersRepository customersRepository,
            IVendorsRepository vendorsRepository,
            IChartOfAccountRepository coaRepository,
            ITagRepository tagRepository,
            ITransactionTagRepository transactionTagRepository,
            ITallyPaymentClassifier paymentClassifier,
            ITallyContractorPaymentMapper contractorPaymentMapper,
            ITallyStatutoryPaymentMapper statutoryPaymentMapper,
            ITallyBankTransactionMapper bankTransactionMapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _vendorInvoiceRepository = vendorInvoiceRepository ?? throw new ArgumentNullException(nameof(vendorInvoiceRepository));
            _vendorPaymentRepository = vendorPaymentRepository ?? throw new ArgumentNullException(nameof(vendorPaymentRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _vendorsRepository = vendorsRepository ?? throw new ArgumentNullException(nameof(vendorsRepository));
            _coaRepository = coaRepository ?? throw new ArgumentNullException(nameof(coaRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _transactionTagRepository = transactionTagRepository ?? throw new ArgumentNullException(nameof(transactionTagRepository));
            _paymentClassifier = paymentClassifier ?? throw new ArgumentNullException(nameof(paymentClassifier));
            _contractorPaymentMapper = contractorPaymentMapper ?? throw new ArgumentNullException(nameof(contractorPaymentMapper));
            _statutoryPaymentMapper = statutoryPaymentMapper ?? throw new ArgumentNullException(nameof(statutoryPaymentMapper));
            _bankTransactionMapper = bankTransactionMapper ?? throw new ArgumentNullException(nameof(bankTransactionMapper));
        }

        public async Task<Result<TallyVoucherImportResultDto>> ImportVouchersAsync(
            Guid batchId,
            Guid companyId,
            TallyVouchersSummaryDto vouchers,
            TallyImportRequestDto request,
            IProgress<TallyImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new TallyVoucherImportResultDto();
            var allVouchers = vouchers.Vouchers;

            // Filter by date range if specified
            if (request.FromDate.HasValue)
                allVouchers = allVouchers.Where(v => v.Date >= request.FromDate.Value).ToList();
            if (request.ToDate.HasValue)
                allVouchers = allVouchers.Where(v => v.Date <= request.ToDate.Value).ToList();

            // Filter by record types if specified
            if (request.RecordTypes != null && request.RecordTypes.Any())
            {
                allVouchers = allVouchers.Where(v =>
                    request.RecordTypes.Any(rt =>
                        v.VoucherType.Equals(rt, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            // Sort by date
            allVouchers = allVouchers.OrderBy(v => v.Date).ThenBy(v => v.VoucherNumber).ToList();

            var total = allVouchers.Count;
            var processed = 0;

            // Group vouchers by type
            var groupedVouchers = allVouchers
                .GroupBy(v => NormalizeVoucherType(v.VoucherType))
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("Starting voucher import: {Total} vouchers across {Types} types",
                total, groupedVouchers.Count);

            // Import each type
            foreach (var (voucherType, voucherList) in groupedVouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var typeResult = voucherType switch
                {
                    "sales" => await ImportSalesVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    "purchase" => await ImportPurchaseVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    "receipt" => await ImportReceiptVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    "payment" => await ImportPaymentVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    "journal" or "contra" => await ImportJournalVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    "credit_note" => await ImportCreditNotesAsync(batchId, companyId, voucherList, cancellationToken),
                    "debit_note" => await ImportDebitNotesAsync(batchId, companyId, voucherList, cancellationToken),
                    "stock_journal" or "physical_stock" or "delivery_note" or "receipt_note" =>
                        await ImportStockVouchersAsync(batchId, companyId, voucherList, cancellationToken),
                    _ => await ImportAsJournalEntryAsync(batchId, companyId, voucherList, cancellationToken)
                };

                if (typeResult.IsSuccess)
                {
                    var counts = typeResult.Value!;
                    result.ByVoucherType[voucherType] = counts.Imported;
                    result.TotalImported += counts.Imported;
                    result.TotalFailed += counts.Failed;
                    result.TotalSkipped += counts.Skipped;

                    // Assign to appropriate result property
                    switch (voucherType)
                    {
                        case "sales": result.Sales = counts; break;
                        case "purchase": result.Purchases = counts; break;
                        case "receipt": result.Receipts = counts; break;
                        case "payment": result.Payments = counts; break;
                        case "journal": case "contra": result.Journals = counts; break;
                        case "credit_note": result.CreditNotes = counts; break;
                        case "debit_note": result.DebitNotes = counts; break;
                        case "stock_journal": result.StockJournals = counts; break;
                    }
                }

                processed += voucherList.Count;

                // Report progress
                progress?.Report(new TallyImportProgressDto
                {
                    BatchId = batchId,
                    Status = "importing",
                    CurrentPhase = "vouchers",
                    TotalVouchers = total,
                    ProcessedVouchers = processed,
                    SuccessfulVouchers = result.TotalImported,
                    FailedVouchers = result.TotalFailed,
                    PercentComplete = total > 0 ? (processed * 100 / total) : 0,
                    CurrentItem = $"Processing {voucherType} vouchers"
                });
            }

            // Calculate totals
            foreach (var v in allVouchers)
            {
                foreach (var entry in v.LedgerEntries)
                {
                    if (entry.Amount > 0)
                        result.TotalDebitAmount += entry.Amount;
                    else
                        result.TotalCreditAmount += Math.Abs(entry.Amount);
                }
            }

            _logger.LogInformation(
                "Voucher import completed: {Imported} imported, {Failed} failed, {Skipped} skipped",
                result.TotalImported, result.TotalFailed, result.TotalSkipped);

            return Result<TallyVoucherImportResultDto>.Success(result);
        }

        public async Task<Result<TallyImportCountsDto>> ImportSalesVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _invoicesRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogVoucherMigration(batchId, voucher, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve customer
                    Guid? customerId = null;
                    if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
                    {
                        var customer = await _customersRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                        if (customer == null && !string.IsNullOrEmpty(voucher.PartyLedgerGuid))
                        {
                            customer = await _customersRepository.GetByTallyGuidAsync(companyId, voucher.PartyLedgerGuid);
                        }
                        customerId = customer?.Id;
                    }

                    // Calculate totals from ledger entries
                    var taxAmount = voucher.LedgerEntries
                        .Sum(e => (e.CgstAmount ?? 0) + (e.SgstAmount ?? 0) + (e.IgstAmount ?? 0) + (e.CessAmount ?? 0));
                    var subtotal = voucher.Amount - taxAmount;

                    var invoice = new Invoices
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        PartyId = customerId,
                        InvoiceNumber = voucher.VoucherNumber,
                        InvoiceDate = voucher.Date,
                        DueDate = CalculateDueDate(voucher),
                        Subtotal = subtotal,
                        TaxAmount = taxAmount,
                        TotalAmount = voucher.Amount,
                        Status = voucher.IsCancelled ? "cancelled" : "paid", // Valid: draft, sent, viewed, partially_paid, paid, overdue, cancelled
                        Notes = voucher.Narration,
                        PoNumber = voucher.ReferenceNumber,
                        PlaceOfSupply = TruncatePlaceOfSupply(voucher.PlaceOfSupply),
                        EInvoiceIrn = voucher.EInvoiceIrn,
                        EwayBillNumber = voucher.EWayBillNumber,
                        TallyVoucherGuid = voucher.Guid,
                        TallyVoucherNumber = voucher.VoucherNumber,
                        TallyVoucherType = voucher.VoucherType,
                        TallyMigrationBatchId = batchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _invoicesRepository.AddAsync(invoice);
                    voucher.TargetId = invoice.Id;
                    voucher.TargetEntity = "invoices";

                    // Create journal entry if required
                    if (true) // Always create GL entries for proper accounting
                    {
                        var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, invoice.Id, "invoices");
                        voucher.JournalEntryId = journalId;
                    }

                    // Handle cost center allocations
                    await CreateTransactionTags(companyId, voucher, "invoices", invoice.Id);

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, invoice.Id, processingOrder, "invoices");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import sales voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportPurchaseVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _vendorInvoiceRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogVoucherMigration(batchId, voucher, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve vendor
                    Guid vendorId;
                    if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
                    {
                        var vendor = await _vendorsRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                        if (vendor == null && !string.IsNullOrEmpty(voucher.PartyLedgerGuid))
                        {
                            vendor = await _vendorsRepository.GetByTallyGuidAsync(companyId, voucher.PartyLedgerGuid);
                        }
                        vendorId = vendor?.Id ?? await GetOrCreateUnknownVendorAsync(companyId, voucher.PartyLedgerName);
                    }
                    else
                    {
                        vendorId = await GetOrCreateUnknownVendorAsync(companyId, "Unknown");
                    }

                    var taxAmount = voucher.LedgerEntries
                        .Sum(e => (e.CgstAmount ?? 0) + (e.SgstAmount ?? 0) + (e.IgstAmount ?? 0) + (e.CessAmount ?? 0));
                    var tdsAmount = voucher.LedgerEntries.Sum(e => e.TdsAmount ?? 0);
                    var subtotal = voucher.Amount - taxAmount;

                    var vendorInvoice = new VendorInvoice
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        PartyId = vendorId,
                        InvoiceNumber = voucher.VoucherNumber,
                        InvoiceDate = voucher.Date,
                        DueDate = CalculateDueDate(voucher),
                        Subtotal = subtotal,
                        TaxAmount = taxAmount,
                        TdsAmount = tdsAmount,
                        TotalAmount = voucher.Amount,
                        Status = voucher.IsCancelled ? "cancelled" : "approved", // Valid: draft, pending_approval, approved, partially_paid, paid, cancelled
                        Notes = voucher.Narration,
                        InternalReference = voucher.ReferenceNumber,
                        PlaceOfSupply = TruncatePlaceOfSupply(voucher.PlaceOfSupply),
                        ReverseCharge = voucher.IsReverseCharge,
                        TallyVoucherGuid = voucher.Guid,
                        TallyVoucherNumber = voucher.VoucherNumber,
                        TallyMigrationBatchId = batchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _vendorInvoiceRepository.AddAsync(vendorInvoice);
                    voucher.TargetId = vendorInvoice.Id;
                    voucher.TargetEntity = "vendor_invoices";

                    // Create journal entry
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, vendorInvoice.Id, "vendor_invoices");
                    voucher.JournalEntryId = journalId;

                    // Handle cost center allocations
                    await CreateTransactionTags(companyId, voucher, "vendor_invoices", vendorInvoice.Id);

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, vendorInvoice.Id, processingOrder, "vendor_invoices");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import purchase voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportReceiptVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _paymentsRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogVoucherMigration(batchId, voucher, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve customer
                    Guid? customerId = null;
                    if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
                    {
                        var customer = await _customersRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                        customerId = customer?.Id;
                    }

                    // Determine payment method from ledger entries
                    var paymentMethod = DeterminePaymentMethod(voucher);

                    var payment = new Payments
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        PartyId = customerId,
                        PaymentDate = voucher.Date,
                        Amount = voucher.Amount,
                        PaymentMethod = paymentMethod,
                        ReferenceNumber = voucher.ReferenceNumber ?? voucher.VoucherNumber,
                        Notes = voucher.Narration,
                        TallyVoucherGuid = voucher.Guid,
                        TallyVoucherNumber = voucher.VoucherNumber,
                        TallyMigrationBatchId = batchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _paymentsRepository.AddAsync(payment);
                    voucher.TargetId = payment.Id;
                    voucher.TargetEntity = "payments";

                    // Handle bill allocations (Agst Ref for invoice matching)
                    await HandleBillAllocations(companyId, voucher, payment.Id, "receipt");

                    // Create journal entry
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, payment.Id, "payments");
                    voucher.JournalEntryId = journalId;

                    // Create bank transaction (Receipt = credit to bank)
                    await _bankTransactionMapper.CreateBankTransactionAsync(
                        batchId, companyId, voucher, "payments", payment.Id, cancellationToken);

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, payment.Id, processingOrder, "payments");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import receipt voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportPaymentVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;
            var otherPayments = new List<TallyVoucherDto>();

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Use classifier to determine payment type
                    var classification = await _paymentClassifier.ClassifyAsync(companyId, voucher, cancellationToken);
                    _logger.LogDebug("Payment {Number} classified as {Type}: {Reason}",
                        voucher.VoucherNumber, classification.Type, classification.ClassificationReason);

                    switch (classification.Type)
                    {
                        case TallyPaymentType.Contractor:
                            // Route to contractor payment mapper
                            var contractorResult = await _contractorPaymentMapper.MapAndSaveAsync(
                                batchId, companyId, voucher, classification, cancellationToken);

                            if (contractorResult.IsSuccess)
                            {
                                voucher.TargetId = contractorResult.Value!.Id;
                                voucher.TargetEntity = "contractor_payments";
                                counts.Imported++;
                                await LogVoucherMigration(batchId, voucher, "success", null, contractorResult.Value.Id, processingOrder, "contractor_payments");

                                // Create bank transaction (SOC: separate from business entity creation)
                                await _bankTransactionMapper.CreateBankTransactionAsync(
                                    batchId, companyId, voucher, "contractor_payments", contractorResult.Value.Id, cancellationToken);
                            }
                            else
                            {
                                counts.Failed++;
                                await LogVoucherMigration(batchId, voucher, "failed", contractorResult.Error?.Message, null, processingOrder);
                            }
                            break;

                        case TallyPaymentType.Vendor:
                            // Existing vendor payment flow
                            await ImportVendorPayment(batchId, companyId, voucher, classification, processingOrder, counts);
                            break;

                        case TallyPaymentType.Statutory:
                            // Route to statutory payment mapper
                            var statutoryResult = await _statutoryPaymentMapper.MapAndSaveAsync(
                                batchId, companyId, voucher, classification, cancellationToken);

                            if (statutoryResult.IsSuccess)
                            {
                                voucher.TargetId = statutoryResult.Value!.Id;
                                voucher.TargetEntity = "statutory_payments";
                                counts.Imported++;
                                await LogVoucherMigration(batchId, voucher, "success", null, statutoryResult.Value.Id, processingOrder, "statutory_payments");

                                // Create bank transaction (SOC: separate from business entity creation)
                                await _bankTransactionMapper.CreateBankTransactionAsync(
                                    batchId, companyId, voucher, "statutory_payments", statutoryResult.Value.Id, cancellationToken);
                            }
                            else
                            {
                                counts.Failed++;
                                await LogVoucherMigration(batchId, voucher, "failed", statutoryResult.Error?.Message, null, processingOrder);
                            }
                            break;

                        case TallyPaymentType.Salary:
                        case TallyPaymentType.LoanEmi:
                        case TallyPaymentType.BankCharge:
                        case TallyPaymentType.InternalTransfer:
                        case TallyPaymentType.Other:
                        default:
                            // Collect for journal entry import
                            otherPayments.Add(voucher);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import payment voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            // Import other payments as journal entries
            if (otherPayments.Any())
            {
                _logger.LogInformation("Importing {Count} non-vendor/contractor payment vouchers as journal entries", otherPayments.Count);
                var journalResult = await ImportJournalVouchersAsync(batchId, companyId, otherPayments, cancellationToken);
                if (journalResult.IsSuccess)
                {
                    counts.Skipped += journalResult.Value!.Imported;
                    _logger.LogInformation("Imported {Count} other payments as journal entries", journalResult.Value.Imported);

                    // Create bank transactions for other payments (they're still outgoing payments)
                    // SOC: Bank transaction creation is separate from journal entry creation
                    foreach (var otherPayment in otherPayments.Where(v => v.JournalEntryId.HasValue))
                    {
                        await _bankTransactionMapper.CreateBankTransactionAsync(
                            batchId, companyId, otherPayment, "journal_entries", otherPayment.JournalEntryId, cancellationToken);
                    }
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        /// <summary>
        /// Import a vendor payment using the existing flow
        /// </summary>
        private async Task ImportVendorPayment(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            int processingOrder,
            TallyImportCountsDto counts)
        {
            // Check if already exists
            var existing = await _vendorPaymentRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
            if (existing != null)
            {
                counts.Skipped++;
                await LogVoucherMigration(batchId, voucher, "skipped", "Already exists", existing.Id, processingOrder);
                return;
            }

            // Resolve vendor - try multiple approaches
            Vendors? vendor = null;

            // 1. Use classification result if party was resolved
            if (classification.PartyId.HasValue)
            {
                vendor = await _vendorsRepository.GetByIdAsync(classification.PartyId.Value);
            }

            // 2. Try PartyLedgerName first (from PARTYLEDGERNAME element in Tally XML)
            if (vendor == null && !string.IsNullOrEmpty(voucher.PartyLedgerName))
            {
                vendor = await _vendorsRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                if (vendor == null && !string.IsNullOrEmpty(voucher.PartyLedgerGuid))
                {
                    vendor = await _vendorsRepository.GetByTallyGuidAsync(companyId, voucher.PartyLedgerGuid);
                }
            }

            // 3. Try target ledger name from classification
            if (vendor == null && !string.IsNullOrEmpty(classification.TargetLedgerName))
            {
                vendor = await _vendorsRepository.GetByNameAsync(companyId, classification.TargetLedgerName);
            }

            // 4. If still no vendor found, log and skip
            if (vendor == null)
            {
                counts.Failed++;
                await LogVoucherMigration(batchId, voucher, "failed",
                    $"Could not resolve vendor for payment: {classification.TargetLedgerName}", null, processingOrder);
                return;
            }

            Guid vendorId = vendor.Id;

            var paymentMethod = DeterminePaymentMethod(voucher);
            var tdsAmount = voucher.LedgerEntries.Sum(e => e.TdsAmount ?? 0);

            var vendorPayment = new VendorPayment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                PartyId = vendorId,
                PaymentDate = voucher.Date,
                Amount = voucher.Amount,
                TdsAmount = tdsAmount,
                PaymentMethod = paymentMethod,
                ReferenceNumber = voucher.ReferenceNumber ?? voucher.VoucherNumber,
                Notes = voucher.Narration,
                Status = voucher.IsCancelled ? "cancelled" : "processed",
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _vendorPaymentRepository.AddAsync(vendorPayment);
            voucher.TargetId = vendorPayment.Id;
            voucher.TargetEntity = "vendor_payments";

            // Handle bill allocations
            await HandleBillAllocations(companyId, voucher, vendorPayment.Id, "payment");

            // Create journal entry
            var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, vendorPayment.Id, "vendor_payments");
            voucher.JournalEntryId = journalId;

            // Create bank transaction (SOC: separate from business entity creation)
            await _bankTransactionMapper.CreateBankTransactionAsync(
                batchId, companyId, voucher, "vendor_payments", vendorPayment.Id);

            counts.Imported++;
            await LogVoucherMigration(batchId, voucher, "success", null, vendorPayment.Id, processingOrder, "vendor_payments");
        }

        public async Task<Result<TallyImportCountsDto>> ImportJournalVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _journalRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogVoucherMigration(batchId, voucher, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Create journal entry directly
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, null, "journal");
                    voucher.JournalEntryId = journalId;
                    voucher.TargetId = journalId;
                    voucher.TargetEntity = "journal_entries";

                    // Handle cost center allocations
                    await CreateTransactionTags(companyId, voucher, "journal_entries", journalId!.Value);

                    // Create bank transactions for bank-affecting entries
                    // Contra vouchers create TWO transactions (debit + credit)
                    await CreateBankTransactionsForJournalAsync(
                        batchId, companyId, voucher, journalId.Value, cancellationToken);

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, journalId, processingOrder, "journal_entries");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import journal voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportStockVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // For now, create stock movements as journal entries with stock impact
                    // A more complete implementation would create actual stock movements
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, null, "stock_journal");
                    voucher.JournalEntryId = journalId;
                    voucher.TargetId = journalId;
                    voucher.TargetEntity = "stock_movements";

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, journalId, processingOrder, "stock_movements");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import stock voucher {Number}", voucher.VoucherNumber);
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        private async Task<Result<TallyImportCountsDto>> ImportCreditNotesAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken)
        {
            // Credit notes are imported as invoices with type = 'credit_note'
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    var existing = await _invoicesRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        continue;
                    }

                    Guid? customerId = null;
                    if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
                    {
                        var customer = await _customersRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                        customerId = customer?.Id;
                    }

                    var invoice = new Invoices
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        PartyId = customerId,
                        InvoiceNumber = voucher.VoucherNumber,
                        InvoiceDate = voucher.Date,
                        DueDate = voucher.Date,
                        InvoiceType = "credit_note",
                        TotalAmount = -voucher.Amount, // Negative for credit note
                        Status = "paid", // Valid: draft, sent, viewed, partially_paid, paid, overdue, cancelled
                        Notes = voucher.Narration,
                        TallyVoucherGuid = voucher.Guid,
                        TallyVoucherNumber = voucher.VoucherNumber,
                        TallyVoucherType = "Credit Note",
                        TallyMigrationBatchId = batchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _invoicesRepository.AddAsync(invoice);
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, invoice.Id, "invoices");

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, invoice.Id, processingOrder, "invoices");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        private async Task<Result<TallyImportCountsDto>> ImportDebitNotesAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken)
        {
            // Debit notes are imported as vendor invoices with type = 'debit_note'
            var counts = new TallyImportCountsDto { Total = vouchers.Count };
            var processingOrder = 0;

            foreach (var voucher in vouchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    var existing = await _vendorInvoiceRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        continue;
                    }

                    Guid vendorId;
                    if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
                    {
                        var vendor = await _vendorsRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                        if (vendor == null && !string.IsNullOrEmpty(voucher.PartyLedgerGuid))
                        {
                            vendor = await _vendorsRepository.GetByTallyGuidAsync(companyId, voucher.PartyLedgerGuid);
                        }
                        vendorId = vendor?.Id ?? await GetOrCreateUnknownVendorAsync(companyId, voucher.PartyLedgerName);
                    }
                    else
                    {
                        vendorId = await GetOrCreateUnknownVendorAsync(companyId, "Unknown");
                    }

                    var vendorInvoice = new VendorInvoice
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        PartyId = vendorId,
                        InvoiceNumber = voucher.VoucherNumber,
                        InvoiceDate = voucher.Date,
                        DueDate = voucher.Date,
                        InvoiceType = "debit_note",
                        TotalAmount = -voucher.Amount, // Negative for debit note
                        Status = "approved", // Valid: draft, pending_approval, approved, partially_paid, paid, cancelled
                        Notes = voucher.Narration,
                        TallyVoucherGuid = voucher.Guid,
                        TallyVoucherNumber = voucher.VoucherNumber,
                        TallyMigrationBatchId = batchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _vendorInvoiceRepository.AddAsync(vendorInvoice);
                    var journalId = await CreateJournalEntryFromVoucher(batchId, companyId, voucher, vendorInvoice.Id, "vendor_invoices");

                    counts.Imported++;
                    await LogVoucherMigration(batchId, voucher, "success", null, vendorInvoice.Id, processingOrder, "vendor_invoices");
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        private async Task<Result<TallyImportCountsDto>> ImportAsJournalEntryAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken)
        {
            // Fallback: import unknown voucher types as journal entries
            return await ImportJournalVouchersAsync(batchId, companyId, vouchers, cancellationToken);
        }

        private async Task<Guid?> CreateJournalEntryFromVoucher(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            Guid? sourceTransactionId,
            string sourceType)
        {
            var journalEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                JournalNumber = $"JE-{voucher.VoucherNumber}",
                JournalDate = voucher.Date,
                PeriodMonth = voucher.Date.Month, // Required: must be 1-12
                Description = voucher.Narration ?? $"Imported from Tally: {voucher.VoucherType} {voucher.VoucherNumber}",
                SourceNumber = voucher.ReferenceNumber,
                SourceType = sourceType,
                SourceId = sourceTransactionId,
                Status = voucher.IsCancelled ? "cancelled" : "posted",
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyVoucherType = voucher.VoucherType,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create journal entry lines from ledger entries
            var lines = new List<JournalEntryLine>();
            var lineNumber = 0;

            foreach (var entry in voucher.LedgerEntries)
            {
                lineNumber++;

                // Resolve account
                var account = await _coaRepository.GetByNameAsync(companyId, entry.LedgerName);
                if (account == null && !string.IsNullOrEmpty(entry.LedgerGuid))
                {
                    account = await _coaRepository.GetByTallyGuidAsync(companyId, entry.LedgerGuid);
                }

                // If account not found, use suspense account
                Guid accountId;
                if (account != null)
                {
                    accountId = account.Id;
                }
                else
                {
                    // Get or create suspense account for unmapped ledgers
                    accountId = await GetOrCreateSuspenseAccountAsync(companyId, entry.LedgerName);
                    _logger.LogWarning("Using suspense account for unmapped ledger: {LedgerName}", entry.LedgerName);
                }

                var line = new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = journalEntry.Id,
                    LineNumber = lineNumber,
                    AccountId = accountId,
                    Description = entry.LedgerName,
                    DebitAmount = entry.Amount > 0 ? entry.Amount : 0,
                    CreditAmount = entry.Amount < 0 ? Math.Abs(entry.Amount) : 0
                };

                lines.Add(line);
            }

            journalEntry.TotalDebit = lines.Sum(l => l.DebitAmount);
            journalEntry.TotalCredit = lines.Sum(l => l.CreditAmount);

            await _journalRepository.AddAsync(journalEntry);
            await _journalRepository.AddLinesAsync(journalEntry.Id, lines);

            return journalEntry.Id;
        }

        private async Task CreateTransactionTags(Guid companyId, TallyVoucherDto voucher, string entityType, Guid entityId)
        {
            var allCostAllocations = voucher.CostAllocations.ToList();

            // Also gather from ledger entries
            foreach (var entry in voucher.LedgerEntries)
            {
                allCostAllocations.AddRange(entry.CostAllocations);
            }

            foreach (var allocation in allCostAllocations.Where(a => !string.IsNullOrEmpty(a.CostCenterName)))
            {
                try
                {
                    // Find the tag (cost center tags have no parent, so parentTagId is null)
                    var tag = await _tagRepository.GetByNameAsync(companyId, allocation.CostCenterName, null);
                    if (tag == null && !string.IsNullOrEmpty(allocation.CostCenterGuid))
                    {
                        tag = await _tagRepository.GetByTallyCostCenterGuidAsync(companyId, allocation.CostCenterGuid);
                    }

                    if (tag != null)
                    {
                        var transactionTag = new TransactionTag
                        {
                            Id = Guid.NewGuid(),
                            TagId = tag.Id,
                            TransactionType = entityType,
                            TransactionId = entityId,
                            AllocatedAmount = allocation.Amount,
                            AllocationMethod = "amount",
                            Source = "imported",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _transactionTagRepository.AddAsync(transactionTag);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create transaction tag for cost center {Name}", allocation.CostCenterName);
                }
            }
        }

        private async Task HandleBillAllocations(Guid companyId, TallyVoucherDto voucher, Guid paymentId, string type)
        {
            var allBillAllocations = voucher.BillAllocations.ToList();

            foreach (var entry in voucher.LedgerEntries)
            {
                allBillAllocations.AddRange(entry.BillAllocations);
            }

            foreach (var allocation in allBillAllocations.Where(a => a.BillType == "Agst Ref" || a.BillType == "Against Reference"))
            {
                try
                {
                    // Find the referenced invoice by voucher number
                    if (type == "receipt")
                    {
                        var invoice = await _invoicesRepository.GetByNumberAsync(companyId, allocation.Name);
                        if (invoice != null)
                        {
                            // Create payment allocation
                            // This would need a PaymentAllocation entity/repository
                            _logger.LogDebug("Would allocate payment {PaymentId} to invoice {InvoiceId} for amount {Amount}",
                                paymentId, invoice.Id, allocation.Amount);
                        }
                    }
                    else if (type == "payment")
                    {
                        var vendorInvoice = await _vendorInvoiceRepository.GetByNumberAsync(companyId, allocation.Name);
                        if (vendorInvoice != null)
                        {
                            // Create payment allocation
                            _logger.LogDebug("Would allocate vendor payment {PaymentId} to invoice {InvoiceId} for amount {Amount}",
                                paymentId, vendorInvoice.Id, allocation.Amount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle bill allocation for {BillName}", allocation.Name);
                }
            }
        }

        private static string NormalizeVoucherType(string voucherType)
        {
            var normalized = voucherType.ToLower().Trim();
            return normalized switch
            {
                "sales" => "sales",
                "purchase" => "purchase",
                "receipt" => "receipt",
                "payment" => "payment",
                "journal" => "journal",
                "contra" => "contra",
                "credit note" => "credit_note",
                "debit note" => "debit_note",
                "stock journal" => "stock_journal",
                "physical stock" => "physical_stock",
                "delivery note" => "delivery_note",
                "receipt note" => "receipt_note",
                "sales order" => "sales_order",
                "purchase order" => "purchase_order",
                "memorandum" => "memorandum",
                _ => normalized.Replace(" ", "_")
            };
        }

        /// <summary>
        /// Creates bank transactions for Journal/Contra vouchers that affect bank accounts.
        /// For Contra vouchers: creates TWO transactions (debit from source bank, credit to destination bank).
        /// For Journal vouchers: creates transaction for any bank-affecting entry.
        /// </summary>
        private async Task CreateBankTransactionsForJournalAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            Guid journalId,
            CancellationToken cancellationToken)
        {
            // Find all bank-affecting ledger entries
            var bankEntries = voucher.LedgerEntries
                .Where(e => IsBankLedger(e.LedgerName))
                .ToList();

            if (!bankEntries.Any())
            {
                _logger.LogDebug("No bank entries in {VoucherType} voucher {Number}",
                    voucher.VoucherType, voucher.VoucherNumber);
                return;
            }

            var isContra = voucher.VoucherType?.ToLower() == "contra";

            if (isContra && bankEntries.Count >= 2)
            {
                // Contra voucher: create TWO bank transactions
                // Debit entry (positive amount) = source bank (money going OUT)
                var debitEntry = bankEntries.FirstOrDefault(e => e.Amount > 0);
                if (debitEntry != null)
                {
                    await _bankTransactionMapper.CreateBankTransactionFromLedgerEntryAsync(
                        batchId, companyId, voucher, debitEntry,
                        "journal_entries", journalId, "debit", cancellationToken);
                }

                // Credit entry (negative amount) = destination bank (money coming IN)
                var creditEntry = bankEntries.FirstOrDefault(e => e.Amount < 0);
                if (creditEntry != null)
                {
                    await _bankTransactionMapper.CreateBankTransactionFromLedgerEntryAsync(
                        batchId, companyId, voucher, creditEntry,
                        "journal_entries", journalId, "credit", cancellationToken);
                }

                _logger.LogDebug("Created contra bank transactions for voucher {Number}: debit {DebitBank}, credit {CreditBank}",
                    voucher.VoucherNumber, debitEntry?.LedgerName, creditEntry?.LedgerName);
            }
            else
            {
                // Regular journal: create bank transaction for each bank entry
                foreach (var entry in bankEntries)
                {
                    // Positive amount = debit (money out), Negative = credit (money in)
                    var transactionType = entry.Amount > 0 ? "debit" : "credit";

                    await _bankTransactionMapper.CreateBankTransactionFromLedgerEntryAsync(
                        batchId, companyId, voucher, entry,
                        "journal_entries", journalId, transactionType, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Determines if a ledger name is likely a bank account.
        /// </summary>
        private static bool IsBankLedger(string? ledgerName)
        {
            if (string.IsNullOrEmpty(ledgerName)) return false;

            var name = ledgerName.ToLower();
            return name.Contains("bank") ||
                   name.Contains("axis") ||
                   name.Contains("hdfc") ||
                   name.Contains("icici") ||
                   name.Contains("sbi") ||
                   name.Contains("kotak") ||
                   name.Contains("yes bank") ||
                   name.Contains("idfc") ||
                   name.Contains("canara") ||
                   name.Contains("union") ||
                   name.Contains("pnb") ||
                   name.Contains("bob") ||
                   name.Contains("indusind");
        }

        private static string DeterminePaymentMethod(TallyVoucherDto voucher)
        {
            // Look at ledger entries to determine if cash or bank
            foreach (var entry in voucher.LedgerEntries)
            {
                var name = entry.LedgerName.ToLower();
                if (name.Contains("cash"))
                    return "cash";
                if (name.Contains("bank") || name.Contains("hdfc") || name.Contains("icici") ||
                    name.Contains("sbi") || name.Contains("axis"))
                    return "bank_transfer";
                if (name.Contains("cheque"))
                    return "cheque";
                if (name.Contains("upi") || name.Contains("gpay") || name.Contains("phonepe"))
                    return "upi";
            }
            return "bank_transfer";
        }

        private static DateOnly CalculateDueDate(TallyVoucherDto voucher)
        {
            var invoiceDate = voucher.Date;

            // Check bill allocations for due date
            var newRefAllocation = voucher.BillAllocations.FirstOrDefault(b => b.BillType == "New Ref");
            if (newRefAllocation?.DueDate != null)
            {
                return newRefAllocation.DueDate.Value;
            }

            // Also check ledger entry bill allocations
            foreach (var entry in voucher.LedgerEntries)
            {
                var allocation = entry.BillAllocations.FirstOrDefault(b => b.BillType == "New Ref" && b.DueDate != null);
                if (allocation?.DueDate != null)
                {
                    return allocation.DueDate.Value;
                }

                // Try to parse credit period
                var creditPeriod = entry.BillAllocations.FirstOrDefault()?.BillCreditPeriod;
                if (!string.IsNullOrEmpty(creditPeriod))
                {
                    if (int.TryParse(creditPeriod.Replace(" Days", "").Replace(" days", "").Trim(), out var days))
                    {
                        return invoiceDate.AddDays(days);
                    }
                }
            }

            // Default: 30 days
            return invoiceDate.AddDays(30);
        }

        private async Task LogVoucherMigration(
            Guid batchId,
            TallyVoucherDto voucher,
            string status,
            string? message,
            Guid? targetId,
            int processingOrder,
            string? targetEntity = null)
        {
            var log = new TallyMigrationLog
            {
                BatchId = batchId,
                RecordType = MapVoucherTypeToRecordType(voucher.VoucherType),
                TallyGuid = voucher.Guid,
                TallyName = $"{voucher.VoucherType}/{voucher.VoucherNumber}",
                TallyDate = voucher.Date,
                TallyAmount = voucher.Amount,
                Status = status,
                ErrorMessage = message,
                TargetId = targetId,
                TargetEntity = targetEntity ?? "voucher",
                ProcessingOrder = processingOrder
            };

            await _logRepository.AddAsync(log);
        }

        private static string MapVoucherTypeToRecordType(string voucherType)
        {
            return voucherType?.ToLower() switch
            {
                "sales" => "voucher_sales",
                "purchase" => "voucher_purchase",
                "receipt" => "voucher_receipt",
                "payment" => "voucher_payment",
                "journal" => "voucher_journal",
                "contra" => "voucher_contra",
                "credit note" => "voucher_credit_note",
                "debit note" => "voucher_debit_note",
                "stock journal" => "voucher_stock_journal",
                "physical stock" => "voucher_physical_stock",
                _ => "voucher_journal" // Default fallback
            };
        }

        /// <summary>
        /// Gets or creates an unknown vendor placeholder for unmapped vendors during import
        /// </summary>
        private async Task<Guid> GetOrCreateUnknownVendorAsync(Guid companyId, string originalVendorName)
        {
            // Try to find existing unknown vendor
            var unknownVendor = await _vendorsRepository.GetByNameAsync(companyId, "UNKNOWN-TALLY-VENDOR");
            if (unknownVendor != null)
            {
                return unknownVendor.Id;
            }

            // Create unknown vendor if it doesn't exist
            var newVendor = new Core.Entities.Vendors
            {
                CompanyId = companyId,
                Name = "UNKNOWN-TALLY-VENDOR",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdVendor = await _vendorsRepository.AddAsync(newVendor);
            _logger.LogWarning("Created unknown vendor placeholder for unmapped vendor: {OriginalName}", originalVendorName);
            return createdVendor.Id;
        }

        /// <summary>
        /// Gets or creates a suspense account for unmapped ledgers during import
        /// </summary>
        private async Task<Guid> GetOrCreateSuspenseAccountAsync(Guid companyId, string ledgerName)
        {
            // Try to find existing suspense account
            var suspenseAccount = await _coaRepository.GetByCodeAsync(companyId, "SUSPENSE-IMPORT");
            if (suspenseAccount != null)
            {
                return suspenseAccount.Id;
            }

            // Create suspense account if it doesn't exist
            var newSuspense = new Core.Entities.Ledger.ChartOfAccount
            {
                CompanyId = companyId,
                AccountCode = "SUSPENSE-IMPORT",
                AccountName = "Tally Import Suspense Account",
                AccountType = "liability",
                AccountSubtype = "current_liability",
                IsActive = true,
                IsSystemAccount = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdAccount = await _coaRepository.AddAsync(newSuspense);
            _logger.LogInformation("Created suspense account for Tally import: {AccountCode}", createdAccount.AccountCode);
            return createdAccount.Id;
        }

        /// <summary>
        /// Truncates PlaceOfSupply to fit varchar(5) - converts state names to state codes
        /// </summary>
        private static string? TruncatePlaceOfSupply(string? placeOfSupply)
        {
            if (string.IsNullOrEmpty(placeOfSupply))
                return null;

            // If it's already a short code (2-5 chars), return as-is
            if (placeOfSupply.Length <= 5)
                return placeOfSupply;

            // Map common Indian state names to GST state codes
            var stateCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Andhra Pradesh", "37" },
                { "Arunachal Pradesh", "12" },
                { "Assam", "18" },
                { "Bihar", "10" },
                { "Chhattisgarh", "22" },
                { "Goa", "30" },
                { "Gujarat", "24" },
                { "Haryana", "06" },
                { "Himachal Pradesh", "02" },
                { "Jharkhand", "20" },
                { "Karnataka", "29" },
                { "Kerala", "32" },
                { "Madhya Pradesh", "23" },
                { "Maharashtra", "27" },
                { "Manipur", "14" },
                { "Meghalaya", "17" },
                { "Mizoram", "15" },
                { "Nagaland", "13" },
                { "Odisha", "21" },
                { "Punjab", "03" },
                { "Rajasthan", "08" },
                { "Sikkim", "11" },
                { "Tamil Nadu", "33" },
                { "Telangana", "36" },
                { "Tripura", "16" },
                { "Uttar Pradesh", "09" },
                { "Uttarakhand", "05" },
                { "West Bengal", "19" },
                { "Delhi", "07" },
                { "Jammu and Kashmir", "01" },
                { "Ladakh", "38" },
                { "Puducherry", "34" },
                { "Chandigarh", "04" },
                { "Andaman and Nicobar Islands", "35" },
                { "Dadra and Nagar Haveli and Daman and Diu", "26" },
                { "Lakshadweep", "31" }
            };

            // Try to find a matching state code
            foreach (var kvp in stateCodeMap)
            {
                if (placeOfSupply.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            // Fallback: truncate to first 5 chars
            return placeOfSupply.Substring(0, 5);
        }
    }
}
