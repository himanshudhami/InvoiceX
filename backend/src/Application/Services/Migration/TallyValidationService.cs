using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Inventory;
using Core.Interfaces.Ledger;
using Core.Interfaces.Migration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Service for validating Tally data before import
    /// </summary>
    public class TallyValidationService : ITallyValidationService
    {
        private readonly ILogger<TallyValidationService> _logger;
        private readonly ICustomersRepository _customersRepository;
        private readonly IVendorsRepository _vendorsRepository;
        private readonly IChartOfAccountRepository _coaRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IJournalEntryRepository _journalRepository;

        public TallyValidationService(
            ILogger<TallyValidationService> logger,
            ICustomersRepository customersRepository,
            IVendorsRepository vendorsRepository,
            IChartOfAccountRepository coaRepository,
            IStockItemRepository stockItemRepository,
            IJournalEntryRepository journalRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _vendorsRepository = vendorsRepository ?? throw new ArgumentNullException(nameof(vendorsRepository));
            _coaRepository = coaRepository ?? throw new ArgumentNullException(nameof(coaRepository));
            _stockItemRepository = stockItemRepository ?? throw new ArgumentNullException(nameof(stockItemRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
        }

        public async Task<Result<TallyValidationResultDto>> ValidateAsync(
            Guid companyId,
            TallyParsedDataDto parsedData,
            CancellationToken cancellationToken = default)
        {
            var result = new TallyValidationResultDto
            {
                IsValid = true,
                CanProceed = true,
                Issues = new List<TallyValidationIssueDto>()
            };

            try
            {
                // Add issues from parsing
                result.Issues.AddRange(parsedData.ValidationIssues);

                // Validate ledgers
                ValidateLedgers(parsedData.Masters.Ledgers, result);

                // Validate stock items
                ValidateStockItems(parsedData.Masters.StockItems, result);

                // Validate vouchers
                ValidateVouchers(parsedData.Vouchers.Vouchers, result);

                // Check for duplicates
                var duplicateResult = await CheckDuplicatesAsync(companyId, parsedData, cancellationToken);
                if (duplicateResult.IsSuccess)
                {
                    var duplicates = duplicateResult.Value!;
                    if (duplicates.DuplicateLedgers > 0)
                    {
                        result.Issues.Add(new TallyValidationIssueDto
                        {
                            Severity = "info",
                            Code = "DUPLICATE_LEDGERS",
                            Message = $"{duplicates.DuplicateLedgers} ledger(s) already exist and will be skipped"
                        });
                    }
                    if (duplicates.DuplicateStockItems > 0)
                    {
                        result.Issues.Add(new TallyValidationIssueDto
                        {
                            Severity = "info",
                            Code = "DUPLICATE_STOCK_ITEMS",
                            Message = $"{duplicates.DuplicateStockItems} stock item(s) already exist and will be skipped"
                        });
                    }
                    if (duplicates.DuplicateVouchers > 0)
                    {
                        result.Issues.Add(new TallyValidationIssueDto
                        {
                            Severity = "info",
                            Code = "DUPLICATE_VOUCHERS",
                            Message = $"{duplicates.DuplicateVouchers} voucher(s) already exist and will be skipped"
                        });
                    }
                }

                // Count severity levels
                result.ErrorCount = result.Issues.Count(i => i.Severity == "error");
                result.WarningCount = result.Issues.Count(i => i.Severity == "warning");
                result.InfoCount = result.Issues.Count(i => i.Severity == "info");

                // Determine if valid and can proceed
                result.IsValid = result.ErrorCount == 0;
                result.CanProceed = result.ErrorCount == 0; // Can still proceed with warnings

                _logger.LogInformation(
                    "Validation completed: {Errors} errors, {Warnings} warnings, {Info} info",
                    result.ErrorCount, result.WarningCount, result.InfoCount);

                return Result<TallyValidationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for company {CompanyId}", companyId);
                return Error.Internal($"Validation failed: {ex.Message}");
            }
        }

        public async Task<Result<TallyDuplicateCheckResultDto>> CheckDuplicatesAsync(
            Guid companyId,
            TallyParsedDataDto parsedData,
            CancellationToken cancellationToken = default)
        {
            var result = new TallyDuplicateCheckResultDto
            {
                Duplicates = new List<TallyDuplicateItemDto>()
            };

            try
            {
                // Check ledger duplicates
                foreach (var ledger in parsedData.Masters.Ledgers.Where(l => !string.IsNullOrEmpty(l.Guid)))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check in customers
                    var existingCustomer = await _customersRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
                    if (existingCustomer != null)
                    {
                        result.DuplicateLedgers++;
                        result.Duplicates.Add(new TallyDuplicateItemDto
                        {
                            RecordType = "ledger",
                            TallyGuid = ledger.Guid,
                            TallyName = ledger.Name,
                            ExistingId = existingCustomer.Id,
                            ExistingName = existingCustomer.Name
                        });
                        continue;
                    }

                    // Check in vendors
                    var existingVendor = await _vendorsRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
                    if (existingVendor != null)
                    {
                        result.DuplicateLedgers++;
                        result.Duplicates.Add(new TallyDuplicateItemDto
                        {
                            RecordType = "ledger",
                            TallyGuid = ledger.Guid,
                            TallyName = ledger.Name,
                            ExistingId = existingVendor.Id,
                            ExistingName = existingVendor.Name
                        });
                        continue;
                    }

                    // Check in COA
                    var existingAccount = await _coaRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
                    if (existingAccount != null)
                    {
                        result.DuplicateLedgers++;
                        result.Duplicates.Add(new TallyDuplicateItemDto
                        {
                            RecordType = "ledger",
                            TallyGuid = ledger.Guid,
                            TallyName = ledger.Name,
                            ExistingId = existingAccount.Id,
                            ExistingName = existingAccount.AccountName
                        });
                    }
                }

                // Check stock item duplicates
                foreach (var item in parsedData.Masters.StockItems.Where(i => !string.IsNullOrEmpty(i.Guid)))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var existingItem = await _stockItemRepository.GetByTallyGuidAsync(companyId, item.Guid);
                    if (existingItem != null)
                    {
                        result.DuplicateStockItems++;
                        result.Duplicates.Add(new TallyDuplicateItemDto
                        {
                            RecordType = "stock_item",
                            TallyGuid = item.Guid,
                            TallyName = item.Name,
                            ExistingId = existingItem.Id,
                            ExistingName = existingItem.Name
                        });
                    }
                }

                // Check voucher duplicates
                foreach (var voucher in parsedData.Vouchers.Vouchers.Where(v => !string.IsNullOrEmpty(v.Guid)))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var existingJournal = await _journalRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
                    if (existingJournal != null)
                    {
                        result.DuplicateVouchers++;
                        result.Duplicates.Add(new TallyDuplicateItemDto
                        {
                            RecordType = "voucher",
                            TallyGuid = voucher.Guid,
                            TallyName = $"{voucher.VoucherType}/{voucher.VoucherNumber}",
                            ExistingId = existingJournal.Id,
                            ExistingName = existingJournal.JournalNumber
                        });
                    }
                }

                return Result<TallyDuplicateCheckResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duplicate check failed for company {CompanyId}", companyId);
                return Error.Internal($"Duplicate check failed: {ex.Message}");
            }
        }

        private void ValidateLedgers(List<TallyLedgerDto> ledgers, TallyValidationResultDto result)
        {
            var ledgerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ledger in ledgers)
            {
                // Check for duplicate names within the file
                if (!ledgerNames.Add(ledger.Name))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "DUPLICATE_NAME",
                        Message = $"Duplicate ledger name: {ledger.Name}",
                        RecordType = "ledger",
                        RecordName = ledger.Name,
                        RecordGuid = ledger.Guid
                    });
                }

                // Validate GSTIN format if present
                if (!string.IsNullOrEmpty(ledger.Gstin) && !IsValidGstin(ledger.Gstin))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "INVALID_GSTIN",
                        Message = $"Invalid GSTIN format: {ledger.Gstin}",
                        RecordType = "ledger",
                        RecordName = ledger.Name,
                        Field = "Gstin",
                        ActualValue = ledger.Gstin
                    });
                }

                // Validate PAN format if present
                if (!string.IsNullOrEmpty(ledger.PanNumber) && !IsValidPan(ledger.PanNumber))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "INVALID_PAN",
                        Message = $"Invalid PAN format: {ledger.PanNumber}",
                        RecordType = "ledger",
                        RecordName = ledger.Name,
                        Field = "PanNumber",
                        ActualValue = ledger.PanNumber
                    });
                }

                // Check for missing group
                if (string.IsNullOrEmpty(ledger.LedgerGroup))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "MISSING_GROUP",
                        Message = $"Ledger has no group: {ledger.Name}",
                        RecordType = "ledger",
                        RecordName = ledger.Name
                    });
                }
            }
        }

        private void ValidateStockItems(List<TallyStockItemDto> items, TallyValidationResultDto result)
        {
            foreach (var item in items)
            {
                // Validate HSN code format if present
                if (!string.IsNullOrEmpty(item.HsnCode) && !IsValidHsnCode(item.HsnCode))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "INVALID_HSN",
                        Message = $"Invalid HSN code format: {item.HsnCode}",
                        RecordType = "stock_item",
                        RecordName = item.Name,
                        Field = "HsnCode",
                        ActualValue = item.HsnCode
                    });
                }

                // Check for negative opening quantity
                if (item.OpeningQuantity < 0)
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "NEGATIVE_OPENING_QTY",
                        Message = $"Negative opening quantity: {item.OpeningQuantity}",
                        RecordType = "stock_item",
                        RecordName = item.Name,
                        Field = "OpeningQuantity"
                    });
                }

                // Check for missing unit
                if (string.IsNullOrEmpty(item.BaseUnits))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "info",
                        Code = "MISSING_UNIT",
                        Message = $"Stock item has no unit: {item.Name}",
                        RecordType = "stock_item",
                        RecordName = item.Name
                    });
                }
            }
        }

        private void ValidateVouchers(List<TallyVoucherDto> vouchers, TallyValidationResultDto result)
        {
            foreach (var voucher in vouchers)
            {
                // Check for balanced entries
                var totalAmount = voucher.LedgerEntries.Sum(e => e.Amount);
                if (Math.Abs(totalAmount) > 0.01m)
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "error",
                        Code = "UNBALANCED_VOUCHER",
                        Message = $"Voucher is unbalanced by {totalAmount:N2}",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber,
                        RecordGuid = voucher.Guid
                    });
                }

                // Check for future-dated vouchers
                if (voucher.Date > DateOnly.FromDateTime(DateTime.Today))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "FUTURE_DATED",
                        Message = $"Voucher is dated in the future: {voucher.Date}",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber
                    });
                }

                // Check for very old vouchers (more than 10 years)
                if (voucher.Date < DateOnly.FromDateTime(DateTime.Today.AddYears(-10)))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "VERY_OLD_VOUCHER",
                        Message = $"Voucher is more than 10 years old: {voucher.Date}",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber
                    });
                }

                // Check for missing party in sales/purchase vouchers
                var voucherType = voucher.VoucherType?.ToLower();
                if ((voucherType == "sales" || voucherType == "purchase") && string.IsNullOrEmpty(voucher.PartyLedgerName))
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "MISSING_PARTY",
                        Message = $"{voucher.VoucherType} voucher has no party ledger",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber
                    });
                }

                // Check for empty ledger entries
                if (voucher.LedgerEntries.Count == 0)
                {
                    result.Issues.Add(new TallyValidationIssueDto
                    {
                        Severity = "warning",
                        Code = "NO_LEDGER_ENTRIES",
                        Message = "Voucher has no ledger entries",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber
                    });
                }
            }
        }

        private static bool IsValidGstin(string gstin)
        {
            // GSTIN format: 2 digit state code + 10 digit PAN + 1 digit entity + Z + 1 check digit
            if (string.IsNullOrWhiteSpace(gstin) || gstin.Length != 15)
                return false;

            // Basic format check
            return System.Text.RegularExpressions.Regex.IsMatch(
                gstin.ToUpper(),
                @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$");
        }

        private static bool IsValidPan(string pan)
        {
            // PAN format: 5 letters + 4 digits + 1 letter
            if (string.IsNullOrWhiteSpace(pan) || pan.Length != 10)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                pan.ToUpper(),
                @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
        }

        private static bool IsValidHsnCode(string hsn)
        {
            // HSN codes are 4, 6, or 8 digits
            if (string.IsNullOrWhiteSpace(hsn))
                return false;

            var digits = new string(hsn.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 && digits.Length <= 8;
        }
    }
}
