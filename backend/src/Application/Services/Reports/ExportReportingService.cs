using Application.DTOs.Reports;
using Application.Interfaces.Reports;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Forex;

namespace Application.Services.Reports
{
    /// <summary>
    /// Service for export-related reports and dashboards
    /// </summary>
    public class ExportReportingService : IExportReportingService
    {
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly IFircTrackingRepository _fircRepository;
        private readonly ILutRegisterRepository _lutRepository;
        private readonly IForexTransactionRepository _forexRepository;

        private const int FemaRealizationDays = 270;

        public ExportReportingService(
            IInvoicesRepository invoicesRepository,
            IPaymentsRepository paymentsRepository,
            ICustomersRepository customersRepository,
            ICompaniesRepository companiesRepository,
            IFircTrackingRepository fircRepository,
            ILutRegisterRepository lutRepository,
            IForexTransactionRepository forexRepository)
        {
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
            _fircRepository = fircRepository ?? throw new ArgumentNullException(nameof(fircRepository));
            _lutRepository = lutRepository ?? throw new ArgumentNullException(nameof(lutRepository));
            _forexRepository = forexRepository ?? throw new ArgumentNullException(nameof(forexRepository));
        }

        // ==================== Receivables Ageing ====================

        /// <inheritdoc />
        public async Task<Result<ExportReceivablesAgeingReportDto>> GetReceivablesAgeingAsync(
            Guid companyId,
            DateOnly? asOfDate = null)
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var exportInvoices = await GetExportInvoicesAsync(companyId);

            var report = new ExportReceivablesAgeingReportDto
            {
                CompanyId = companyId,
                AsOfDate = effectiveDate
            };

            foreach (var invoice in exportInvoices)
            {
                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paidAmount = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paidAmount;

                if (outstanding <= 0.01m) continue;  // Fully paid

                var customer = invoice.PartyId.HasValue
                    ? await _customersRepository.GetByIdAsync(invoice.PartyId.Value)
                    : null;

                var daysOutstanding = (effectiveDate.ToDateTime(TimeOnly.MinValue) - invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;
                var femaDeadline = invoice.InvoiceDate.AddDays(FemaRealizationDays);
                var daysToFema = (femaDeadline.ToDateTime(TimeOnly.MinValue) - effectiveDate.ToDateTime(TimeOnly.MinValue)).Days;

                // Calculate INR value using exchange rate (default to 85 if not set)
                var exchangeRate = invoice.InvoiceExchangeRate ?? invoice.ExchangeRate ?? 85m;
                var outstandingInr = outstanding * exchangeRate;

                var ageingInvoice = new AgeingInvoiceDto
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    DaysOutstanding = daysOutstanding,
                    PartyId = invoice.PartyId ?? Guid.Empty,
                    CustomerName = customer?.Name ?? "Unknown",
                    Currency = invoice.Currency ?? "USD",
                    InvoiceAmount = invoice.TotalAmount,
                    PaidAmount = paidAmount,
                    OutstandingAmount = outstanding,
                    OutstandingAmountInr = outstandingInr,
                    FemaDeadline = femaDeadline,
                    DaysToFemaDeadline = daysToFema,
                    IsFemaOverdue = daysToFema < 0
                };

                // Categorize by ageing bucket
                if (daysOutstanding <= 30)
                {
                    ageingInvoice.AgeingBucket = "Current";
                    report.Current += outstanding;
                    report.CurrentInr += outstandingInr;
                    report.CurrentCount++;
                }
                else if (daysOutstanding <= 60)
                {
                    ageingInvoice.AgeingBucket = "31-60 Days";
                    report.Days31To60 += outstanding;
                    report.Days31To60Inr += outstandingInr;
                    report.Days31To60Count++;
                }
                else if (daysOutstanding <= 90)
                {
                    ageingInvoice.AgeingBucket = "61-90 Days";
                    report.Days61To90 += outstanding;
                    report.Days61To90Inr += outstandingInr;
                    report.Days61To90Count++;
                }
                else if (daysOutstanding <= 180)
                {
                    ageingInvoice.AgeingBucket = "91-180 Days";
                    report.Days91To180 += outstanding;
                    report.Days91To180Inr += outstandingInr;
                    report.Days91To180Count++;
                }
                else if (daysOutstanding <= 270)
                {
                    ageingInvoice.AgeingBucket = "181-270 Days";
                    report.Days181To270 += outstanding;
                    report.Days181To270Inr += outstandingInr;
                    report.Days181To270Count++;
                }
                else
                {
                    ageingInvoice.AgeingBucket = "Over 270 Days";
                    report.Over270Days += outstanding;
                    report.Over270DaysInr += outstandingInr;
                    report.Over270DaysCount++;
                }

                report.Invoices.Add(ageingInvoice);

                // Currency breakdown
                var currency = invoice.Currency ?? "USD";
                if (!report.CurrencyBreakdown.ContainsKey(currency))
                {
                    report.CurrencyBreakdown[currency] = new CurrencyAgeingDto { Currency = currency };
                }
                var currencyAgeing = report.CurrencyBreakdown[currency];
                currencyAgeing.TotalAmount += outstanding;
                currencyAgeing.TotalAmountInr += outstandingInr;
                currencyAgeing.InvoiceCount++;

                if (daysOutstanding <= 30)
                    currencyAgeing.Current += outstanding;
                else if (daysOutstanding <= 60)
                    currencyAgeing.Days31To60 += outstanding;
                else if (daysOutstanding <= 90)
                    currencyAgeing.Days61To90 += outstanding;
                else
                    currencyAgeing.Over90Days += outstanding;
            }

            report.TotalReceivablesForeign = report.Current + report.Days31To60 + report.Days61To90 +
                                              report.Days91To180 + report.Days181To270 + report.Over270Days;
            report.TotalReceivablesInr = report.CurrentInr + report.Days31To60Inr + report.Days61To90Inr +
                                          report.Days91To180Inr + report.Days181To270Inr + report.Over270DaysInr;

            report.Invoices = report.Invoices.OrderByDescending(i => i.DaysOutstanding).ToList();

            return Result<ExportReceivablesAgeingReportDto>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<CustomerExportReceivableDto>>> GetCustomerWiseReceivablesAsync(
            Guid companyId,
            DateOnly? asOfDate = null)
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var exportInvoices = await GetExportInvoicesAsync(companyId);

            var customerReceivables = new Dictionary<Guid, CustomerExportReceivableDto>();

            foreach (var invoice in exportInvoices)
            {
                var customerId = invoice.PartyId ?? Guid.Empty;
                if (!customerReceivables.ContainsKey(customerId))
                {
                    var customer = customerId != Guid.Empty
                        ? await _customersRepository.GetByIdAsync(customerId)
                        : null;

                    customerReceivables[customerId] = new CustomerExportReceivableDto
                    {
                        PartyId = customerId,
                        CustomerName = customer?.Name ?? "Unknown",
                        Country = customer?.Country,
                        PrimaryCurrency = invoice.Currency ?? "USD"
                    };
                }

                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paidAmount = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paidAmount;
                var daysOutstanding = (effectiveDate.ToDateTime(TimeOnly.MinValue) - invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;
                var exchangeRate = invoice.InvoiceExchangeRate ?? invoice.ExchangeRate ?? 85m;
                var outstandingInr = outstanding * exchangeRate;

                var cr = customerReceivables[customerId];
                cr.InvoiceCount++;
                cr.TotalInvoiced += invoice.TotalAmount;
                cr.TotalPaid += paidAmount;
                cr.TotalOutstanding += outstanding;
                cr.TotalOutstandingInr += outstandingInr;

                if (daysOutstanding > cr.OldestInvoiceDays)
                    cr.OldestInvoiceDays = daysOutstanding;

                if (daysOutstanding > FemaRealizationDays && outstanding > 0.01m)
                {
                    cr.FemaOverdueCount++;
                    cr.FemaOverdueAmount += outstanding;
                }
            }

            return Result<IEnumerable<CustomerExportReceivableDto>>.Success(
                customerReceivables.Values.OrderByDescending(c => c.TotalOutstanding));
        }

        // ==================== Forex Reports ====================

        /// <inheritdoc />
        public async Task<Result<ForexGainLossReportDto>> GetForexGainLossReportAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (forexTransactions, _) = await _forexRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);
            var periodTransactions = forexTransactions
                .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
                .ToList();

            var report = new ForexGainLossReportDto
            {
                CompanyId = companyId,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Realized transactions
            var realizedTxns = periodTransactions.Where(t => t.TransactionType == "settlement").ToList();
            foreach (var txn in realizedTxns)
            {
                var gainLoss = txn.ForexGainLoss ?? 0m;
                if (gainLoss > 0)
                    report.RealizedGainTotal += gainLoss;
                else
                    report.RealizedLossTotal += Math.Abs(gainLoss);

                report.RealizedTransactions.Add(new ForexTransactionDetailDto
                {
                    TransactionId = txn.Id,
                    TransactionDate = txn.TransactionDate,
                    TransactionType = txn.TransactionType,
                    DocumentNumber = txn.SourceNumber ?? string.Empty,
                    Currency = txn.Currency,
                    ForeignAmount = txn.ForeignAmount,
                    BookingRate = txn.ExchangeRate,
                    SettlementRate = txn.ExchangeRate,
                    BookingAmountInr = txn.InrAmount - gainLoss,
                    SettlementAmountInr = txn.InrAmount,
                    GainLoss = gainLoss
                });
            }

            report.RealizedTransactionCount = realizedTxns.Count;

            // Monthly trend
            var monthlyGroups = periodTransactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            foreach (var group in monthlyGroups)
            {
                var realized = group.Where(t => t.GainLossType == "realized").Sum(t => t.ForexGainLoss ?? 0m);
                var unrealized = group.Where(t => t.GainLossType == "unrealized").Sum(t => t.ForexGainLoss ?? 0m);

                report.MonthlyTrend.Add(new MonthlyForexSummaryDto
                {
                    Month = group.Key.Month,
                    Year = group.Key.Year,
                    MonthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMM yyyy"),
                    RealizedGainLoss = realized,
                    UnrealizedGainLoss = unrealized,
                    TotalGainLoss = realized + unrealized,
                    TransactionCount = group.Count()
                });
            }

            return Result<ForexGainLossReportDto>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<UnrealizedForexPositionDto>> GetUnrealizedForexPositionAsync(
            Guid companyId,
            DateOnly asOfDate,
            decimal currentExchangeRate)
        {
            var exportInvoices = await GetExportInvoicesAsync(companyId);

            var position = new UnrealizedForexPositionDto
            {
                CompanyId = companyId,
                AsOfDate = asOfDate,
                CurrentExchangeRate = currentExchangeRate
            };

            foreach (var invoice in exportInvoices)
            {
                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paidAmount = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paidAmount;

                if (outstanding <= 0.01m) continue;

                var bookingRate = invoice.InvoiceExchangeRate ?? invoice.ExchangeRate ?? 83m;
                var bookingInr = outstanding * bookingRate;
                var currentInr = outstanding * currentExchangeRate;
                var unrealizedGainLoss = currentInr - bookingInr;

                position.TotalOpenPositionForeign += outstanding;
                position.TotalOpenPositionInrAtBooking += bookingInr;
                position.TotalOpenPositionInrAtCurrent += currentInr;
                position.TotalUnrealizedGainLoss += unrealizedGainLoss;

                var currency = invoice.Currency ?? "USD";
                if (!position.CurrencyBreakdown.ContainsKey(currency))
                {
                    position.CurrencyBreakdown[currency] = new CurrencyForexPositionDto
                    {
                        Currency = currency,
                        CurrentRate = currentExchangeRate
                    };
                }

                var currPos = position.CurrencyBreakdown[currency];
                currPos.OpenAmount += outstanding;
                currPos.BookingValueInr += bookingInr;
                currPos.CurrentValueInr += currentInr;
                currPos.UnrealizedGainLoss += unrealizedGainLoss;
            }

            // Calculate average booking rate per currency
            foreach (var curr in position.CurrencyBreakdown.Values)
            {
                if (curr.OpenAmount > 0)
                    curr.AvgBookingRate = curr.BookingValueInr / curr.OpenAmount;
            }

            return Result<UnrealizedForexPositionDto>.Success(position);
        }

        // ==================== FEMA Compliance Dashboard ====================

        /// <inheritdoc />
        public async Task<Result<FemaComplianceDashboardDto>> GetFemaComplianceDashboardAsync(Guid companyId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var exportInvoices = (await GetExportInvoicesAsync(companyId)).ToList();

            var dashboard = new FemaComplianceDashboardDto
            {
                CompanyId = companyId,
                TotalOpenInvoices = 0
            };

            decimal totalReceivables = 0;
            var alerts = new List<FemaViolationAlertDto>();

            foreach (var invoice in exportInvoices)
            {
                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paidAmount = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paidAmount;
                var femaDeadline = invoice.InvoiceDate.AddDays(FemaRealizationDays);
                var daysToDeadline = (femaDeadline.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                if (outstanding <= 0.01m)
                {
                    dashboard.FullyRealizedCount++;
                    dashboard.FullyRealizedAmount += invoice.TotalAmount;
                }
                else if (paidAmount > 0)
                {
                    dashboard.PartiallyRealizedCount++;
                    dashboard.PartiallyRealizedAmount += invoice.TotalAmount;
                    dashboard.TotalOpenInvoices++;
                    totalReceivables += outstanding;

                    if (daysToDeadline < 0)
                    {
                        dashboard.OverdueCount++;
                        dashboard.OverdueAmount += outstanding;
                        alerts.Add(CreateFemaAlert("overdue", invoice, outstanding, daysToDeadline));
                    }
                }
                else
                {
                    dashboard.PendingRealizationCount++;
                    dashboard.PendingRealizationAmount += invoice.TotalAmount;
                    dashboard.TotalOpenInvoices++;
                    totalReceivables += outstanding;

                    if (daysToDeadline < 0)
                    {
                        dashboard.OverdueCount++;
                        dashboard.OverdueAmount += outstanding;
                        alerts.Add(CreateFemaAlert("overdue", invoice, outstanding, daysToDeadline));
                    }
                    else if (daysToDeadline <= 30)
                    {
                        alerts.Add(CreateFemaAlert("approaching_deadline", invoice, outstanding, daysToDeadline));
                    }
                }
            }

            dashboard.TotalExportReceivables = totalReceivables;

            // FIRC status
            var fircs = (await _fircRepository.GetByCompanyIdAsync(companyId)).ToList();
            dashboard.FircsReceived = fircs.Count;
            var linkedFircs = fircs.Count(f => f.PaymentId.HasValue);
            dashboard.FircsPending = dashboard.TotalOpenInvoices - linkedFircs;
            dashboard.FircsCoverage = dashboard.TotalOpenInvoices > 0
                ? (decimal)linkedFircs / dashboard.TotalOpenInvoices * 100
                : 100;

            // EDPMS status
            dashboard.EdpmsReported = fircs.Count(f => f.EdpmsReported);
            dashboard.EdpmsPending = fircs.Count(f => !f.EdpmsReported);

            // LUT status
            var activeLut = await _lutRepository.GetValidForDateAsync(companyId, today);
            dashboard.HasActiveLut = activeLut != null;
            dashboard.ActiveLutNumber = activeLut?.LutNumber;
            if (activeLut != null)
            {
                dashboard.DaysToLutExpiry = (activeLut.ValidTo.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
                if (dashboard.DaysToLutExpiry <= 30)
                {
                    alerts.Add(new FemaViolationAlertDto
                    {
                        AlertType = "lut_expiring",
                        Severity = dashboard.DaysToLutExpiry <= 7 ? "critical" : "warning",
                        Title = "LUT Expiring Soon",
                        Description = $"LUT {activeLut.LutNumber} expires in {dashboard.DaysToLutExpiry} days",
                        RelatedEntityId = activeLut.Id,
                        RelatedEntityType = "lut",
                        DocumentNumber = activeLut.LutNumber
                    });
                }
            }
            else
            {
                alerts.Add(new FemaViolationAlertDto
                {
                    AlertType = "no_active_lut",
                    Severity = "critical",
                    Title = "No Active LUT",
                    Description = "No valid LUT found. Export invoices may not be compliant."
                });
            }

            // Calculate compliance score
            int score = 100;
            if (dashboard.OverdueCount > 0) score -= 30;
            if (!dashboard.HasActiveLut) score -= 25;
            if (dashboard.EdpmsPending > dashboard.EdpmsReported) score -= 15;
            if (dashboard.FircsPending > 0) score -= 10;
            dashboard.ComplianceScore = Math.Max(0, score);

            dashboard.OverallStatus = score switch
            {
                >= 90 => "compliant",
                >= 70 => "warning",
                >= 50 => "critical",
                _ => "non_compliant"
            };

            dashboard.CriticalAlerts = alerts.Count(a => a.Severity == "critical");
            dashboard.WarningAlerts = alerts.Count(a => a.Severity == "warning");
            dashboard.TopAlerts = alerts.OrderByDescending(a => a.Severity == "critical")
                                        .ThenByDescending(a => a.Amount)
                                        .Take(5)
                                        .ToList();

            return Result<FemaComplianceDashboardDto>.Success(dashboard);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<FemaViolationAlertDto>>> GetFemaViolationAlertsAsync(Guid companyId)
        {
            var dashboardResult = await GetFemaComplianceDashboardAsync(companyId);
            if (dashboardResult.IsFailure)
                return dashboardResult.Error!;

            return Result<IEnumerable<FemaViolationAlertDto>>.Success(dashboardResult.Value!.TopAlerts);
        }

        // ==================== Export Realization ====================

        /// <inheritdoc />
        public async Task<Result<ExportRealizationReportDto>> GetExportRealizationReportAsync(
            Guid companyId,
            string? financialYear = null)
        {
            var fy = financialYear ?? GetCurrentFinancialYear();
            var (fyStart, fyEnd) = GetFinancialYearDates(fy);

            var exportInvoices = (await GetExportInvoicesAsync(companyId))
                .Where(i => i.InvoiceDate >= fyStart && i.InvoiceDate <= fyEnd)
                .ToList();

            var report = new ExportRealizationReportDto
            {
                CompanyId = companyId,
                FinancialYear = fy,
                TotalExportInvoices = exportInvoices.Count
            };

            var customerData = new Dictionary<Guid, CustomerRealizationDto>();
            var monthlyData = new Dictionary<(int Year, int Month), MonthlyRealizationDto>();
            var totalRealizationDays = 0;
            var realizedInvoiceCount = 0;

            foreach (var invoice in exportInvoices)
            {
                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paidAmount = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paidAmount;

                report.TotalExportValue += invoice.TotalAmount;
                report.TotalRealizedValue += paidAmount;
                report.TotalPendingValue += outstanding;

                // Customer tracking
                var customerId = invoice.PartyId ?? Guid.Empty;
                if (!customerData.ContainsKey(customerId))
                {
                    var customer = customerId != Guid.Empty
                        ? await _customersRepository.GetByIdAsync(customerId)
                        : null;
                    customerData[customerId] = new CustomerRealizationDto
                    {
                        PartyId = customerId,
                        CustomerName = customer?.Name ?? "Unknown"
                    };
                }
                var cd = customerData[customerId];
                cd.InvoiceCount++;
                cd.TotalExportValue += invoice.TotalAmount;
                cd.RealizedValue += paidAmount;
                cd.PendingValue += outstanding;

                // Monthly tracking
                var monthKey = (invoice.InvoiceDate.Year, invoice.InvoiceDate.Month);
                if (!monthlyData.ContainsKey(monthKey))
                {
                    monthlyData[monthKey] = new MonthlyRealizationDto
                    {
                        Month = monthKey.Month,
                        Year = monthKey.Year,
                        MonthName = new DateTime(monthKey.Year, monthKey.Month, 1).ToString("MMM yyyy")
                    };
                }
                var md = monthlyData[monthKey];
                md.InvoiceCount++;
                md.InvoicedAmount += invoice.TotalAmount;
                md.RealizedAmount += paidAmount;

                // Calculate realization days for fully realized
                if (outstanding <= 0.01m && payments.Any())
                {
                    var firstPayment = payments.OrderBy(p => p.PaymentDate).First();
                    var days = (firstPayment.PaymentDate.ToDateTime(TimeOnly.MinValue) -
                               invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;
                    totalRealizationDays += days;
                    realizedInvoiceCount++;

                    if (days > cd.AvgRealizationDays)
                        cd.AvgRealizationDays = days;
                }

                // At-risk tracking
                var femaDeadline = invoice.InvoiceDate.AddDays(FemaRealizationDays);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var daysToDeadline = (femaDeadline.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                if (outstanding > 0.01m && daysToDeadline <= 60)
                {
                    var customer = invoice.PartyId.HasValue
                        ? await _customersRepository.GetByIdAsync(invoice.PartyId.Value)
                        : null;

                    report.AtRiskInvoices.Add(new AtRiskInvoiceDto
                    {
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                        InvoiceDate = invoice.InvoiceDate,
                        FemaDeadline = femaDeadline,
                        DaysToDeadline = daysToDeadline,
                        CustomerName = customer?.Name ?? "Unknown",
                        OutstandingAmount = outstanding,
                        Currency = invoice.Currency ?? "USD",
                        RiskLevel = daysToDeadline switch
                        {
                            < 0 => "critical",
                            <= 14 => "high",
                            <= 30 => "medium",
                            _ => "low"
                        }
                    });
                }
            }

            report.RealizationPercentage = report.TotalExportValue > 0
                ? Math.Round((double)report.TotalRealizedValue / (double)report.TotalExportValue * 100, 2)
                : 0;

            report.AvgRealizationDays = realizedInvoiceCount > 0
                ? (double)totalRealizationDays / realizedInvoiceCount
                : 0;

            // Status summary
            report.ByStatus = new List<RealizationStatusSummaryDto>
            {
                new() { Status = "Fully Realized", Count = exportInvoices.Count(i => GetOutstanding(i).Result <= 0.01m), Amount = report.TotalRealizedValue },
                new() { Status = "Partially Realized", Count = exportInvoices.Count(i => { var o = GetOutstanding(i).Result; return o > 0.01m && o < i.TotalAmount; }) },
                new() { Status = "Pending", Count = exportInvoices.Count(i => GetOutstanding(i).Result >= i.TotalAmount - 0.01m), Amount = report.TotalPendingValue }
            };

            report.ByCustomer = customerData.Values
                .Select(c =>
                {
                    c.RealizationPercentage = c.TotalExportValue > 0
                        ? (double)c.RealizedValue / (double)c.TotalExportValue * 100
                        : 0;
                    return c;
                })
                .OrderByDescending(c => c.TotalExportValue)
                .ToList();

            report.MonthlyBreakdown = monthlyData.Values
                .Select(m =>
                {
                    m.RealizationPercentage = m.InvoicedAmount > 0
                        ? (double)m.RealizedAmount / (double)m.InvoicedAmount * 100
                        : 0;
                    return m;
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            report.AtRiskInvoices = report.AtRiskInvoices
                .OrderBy(a => a.DaysToDeadline)
                .ToList();

            return Result<ExportRealizationReportDto>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<MonthlyRealizationTrendDto>>> GetRealizationTrendAsync(
            Guid companyId,
            int months = 12)
        {
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var startDate = endDate.AddMonths(-months + 1);
            startDate = new DateOnly(startDate.Year, startDate.Month, 1);

            var exportInvoices = (await GetExportInvoicesAsync(companyId))
                .Where(i => i.InvoiceDate >= startDate)
                .ToList();

            var trend = new List<MonthlyRealizationTrendDto>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var monthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthInvoices = exportInvoices
                    .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate <= monthEnd)
                    .ToList();

                decimal invoiced = 0, realized = 0, outstanding = 0;
                int totalDays = 0, realizedCount = 0;

                foreach (var invoice in monthInvoices)
                {
                    var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                    var paid = payments.Sum(p => p.Amount);
                    var out_ = invoice.TotalAmount - paid;

                    invoiced += invoice.TotalAmount;
                    realized += paid;
                    outstanding += out_;

                    if (out_ <= 0.01m && payments.Any())
                    {
                        var firstPayment = payments.OrderBy(p => p.PaymentDate).First();
                        totalDays += (firstPayment.PaymentDate.ToDateTime(TimeOnly.MinValue) -
                                     invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;
                        realizedCount++;
                    }
                }

                trend.Add(new MonthlyRealizationTrendDto
                {
                    Month = currentDate.Month,
                    Year = currentDate.Year,
                    MonthName = currentDate.ToString("MMM yyyy"),
                    Invoiced = invoiced,
                    Realized = realized,
                    Outstanding = outstanding,
                    RealizationRate = invoiced > 0 ? (double)realized / (double)invoiced * 100 : 0,
                    AvgDaysToRealize = realizedCount > 0 ? totalDays / realizedCount : 0
                });

                currentDate = currentDate.AddMonths(1);
            }

            return Result<IEnumerable<MonthlyRealizationTrendDto>>.Success(trend);
        }

        // ==================== Combined Dashboard ====================

        /// <inheritdoc />
        public async Task<Result<ExportDashboardDto>> GetExportDashboardAsync(Guid companyId)
        {
            var currentFy = GetCurrentFinancialYear();
            var (fyStart, fyEnd) = GetFinancialYearDates(currentFy);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var exportInvoices = (await GetExportInvoicesAsync(companyId)).ToList();
            var fyInvoices = exportInvoices.Where(i => i.InvoiceDate >= fyStart && i.InvoiceDate <= fyEnd).ToList();

            var dashboard = new ExportDashboardDto
            {
                CompanyId = companyId,
                TotalInvoicesFy = fyInvoices.Count
            };

            decimal totalReceivables = 0;
            decimal totalRealizedFy = 0;
            int overdueCount = 0;
            var customerReceivables = new Dictionary<string, decimal>();
            var currencyReceivables = new Dictionary<string, decimal>();

            foreach (var invoice in exportInvoices)
            {
                var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
                var paid = payments.Sum(p => p.Amount);
                var outstanding = invoice.TotalAmount - paid;

                if (invoice.InvoiceDate >= fyStart)
                {
                    dashboard.TotalExportRevenueFy += invoice.TotalAmount;
                    totalRealizedFy += paid;
                }

                if (outstanding > 0.01m)
                {
                    totalReceivables += outstanding;

                    var femaDeadline = invoice.InvoiceDate.AddDays(FemaRealizationDays);
                    if (today > femaDeadline)
                        overdueCount++;

                    // Customer breakdown
                    var customer = invoice.PartyId.HasValue
                        ? await _customersRepository.GetByIdAsync(invoice.PartyId.Value)
                        : null;
                    var customerName = customer?.Name ?? "Unknown";
                    customerReceivables[customerName] = customerReceivables.GetValueOrDefault(customerName) + outstanding;

                    // Currency breakdown
                    var currency = invoice.Currency ?? "USD";
                    currencyReceivables[currency] = currencyReceivables.GetValueOrDefault(currency) + outstanding;
                }
            }

            dashboard.TotalExportReceivables = totalReceivables;
            dashboard.TotalRealizedFy = totalRealizedFy;
            dashboard.OverdueInvoices = overdueCount;
            dashboard.TotalCustomers = customerReceivables.Count;

            // Forex summary
            var forexResult = await GetForexGainLossReportAsync(companyId, fyStart, fyEnd);
            if (forexResult.IsSuccess)
            {
                dashboard.NetForexGainLossFy = forexResult.Value!.NetRealizedGainLoss;
                dashboard.ForexTrend = forexResult.Value.MonthlyTrend;
            }

            // Unrealized position
            var positionResult = await GetUnrealizedForexPositionAsync(companyId, today, 83.5m);
            if (positionResult.IsSuccess)
            {
                dashboard.UnrealizedForexPosition = positionResult.Value!.TotalUnrealizedGainLoss;
            }

            // Compliance
            var complianceResult = await GetFemaComplianceDashboardAsync(companyId);
            if (complianceResult.IsSuccess)
            {
                dashboard.FemaComplianceScore = complianceResult.Value!.ComplianceScore;
                dashboard.HasActiveLut = complianceResult.Value.HasActiveLut;
                dashboard.PendingFircs = complianceResult.Value.FircsPending;
                dashboard.CriticalAlerts = complianceResult.Value.CriticalAlerts;
                dashboard.WarningAlerts = complianceResult.Value.WarningAlerts;
            }

            // Realization trend
            var trendResult = await GetRealizationTrendAsync(companyId, 12);
            if (trendResult.IsSuccess)
            {
                dashboard.RealizationTrend = trendResult.Value!.ToList();
                var avgDays = dashboard.RealizationTrend.Where(t => t.AvgDaysToRealize > 0).Select(t => t.AvgDaysToRealize);
                dashboard.AvgRealizationDays = avgDays.Any() ? avgDays.Average() : 0;
            }

            dashboard.ReceivablesByCustomer = customerReceivables.OrderByDescending(c => c.Value).Take(10).ToDictionary(c => c.Key, c => c.Value);
            dashboard.ReceivablesByCurrency = currencyReceivables;

            return Result<ExportDashboardDto>.Success(dashboard);
        }

        // ==================== Helper Methods ====================

        private async Task<IEnumerable<Core.Entities.Invoices>> GetExportInvoicesAsync(Guid companyId)
        {
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (allInvoices, _) = await _invoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            return allInvoices.Where(i =>
                !string.IsNullOrEmpty(i.Currency) &&
                i.Currency != "INR" &&
                i.Status != "draft" &&
                i.Status != "cancelled");
        }

        private async Task<decimal> GetOutstanding(Core.Entities.Invoices invoice)
        {
            var payments = await _paymentsRepository.GetByInvoiceIdAsync(invoice.Id);
            return invoice.TotalAmount - payments.Sum(p => p.Amount);
        }

        private static FemaViolationAlertDto CreateFemaAlert(string type, Core.Entities.Invoices invoice, decimal outstanding, int daysToDeadline)
        {
            return new FemaViolationAlertDto
            {
                AlertType = type,
                Severity = type == "overdue" ? "critical" : (daysToDeadline <= 14 ? "warning" : "info"),
                Title = type == "overdue" ? "FEMA Deadline Exceeded" : "FEMA Deadline Approaching",
                Description = type == "overdue"
                    ? $"Invoice {invoice.InvoiceNumber} is {Math.Abs(daysToDeadline)} days past FEMA deadline"
                    : $"Invoice {invoice.InvoiceNumber} has {daysToDeadline} days until FEMA deadline",
                RelatedEntityId = invoice.Id,
                RelatedEntityType = "invoice",
                DocumentNumber = invoice.InvoiceNumber,
                Amount = outstanding,
                Currency = invoice.Currency,
                DaysOverdue = type == "overdue" ? Math.Abs(daysToDeadline) : null
            };
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.UtcNow;
            var year = today.Month >= 4 ? today.Year : today.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static (DateOnly Start, DateOnly End) GetFinancialYearDates(string fy)
        {
            var parts = fy.Split('-');
            var startYear = int.Parse(parts[0]);
            return (new DateOnly(startYear, 4, 1), new DateOnly(startYear + 1, 3, 31));
        }
    }
}
