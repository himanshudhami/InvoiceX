using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using Core.Interfaces.Statutory;
using Microsoft.Extensions.Logging;

namespace Application.Services.Statutory
{
    /// <summary>
    /// Service for TDS Challan 281 generation and management.
    /// Handles challan creation, deposit recording, and reconciliation.
    ///
    /// Key Responsibilities:
    /// - Generate challan data from payroll transactions
    /// - Calculate interest on late payments (1.5% per month)
    /// - Track challan deposits and BSR/CIN details
    /// - Reconcile TDS deducted vs deposited
    /// </summary>
    public class TdsChallanService : ITdsChallanService
    {
        private readonly IStatutoryPaymentRepository _statutoryPaymentRepo;
        private readonly IPayrollTransactionRepository _payrollTransactionRepo;
        private readonly IPayrollRunRepository _payrollRunRepo;
        private readonly IContractorPaymentRepository _contractorPaymentRepo;
        private readonly ICompaniesRepository _companiesRepo;
        private readonly ICompanyStatutoryConfigRepository _statutoryConfigRepo;
        private readonly IEmployeesRepository _employeesRepo;
        private readonly ILogger<TdsChallanService> _logger;

        // Interest rate per month for late TDS payment
        private const decimal InterestRatePerMonth = 0.015m; // 1.5%

        public TdsChallanService(
            IStatutoryPaymentRepository statutoryPaymentRepo,
            IPayrollTransactionRepository payrollTransactionRepo,
            IPayrollRunRepository payrollRunRepo,
            IContractorPaymentRepository contractorPaymentRepo,
            ICompaniesRepository companiesRepo,
            ICompanyStatutoryConfigRepository statutoryConfigRepo,
            IEmployeesRepository employeesRepo,
            ILogger<TdsChallanService> logger)
        {
            _statutoryPaymentRepo = statutoryPaymentRepo ?? throw new ArgumentNullException(nameof(statutoryPaymentRepo));
            _payrollTransactionRepo = payrollTransactionRepo ?? throw new ArgumentNullException(nameof(payrollTransactionRepo));
            _payrollRunRepo = payrollRunRepo ?? throw new ArgumentNullException(nameof(payrollRunRepo));
            _contractorPaymentRepo = contractorPaymentRepo ?? throw new ArgumentNullException(nameof(contractorPaymentRepo));
            _companiesRepo = companiesRepo ?? throw new ArgumentNullException(nameof(companiesRepo));
            _statutoryConfigRepo = statutoryConfigRepo ?? throw new ArgumentNullException(nameof(statutoryConfigRepo));
            _employeesRepo = employeesRepo ?? throw new ArgumentNullException(nameof(employeesRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== Challan Generation ====================

        public async Task<Result<TdsChallanData>> GenerateChallanAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string challanType)
        {
            _logger.LogInformation(
                "Generating TDS challan for Company {CompanyId}, Period {Month}/{Year}, Type {Type}",
                companyId, periodMonth, periodYear, challanType);

            try
            {
                var company = await _companiesRepo.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(companyId);

                var financialYear = GetFinancialYear(periodMonth, periodYear);
                var dueDate = GetDueDate(periodMonth, periodYear);
                var assessmentYear = GetAssessmentYear(financialYear);

                var challan = new TdsChallanData
                {
                    CompanyId = companyId,
                    PeriodMonth = periodMonth,
                    PeriodYear = periodYear,
                    ChallanType = challanType,
                    FinancialYear = financialYear,
                    Tan = statutoryConfig?.TanNumber ?? string.Empty,
                    Pan = company.PanNumber ?? string.Empty,
                    CompanyName = company.Name ?? string.Empty,
                    Address = $"{company.AddressLine1} {company.AddressLine2}".Trim(),
                    City = company.City ?? string.Empty,
                    State = company.State ?? string.Empty,
                    Pincode = company.ZipCode ?? string.Empty,
                    MajorHead = "0021",
                    MinorHead = challanType == "salary" ? "200" : "206",
                    AssessmentYear = assessmentYear,
                    DueDate = dueDate,
                    IsOverdue = DateOnly.FromDateTime(DateTime.Today) > dueDate,
                    DaysOverdue = Math.Max(0, DateOnly.FromDateTime(DateTime.Today).DayNumber - dueDate.DayNumber)
                };

                // Get TDS data based on type
                if (challanType == "salary")
                {
                    await PopulateSalaryTdsData(challan, companyId, periodMonth, periodYear);
                }
                else
                {
                    await PopulateNonSalaryTdsData(challan, companyId, periodMonth, periodYear);
                }

                // Calculate totals
                challan.TotalAmount = challan.TdsAmount + challan.SurchargeAmount +
                                     challan.CessAmount + challan.InterestAmount + challan.PenaltyAmount;

                return Result<TdsChallanData>.Success(challan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TDS challan for Company {CompanyId}", companyId);
                return Error.Internal($"Error generating challan: {ex.Message}");
            }
        }

        public async Task<Result<TdsChalllanSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var (startYear, endYear) = ParseFinancialYear(financialYear);
                var summary = new TdsChalllanSummary
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear,
                    MonthlyStatus = new List<MonthlyTdsChallanStatus>()
                };

                // Get all statutory payments for the FY
                var payments = await _statutoryPaymentRepo.GetByFinancialYearAsync(companyId, financialYear);
                var tdsPayments = payments.Where(p => p.PaymentType == "TDS_192" || p.PaymentType == "TDS_26Q").ToList();

                // Process each month of the FY (Apr to Mar)
                for (int i = 0; i < 12; i++)
                {
                    var month = (i % 12) + 4; // Apr=4, ..., Mar=3
                    var year = month <= 12 ? startYear : endYear;
                    if (month > 12) month -= 12;

                    var monthPayment = tdsPayments.FirstOrDefault(p =>
                        p.PeriodMonth == month && p.PeriodYear == year);

                    // Get TDS deducted for the month
                    var tdsDeducted = await GetTdsDeductedForMonth(companyId, month, year);
                    var dueDate = GetDueDate(month, year);

                    var status = new MonthlyTdsChallanStatus
                    {
                        Month = month,
                        Year = year,
                        MonthName = new DateTime(year, month, 1).ToString("MMM yyyy"),
                        TdsDeducted = tdsDeducted,
                        TdsDeposited = monthPayment?.TotalAmount ?? 0,
                        Variance = tdsDeducted - (monthPayment?.TotalAmount ?? 0),
                        DueDate = dueDate,
                        PaymentDate = monthPayment?.PaymentDate,
                        Status = GetChallanStatus(monthPayment, dueDate),
                        ChallanNumber = monthPayment?.ReferenceNumber,
                        StatutoryPaymentId = monthPayment?.Id
                    };

                    summary.MonthlyStatus.Add(status);

                    // Update summary totals
                    summary.TotalTdsDeducted += status.TdsDeducted;
                    summary.TotalTdsDeposited += status.TdsDeposited;

                    if (status.Status == "paid")
                        summary.PaidCount++;
                    else if (status.Status == "overdue")
                        summary.OverdueCount++;
                    else
                        summary.PendingCount++;
                }

                summary.TotalVariance = summary.TotalTdsDeducted - summary.TotalTdsDeposited;

                return Result<TdsChalllanSummary>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly summary for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting monthly summary: {ex.Message}");
            }
        }

        public async Task<Result<TdsChallanPreview>> PreviewChallanAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string challanType,
            DateOnly? proposedPaymentDate = null)
        {
            var challanResult = await GenerateChallanAsync(companyId, periodMonth, periodYear, challanType);
            if (challanResult.IsFailure)
                return Error.Validation(challanResult.Error!.Message);

            var challan = challanResult.Value!;
            var paymentDate = proposedPaymentDate ?? DateOnly.FromDateTime(DateTime.Today);
            var interest = CalculateInterest(challan.TdsAmount + challan.SurchargeAmount + challan.CessAmount,
                                            challan.DueDate, paymentDate);

            var preview = new TdsChallanPreview
            {
                ChallanData = challan,
                ProposedPaymentDate = paymentDate,
                BaseAmount = challan.TdsAmount + challan.SurchargeAmount + challan.CessAmount,
                InterestAmount = interest,
                TotalPayable = challan.TdsAmount + challan.SurchargeAmount + challan.CessAmount + interest,
                Warnings = new List<string>()
            };

            if (interest > 0)
            {
                var monthsLate = Math.Ceiling((paymentDate.DayNumber - challan.DueDate.DayNumber) / 30.0);
                preview.InterestCalculation = $"Interest @ 1.5% per month for {monthsLate} month(s): {interest:N2}";
                preview.Warnings.Add($"Late payment will attract interest of ₹{interest:N2}");
                preview.HasWarnings = true;
            }

            if (string.IsNullOrEmpty(challan.Tan))
            {
                preview.Warnings.Add("TAN not configured for the company");
                preview.HasWarnings = true;
            }

            return Result<TdsChallanPreview>.Success(preview);
        }

        // ==================== Challan Operations ====================

        public async Task<Result<StatutoryPayment>> CreateChallanPaymentAsync(CreateTdsChallanRequest request)
        {
            try
            {
                var previewResult = await PreviewChallanAsync(
                    request.CompanyId, request.PeriodMonth, request.PeriodYear,
                    request.ChallanType, request.ProposedPaymentDate);

                if (previewResult.IsFailure)
                    return Error.Validation(previewResult.Error!.Message);

                var preview = previewResult.Value!;

                // Check for existing payment
                var existingPayment = await _statutoryPaymentRepo.GetByPeriodAsync(
                    request.CompanyId,
                    request.ChallanType == "salary" ? "TDS_192" : "TDS_26Q",
                    preview.ChallanData.FinancialYear,
                    request.PeriodMonth);

                if (existingPayment != null && existingPayment.Status != "cancelled")
                    return Error.Conflict($"Challan already exists for this period with status: {existingPayment.Status}");

                var payment = new StatutoryPayment
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    PaymentType = request.ChallanType == "salary" ? "TDS_192" : "TDS_26Q",
                    FinancialYear = preview.ChallanData.FinancialYear,
                    PeriodMonth = request.PeriodMonth,
                    PeriodYear = request.PeriodYear,
                    Quarter = GetQuarter(request.PeriodMonth),
                    PrincipalAmount = preview.BaseAmount,
                    InterestAmount = preview.InterestAmount,
                    PenaltyAmount = 0,
                    LateFee = 0,
                    TotalAmount = preview.TotalPayable,
                    DueDate = preview.ChallanData.DueDate,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy,
                    Notes = request.Notes
                };

                var created = await _statutoryPaymentRepo.AddAsync(payment);

                _logger.LogInformation(
                    "Created TDS challan payment {PaymentId} for Company {CompanyId}, Period {Month}/{Year}",
                    created.Id, request.CompanyId, request.PeriodMonth, request.PeriodYear);

                return Result<StatutoryPayment>.Success(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating challan payment for Company {CompanyId}", request.CompanyId);
                return Error.Internal($"Error creating challan payment: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> RecordChallanDepositAsync(
            Guid paymentId,
            RecordChallanDepositRequest request)
        {
            try
            {
                var payment = await _statutoryPaymentRepo.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment {paymentId} not found");

                if (payment.Status == "paid" || payment.Status == "verified")
                    return Error.Conflict($"Payment already marked as {payment.Status}");

                payment.PaymentDate = request.PaymentDate;
                payment.PaymentMode = request.PaymentMode;
                payment.BsrCode = request.BsrCode;
                payment.ReferenceNumber = request.ChallanNumber;
                payment.ReceiptNumber = request.ReceiptNumber;
                payment.BankName = request.BankName;
                payment.BankAccountId = request.BankAccountId;
                payment.BankReference = request.BankReference;
                payment.TotalAmount = request.ActualAmountPaid;
                payment.Status = "paid";
                payment.PaidBy = request.PaidBy;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.Notes = string.IsNullOrEmpty(payment.Notes)
                    ? request.Notes
                    : $"{payment.Notes}\n{request.Notes}";

                await _statutoryPaymentRepo.UpdateAsync(payment);

                _logger.LogInformation(
                    "Recorded challan deposit {PaymentId} with BSR {BsrCode}, Challan {ChallanNumber}",
                    paymentId, request.BsrCode, request.ChallanNumber);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording challan deposit {PaymentId}", paymentId);
                return Error.Internal($"Error recording deposit: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> UpdateCinAsync(
            Guid paymentId,
            UpdateCinRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Updating CIN for payment {PaymentId}: BSR {BsrCode}, CIN {Cin}",
                    paymentId, request.BsrCode, request.Cin);

                var payment = await _statutoryPaymentRepo.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment {paymentId} not found");

                // CIN can only be updated for paid challans
                if (payment.Status != "paid" && payment.Status != "verified")
                    return Error.Validation($"CIN can only be updated for paid challans. Current status: {payment.Status}");

                // Validate CIN format (20 digits: BSR 7 + Date 8 + Serial 5)
                if (!string.IsNullOrEmpty(request.Cin) && request.Cin.Length != 20)
                {
                    _logger.LogWarning("Invalid CIN length {Length} for payment {PaymentId}", request.Cin.Length, paymentId);
                    // Don't reject - some banks may have different formats
                }

                // Update CIN-related fields
                payment.BsrCode = request.BsrCode;
                payment.ReceiptNumber = request.Cin; // CIN is stored in ReceiptNumber

                // Update deposit date if provided
                if (request.DepositDate.HasValue)
                {
                    payment.PaymentDate = request.DepositDate.Value;
                }

                // Add remarks to notes for audit trail
                if (!string.IsNullOrEmpty(request.Remarks))
                {
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
                    var cinNote = $"[{timestamp}] CIN Updated: {request.Cin}";
                    if (!string.IsNullOrEmpty(request.ChallanSerialNumber))
                    {
                        cinNote += $", Serial: {request.ChallanSerialNumber}";
                    }
                    cinNote += $". {request.Remarks}";

                    payment.Notes = string.IsNullOrEmpty(payment.Notes)
                        ? cinNote
                        : $"{payment.Notes}\n{cinNote}";
                }

                payment.UpdatedAt = DateTime.UtcNow;

                await _statutoryPaymentRepo.UpdateAsync(payment);

                _logger.LogInformation(
                    "Updated CIN for payment {PaymentId} successfully",
                    paymentId);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CIN for payment {PaymentId}", paymentId);
                return Error.Internal($"Error updating CIN: {ex.Message}");
            }
        }

        public async Task<Result<ChallanVerificationResult>> VerifyChallanAsync(
            Guid paymentId,
            ChallanVerificationRequest request)
        {
            try
            {
                var payment = await _statutoryPaymentRepo.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment {paymentId} not found");

                if (payment.Status != "paid")
                    return Error.Validation("Only paid challans can be verified");

                payment.Status = "verified";
                payment.VerifiedBy = request.VerifiedBy;
                payment.VerifiedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.Notes = string.IsNullOrEmpty(payment.Notes)
                    ? request.Remarks
                    : $"{payment.Notes}\nVerification: {request.Remarks}";

                await _statutoryPaymentRepo.UpdateAsync(payment);

                var result = new ChallanVerificationResult
                {
                    IsVerified = true,
                    Status = "verified",
                    OltasStatus = request.OltasReference,
                    Remarks = request.Remarks,
                    VerifiedAt = DateTime.UtcNow,
                    VerifiedBy = request.VerifiedBy
                };

                _logger.LogInformation("Verified challan {PaymentId}", paymentId);

                return Result<ChallanVerificationResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying challan {PaymentId}", paymentId);
                return Error.Internal($"Error verifying challan: {ex.Message}");
            }
        }

        // ==================== Retrieval ====================

        public async Task<Result<(IEnumerable<TdsChallanListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation(
                    "Getting paged TDS challans for Company {CompanyId}, Page {Page}, Size {Size}",
                    companyId, pageNumber, pageSize);

                var company = await _companiesRepo.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(companyId);
                var tan = statutoryConfig?.TanNumber ?? string.Empty;

                // Get all TDS payments for the company
                IEnumerable<StatutoryPayment> payments;

                if (!string.IsNullOrEmpty(financialYear))
                {
                    payments = await _statutoryPaymentRepo.GetByFinancialYearAsync(companyId, financialYear);
                }
                else
                {
                    var pending = await _statutoryPaymentRepo.GetPendingAsync(companyId);
                    var allPayments = await _statutoryPaymentRepo.GetByFinancialYearAsync(companyId, GetCurrentFinancialYear());
                    payments = pending.Union(allPayments).DistinctBy(p => p.Id);
                }

                // Filter to TDS payments only
                var tdsPayments = payments
                    .Where(p => p.PaymentType == "TDS_192" || p.PaymentType == "TDS_26Q")
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    tdsPayments = tdsPayments.Where(p => p.Status == status);
                }

                // Apply search filter (search by challan number or reference)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    tdsPayments = tdsPayments.Where(p =>
                        (p.ReferenceNumber != null && p.ReferenceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.ReceiptNumber != null && p.ReceiptNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = tdsPayments.Count();

                // Order by period (most recent first) and apply pagination
                var pagedPayments = tdsPayments
                    .OrderByDescending(p => p.PeriodYear)
                    .ThenByDescending(p => p.PeriodMonth)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to DTOs
                var items = pagedPayments.Select(p => new TdsChallanListDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    Tan = tan,
                    ChallanType = p.PaymentType == "TDS_192" ? "salary" : "contractor",
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    FinancialYear = p.FinancialYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    BasicTax = p.PrincipalAmount,
                    Surcharge = 0, // Could be calculated from detailed breakdown
                    EducationCess = 0,
                    Interest = p.InterestAmount,
                    LateFee = p.LateFee,
                    TotalAmount = p.TotalAmount,
                    DueDate = p.DueDate,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status,
                    BsrCode = p.BsrCode,
                    CinNumber = p.ReceiptNumber,
                    CreatedAt = p.CreatedAt
                });

                return Result<(IEnumerable<TdsChallanListDto> Items, int TotalCount)>.Success((items, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged TDS challans for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting challans: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<PendingChallanDto>>> GetPendingChallansAsync(
            Guid companyId,
            string? financialYear = null)
        {
            try
            {
                var pending = await _statutoryPaymentRepo.GetPendingAsync(companyId);
                var tdsChallans = pending
                    .Where(p => p.PaymentType.StartsWith("TDS_"))
                    .Where(p => string.IsNullOrEmpty(financialYear) || p.FinancialYear == financialYear)
                    .Select(p => new PendingChallanDto
                    {
                        StatutoryPaymentId = p.Id,
                        PeriodMonth = p.PeriodMonth,
                        PeriodYear = p.PeriodYear,
                        MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                        ChallanType = p.PaymentType == "TDS_192" ? "salary" : "non-salary",
                        TdsAmount = p.PrincipalAmount,
                        InterestAmount = p.InterestAmount,
                        TotalAmount = p.TotalAmount,
                        DueDate = p.DueDate,
                        IsOverdue = p.IsOverdue,
                        DaysOverdue = p.DaysOverdue,
                        Status = p.Status
                    });

                return Result<IEnumerable<PendingChallanDto>>.Success(tdsChallans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending challans for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting pending challans: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<OverdueChallanDto>>> GetOverdueChallansAsync(Guid companyId)
        {
            try
            {
                var overdue = await _statutoryPaymentRepo.GetOverdueAsync(companyId);
                var tdsChallans = overdue
                    .Where(p => p.PaymentType.StartsWith("TDS_"))
                    .Select(p =>
                    {
                        var interest = CalculateInterest(p.PrincipalAmount, p.DueDate, DateOnly.FromDateTime(DateTime.Today));
                        return new OverdueChallanDto
                        {
                            StatutoryPaymentId = p.Id,
                            PeriodMonth = p.PeriodMonth,
                            PeriodYear = p.PeriodYear,
                            MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                            ChallanType = p.PaymentType == "TDS_192" ? "salary" : "non-salary",
                            TdsAmount = p.PrincipalAmount,
                            InterestAmount = interest,
                            TotalAmount = p.TotalAmount,
                            DueDate = p.DueDate,
                            IsOverdue = true,
                            DaysOverdue = p.DaysOverdue,
                            Status = "overdue",
                            EstimatedInterest = interest,
                            TotalWithInterest = p.PrincipalAmount + interest,
                            UrgencyLevel = GetUrgencyLevel(p.DaysOverdue)
                        };
                    });

                return Result<IEnumerable<OverdueChallanDto>>.Success(tdsChallans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue challans for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting overdue challans: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<PaidChallanDto>>> GetPaidChallansAsync(
            Guid companyId,
            string financialYear,
            string? quarter = null)
        {
            try
            {
                var payments = await _statutoryPaymentRepo.GetByFinancialYearAsync(companyId, financialYear);
                var paidChallans = payments
                    .Where(p => p.PaymentType.StartsWith("TDS_"))
                    .Where(p => p.Status == "paid" || p.Status == "verified" || p.Status == "filed")
                    .Where(p => string.IsNullOrEmpty(quarter) || p.Quarter == quarter)
                    .Select(p => new PaidChallanDto
                    {
                        StatutoryPaymentId = p.Id,
                        PeriodMonth = p.PeriodMonth,
                        PeriodYear = p.PeriodYear,
                        MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                        ChallanType = p.PaymentType == "TDS_192" ? "salary" : "non-salary",
                        TdsAmount = p.PrincipalAmount,
                        InterestAmount = p.InterestAmount,
                        TotalPaid = p.TotalAmount,
                        DueDate = p.DueDate,
                        PaymentDate = p.PaymentDate ?? DateOnly.MinValue,
                        BsrCode = p.BsrCode ?? string.Empty,
                        ChallanNumber = p.ReferenceNumber,
                        Cin = p.ReceiptNumber,
                        Status = p.Status
                    });

                return Result<IEnumerable<PaidChallanDto>>.Success(paidChallans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paid challans for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting paid challans: {ex.Message}");
            }
        }

        public async Task<Result<TdsChallanDetailDto>> GetChallanByIdAsync(Guid paymentId)
        {
            try
            {
                var payment = await _statutoryPaymentRepo.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment {paymentId} not found");

                var company = await _companiesRepo.GetByIdAsync(payment.CompanyId);
                var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(payment.CompanyId);

                var detail = new TdsChallanDetailDto
                {
                    Id = payment.Id,
                    CompanyId = payment.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    Tan = statutoryConfig?.TanNumber ?? string.Empty,
                    PeriodMonth = payment.PeriodMonth,
                    PeriodYear = payment.PeriodYear,
                    FinancialYear = payment.FinancialYear,
                    ChallanType = payment.PaymentType == "TDS_192" ? "salary" : "non-salary",
                    TdsAmount = payment.PrincipalAmount,
                    SurchargeAmount = 0,
                    CessAmount = 0,
                    InterestAmount = payment.InterestAmount,
                    PenaltyAmount = payment.PenaltyAmount,
                    TotalAmount = payment.TotalAmount,
                    DueDate = payment.DueDate,
                    PaymentDate = payment.PaymentDate,
                    Status = payment.Status,
                    BsrCode = payment.BsrCode,
                    ChallanNumber = payment.ReferenceNumber,
                    Cin = payment.ReceiptNumber,
                    PaymentMode = payment.PaymentMode,
                    BankName = payment.BankName,
                    BankReference = payment.BankReference,
                    VerifiedAt = payment.VerifiedAt,
                    VerifiedBy = payment.VerifiedBy
                };

                return Result<TdsChallanDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting challan {PaymentId}", paymentId);
                return Error.Internal($"Error getting challan: {ex.Message}");
            }
        }

        // ==================== Interest & Penalty Calculation ====================

        public decimal CalculateInterest(
            decimal tdsAmount,
            DateOnly dueDate,
            DateOnly actualPaymentDate)
        {
            if (actualPaymentDate <= dueDate)
                return 0;

            // Interest @ 1.5% per month (simple interest)
            // Part of month counted as full month
            var daysDiff = actualPaymentDate.DayNumber - dueDate.DayNumber;
            var months = Math.Ceiling(daysDiff / 30.0);

            var interest = tdsAmount * InterestRatePerMonth * (decimal)months;

            return Math.Round(interest, 0); // Round to nearest rupee
        }

        public DateOnly GetDueDate(int periodMonth, int periodYear)
        {
            // March has due date of 30th April (end of FY)
            // Other months: 7th of following month
            if (periodMonth == 3)
            {
                return new DateOnly(periodYear, 4, 30);
            }

            var nextMonth = periodMonth == 12 ? 1 : periodMonth + 1;
            var nextYear = periodMonth == 12 ? periodYear + 1 : periodYear;

            return new DateOnly(nextYear, nextMonth, 7);
        }

        // ==================== Reconciliation ====================

        public async Task<Result<TdsChallanReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear,
            string? quarter = null)
        {
            try
            {
                var (startYear, endYear) = ParseFinancialYear(financialYear);
                var reconciliation = new TdsChallanReconciliation
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear,
                    Quarter = quarter,
                    SalaryTds = new List<ReconciliationItem>(),
                    NonSalaryTds = new List<ReconciliationItem>(),
                    Mismatches = new List<ReconciliationMismatch>()
                };

                var months = GetMonthsForQuarter(quarter, startYear, endYear);

                foreach (var (month, year) in months)
                {
                    // Salary TDS
                    var salaryDeducted = await GetTdsDeductedForMonth(companyId, month, year);
                    var salaryDeposited = await GetTdsDepositedForMonth(companyId, month, year, "TDS_192");

                    reconciliation.SalaryTds.Add(new ReconciliationItem
                    {
                        Month = month,
                        Year = year,
                        Deducted = salaryDeducted,
                        Deposited = salaryDeposited,
                        Variance = salaryDeducted - salaryDeposited,
                        Status = salaryDeducted == salaryDeposited ? "matched" :
                                salaryDeposited > 0 ? "partial" : "undeposited"
                    });

                    reconciliation.TotalTdsDeducted += salaryDeducted;
                    reconciliation.TotalTdsDeposited += salaryDeposited;

                    if (salaryDeducted != salaryDeposited)
                    {
                        reconciliation.Mismatches.Add(new ReconciliationMismatch
                        {
                            MismatchType = salaryDeposited == 0 ? "no_deposit" : "partial_deposit",
                            Description = $"Salary TDS mismatch for {new DateTime(year, month, 1):MMM yyyy}",
                            Month = month,
                            Amount = salaryDeducted - salaryDeposited,
                            SuggestedAction = salaryDeposited == 0
                                ? "Create and deposit challan"
                                : "Verify challan amount"
                        });
                    }
                }

                reconciliation.Variance = reconciliation.TotalTdsDeducted - reconciliation.TotalTdsDeposited;

                return Result<TdsChallanReconciliation>.Success(reconciliation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling challans for Company {CompanyId}", companyId);
                return Error.Internal($"Error reconciling: {ex.Message}");
            }
        }

        public async Task<Result> LinkChallanToPayrollAsync(
            Guid challanPaymentId,
            IEnumerable<Guid> payrollTransactionIds)
        {
            try
            {
                foreach (var txnId in payrollTransactionIds)
                {
                    var allocation = new StatutoryPaymentAllocation
                    {
                        Id = Guid.NewGuid(),
                        StatutoryPaymentId = challanPaymentId,
                        PayrollTransactionId = txnId,
                        AllocationType = "both",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _statutoryPaymentRepo.AddAllocationAsync(allocation);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking challan {ChallanId} to payroll", challanPaymentId);
                return Error.Internal($"Error linking challan: {ex.Message}");
            }
        }

        // ==================== Private Helper Methods ====================

        private async Task PopulateSalaryTdsData(TdsChallanData challan, Guid companyId, int month, int year)
        {
            var transactions = await _payrollTransactionRepo.GetAllAsync();
            var companyEmployees = await GetCompanyEmployeeIds(companyId);

            var monthTransactions = transactions
                .Where(t => companyEmployees.Contains(t.EmployeeId) &&
                           t.PayrollMonth == month &&
                           t.PayrollYear == year &&
                           t.PayrollType == "employee")
                .ToList();

            challan.TdsAmount = monthTransactions.Sum(t => t.TdsDeducted);
            challan.EmployeeCount = monthTransactions.Select(t => t.EmployeeId).Distinct().Count();
            challan.TransactionCount = monthTransactions.Count;

            challan.SourceRecords = monthTransactions
                .Where(t => t.TdsDeducted > 0)
                .Select(t => new TdsChallanSourceRecord
                {
                    SourceId = t.Id,
                    SourceType = "payroll_transaction",
                    TdsAmount = t.TdsDeducted,
                    TotalTds = t.TdsDeducted
                })
                .ToList();
        }

        private async Task PopulateNonSalaryTdsData(TdsChallanData challan, Guid companyId, int month, int year)
        {
            var payments = await _contractorPaymentRepo.GetByCompanyIdAsync(companyId);

            var monthPayments = payments
                .Where(p => p.PaymentMonth == month && p.PaymentYear == year && p.TdsAmount > 0)
                .ToList();

            challan.TdsAmount = monthPayments.Sum(p => p.TdsAmount);
            challan.TransactionCount = monthPayments.Count;
            challan.EmployeeCount = monthPayments.Select(p => p.EmployeeId).Distinct().Count();

            challan.SourceRecords = monthPayments
                .Select(p => new TdsChallanSourceRecord
                {
                    SourceId = p.Id,
                    SourceType = "contractor_payment",
                    GrossAmount = p.GrossAmount,
                    TdsAmount = p.TdsAmount,
                    TotalTds = p.TdsAmount
                })
                .ToList();
        }

        private async Task<HashSet<Guid>> GetCompanyEmployeeIds(Guid companyId)
        {
            var allEmployees = await _employeesRepo.GetAllAsync();
            var companyEmployeeIds = allEmployees
                .Where(e => e.CompanyId.HasValue && e.CompanyId.Value == companyId)
                .Select(e => e.Id)
                .ToHashSet();

            _logger.LogDebug("Found {Count} employees for company {CompanyId}", companyEmployeeIds.Count, companyId);
            return companyEmployeeIds;
        }

        private async Task<decimal> GetTdsDeductedForMonth(Guid companyId, int month, int year)
        {
            var companyEmployeeIds = await GetCompanyEmployeeIds(companyId);
            if (companyEmployeeIds.Count == 0)
            {
                _logger.LogWarning("No employees found for company {CompanyId}", companyId);
                return 0;
            }

            var transactions = await _payrollTransactionRepo.GetAllAsync();
            var tdsDeducted = transactions
                .Where(t => companyEmployeeIds.Contains(t.EmployeeId) &&
                           t.PayrollMonth == month &&
                           t.PayrollYear == year &&
                           t.PayrollType == "employee")
                .Sum(t => t.TdsDeducted);

            _logger.LogDebug("TDS deducted for company {CompanyId}, {Month}/{Year}: {Amount}",
                companyId, month, year, tdsDeducted);
            return tdsDeducted;
        }

        private async Task<decimal> GetTdsDepositedForMonth(Guid companyId, int month, int year, string paymentType)
        {
            var payment = await _statutoryPaymentRepo.GetByPeriodAsync(companyId, paymentType,
                GetFinancialYear(month, year), month);
            return payment?.IsPaid == true ? payment.TotalAmount : 0;
        }

        private static string GetFinancialYear(int month, int year)
        {
            // Indian FY: Apr to Mar
            // Apr-Dec = startYear, Jan-Mar = endYear
            if (month >= 4)
                return $"{year}-{(year + 1) % 100:D2}";
            else
                return $"{year - 1}-{year % 100:D2}";
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.Today;
            return GetFinancialYear(today.Month, today.Year);
        }

        private static string GetAssessmentYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            return $"{startYear + 1}-{startYear + 2}";
        }

        private static (int startYear, int endYear) ParseFinancialYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = parts[1].Length == 2
                ? int.Parse($"20{parts[1]}")
                : int.Parse(parts[1]);
            return (startYear, endYear);
        }

        private static string GetQuarter(int month)
        {
            return month switch
            {
                4 or 5 or 6 => "Q1",
                7 or 8 or 9 => "Q2",
                10 or 11 or 12 => "Q3",
                1 or 2 or 3 => "Q4",
                _ => throw new ArgumentException($"Invalid month: {month}")
            };
        }

        private static string GetChallanStatus(StatutoryPayment? payment, DateOnly dueDate)
        {
            if (payment == null || payment.Status == "pending")
            {
                return DateOnly.FromDateTime(DateTime.Today) > dueDate ? "overdue" : "pending";
            }
            return payment.Status;
        }

        private static string GetUrgencyLevel(int daysOverdue)
        {
            return daysOverdue switch
            {
                <= 7 => "low",
                <= 30 => "medium",
                <= 60 => "high",
                _ => "critical"
            };
        }

        private static IEnumerable<(int month, int year)> GetMonthsForQuarter(string? quarter, int startYear, int endYear)
        {
            if (string.IsNullOrEmpty(quarter))
            {
                // All months of FY
                for (int i = 4; i <= 12; i++)
                    yield return (i, startYear);
                for (int i = 1; i <= 3; i++)
                    yield return (i, endYear);
            }
            else
            {
                var months = quarter switch
                {
                    "Q1" => new[] { (4, startYear), (5, startYear), (6, startYear) },
                    "Q2" => new[] { (7, startYear), (8, startYear), (9, startYear) },
                    "Q3" => new[] { (10, startYear), (11, startYear), (12, startYear) },
                    "Q4" => new[] { (1, endYear), (2, endYear), (3, endYear) },
                    _ => Array.Empty<(int, int)>()
                };

                foreach (var m in months)
                    yield return m;
            }
        }
    }
}
