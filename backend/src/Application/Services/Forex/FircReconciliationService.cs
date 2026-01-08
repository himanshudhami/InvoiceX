using Application.DTOs.Forex;
using Application.Interfaces.Forex;
using Core.Common;
using Core.Entities.Forex;
using Core.Interfaces;
using Core.Interfaces.Forex;

namespace Application.Services.Forex
{
    /// <summary>
    /// Service for FIRC (Foreign Inward Remittance Certificate) reconciliation
    /// Handles FEMA/RBI compliance for export receivables
    /// </summary>
    public class FircReconciliationService : IFircReconciliationService
    {
        private readonly IFircTrackingRepository _fircRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly ICustomersRepository _customersRepository;

        // FEMA requires export realization within 9 months (270 days)
        private const int FemaRealizationDays = 270;

        public FircReconciliationService(
            IFircTrackingRepository fircRepository,
            IPaymentsRepository paymentsRepository,
            IInvoicesRepository invoicesRepository,
            ICustomersRepository customersRepository)
        {
            _fircRepository = fircRepository ?? throw new ArgumentNullException(nameof(fircRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
        }

        // ==================== CRUD Operations ====================

        /// <inheritdoc />
        public async Task<Result<FircTracking>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Error.Validation("FIRC ID cannot be empty");

            var firc = await _fircRepository.GetByIdAsync(id);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {id} not found");

            return Result<FircTracking>.Success(firc);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<FircTracking> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? searchTerm = null,
            string? status = null,
            bool? edpmsReported = null)
        {
            var filters = new Dictionary<string, object>();

            if (companyId.HasValue)
                filters["company_id"] = companyId.Value;
            if (!string.IsNullOrEmpty(status))
                filters["status"] = status;
            if (edpmsReported.HasValue)
                filters["edpms_reported"] = edpmsReported.Value;

            var result = await _fircRepository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, "firc_date", true, filters);

            return Result<(IEnumerable<FircTracking> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<FircTracking>> CreateAsync(CreateFircDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (dto.ForeignAmount <= 0)
                return Error.Validation("Foreign amount must be greater than zero");

            // Check for duplicate FIRC number
            if (!string.IsNullOrEmpty(dto.FircNumber))
            {
                var existing = await _fircRepository.GetByFircNumberAsync(dto.FircNumber);
                if (existing != null)
                    return Error.Conflict($"FIRC with number {dto.FircNumber} already exists");
            }

            var firc = new FircTracking
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                FircNumber = dto.FircNumber,
                FircDate = dto.FircDate,
                BankName = dto.BankName,
                BankBranch = dto.BankBranch,
                BankSwiftCode = dto.BankSwiftCode,
                PurposeCode = dto.PurposeCode,
                ForeignCurrency = dto.ForeignCurrency,
                ForeignAmount = dto.ForeignAmount,
                InrAmount = dto.InrAmount,
                ExchangeRate = dto.ExchangeRate,
                RemitterName = dto.RemitterName,
                RemitterCountry = dto.RemitterCountry,
                RemitterBank = dto.RemitterBank,
                BeneficiaryName = dto.BeneficiaryName,
                BeneficiaryAccount = dto.BeneficiaryAccount,
                PaymentId = dto.PaymentId,
                Status = dto.PaymentId.HasValue ? "linked" : "received",
                Notes = dto.Notes,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _fircRepository.AddAsync(firc);
            return Result<FircTracking>.Success(created);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateFircDto dto)
        {
            var firc = await _fircRepository.GetByIdAsync(id);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {id} not found");

            // Update fields if provided
            if (!string.IsNullOrEmpty(dto.FircNumber))
            {
                // Check for duplicate
                var existing = await _fircRepository.GetByFircNumberAsync(dto.FircNumber);
                if (existing != null && existing.Id != id)
                    return Error.Conflict($"FIRC with number {dto.FircNumber} already exists");
                firc.FircNumber = dto.FircNumber;
            }

            if (dto.FircDate.HasValue)
                firc.FircDate = dto.FircDate;
            if (!string.IsNullOrEmpty(dto.BankName))
                firc.BankName = dto.BankName;
            if (dto.BankBranch != null)
                firc.BankBranch = dto.BankBranch;
            if (dto.BankSwiftCode != null)
                firc.BankSwiftCode = dto.BankSwiftCode;
            if (!string.IsNullOrEmpty(dto.PurposeCode))
                firc.PurposeCode = dto.PurposeCode;
            if (dto.RemitterName != null)
                firc.RemitterName = dto.RemitterName;
            if (dto.RemitterCountry != null)
                firc.RemitterCountry = dto.RemitterCountry;
            if (dto.RemitterBank != null)
                firc.RemitterBank = dto.RemitterBank;
            if (!string.IsNullOrEmpty(dto.BeneficiaryName))
                firc.BeneficiaryName = dto.BeneficiaryName;
            if (dto.BeneficiaryAccount != null)
                firc.BeneficiaryAccount = dto.BeneficiaryAccount;
            if (!string.IsNullOrEmpty(dto.Status))
                firc.Status = dto.Status;
            if (dto.Notes != null)
                firc.Notes = dto.Notes;

            firc.UpdatedAt = DateTime.UtcNow;

            await _fircRepository.UpdateAsync(firc);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            var firc = await _fircRepository.GetByIdAsync(id);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {id} not found");

            // Don't allow deletion if EDPMS reported
            if (firc.EdpmsReported)
                return Error.Validation("Cannot delete FIRC that has been reported to EDPMS");

            await _fircRepository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Payment Linking ====================

        /// <inheritdoc />
        public async Task<Result> LinkToPaymentAsync(Guid fircId, Guid paymentId)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            var payment = await _paymentsRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {paymentId} not found");

            // Check if payment is already linked to another FIRC
            var existingFirc = await _fircRepository.GetByPaymentIdAsync(paymentId);
            if (existingFirc != null && existingFirc.Id != fircId)
                return Error.Conflict($"Payment is already linked to FIRC {existingFirc.FircNumber}");

            firc.PaymentId = paymentId;
            firc.Status = "linked";
            firc.UpdatedAt = DateTime.UtcNow;

            await _fircRepository.UpdateAsync(firc);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UnlinkFromPaymentAsync(Guid fircId)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            firc.PaymentId = null;
            firc.Status = "received";
            firc.UpdatedAt = DateTime.UtcNow;

            await _fircRepository.UpdateAsync(firc);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<FircTracking>>> GetUnlinkedAsync(Guid companyId)
        {
            var unlinked = await _fircRepository.GetUnlinkedAsync(companyId);
            return Result<IEnumerable<FircTracking>>.Success(unlinked);
        }

        /// <inheritdoc />
        public async Task<Result<FircAutoMatchResultDto>> AutoMatchFircsAsync(
            Guid companyId,
            decimal amountTolerance = 0.01m,
            int dateTolerance = 5)
        {
            var result = new FircAutoMatchResultDto();

            // Get unlinked FIRCs
            var unlinkedFircs = (await _fircRepository.GetUnlinkedAsync(companyId)).ToList();
            result.FircsProcessed = unlinkedFircs.Count;

            if (unlinkedFircs.Count == 0)
                return Result<FircAutoMatchResultDto>.Success(result);

            // Get unlinked payments (payments without a FIRC)
            var allPayments = await _paymentsRepository.GetByCompanyIdAsync(companyId);
            var paymentsWithFirc = new HashSet<Guid>();
            foreach (var firc in await _fircRepository.GetByCompanyIdAsync(companyId))
            {
                if (firc.PaymentId.HasValue)
                    paymentsWithFirc.Add(firc.PaymentId.Value);
            }

            var unlinkedPayments = allPayments
                .Where(p => !paymentsWithFirc.Contains(p.Id) && p.Currency != "INR")
                .ToList();

            foreach (var firc in unlinkedFircs)
            {
                var bestMatch = FindBestPaymentMatch(firc, unlinkedPayments, amountTolerance, dateTolerance);

                if (bestMatch.Payment != null)
                {
                    // Link the FIRC to the payment
                    firc.PaymentId = bestMatch.Payment.Id;
                    firc.Status = "linked";
                    firc.UpdatedAt = DateTime.UtcNow;
                    await _fircRepository.UpdateAsync(firc);

                    // Remove from available payments
                    unlinkedPayments.Remove(bestMatch.Payment);

                    result.FircsMatched++;
                    result.TotalAmountMatched += firc.ForeignAmount;
                    result.Matches.Add(new FircMatchDto
                    {
                        FircId = firc.Id,
                        PaymentId = bestMatch.Payment.Id,
                        FircNumber = firc.FircNumber,
                        FircAmount = firc.ForeignAmount,
                        PaymentAmount = bestMatch.Payment.Amount,
                        AmountDifference = Math.Abs(firc.ForeignAmount - bestMatch.Payment.Amount),
                        DaysDifference = bestMatch.DaysDifference,
                        MatchScore = bestMatch.Score,
                        MatchReason = bestMatch.Reason
                    });
                }
                else
                {
                    result.FircsSkipped++;
                }
            }

            return Result<FircAutoMatchResultDto>.Success(result);
        }

        private (Core.Entities.Payments? Payment, int Score, int DaysDifference, string Reason) FindBestPaymentMatch(
            FircTracking firc,
            List<Core.Entities.Payments> payments,
            decimal amountTolerance,
            int dateTolerance)
        {
            Core.Entities.Payments? bestMatch = null;
            int bestScore = 0;
            int bestDaysDiff = int.MaxValue;
            string bestReason = string.Empty;

            foreach (var payment in payments)
            {
                // Currency must match
                if (payment.Currency != firc.ForeignCurrency)
                    continue;

                int score = 0;
                var reasons = new List<string>();

                // Amount matching (within tolerance)
                var amountDiff = Math.Abs(firc.ForeignAmount - payment.Amount);
                var percentDiff = amountDiff / firc.ForeignAmount;

                if (amountDiff <= amountTolerance)
                {
                    score += 50;  // Exact match
                    reasons.Add("Exact amount match");
                }
                else if (percentDiff <= 0.01m)  // 1% tolerance
                {
                    score += 40;
                    reasons.Add("Amount within 1%");
                }
                else if (percentDiff <= 0.05m)  // 5% tolerance
                {
                    score += 20;
                    reasons.Add("Amount within 5%");
                }
                else
                {
                    continue;  // Amount difference too large
                }

                // Date matching
                if (firc.FircDate.HasValue)
                {
                    var daysDiff = Math.Abs((firc.FircDate.Value.ToDateTime(TimeOnly.MinValue) -
                                             payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);

                    if (daysDiff <= dateTolerance)
                    {
                        score += 30;
                        reasons.Add($"Date within {dateTolerance} days");
                    }
                    else if (daysDiff <= dateTolerance * 2)
                    {
                        score += 15;
                        reasons.Add($"Date within {dateTolerance * 2} days");
                    }

                    if (score > bestScore || (score == bestScore && daysDiff < bestDaysDiff))
                    {
                        bestMatch = payment;
                        bestScore = score;
                        bestDaysDiff = daysDiff;
                        bestReason = string.Join("; ", reasons);
                    }
                }
                else if (score > bestScore)
                {
                    bestMatch = payment;
                    bestScore = score;
                    bestDaysDiff = 0;
                    bestReason = string.Join("; ", reasons);
                }
            }

            // Require minimum score of 50 (at least amount match)
            if (bestScore >= 50)
                return (bestMatch, bestScore, bestDaysDiff, bestReason);

            return (null, 0, 0, string.Empty);
        }

        // ==================== Invoice Linking ====================

        /// <inheritdoc />
        public async Task<Result> LinkToInvoicesAsync(Guid fircId, IEnumerable<FircInvoiceAllocationDto> allocations)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            var allocationList = allocations.ToList();
            var totalAllocated = allocationList.Sum(a => a.AllocatedAmount);

            // Validate total allocation doesn't exceed FIRC amount
            if (totalAllocated > firc.ForeignAmount * 1.01m)  // 1% tolerance for rounding
                return Error.Validation($"Total allocation ({totalAllocated}) exceeds FIRC amount ({firc.ForeignAmount})");

            // Validate each invoice exists
            foreach (var allocation in allocationList)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(allocation.InvoiceId);
                if (invoice == null)
                    return Error.NotFound($"Invoice with ID {allocation.InvoiceId} not found");

                var link = new FircInvoiceLink
                {
                    Id = Guid.NewGuid(),
                    FircId = fircId,
                    InvoiceId = allocation.InvoiceId,
                    AllocatedAmount = allocation.AllocatedAmount,
                    AllocatedAmountInr = allocation.AllocatedAmountInr ?? allocation.AllocatedAmount * firc.ExchangeRate,
                    CreatedAt = DateTime.UtcNow
                };

                await _fircRepository.AddInvoiceLinkAsync(link);
            }

            // Update FIRC status
            if (firc.PaymentId.HasValue)
                firc.Status = "reconciled";
            else
                firc.Status = "linked";

            firc.UpdatedAt = DateTime.UtcNow;
            await _fircRepository.UpdateAsync(firc);

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> RemoveInvoiceLinkAsync(Guid fircId, Guid invoiceId)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            await _fircRepository.RemoveInvoiceLinkAsync(fircId, invoiceId);

            // Update status if no more invoice links
            var remainingLinks = await _fircRepository.GetInvoiceLinksAsync(fircId);
            if (!remainingLinks.Any())
            {
                firc.Status = firc.PaymentId.HasValue ? "linked" : "received";
                firc.UpdatedAt = DateTime.UtcNow;
                await _fircRepository.UpdateAsync(firc);
            }

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<FircInvoiceLink>>> GetInvoiceLinksAsync(Guid fircId)
        {
            var links = await _fircRepository.GetInvoiceLinksAsync(fircId);
            return Result<IEnumerable<FircInvoiceLink>>.Success(links);
        }

        // ==================== EDPMS Compliance ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<FircTracking>>> GetPendingEdpmsReportingAsync(Guid companyId)
        {
            var pending = await _fircRepository.GetPendingEdpmsReportingAsync(companyId);
            return Result<IEnumerable<FircTracking>>.Success(pending);
        }

        /// <inheritdoc />
        public async Task<Result> MarkEdpmsReportedAsync(Guid fircId, DateOnly reportDate, string? reference)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            await _fircRepository.MarkEdpmsReportedAsync(fircId, reportDate, reference);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<EdpmsComplianceSummaryDto>> GetEdpmsComplianceSummaryAsync(Guid companyId)
        {
            var allFircs = (await _fircRepository.GetByCompanyIdAsync(companyId)).ToList();
            var pending = allFircs.Where(f => !f.EdpmsReported).ToList();

            var summary = new EdpmsComplianceSummaryDto
            {
                CompanyId = companyId,
                TotalFircs = allFircs.Count,
                ReportedCount = allFircs.Count(f => f.EdpmsReported),
                PendingCount = pending.Count,
                TotalReportedAmount = allFircs.Where(f => f.EdpmsReported).Sum(f => f.InrAmount),
                TotalPendingAmount = pending.Sum(f => f.InrAmount),
                PendingItems = pending.Select(f => new PendingEdpmsItemDto
                {
                    FircId = f.Id,
                    FircNumber = f.FircNumber,
                    FircDate = f.FircDate,
                    BankName = f.BankName,
                    ForeignAmount = f.ForeignAmount,
                    ForeignCurrency = f.ForeignCurrency,
                    InrAmount = f.InrAmount,
                    DaysPending = f.FircDate.HasValue
                        ? (int)(DateTime.UtcNow.Date - f.FircDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays
                        : 0
                }).OrderByDescending(p => p.DaysPending).ToList()
            };

            return Result<EdpmsComplianceSummaryDto>.Success(summary);
        }

        // ==================== Realization Tracking (9-month FEMA) ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<RealizationAlertDto>>> GetRealizationAlertsAsync(
            Guid companyId,
            int alertDaysBeforeDeadline = 30)
        {
            var exportInvoices = await GetExportInvoicesWithRealizationStatus(companyId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var alerts = exportInvoices
                .Where(i => !i.IsFullyRealized)
                .Select(i =>
                {
                    var daysRemaining = (i.DeadlineDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
                    var alertLevel = daysRemaining switch
                    {
                        < 0 => "overdue",
                        <= 7 => "critical",
                        <= 30 => "warning",
                        _ => "normal"
                    };

                    return new RealizationAlertDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate,
                        DeadlineDate = i.DeadlineDate,
                        DaysRemaining = daysRemaining,
                        PartyId = i.PartyId,
                        CustomerName = i.CustomerName,
                        ForeignAmount = i.ForeignAmount,
                        Currency = i.Currency,
                        AmountInr = i.AmountInr,
                        AmountRealized = i.AmountRealized,
                        AmountPending = i.AmountPending,
                        AlertLevel = alertLevel
                    };
                })
                .Where(a => a.DaysRemaining <= alertDaysBeforeDeadline || a.AlertLevel == "overdue")
                .OrderBy(a => a.DaysRemaining)
                .ToList();

            return Result<IEnumerable<RealizationAlertDto>>.Success(alerts);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<RealizationStatusDto>>> GetRealizationStatusAsync(
            Guid companyId,
            DateOnly? asOfDate = null)
        {
            var statuses = await GetExportInvoicesWithRealizationStatus(companyId, asOfDate);
            return Result<IEnumerable<RealizationStatusDto>>.Success(statuses);
        }

        /// <inheritdoc />
        public async Task<Result<RealizationSummaryDto>> GetRealizationSummaryAsync(
            Guid companyId,
            DateOnly? asOfDate = null)
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var statuses = (await GetExportInvoicesWithRealizationStatus(companyId, effectiveDate)).ToList();

            var summary = new RealizationSummaryDto
            {
                CompanyId = companyId,
                AsOfDate = effectiveDate,
                TotalExportInvoices = statuses.Count,
                FullyRealizedCount = statuses.Count(s => s.Status == "realized"),
                PartiallyRealizedCount = statuses.Count(s => s.Status == "partially_realized"),
                PendingCount = statuses.Count(s => s.Status == "pending"),
                OverdueCount = statuses.Count(s => s.IsOverdue),
                TotalExportAmount = statuses.Sum(s => s.ForeignAmount),
                TotalRealizedAmount = statuses.Sum(s => s.AmountRealized),
                TotalPendingAmount = statuses.Sum(s => s.AmountPending),
                TotalOverdueAmount = statuses.Where(s => s.IsOverdue).Sum(s => s.AmountPending),
                TotalExportAmountInr = statuses.Sum(s => s.AmountInr ?? 0),
                TotalRealizedAmountInr = statuses.SelectMany(s => s.Payments).Sum(p => p.InrAmount),
                PrimaryCurrency = statuses.FirstOrDefault()?.Currency ?? "USD"
            };

            summary.TotalPendingAmountInr = summary.TotalExportAmountInr - summary.TotalRealizedAmountInr;
            summary.TotalOverdueAmountInr = statuses.Where(s => s.IsOverdue)
                .Sum(s => (s.AmountInr ?? 0) - s.Payments.Sum(p => p.InrAmount));

            // Currency breakdown
            var byCurrency = statuses.GroupBy(s => s.Currency);
            foreach (var group in byCurrency)
            {
                summary.CurrencyBreakdown[group.Key] = new CurrencyRealizationDto
                {
                    Currency = group.Key,
                    InvoiceCount = group.Count(),
                    TotalAmount = group.Sum(s => s.ForeignAmount),
                    RealizedAmount = group.Sum(s => s.AmountRealized),
                    PendingAmount = group.Sum(s => s.AmountPending)
                };
            }

            return Result<RealizationSummaryDto>.Success(summary);
        }

        private async Task<IEnumerable<RealizationStatusDto>> GetExportInvoicesWithRealizationStatus(
            Guid companyId,
            DateOnly? asOfDate = null)
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Get export invoices (non-INR currency, not draft/cancelled)
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (allInvoices, _) = await _invoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);
            var exportInvoices = allInvoices
                .Where(i => !string.IsNullOrEmpty(i.Currency) &&
                            i.Currency != "INR" &&
                            i.Status != "draft" &&
                            i.Status != "cancelled")
                .ToList();

            var result = new List<RealizationStatusDto>();

            foreach (var invoice in exportInvoices)
            {
                Core.Entities.Customers? customer = null;
                if (invoice.PartyId.HasValue)
                    customer = await _customersRepository.GetByIdAsync(invoice.PartyId.Value);

                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paymentList = payments
                    .Where(p => asOfDate == null || p.PaymentDate <= asOfDate.Value)
                    .ToList();

                var totalRealized = paymentList.Sum(p => p.Amount);
                var invoiceDate = invoice.InvoiceDate;
                var deadlineDate = invoiceDate.AddDays(FemaRealizationDays);
                var isOverdue = effectiveDate > deadlineDate && totalRealized < invoice.TotalAmount;
                var daysToDeadline = (deadlineDate.ToDateTime(TimeOnly.MinValue) - effectiveDate.ToDateTime(TimeOnly.MinValue)).Days;

                var status = new RealizationStatusDto
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    InvoiceDate = invoiceDate,
                    DeadlineDate = deadlineDate,
                    PartyId = invoice.PartyId ?? Guid.Empty,
                    CustomerName = customer?.Name ?? "Unknown",
                    ForeignAmount = invoice.TotalAmount,
                    Currency = invoice.Currency ?? "USD",
                    AmountInr = invoice.ForeignCurrencyAmount,
                    AmountRealized = totalRealized,
                    AmountPending = invoice.TotalAmount - totalRealized,
                    IsOverdue = isOverdue,
                    DaysToDeadline = daysToDeadline,
                    Status = totalRealized >= invoice.TotalAmount - 0.01m
                        ? "realized"
                        : totalRealized > 0
                            ? "partially_realized"
                            : isOverdue
                                ? "overdue"
                                : "pending"
                };

                // Get FIRC details for each payment
                foreach (var payment in paymentList)
                {
                    var firc = await _fircRepository.GetByPaymentIdAsync(payment.Id);
                    var inrAmount = payment.AmountInInr ?? payment.Amount;
                    var exchangeRate = payment.Amount > 0 ? inrAmount / payment.Amount : 1;
                    status.Payments.Add(new RealizationPaymentDto
                    {
                        PaymentId = payment.Id,
                        PaymentDate = payment.PaymentDate,
                        ForeignAmount = payment.Amount,
                        InrAmount = inrAmount,
                        ExchangeRate = exchangeRate,
                        FircId = firc?.Id,
                        FircNumber = firc?.FircNumber
                    });
                }

                result.Add(status);
            }

            return result.OrderBy(r => r.DaysToDeadline);
        }

        // ==================== Reconciliation Reports ====================

        /// <inheritdoc />
        public async Task<Result<FircReconciliationReportDto>> GetReconciliationReportAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            var allFircs = (await _fircRepository.GetByCompanyIdAsync(companyId))
                .Where(f => !f.FircDate.HasValue || (f.FircDate >= fromDate && f.FircDate <= toDate))
                .ToList();

            var report = new FircReconciliationReportDto
            {
                CompanyId = companyId,
                FromDate = fromDate,
                ToDate = toDate,
                TotalFircs = allFircs.Count,
                LinkedToPayments = allFircs.Count(f => f.PaymentId.HasValue),
                UnlinkedFircs = allFircs.Count(f => !f.PaymentId.HasValue),
                TotalFircAmountForeign = allFircs.Sum(f => f.ForeignAmount),
                TotalFircAmountInr = allFircs.Sum(f => f.InrAmount)
            };

            foreach (var firc in allFircs)
            {
                var invoiceLinks = await _fircRepository.GetInvoiceLinksAsync(firc.Id);
                var linksList = invoiceLinks.ToList();

                Core.Entities.Payments? payment = null;
                if (firc.PaymentId.HasValue)
                    payment = await _paymentsRepository.GetByIdAsync(firc.PaymentId.Value);

                var item = new FircReconciliationItemDto
                {
                    FircId = firc.Id,
                    FircNumber = firc.FircNumber,
                    FircDate = firc.FircDate,
                    BankName = firc.BankName,
                    ForeignAmount = firc.ForeignAmount,
                    Currency = firc.ForeignCurrency,
                    InrAmount = firc.InrAmount,
                    Status = firc.Status,
                    PaymentId = firc.PaymentId,
                    PaymentDate = payment?.PaymentDate,
                    PaymentAmount = payment?.Amount,
                    PaymentAmountInr = payment?.AmountInInr,
                    LinkedInvoiceCount = linksList.Count,
                    TotalAllocatedAmount = linksList.Sum(l => l.AllocatedAmount),
                    EdpmsReported = firc.EdpmsReported,
                    EdpmsReportDate = firc.EdpmsReportDate
                };

                // Determine reconciliation status
                if (firc.PaymentId.HasValue && linksList.Count > 0)
                {
                    item.ReconciliationStatus = "full";
                    report.FullyReconciledCount++;
                }
                else if (firc.PaymentId.HasValue || linksList.Count > 0)
                {
                    item.ReconciliationStatus = "partial";
                    report.PartiallyReconciledCount++;
                }
                else
                {
                    item.ReconciliationStatus = "none";
                    report.UnreconciledCount++;
                }

                // Check for discrepancies
                if (payment != null && Math.Abs(firc.ForeignAmount - payment.Amount) > 0.01m)
                {
                    var diff = firc.ForeignAmount - payment.Amount;
                    item.AmountDifference = diff;
                    report.TotalDiscrepancyAmount += Math.Abs(diff);
                    report.Discrepancies.Add(new FircDiscrepancyDto
                    {
                        FircId = firc.Id,
                        FircNumber = firc.FircNumber,
                        DiscrepancyType = "amount_mismatch",
                        Description = $"FIRC amount {firc.ForeignAmount} differs from payment amount {payment.Amount}",
                        ExpectedAmount = firc.ForeignAmount,
                        ActualAmount = payment.Amount,
                        Difference = diff
                    });
                }

                report.Items.Add(item);
            }

            report.LinkedToInvoices = report.Items.Count(i => i.LinkedInvoiceCount > 0);

            return Result<FircReconciliationReportDto>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<FircValidationResultDto>> ValidateFircAsync(Guid fircId)
        {
            var firc = await _fircRepository.GetByIdAsync(fircId);
            if (firc == null)
                return Error.NotFound($"FIRC with ID {fircId} not found");

            var result = new FircValidationResultDto
            {
                FircId = fircId,
                IsValid = true
            };

            // Check for missing FIRC number
            if (string.IsNullOrEmpty(firc.FircNumber))
            {
                result.Warnings.Add("FIRC number is missing");
                result.HasMissingFircNumber = true;
            }

            // Check for missing FIRC date
            if (!firc.FircDate.HasValue)
            {
                result.Warnings.Add("FIRC date is missing");
                result.HasMissingFircDate = true;
            }

            // Check for missing payment link
            if (!firc.PaymentId.HasValue)
            {
                result.Warnings.Add("FIRC is not linked to a payment");
                result.HasMissingPaymentLink = true;
            }

            // Check for missing invoice links
            var invoiceLinks = await _fircRepository.GetInvoiceLinksAsync(fircId);
            if (!invoiceLinks.Any())
            {
                result.Warnings.Add("FIRC is not linked to any invoices");
                result.HasMissingInvoiceLink = true;
            }

            // Check EDPMS status
            if (!firc.EdpmsReported)
            {
                result.Warnings.Add("FIRC has not been reported to EDPMS");
                result.HasEdpmsPending = true;
            }

            // Check for amount mismatch with payment
            if (firc.PaymentId.HasValue)
            {
                var payment = await _paymentsRepository.GetByIdAsync(firc.PaymentId.Value);
                if (payment != null && Math.Abs(firc.ForeignAmount - payment.Amount) > 0.01m)
                {
                    result.Errors.Add($"Amount mismatch: FIRC amount {firc.ForeignAmount} differs from payment amount {payment.Amount}");
                    result.HasAmountMismatch = true;
                    result.IsValid = false;
                }
            }

            // Validate required fields
            if (string.IsNullOrEmpty(firc.BankName))
            {
                result.Errors.Add("Bank name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(firc.PurposeCode))
            {
                result.Errors.Add("Purpose code is required");
                result.IsValid = false;
            }

            if (firc.ForeignAmount <= 0)
            {
                result.Errors.Add("Foreign amount must be greater than zero");
                result.IsValid = false;
            }

            return Result<FircValidationResultDto>.Success(result);
        }
    }
}
